using UnityEngine;
using BoozeBlocks.Player;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DrunkVisualAnimator : MonoBehaviour
    {
        private Rigidbody body;
        private Transform visualRoot;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private PlayerStateMachine stateMachine;
        private PlayerMotor motor;
        private PlayerVitals vitals;
        private float phase;
        private float fallBlend;
        private Vector3 previousVelocity;
        private Vector3 smoothedAcceleration;
        private float smoothedStride;
        private float strideVelocity;
        private bool previousGrounded = true;
        private float landPulse;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            stateMachine = GetComponent<PlayerStateMachine>();
            motor = GetComponent<PlayerMotor>();
            vitals = GetComponent<PlayerVitals>();
        }

        public void Initialize(Transform targetVisual, int playerIndex)
        {
            Initialize(targetVisual, playerIndex, null, null, null, null);
        }

        public void Initialize(Transform targetVisual, int playerIndex, Transform leftArmPivot,
            Transform rightArmPivot, Transform leftLegPivot, Transform rightLegPivot)
        {
            visualRoot = targetVisual;
            leftArm = leftArmPivot;
            rightArm = rightArmPivot;
            leftLeg = leftLegPivot;
            rightLeg = rightLegPivot;
            phase = playerIndex * 1.73f;
        }

        private void LateUpdate()
        {
            EnsureDependencies();
            if (visualRoot == null || body == null) return;
            float speed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
            float time = Time.time + phase;
            float targetStride = Mathf.Clamp01(speed / 5f);
            smoothedStride = Mathf.SmoothDamp(smoothedStride, targetStride, ref strideVelocity, 0.12f);
            float stride = smoothedStride;
            float buzz = vitals != null ? vitals.BuzzInstability : 0f;

            bool currentGrounded = motor == null || motor.IsGrounded;
            bool knockedDownNow = stateMachine != null &&
                (stateMachine.State == PlayerState.KnockedDown || stateMachine.State == PlayerState.Eliminated);
            if (currentGrounded && !previousGrounded && !knockedDownNow)
            {
                landPulse = 1f;
            }
            previousGrounded = currentGrounded;
            landPulse = Mathf.MoveTowards(landPulse, 0f, Time.deltaTime * 3.5f);
            Vector3 acceleration = Time.deltaTime > 0.0001f
                ? (body.linearVelocity - previousVelocity) / Time.deltaTime
                : Vector3.zero;
            previousVelocity = body.linearVelocity;
            smoothedAcceleration = Vector3.Lerp(smoothedAcceleration, acceleration,
                1f - Mathf.Exp(-7f * Time.deltaTime));
            Vector3 localAcceleration = transform.InverseTransformDirection(smoothedAcceleration);
            float inertiaPitch = Mathf.Clamp(-localAcceleration.z * 0.55f, -12f, 12f);
            float inertiaRoll = Mathf.Clamp(localAcceleration.x * 0.70f, -14f, 14f);
            float gaitSway = Mathf.Sin(time * (2.2f + stride * 5f)) * (4f + stride * 11f);
            float drunkSway = (Mathf.Sin(time * 1.31f) * 8f + Mathf.Sin(time * 2.77f + phase) * 4f) * buzz;
            float sway = gaitSway + drunkSway + inertiaRoll;
            float lean = Mathf.Sin(time * 1.17f + phase) * (2f + buzz * 8f) + inertiaPitch;
            bool knockedDown = knockedDownNow;
            fallBlend = Mathf.MoveTowards(fallBlend, knockedDown ? 1f : 0f,
                Time.deltaTime * (knockedDown ? 5f : 2.8f));

            float fallDirection = Mathf.Sin(phase + 0.5f) >= 0f ? 1f : -1f;
            float fallAngle = motor != null && motor.KnockdownPhysicsActive ? 24f : 78f;
            visualRoot.localRotation = Quaternion.Euler(lean * (1f - fallBlend), 0f,
                sway * (1f - fallBlend) + fallDirection * fallAngle * fallBlend);
            float step = Mathf.Abs(Mathf.Sin(time * (5.2f + speed)));
            float bob = step * (0.07f + buzz * 0.035f) * stride;
            float landDip = Mathf.Sin(landPulse * Mathf.PI);
            visualRoot.localPosition = Vector3.up * (bob - fallBlend * 0.42f - landDip * 0.08f);
            float squash = step * stride * 0.035f;
            float landSquash = landDip * 0.18f;
            visualRoot.localScale = new Vector3(1f + squash + landSquash,
                1f - squash - landSquash * 0.85f,
                1f + squash + landSquash);

            float cycle = Mathf.Sin(time * (4.5f + speed * 1.05f));
            float airborneFlail = motor != null && !motor.IsGrounded
                ? Mathf.Sin(time * 9f) * (18f + buzz * 20f)
                : 0f;
            float legSwing = cycle * (42f + buzz * 12f) * stride;
            float armSwing = -cycle * (34f + buzz * 16f) * stride + airborneFlail;
            AnimateLimb(leftLeg, legSwing - airborneFlail * 0.35f, -7f * stride, fallBlend, -32f);
            AnimateLimb(rightLeg, -legSwing + airborneFlail * 0.35f, 7f * stride, fallBlend, 32f);
            AnimateLimb(leftArm, armSwing, -10f - buzz * 8f, fallBlend, -72f);
            AnimateLimb(rightArm, -armSwing, 10f + buzz * 8f, fallBlend, 72f);
        }

        private void EnsureDependencies()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (stateMachine == null) stateMachine = GetComponent<PlayerStateMachine>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (vitals == null) vitals = GetComponent<PlayerVitals>();
            if (visualRoot == null) visualRoot = transform.Find("Wobbly Adult Visual");
            if (visualRoot == null) return;
            if (leftArm == null) leftArm = visualRoot.Find("Arm Pivot L");
            if (rightArm == null) rightArm = visualRoot.Find("Arm Pivot R");
            if (leftLeg == null) leftLeg = visualRoot.Find("Leg Pivot L");
            if (rightLeg == null) rightLeg = visualRoot.Find("Leg Pivot R");
        }

        private static void AnimateLimb(Transform limb, float swing, float drunkTilt,
            float currentFallBlend, float fallPose)
        {
            if (limb == null) return;
            float x = Mathf.Lerp(swing, fallPose, currentFallBlend);
            limb.localRotation = Quaternion.Euler(x, 0f, drunkTilt * (1f - currentFallBlend));
        }
    }
}
