using System;
using System.Collections;
using System.Collections.Generic;
using BoozeBlocks.Player;
using BoozeBlocks.Prototype;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace BoozeBlocks.Horde
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class HordeDirector : MonoBehaviour
    {
        private const int KidLayer = 2; // Built-in Ignore Raycast layer.
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("BoozeBlocks.Horde.Update");
        private static readonly ProfilerMarker PopulationMarker = new ProfilerMarker("BoozeBlocks.Horde.Population");
        private static readonly ProfilerMarker TargetingMarker = new ProfilerMarker("BoozeBlocks.Horde.Targeting");
        private static readonly ProfilerMarker PressureMarker = new ProfilerMarker("BoozeBlocks.Horde.Pressure");

        [SerializeField] private HordeBalanceConfig config;
        [SerializeField, Min(1f)] private float spawnRadius = 15f;
        [SerializeField] private Vector2 kidVisualScaleRange = new Vector2(0.44f, 0.50f);

        private Queue<KidUnit> pool = new Queue<KidUnit>(96);
        private List<KidUnit> activeUnits = new List<KidUnit>(96);
        private List<PlayerVitals> activePlayers = new List<PlayerVitals>(8);
        private Dictionary<PlayerVitals, int> playerIndices = new Dictionary<PlayerVitals, int>(8);
        private KidSpatialGrid spatialGrid = new KidSpatialGrid(2f);
        private Material[] kidShirtMaterials = new Material[3];
        private Material kidSkinMaterial;
        private Material kidHairMaterial;
        private Material kidCharacterMaterial;
        private GameObject kidCharacterPrefab;
        private Coroutine populationRoutine;
        private float retargetTimer;
        private int[] assignmentCounts = Array.Empty<int>();
        private int[] contactCounts = Array.Empty<int>();
        private Vector3[] contactDirections = Array.Empty<Vector3>();
        private int desiredUnitCount;
        private int waveUnitLimit;
        private int nextUnitId;
        private int nextSpawnAttempt;
        private bool initialized;
        private bool hasSimulationAuthority = true;
        private HordeWaveModel wave;
        private int remoteWaveNumber = 1;
        private int remoteDifficultyStep;
        private bool remoteWaveActive = true;
        private float remoteWaveRemaining;

        public event Action<int, bool> PhaseChanged;
        public event Action<Vector3> UnitHit;
        public event Action<Vector3> UnitDefeated;

        public int ActiveUnitCount => activeUnits?.Count ?? 0;
        public int DesiredUnitCount => desiredUnitCount;
        public int WaveUnitLimit => waveUnitLimit;
        public int ActivePlayerCount => activePlayers?.Count ?? 0;
        public int CurrentWave => hasSimulationAuthority ? wave?.WaveNumber ?? 1 : remoteWaveNumber;
        public int DifficultyStep => hasSimulationAuthority ? wave?.DifficultyStep ?? 0 : remoteDifficultyStep;
        public bool IsWaveActive => hasSimulationAuthority ? wave == null || wave.IsWaveActive : remoteWaveActive;
        public bool IsPreparing => hasSimulationAuthority
            ? wave?.Phase == HordeWavePhase.Preparation
            : !remoteWaveActive && remoteWaveNumber == 1 && remoteWaveRemaining > BreakDuration;
        public float WaveRemainingTime => hasSimulationAuthority ? wave?.RemainingTime ?? 0f : remoteWaveRemaining;
        public bool HasSimulationAuthority => hasSimulationAuthority;

        public int ApplyDefenseSweep(Vector3 origin, Vector3 forward, float radius, float minimumDot,
            float knockbackDistance, float stunDuration, float damage, out int defeated)
        {
            EnsureRuntimeCollections();
            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            float radiusSquared = radius * radius;
            int affected = 0;
            defeated = 0;
            for (int i = activeUnits.Count - 1; i >= 0; i--)
            {
                KidUnit unit = activeUnits[i];
                if (unit == null || !unit.isActiveAndEnabled) continue;
                Vector3 delta = Vector3.ProjectOnPlane(unit.Position - origin, Vector3.up);
                float distanceSquared = delta.sqrMagnitude;
                if (distanceSquared > radiusSquared || distanceSquared <= 0.001f) continue;
                if (Vector3.Dot(flatForward, delta.normalized) < minimumDot) continue;
                Vector3 hitPosition = unit.Position;
                bool wasDefeated = unit.ReceiveHit(damage, origin, knockbackDistance, stunDuration);
                affected++;
                UnitHit?.Invoke(hitPosition);
                if (!wasDefeated) continue;
                activeUnits.RemoveAt(i);
                unit.Deactivate();
                pool.Enqueue(unit);
                defeated++;
                UnitDefeated?.Invoke(hitPosition);
            }
            return affected;
        }

        private int BaseUnits => config != null ? config.BaseUnits : 14;
        private int UnitsPerAdditionalPlayer => config != null ? config.UnitsPerAdditionalPlayer : 6;
        private int UnitsPerDifficultyStep => config != null ? config.UnitsPerDifficultyStep : 6;
        private int MaximumUnits => config != null ? config.MaximumUnits : 96;
        private int WavesPerDifficultyStep => config != null ? config.WavesPerDifficultyStep : 2;
        private float InitialPreparationDuration => config != null ? config.InitialPreparationDuration : 25f;
        private float WaveDuration => config != null ? config.WaveDuration : 60f;
        private float BreakDuration => config != null ? config.BreakDuration : 15f;
        private float SpawnRampDuration => config != null ? config.SpawnRampDuration : 20f;
        private float RetargetInterval => config != null ? config.RetargetInterval : 0.65f;
        private float SpawnInterval => config != null ? config.SpawnInterval : 0.10f;
        private float KidHealth => (config != null ? config.BaseKidHealth : 3f) +
                                   DifficultyStep * (config != null ? config.KidHealthPerDifficultyStep : 0.5f);
        private float BarricadeAttackDamage => (config != null ? config.BarricadeAttackDamage : 4f) +
                                               DifficultyStep *
                                               (config != null
                                                   ? config.BarricadeAttackDamagePerDifficulty
                                                   : 0.5f);
        private float BarricadePressureDamage => (config != null ? config.BarricadePressureDamage : 2f) +
                                                 DifficultyStep *
                                                 (config != null
                                                     ? config.BarricadePressureDamagePerDifficulty
                                                     : 0.25f);
        private float FirstUnitPressure => config != null ? config.FirstUnitPressure : 7f;
        private float AdditionalPressureMultiplier => config != null ? config.AdditionalUnitPressureMultiplier : 0.55f;
        private float FirstUnitHealthDamage => config != null ? config.FirstUnitHealthDamage : 0.65f;
        private float AdditionalHealthDamageMultiplier => config != null
            ? config.AdditionalUnitHealthDamageMultiplier
            : 0.50f;
        private float TargetCrowdPenalty => config != null ? config.TargetCrowdPenalty : 36f;

        private void OnEnable()
        {
            EnsureRuntimeCollections();
            PlayerRegistry.Changed += HandlePlayersChanged;
            if (initialized)
            {
                RefreshPlayersAndPopulation();
                AssignTargets();
            }
        }

        private void OnDisable()
        {
            PlayerRegistry.Changed -= HandlePlayersChanged;
            ClearPopulation();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < (kidShirtMaterials?.Length ?? 0); i++)
            {
                if (kidShirtMaterials[i] != null) Destroy(kidShirtMaterials[i]);
            }
            if (kidSkinMaterial != null) Destroy(kidSkinMaterial);
            if (kidHairMaterial != null) Destroy(kidHairMaterial);
            if (kidCharacterMaterial != null) Destroy(kidCharacterMaterial);
        }

        private void Start()
        {
            EnsureRuntimeCollections();
            kidShirtMaterials[0] = CreateKidMaterial(new Color(1f, 0.67f, 0.08f));
            kidShirtMaterials[1] = CreateKidMaterial(new Color(0.96f, 0.22f, 0.28f));
            kidShirtMaterials[2] = CreateKidMaterial(new Color(0.08f, 0.67f, 0.72f));
            kidSkinMaterial = CreateKidMaterial(new Color(1f, 0.73f, 0.50f));
            kidHairMaterial = CreateKidMaterial(new Color(0.12f, 0.08f, 0.05f));
            Shader characterShader = Shader.Find("BoozeBlocks/StylizedCharacter");
            if (characterShader != null)
            {
                kidCharacterMaterial = new Material(characterShader) { enableInstancing = true };
            }
            kidCharacterPrefab = Resources.Load<GameObject>("KidCharacter");
            wave = CreateWaveModel();
            initialized = true;
            RefreshPlayersAndPopulation();
        }

        private void Update()
        {
            if (!initialized || !hasSimulationAuthority) return;
            using (UpdateMarker.Auto())
            {
                if (wave == null) wave = CreateWaveModel();
                bool waveChanged = wave.Tick(Time.deltaTime);
                if (waveChanged) PhaseChanged?.Invoke(wave.WaveNumber, wave.IsWaveActive);
                retargetTimer -= Time.deltaTime;
                if (!waveChanged && retargetTimer > 0f) return;
                retargetTimer = RetargetInterval;
                if (IsWaveActive) HordeEntranceRegistry.ApplyHordePressure(BarricadePressureDamage);
                RefreshPlayersAndPopulation();
                AssignTargets();
            }
        }

        private void FixedUpdate()
        {
            if (!initialized || !hasSimulationAuthority || !IsWaveActive) return;
            using (PressureMarker.Auto())
            {
                EnsureRuntimeCollections();
                spatialGrid.Rebuild(activeUnits);
                ApplyAggregatedPressure();
            }
        }

        private void HandlePlayersChanged()
        {
            if (!initialized) return;
            retargetTimer = 0f;
            RefreshPlayersAndPopulation();
            AssignTargets();
        }

        private void RefreshPlayersAndPopulation()
        {
            using (PopulationMarker.Auto())
            {
                EnsureRuntimeCollections();
                PlayerRegistry.Fill(activePlayers, false);
                playerIndices.Clear();
                for (int i = 0; i < activePlayers.Count; i++) playerIndices[activePlayers[i]] = i;
                waveUnitLimit = hasSimulationAuthority
                    ? HordeScalingModel.CalculateActiveUnits(activePlayers.Count, DifficultyStep,
                        BaseUnits, UnitsPerAdditionalPlayer, UnitsPerDifficultyStep, MaximumUnits)
                    : desiredUnitCount;
                desiredUnitCount = hasSimulationAuthority && IsWaveActive
                    ? HordeScalingModel.CalculateRampedUnits(waveUnitLimit,
                        wave?.ActiveElapsedTime ?? SpawnRampDuration, SpawnRampDuration)
                    : 0;
                EnsureCounterCapacity(activePlayers.Count);

                if (activeUnits.Count != desiredUnitCount && populationRoutine == null)
                {
                    populationRoutine = StartCoroutine(ReconcilePopulation());
                }
            }
        }

        private void EnsureRuntimeCollections()
        {
            pool ??= new Queue<KidUnit>(96);
            activeUnits ??= new List<KidUnit>(96);
            activePlayers ??= new List<PlayerVitals>(8);
            playerIndices ??= new Dictionary<PlayerVitals, int>(8);
            spatialGrid ??= new KidSpatialGrid(2f);
            if (kidShirtMaterials == null || kidShirtMaterials.Length != 3)
            {
                kidShirtMaterials = new Material[3];
            }
        }

        private IEnumerator ReconcilePopulation()
        {
            WaitForSeconds spawnWait = SpawnInterval > 0f ? new WaitForSeconds(SpawnInterval) : null;
            WaitForSeconds blockedWait = new WaitForSeconds(0.15f);
            while (activeUnits.Count != desiredUnitCount)
            {
                if (activeUnits.Count < desiredUnitCount)
                {
                    Vector3 position;
                    int spawnAttempt = nextSpawnAttempt++;
                    if (HordeEntranceRegistry.TryGetSpawnPoint(spawnAttempt, BarricadeAttackDamage,
                            out Vector3 entrancePosition))
                    {
                        position = new Vector3(entrancePosition.x, 1f, entrancePosition.z);
                    }
                    else if (HordeEntranceRegistry.HasEntrances)
                    {
                        yield return blockedWait;
                        continue;
                    }
                    else
                    {
                        Vector2 circle = UnityEngine.Random.insideUnitCircle.normalized * spawnRadius;
                        position = transform.position + new Vector3(circle.x, 1f, circle.y);
                    }
                    KidUnit unit = pool.Count > 0 ? pool.Dequeue() : CreateKid();
                    PlayerVitals target = activePlayers.Count > 0
                        ? activePlayers[activeUnits.Count % activePlayers.Count]
                        : null;
                    activeUnits.Add(unit);
                    unit.Activate(position, target, KidHealth);
                    if (spawnWait != null) yield return spawnWait;
                    else yield return null;
                }
                else
                {
                    int lastIndex = activeUnits.Count - 1;
                    KidUnit unit = activeUnits[lastIndex];
                    activeUnits.RemoveAt(lastIndex);
                    unit.Deactivate();
                    pool.Enqueue(unit);
                }
            }

            // Ensure StartCoroutine assigns its handle before this routine clears it.
            yield return null;
            populationRoutine = null;
            AssignTargets();
        }

        private void AssignTargets()
        {
            using (TargetingMarker.Auto())
            {
                EnsureCounterCapacity(activePlayers.Count);
                Array.Clear(assignmentCounts, 0, assignmentCounts.Length);

                for (int unitIndex = 0; unitIndex < activeUnits.Count; unitIndex++)
                {
                    KidUnit unit = activeUnits[unitIndex];
                    PlayerVitals bestTarget = null;
                    int bestPlayerIndex = -1;
                    float bestScore = float.MaxValue;

                    for (int playerIndex = 0; playerIndex < activePlayers.Count; playerIndex++)
                    {
                        PlayerVitals player = activePlayers[playerIndex];
                        float distance = (unit.Position - player.transform.position).sqrMagnitude;
                        float score = distance + assignmentCounts[playerIndex] * TargetCrowdPenalty;
                        if (score >= bestScore) continue;
                        bestScore = score;
                        bestTarget = player;
                        bestPlayerIndex = playerIndex;
                    }

                    unit.SetPlayerTarget(bestTarget);
                    if (bestPlayerIndex >= 0) assignmentCounts[bestPlayerIndex]++;
                }
            }
        }

        private void ApplyAggregatedPressure()
        {
            if (activePlayers.Count == 0) return;
            EnsureCounterCapacity(activePlayers.Count);
            Array.Clear(contactCounts, 0, contactCounts.Length);
            Array.Clear(contactDirections, 0, contactDirections.Length);

            for (int i = 0; i < activeUnits.Count; i++)
            {
                if (!activeUnits[i].TryGetPressureTarget(out PlayerVitals target)) continue;
                if (!playerIndices.TryGetValue(target, out int playerIndex)) continue;
                contactCounts[playerIndex]++;
                Vector3 pushDirection = Vector3.ProjectOnPlane(target.transform.position - activeUnits[i].Position,
                    Vector3.up).normalized;
                contactDirections[playerIndex] += pushDirection;
            }

            for (int i = 0; i < activePlayers.Count; i++)
            {
                float pressurePerSecond = HordeScalingModel.CalculatePressurePerSecond(contactCounts[i],
                    FirstUnitPressure, AdditionalPressureMultiplier);
                if (pressurePerSecond > 0f)
                {
                    activePlayers[i].ApplyPressure(pressurePerSecond * Time.fixedDeltaTime);
                    float pushStrength = Mathf.Min(0.85f, 0.10f + contactCounts[i] * 0.025f);
                    activePlayers[i].GetComponent<PlayerMotor>()?.ApplyCrowdPush(contactDirections[i], pushStrength);
                }
                float healthDamagePerSecond = HordeScalingModel.CalculatePressurePerSecond(contactCounts[i],
                    FirstUnitHealthDamage, AdditionalHealthDamageMultiplier);
                if (healthDamagePerSecond > 0f)
                {
                    activePlayers[i].ApplyDamage(healthDamagePerSecond * Time.fixedDeltaTime);
                }
            }
        }

        private void EnsureCounterCapacity(int playerCount)
        {
            assignmentCounts ??= Array.Empty<int>();
            contactCounts ??= Array.Empty<int>();
            contactDirections ??= Array.Empty<Vector3>();
            if (assignmentCounts.Length < playerCount) assignmentCounts = new int[playerCount];
            if (contactCounts.Length < playerCount) contactCounts = new int[playerCount];
            if (contactDirections.Length < playerCount) contactDirections = new Vector3[playerCount];
        }

        public void SetSimulationAuthority(bool isAuthoritative)
        {
            if (hasSimulationAuthority == isAuthoritative) return;
            hasSimulationAuthority = isAuthoritative;
            if (!initialized) return;
            if (!hasSimulationAuthority)
            {
                if (populationRoutine != null) StopCoroutine(populationRoutine);
                populationRoutine = null;
                ReturnAllUnitsToPool();
            }
            RefreshPlayersAndPopulation();
        }

        public bool SkipPreparation()
        {
            if (!hasSimulationAuthority || wave?.Phase != HordeWavePhase.Preparation) return false;
            wave.Tick(wave.RemainingTime);
            PhaseChanged?.Invoke(wave.WaveNumber, true);
            retargetTimer = RetargetInterval;
            RefreshPlayersAndPopulation();
            AssignTargets();
            return true;
        }

        public void ResetForMenu()
        {
            ClearPopulation();
            waveUnitLimit = 0;
            remoteWaveNumber = 1;
            remoteDifficultyStep = 0;
            remoteWaveActive = false;
            remoteWaveRemaining = InitialPreparationDuration;
            if (initialized) wave = CreateWaveModel();
        }

        public void FillPoseSnapshots(List<HordePoseSnapshot> destination)
        {
            destination.Clear();
            if (!hasSimulationAuthority) return;
            for (int i = 0; i < activeUnits.Count; i++)
            {
                KidUnit unit = activeUnits[i];
                if (unit == null || !unit.isActiveAndEnabled) continue;
                destination.Add(new HordePoseSnapshot(unit.Position, unit.transform.eulerAngles.y));
            }
        }

        public void ApplyRemoteSnapshot(IReadOnlyList<HordePoseSnapshot> poses, int waveNumber,
            int difficultyStep, bool waveActive, float waveRemaining)
        {
            if (hasSimulationAuthority || !initialized || poses == null) return;
            remoteWaveNumber = Mathf.Max(1, waveNumber);
            remoteDifficultyStep = Mathf.Max(0, difficultyStep);
            remoteWaveActive = waveActive;
            remoteWaveRemaining = Mathf.Max(0f, waveRemaining);
            desiredUnitCount = Mathf.Min(MaximumUnits, poses.Count);
            waveUnitLimit = Mathf.Max(desiredUnitCount, waveUnitLimit);

            while (activeUnits.Count < desiredUnitCount)
            {
                KidUnit unit = pool.Count > 0 ? pool.Dequeue() : CreateKid();
                HordePoseSnapshot initial = poses[activeUnits.Count];
                activeUnits.Add(unit);
                unit.ActivateReplica(initial.Position, initial.Yaw);
            }
            while (activeUnits.Count > desiredUnitCount)
            {
                int last = activeUnits.Count - 1;
                KidUnit unit = activeUnits[last];
                activeUnits.RemoveAt(last);
                unit.Deactivate();
                pool.Enqueue(unit);
            }
            for (int i = 0; i < activeUnits.Count; i++)
            {
                HordePoseSnapshot pose = poses[i];
                activeUnits[i].ApplyReplicaPose(pose.Position, pose.Yaw);
            }
        }

        private void ReturnAllUnitsToPool()
        {
            for (int i = activeUnits.Count - 1; i >= 0; i--)
            {
                KidUnit unit = activeUnits[i];
                activeUnits.RemoveAt(i);
                if (unit == null) continue;
                unit.Deactivate();
                pool.Enqueue(unit);
            }
            desiredUnitCount = 0;
        }

        private void ClearPopulation()
        {
            if (populationRoutine != null) StopCoroutine(populationRoutine);
            populationRoutine = null;
            EnsureRuntimeCollections();
            ReturnAllUnitsToPool();
        }

        private HordeWaveModel CreateWaveModel()
        {
            return new HordeWaveModel(WaveDuration, BreakDuration, WavesPerDifficultyStep,
                InitialPreparationDuration);
        }

        private KidUnit CreateKid()
        {
            if (kidCharacterPrefab != null)
            {
                GameObject meshKid = new GameObject($"Kid_{nextUnitId:000}");
                meshKid.transform.SetParent(transform);
                meshKid.SetActive(false);
                GameObject visual = Instantiate(kidCharacterPrefab, meshKid.transform);
                visual.name = "Human Kid Visual";
                visual.transform.localPosition = Vector3.down;
                visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                float minimumScale = kidVisualScaleRange.x >= 0.2f ? kidVisualScaleRange.x : 0.44f;
                float maximumScale = kidVisualScaleRange.y >= minimumScale ? kidVisualScaleRange.y : 0.50f;
                float sizeVariation = (nextUnitId % 4) / 3f;
                float visualScale = Mathf.Lerp(minimumScale, maximumScale, sizeVariation);
                visual.transform.localScale = Vector3.one * visualScale;
                SetLayerRecursively(meshKid.transform, KidLayer);
                Renderer[] renderers = meshKid.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    ConfigureKidRenderer(renderers[i], kidCharacterMaterial);
                    ApplyKidPalette(renderers[i], nextUnitId);
                }
                KidUnit meshUnit = meshKid.AddComponent<KidUnit>();
                meshUnit.Initialize(nextUnitId++, spatialGrid);
                return meshUnit;
            }

            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            instance.SetActive(false);
            instance.layer = KidLayer;
            instance.name = $"Kid_{nextUnitId:000}";
            instance.transform.SetParent(transform);
            instance.transform.localScale = new Vector3(0.65f, 0.7f, 0.65f);
            Material shirtMaterial = kidShirtMaterials[nextUnitId % kidShirtMaterials.Length];
            Renderer kidRenderer = instance.GetComponent<Renderer>();
            ConfigureKidRenderer(kidRenderer, shirtMaterial);
            CreateKidPart("Head", PrimitiveType.Sphere, instance.transform,
                new Vector3(0f, 1.18f, 0.02f), Vector3.one * 0.78f, kidSkinMaterial);
            CreateKidPart("Nose", PrimitiveType.Sphere, instance.transform,
                new Vector3(0f, 1.15f, 0.66f), Vector3.one * 0.16f, kidSkinMaterial);
            CreateKidPart("Hair", PrimitiveType.Sphere, instance.transform,
                new Vector3(0f, 1.53f, -0.02f), new Vector3(0.82f, 0.24f, 0.76f), kidHairMaterial);
            CreateKidPart("Arm L", PrimitiveType.Capsule, instance.transform,
                new Vector3(-0.72f, 0.05f, 0f), new Vector3(0.20f, 0.52f, 0.20f), shirtMaterial,
                new Vector3(0f, 0f, -18f));
            CreateKidPart("Arm R", PrimitiveType.Capsule, instance.transform,
                new Vector3(0.72f, 0.05f, 0f), new Vector3(0.20f, 0.52f, 0.20f), shirtMaterial,
                new Vector3(0f, 0f, 18f));
            CreateKidPart("Leg L", PrimitiveType.Cylinder, instance.transform,
                new Vector3(-0.28f, -1.0f, 0f), new Vector3(0.22f, 0.38f, 0.22f), kidHairMaterial);
            CreateKidPart("Leg R", PrimitiveType.Cylinder, instance.transform,
                new Vector3(0.28f, -1.0f, 0f), new Vector3(0.22f, 0.38f, 0.22f), kidHairMaterial);
            KidUnit unit = instance.AddComponent<KidUnit>();
            unit.Initialize(nextUnitId++, spatialGrid);
            return unit;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++) SetLayerRecursively(root.GetChild(i), layer);
        }

        private static void CreateKidPart(string name, PrimitiveType type, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material, Vector3? localEulerAngles = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.layer = KidLayer;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEulerAngles ?? Vector3.zero);
            part.transform.localScale = localScale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            ConfigureKidRenderer(part.GetComponent<Renderer>(), material);
        }

        private static void ConfigureKidRenderer(Renderer renderer, Material material)
        {
            if (material != null) renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void ApplyKidPalette(Renderer renderer, int unitId)
        {
            Color clothingColor = (unitId % 3) switch
            {
                0 => new Color(1f, 0.58f, 0.05f),
                1 => new Color(0.95f, 0.18f, 0.25f),
                _ => new Color(0.04f, 0.66f, 0.70f)
            };
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor("_ShirtColor", clothingColor);
            properties.SetColor("_SkinColor", new Color(1f, 0.72f, 0.50f));
            properties.SetColor("_PantsColor", new Color(0.08f, 0.18f, 0.28f));
            properties.SetColor("_DarkColor", new Color(0.05f, 0.03f, 0.02f));
            renderer.SetPropertyBlock(properties);
        }

        private static Material CreateKidMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            material.enableInstancing = true;
            return material;
        }
    }
}
