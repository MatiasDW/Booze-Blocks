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
        private float phase;
        private float fallBlend;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            stateMachine = GetComponent<PlayerStateMachine>();
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
            float stride = Mathf.Clamp01(speed / 5f);
            float sway = Mathf.Sin(time * (2.2f + stride * 5f)) * (5f + stride * 8f);
            float lean = Mathf.Sin(time * 1.35f) * 4f;
            bool knockedDown = stateMachine != null &&
                (stateMachine.State == PlayerState.KnockedDown || stateMachine.State == PlayerState.Eliminated);
            fallBlend = Mathf.MoveTowards(fallBlend, knockedDown ? 1f : 0f,
                Time.deltaTime * (knockedDown ? 5f : 2.8f));

            float fallDirection = Mathf.Sin(phase + 0.5f) >= 0f ? 1f : -1f;
            visualRoot.localRotation = Quaternion.Euler(lean * (1f - fallBlend), 0f,
                sway * (1f - fallBlend) + fallDirection * 78f * fallBlend);
            float bob = Mathf.Abs(Mathf.Sin(time * 6f)) * 0.08f * stride;
            visualRoot.localPosition = Vector3.up * (bob - fallBlend * 0.42f);

            float cycle = Mathf.Sin(time * (4.5f + speed * 1.05f));
            float legSwing = cycle * 34f * stride;
            float armSwing = -cycle * 28f * stride;
            AnimateLimb(leftLeg, legSwing, -4f * stride, fallBlend, -24f);
            AnimateLimb(rightLeg, -legSwing, 4f * stride, fallBlend, 24f);
            AnimateLimb(leftArm, armSwing, -7f, fallBlend, -62f);
            AnimateLimb(rightArm, -armSwing, 7f, fallBlend, 62f);
        }

        private void EnsureDependencies()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (stateMachine == null) stateMachine = GetComponent<PlayerStateMachine>();
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
