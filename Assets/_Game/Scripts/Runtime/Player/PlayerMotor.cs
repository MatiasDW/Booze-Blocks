using System;
using UnityEngine;

namespace BoozeBlocks.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerInputReader))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public event Action Jumped;

        [SerializeField, Min(0f)] private float maxSpeed = 6.5f;
        [SerializeField, Min(0f)] private float acceleration = 20f;
        [SerializeField, Min(0f)] private float deceleration = 12f;
        [SerializeField, Min(0f)] private float turnSpeed = 13f;
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
        private bool grounded;
        private bool knockdownPhysicsActive;

        public bool ControlEnabled => controlEnabled;
        public bool IsGrounded => grounded;
        public bool KnockdownPhysicsActive => knockdownPhysicsActive;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            input = GetComponent<PlayerInputReader>();
            vitals = GetComponent<PlayerVitals>();
            driftPhase = transform.position.x * 0.37f + transform.position.z * 0.19f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
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
            Vector3 facingDirection = input.TryGetRemoteWorldDirection(out Vector3 remoteDirection)
                ? remoteDirection
                : GetCameraRelativeDirection(moveInput);
            Vector3 desiredDirection = facingDirection;
            float buzzInstability = vitals != null ? vitals.BuzzInstability : 0f;
            if (desiredDirection.sqrMagnitude > 0.001f && vitals != null)
            {
                Vector3 sideways = Vector3.Cross(Vector3.up, desiredDirection);
                float fastDrift = Mathf.Sin(Time.fixedTime * 2.15f + driftPhase) * 0.20f;
                float slowDrift = Mathf.Sin(Time.fixedTime * 0.83f + driftPhase * 1.7f) * 0.10f;
                float drift = (fastDrift + slowDrift) * buzzInstability;
                desiredDirection = (desiredDirection + sideways * drift).normalized;
            }

            grounded = TryGetGroundNormal(out Vector3 groundNormal);
            if (grounded) lastGroundedTime = Time.time;
            if (Time.time <= jumpQueuedUntil && Time.time - lastGroundedTime <= coyoteTime)
            {
                float buzzJumpBonus = Mathf.Lerp(1f, 1.12f, vitals != null ? vitals.BuzzRatio : 0f);
                float verticalBoost = Mathf.Max(0f, jumpSpeed * buzzJumpBonus - body.linearVelocity.y);
                body.AddForce(Vector3.up * verticalBoost, ForceMode.VelocityChange);
                Jumped?.Invoke();
                jumpQueuedUntil = 0f;
                lastGroundedTime = float.NegativeInfinity;
                grounded = false;
            }
            if (grounded && desiredDirection.sqrMagnitude > 0.001f)
            {
                desiredDirection = Vector3.ProjectOnPlane(desiredDirection, groundNormal).normalized;
            }
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            float speedMultiplier = vitals != null ? vitals.BuzzSpeedMultiplier : 1f;
            float speedPulse = 1f + Mathf.Sin(Time.fixedTime * 3.1f + driftPhase) * buzzInstability * 0.045f;
            Vector3 desiredVelocity = desiredDirection * (maxSpeed * speedMultiplier * speedPulse);
            float rate = desiredDirection.sqrMagnitude > 0.001f ? acceleration : deceleration;
            rate *= Mathf.Lerp(1f, desiredDirection.sqrMagnitude > 0.001f ? 0.72f : 0.52f, buzzInstability);
            if (!grounded) rate *= airControl;

            Vector3 velocityChange = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, rate * Time.fixedDeltaTime) - horizontalVelocity;
            body.AddForce(velocityChange, ForceMode.VelocityChange);

            body.angularVelocity = Vector3.zero;
            if (facingDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
                float effectiveTurnSpeed = turnSpeed * Mathf.Lerp(1f, 0.48f, buzzInstability);
                float rotationBlend = 1f - Mathf.Exp(-effectiveTurnSpeed * Time.fixedDeltaTime);
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
            Vector3 torqueAxis = Vector3.Cross(Vector3.up, direction).normalized + Vector3.up * 0.25f;
            body.AddTorque(torqueAxis * 5.5f, ForceMode.VelocityChange);
        }

        public void SetKnockdownPhysics(bool enabled)
        {
            EnsureDependencies();
            if (body == null || knockdownPhysicsActive == enabled) return;
            knockdownPhysicsActive = enabled;
            if (enabled)
            {
                body.constraints = RigidbodyConstraints.None;
                return;
            }

            body.angularVelocity = Vector3.zero;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (forward.sqrMagnitude <= 0.001f) forward = Vector3.forward;
            body.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
