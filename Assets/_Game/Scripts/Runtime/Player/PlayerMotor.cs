using UnityEngine;

namespace BoozeBlocks.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerInputReader))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float maxSpeed = 6.5f;
        [SerializeField, Min(0f)] private float acceleration = 20f;
        [SerializeField, Min(0f)] private float deceleration = 12f;
        [SerializeField, Min(0f)] private float turnSpeed = 9f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.28f;
        [SerializeField, Min(0f)] private float crowdPushCooldown = 0.14f;
        [SerializeField, Min(0f)] private float jumpSpeed = 5.2f;
        [SerializeField, Min(0f)] private float jumpBuffer = 0.12f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.10f;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private PlayerInputReader input;
        private PlayerVitals vitals;
        private Transform cameraTransform;
        private bool controlEnabled = true;
        private Vector3 lastCrowdDirection;
        private float nextCrowdPushTime;
        private float driftPhase;
        private float jumpQueuedUntil;
        private float lastGroundedTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            input = GetComponent<PlayerInputReader>();
            vitals = GetComponent<PlayerVitals>();
            driftPhase = transform.position.x * 0.37f + transform.position.z * 0.19f;
        }

        private void Start()
        {
            if (vitals == null) vitals = GetComponent<PlayerVitals>();
            if (UnityEngine.Camera.main != null) cameraTransform = UnityEngine.Camera.main.transform;
        }

        private void Update()
        {
            EnsureDependencies();
            if (controlEnabled && input != null && input.JumpPressed)
            {
                jumpQueuedUntil = Time.time + jumpBuffer;
            }
        }

        private void FixedUpdate()
        {
            EnsureDependencies();
            if (body == null || input == null) return;
            if (!controlEnabled) return;

            Vector2 moveInput = controlEnabled ? input.Move : Vector2.zero;
            Vector3 facingDirection = GetCameraRelativeDirection(moveInput);
            Vector3 desiredDirection = facingDirection;
            if (desiredDirection.sqrMagnitude > 0.001f && vitals != null)
            {
                Vector3 sideways = Vector3.Cross(Vector3.up, desiredDirection);
                float drift = Mathf.Sin(Time.fixedTime * 2.4f + driftPhase) *
                              vitals.BuzzRatio * 0.07f;
                desiredDirection = (desiredDirection + sideways * drift).normalized;
            }

            bool grounded = TryGetGroundNormal(out Vector3 groundNormal);
            if (grounded) lastGroundedTime = Time.time;
            if (Time.time <= jumpQueuedUntil && Time.time - lastGroundedTime <= coyoteTime)
            {
                float verticalBoost = Mathf.Max(0f, jumpSpeed - body.linearVelocity.y);
                body.AddForce(Vector3.up * verticalBoost, ForceMode.VelocityChange);
                jumpQueuedUntil = 0f;
                lastGroundedTime = float.NegativeInfinity;
                grounded = false;
            }
            if (grounded && desiredDirection.sqrMagnitude > 0.001f)
            {
                desiredDirection = Vector3.ProjectOnPlane(desiredDirection, groundNormal).normalized;
            }
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 desiredVelocity = desiredDirection * maxSpeed;
            float rate = desiredDirection.sqrMagnitude > 0.001f ? acceleration : deceleration;
            if (!grounded) rate *= airControl;

            Vector3 velocityChange = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, rate * Time.fixedDeltaTime) - horizontalVelocity;
            body.AddForce(velocityChange, ForceMode.VelocityChange);

            body.angularVelocity = Vector3.zero;
            if (facingDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
                float rotationBlend = 1f - Mathf.Exp(-turnSpeed * Time.fixedDeltaTime);
                body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, rotationBlend));
            }
        }

        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled;
            if (!enabled) jumpQueuedUntil = 0f;
        }

        public void ApplyCrowdPush(Vector3 direction, float strength)
        {
            if (body == null || Time.time < nextCrowdPushTime) return;
            Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (flatDirection.sqrMagnitude <= 0.001f) return;
            lastCrowdDirection = flatDirection;
            body.AddForce(flatDirection * Mathf.Clamp(strength, 0f, 1.2f), ForceMode.VelocityChange);
            nextCrowdPushTime = Time.time + crowdPushCooldown;
        }

        public void ApplyKnockdownImpulse()
        {
            if (body == null) return;
            Vector3 direction = lastCrowdDirection.sqrMagnitude > 0.001f ? lastCrowdDirection : -transform.forward;
            body.AddForce(direction * 2.8f + Vector3.up * 1.4f, ForceMode.VelocityChange);
        }

        private Vector3 GetCameraRelativeDirection(Vector2 moveInput)
        {
            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
            }
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            return (forward.normalized * moveInput.y + right.normalized * moveInput.x).normalized;
        }

        private bool TryGetGroundNormal(out Vector3 normal)
        {
            float radius = capsule.radius * 0.9f;
            float distance = capsule.height * 0.5f - radius + 0.12f;
            bool grounded = Physics.SphereCast(body.position + Vector3.up * 0.05f, radius, Vector3.down,
                out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            normal = grounded ? hit.normal : Vector3.up;
            return grounded;
        }

        private void EnsureDependencies()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (capsule == null) capsule = GetComponent<CapsuleCollider>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (vitals == null) vitals = GetComponent<PlayerVitals>();
        }
    }
}
