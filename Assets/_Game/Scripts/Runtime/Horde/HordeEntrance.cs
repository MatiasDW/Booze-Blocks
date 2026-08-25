using System;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Horde
{
    [DisallowMultipleComponent]
    public sealed class HordeEntrance : MonoBehaviour, IInteractable, IInteractionResult
    {
        [SerializeField] private string barricadeName = "tablones";
        [SerializeField, Min(1f)] private float blockDuration = 14f;
        [SerializeField, Min(0f)] private float rebuildCooldown = 9f;
        [SerializeField, Min(0.1f)] private float constructionDuration = 2.75f;
        [SerializeField, Min(0.5f)] private float maximumBuilderDistance = 3.2f;

        private GameObject barricadeVisual;
        private GameObject integrityBarBackground;
        private GameObject integrityBarFill;
        private Transform[] barricadePieces = Array.Empty<Transform>();
        private BarricadeIntegrityModel integrity;
        private float rebuildAt;
        private bool lastVisualState;
        private float durationMultiplier = 1f;
        private PlayerVitals builder;
        private float constructionProgress;
        private Vector3 barricadeScale = Vector3.one;
        private Quaternion barricadeRotation = Quaternion.identity;
        private Vector3 integrityFillScale = Vector3.one;
        private Vector3 integrityFillPosition;
        private PlayerActionFeedback builderFeedback;
        private bool hasSimulationAuthority = true;
        private float remoteBuildProgress;
        private float damagePulse;
        private int entranceSalt;

        public event Action Blocked;
        public event Action BuildingStarted;
        public event Action BuildingCancelled;
        public event Action<HordeEntrance> SectionBroken;
        public event Action<HordeEntrance> Destroyed;

        public bool IsBlocked => integrity?.IsActive ?? false;
        public bool IsBuilding => builder != null || !hasSimulationAuthority && remoteBuildProgress > 0f;
        public bool CanSpawn => !IsBlocked;
        public float CurrentIntegrity => integrity?.CurrentIntegrity ?? 0f;
        public float MaximumIntegrity => integrity?.MaximumIntegrity ?? Mathf.Max(1f, blockDuration * 10f);
        public float IntegrityRatio => integrity?.IntegrityRatio ?? 0f;
        public float FlowRatio => integrity?.FlowRatio ?? 1f;
        public int RemainingSections => integrity?.RemainingSections ?? 0;
        public float BuildProgressRatio => !hasSimulationAuthority
            ? remoteBuildProgress
            : IsBuilding ? Mathf.Clamp01(constructionProgress / constructionDuration) : 0f;
        public string Prompt => IsBuilding
            ? $"Construyendo {barricadeName}... {Mathf.RoundToInt(BuildProgressRatio * 100f)}%"
            : $"E - Construir bloqueo con {barricadeName}";
        public string InteractionResult => $"Construccion iniciada: permanece cerca durante {constructionDuration:0.0}s.";

        private void OnEnable()
        {
            HordeEntranceRegistry.Register(this);
        }

        private void OnDisable()
        {
            HordeEntranceRegistry.Unregister(this);
        }

        private void Update()
        {
            if (hasSimulationAuthority && IsBuilding) UpdateConstruction();
            damagePulse = Mathf.Max(0f, damagePulse - Time.deltaTime * 5f);
            RefreshVisual();
        }

        public void Configure(string methodName, float duration, float cooldown, GameObject visual)
        {
            barricadeName = methodName;
            blockDuration = Mathf.Max(1f, duration);
            rebuildCooldown = Mathf.Max(0f, cooldown);
            barricadeVisual = visual;
            entranceSalt = Animator.StringToHash(gameObject.name);
            if (barricadeVisual != null)
            {
                barricadeScale = barricadeVisual.transform.localScale;
                barricadeRotation = barricadeVisual.transform.localRotation;
                int count = Mathf.Max(1, barricadeVisual.transform.childCount);
                barricadePieces = new Transform[count];
                for (int i = 0; i < count; i++)
                    barricadePieces[i] = barricadeVisual.transform.GetChild(i);
                integrity = CreateIntegrityModel();
                barricadeVisual.SetActive(false);
            }
        }

        public void ConfigureIntegrityVisual(GameObject background, GameObject fill)
        {
            integrityBarBackground = background;
            integrityBarFill = fill;
            if (integrityBarFill != null)
            {
                integrityFillScale = integrityBarFill.transform.localScale;
                integrityFillPosition = integrityBarFill.transform.localPosition;
            }
            if (integrityBarBackground != null) integrityBarBackground.SetActive(false);
            if (integrityBarFill != null) integrityBarFill.SetActive(false);
        }

        public void SetDurationMultiplier(float multiplier)
        {
            durationMultiplier = Mathf.Clamp(multiplier, 0.25f, 2f);
            if (!IsBlocked) integrity = CreateIntegrityModel();
        }

        public void SetConstructionDuration(float duration)
        {
            constructionDuration = Mathf.Max(0.1f, duration);
        }

        public bool IsBeingBuiltBy(PlayerVitals player)
        {
            return IsBuilding && builder == player;
        }

        public bool CanInteract(PlayerVitals player)
        {
            return player != null && !player.Model.IsEliminated && !IsBuilding && !IsBlocked && Time.time >= rebuildAt;
        }

        public void Interact(PlayerVitals player)
        {
            if (!hasSimulationAuthority || !CanInteract(player)) return;
            builder = player;
            builderFeedback = player.GetComponent<PlayerActionFeedback>();
            constructionProgress = 0f;
            lastVisualState = false;
            BuildingStarted?.Invoke();
        }

        public void SetSimulationAuthority(bool isAuthoritative)
        {
            hasSimulationAuthority = isAuthoritative;
            if (isAuthoritative) remoteBuildProgress = 0f;
        }

        public void ApplyRemoteState(float integrityRatio, float buildProgressRatio)
        {
            if (hasSimulationAuthority) return;
            integrity ??= CreateIntegrityModel();
            integrity.ApplySnapshot(integrityRatio);
            remoteBuildProgress = IsBlocked ? 0f : Mathf.Clamp01(buildProgressRatio);
            lastVisualState = false;
            RefreshVisual();
        }

        public bool TryResolveSpawn(int sequence, float hordeDamage)
        {
            if (!IsBlocked) return true;
            if (hasSimulationAuthority && hordeDamage > 0f) ApplyHordeAttack(hordeDamage);
            return !IsBlocked || integrity.AllowsSpawn(sequence, entranceSalt);
        }

        public void ApplyHordeAttack(float damage)
        {
            if (!hasSimulationAuthority || !IsBlocked || damage <= 0f) return;
            BarricadeDamageResult result = integrity.ApplyDamage(damage);
            damagePulse = 1f;
            if (result.SectionBroken && !result.Destroyed) SectionBroken?.Invoke(this);
            if (result.Destroyed)
            {
                rebuildAt = Time.time + rebuildCooldown;
                lastVisualState = false;
                Destroyed?.Invoke(this);
            }
            RefreshVisual();
        }

        private void UpdateConstruction()
        {
            if (builder == null || !builder.isActiveAndEnabled || builder.Model.IsEliminated ||
                (builder.transform.position - transform.position).sqrMagnitude >
                maximumBuilderDistance * maximumBuilderDistance)
            {
                CancelConstruction();
                return;
            }

            constructionProgress += Time.deltaTime;
            if (constructionProgress < constructionDuration) return;
            builder = null;
            constructionProgress = 0f;
            integrity = CreateIntegrityModel();
            integrity.Restore();
            rebuildAt = float.PositiveInfinity;
            lastVisualState = false;
            builderFeedback?.Show(
                $"Entrada bloqueada con {barricadeName}: {RemainingSections} secciones.");
            builderFeedback = null;
            Blocked?.Invoke();
        }

        private void CancelConstruction()
        {
            if (!IsBuilding) return;
            builder = null;
            constructionProgress = 0f;
            lastVisualState = false;
            builderFeedback?.Show("Construccion cancelada: te alejaste de la entrada.");
            builderFeedback = null;
            BuildingCancelled?.Invoke();
        }

        private void RefreshVisual()
        {
            if (barricadeVisual == null) return;
            bool visible = IsBlocked || IsBuilding;
            if (visible != lastVisualState)
            {
                lastVisualState = visible;
                barricadeVisual.SetActive(visible);
            }
            bool showIntegrity = IsBlocked;
            if (integrityBarBackground != null) integrityBarBackground.SetActive(showIntegrity);
            if (integrityBarFill != null) integrityBarFill.SetActive(showIntegrity);
            if (!visible)
            {
                barricadeVisual.transform.localRotation = barricadeRotation;
                return;
            }
            float progress = hasSimulationAuthority ? BuildProgressRatio : remoteBuildProgress;
            float height = IsBlocked ? 1f : Mathf.Lerp(0.15f, 1f, progress);
            barricadeVisual.transform.localScale = Vector3.Scale(barricadeScale, new Vector3(1f, height, 1f));
            float shake = IsBlocked ? Mathf.Sin(damagePulse * Mathf.PI * 4f) * damagePulse * 5f : 0f;
            barricadeVisual.transform.localRotation = barricadeRotation * Quaternion.Euler(0f, 0f, shake);

            int visiblePieces = IsBlocked ? RemainingSections : barricadePieces.Length;
            for (int i = 0; i < barricadePieces.Length; i++)
            {
                if (barricadePieces[i] != null)
                    barricadePieces[i].gameObject.SetActive(i < visiblePieces);
            }

            if (integrityBarFill != null)
            {
                float ratio = Mathf.Clamp01(IntegrityRatio);
                Vector3 scale = integrityFillScale;
                scale.x *= ratio;
                integrityBarFill.transform.localScale = scale;
                Vector3 position = integrityFillPosition;
                position.x -= integrityFillScale.x * (1f - ratio) * 0.5f;
                integrityBarFill.transform.localPosition = position;
            }
        }

        public Vector3 GetSpawnPosition(int sequence)
        {
            float hash = Mathf.Repeat(sequence * 0.6180339f, 1f) * 2f - 1f;
            return transform.position + transform.right * (hash * 1.4f) + transform.forward * 0.8f;
        }

        private BarricadeIntegrityModel CreateIntegrityModel()
        {
            int sections = Mathf.Max(1, barricadePieces.Length);
            return new BarricadeIntegrityModel(blockDuration * 10f * durationMultiplier, sections);
        }
    }
}
