using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInventory))]
    public sealed class PlayerDrinkAnimator : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float duration = 1.1f;

        private PlayerInventory inventory;
        private PlayerInputReader input;
        private PlayerVitals vitals;
        private Transform bottle;
        private Transform bottleLabel;
        private Transform rightArm;
        private Vector3 restingPosition;
        private Quaternion restingRotation;
        private float startedAt = float.NegativeInfinity;
        private int previousServings;

        public bool IsPlaying => bottle != null && bottle.gameObject.activeSelf;
        public Transform Bottle => bottle;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            input = GetComponent<PlayerInputReader>();
            vitals = GetComponent<PlayerVitals>();
        }

        private void OnEnable()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (inventory != null) inventory.Drank += Play;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Drank -= Play;
            if (bottle != null) bottle.gameObject.SetActive(false);
        }

        public void Initialize(Transform bottleRoot, Transform rightArmPivot)
        {
            bottle = bottleRoot;
            bottleLabel = bottle != null ? bottle.Find("BOOZE Floating Text") : null;
            rightArm = rightArmPivot;
            if (bottle != null)
            {
                restingPosition = bottle.localPosition;
                restingRotation = bottle.localRotation;
                bottle.gameObject.SetActive(false);
            }
            previousServings = inventory != null ? inventory.Model.DrinkServings : 0;
        }

        private void Update()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (vitals == null) vitals = GetComponent<PlayerVitals>();
            if (inventory == null) return;

            int servings = inventory.Model.DrinkServings;
            if (servings < previousServings) Play();
            previousServings = servings;

            // Non-authoritative clients animate immediately while the host validates the action.
            if (input != null && input.DrinkPressed && servings > 0 &&
                (vitals == null || vitals.BuzzRatio < 0.999f))
            {
                Play();
            }
        }

        private void LateUpdate()
        {
            if (!IsPlaying) return;
            float normalized = Mathf.Clamp01((Time.time - startedAt) / duration);
            float lift = normalized < 0.28f
                ? Mathf.SmoothStep(0f, 1f, normalized / 0.28f)
                : normalized > 0.72f
                    ? Mathf.SmoothStep(1f, 0f, (normalized - 0.72f) / 0.28f)
                    : 1f;
            float sip = Mathf.Sin(Mathf.Clamp01((normalized - 0.22f) / 0.56f) * Mathf.PI);
            Vector3 mouthPosition = new Vector3(0.46f, 0.92f, 0.52f);
            bottle.localPosition = Vector3.Lerp(restingPosition, mouthPosition, lift) +
                                   Vector3.up * (sip * 0.035f);
            Quaternion drinkingRotation = Quaternion.Euler(68f + sip * 8f, 0f, -14f);
            bottle.localRotation = Quaternion.Slerp(restingRotation, drinkingRotation, lift);
            FaceLabelTowardCamera();

            if (rightArm != null)
            {
                Quaternion armPose = Quaternion.Euler(-108f, 4f, 22f);
                rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, armPose, lift);
            }

            if (normalized < 1f) return;
            bottle.localPosition = restingPosition;
            bottle.localRotation = restingRotation;
            bottle.gameObject.SetActive(false);
        }

        private void FaceLabelTowardCamera()
        {
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (bottleLabel == null || camera == null) return;
            Vector3 towardCamera = (camera.transform.position - bottle.position).normalized;
            bottleLabel.position = bottle.position + towardCamera * 0.205f;
            Vector3 facing = bottleLabel.position - camera.transform.position;
            if (facing.sqrMagnitude > 0.001f)
                bottleLabel.rotation = Quaternion.LookRotation(facing, camera.transform.up);
        }

        public void Play()
        {
            if (bottle == null) return;
            startedAt = Time.time;
            bottle.localPosition = restingPosition;
            bottle.localRotation = restingRotation;
            bottle.gameObject.SetActive(true);
        }
    }
}
