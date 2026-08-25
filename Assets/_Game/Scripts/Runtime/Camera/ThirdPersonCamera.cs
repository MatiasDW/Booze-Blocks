using UnityEngine;
using UnityEngine.InputSystem;

namespace BoozeBlocks.Camera
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField, Min(1f)] private float distance = 8f;
        [SerializeField, Min(0f)] private float positionSharpness = 14f;
        [SerializeField, Min(0f)] private float rotationSharpness = 16f;
        [SerializeField, Min(0f)] private float lookaheadStrength = 0.55f;
        [SerializeField, Min(0f)] private float lookaheadMaxDistance = 1.4f;
        [SerializeField, Min(0f)] private float lookaheadSharpness = 6f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float gamepadSensitivity = 110f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-15f, 65f);
        [SerializeField] private bool lockCursor = true;
        [SerializeField, Min(30f)] private float baseFieldOfView = 62f;
        [SerializeField, Min(0f)] private float fovKickSharpness = 5f;

        private Transform target;
        private Rigidbody targetBody;
        private UnityEngine.Camera cachedCamera;
        private Vector3 smoothedLookahead;
        private float yaw;
        private float pitch = 22f;
        private bool shakeEnabled = true;
        private float shakeStrength;
        private float shakeSeed;
        private float fovKick;
        private float smoothedFovKick;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            targetBody = target != null ? target.GetComponent<Rigidbody>() : null;
            if (target != null) yaw = target.eulerAngles.y;
            smoothedLookahead = Vector3.zero;
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.04f, 0.30f);
        }

        public void SetShakeEnabled(bool enabled)
        {
            shakeEnabled = enabled;
            if (!enabled) shakeStrength = 0f;
        }

        public void AddImpulse(float strength)
        {
            if (!shakeEnabled) return;
            shakeStrength = Mathf.Clamp01(shakeStrength + Mathf.Max(0f, strength));
        }

        public void SetFovKick(float extraDegrees)
        {
            fovKick = Mathf.Clamp(extraDegrees, -10f, 12f);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
        }

        private void Awake()
        {
            cachedCamera = GetComponent<UnityEngine.Camera>();
            if (cachedCamera != null) cachedCamera.fieldOfView = baseFieldOfView;
            shakeSeed = transform.position.x * 0.13f + transform.position.z * 0.29f + 17f;
        }

        private void OnEnable()
        {
            if (lockCursor) SetCursorLocked(true);
        }

        private void OnDisable()
        {
            if (lockCursor) SetCursorLocked(false);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (lockCursor)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    SetCursorLocked(false);
                }
                else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                         Cursor.lockState != CursorLockMode.Locked)
                {
                    SetCursorLocked(true);
                }
            }

            Vector2 look = Vector2.zero;
            if (Mouse.current != null && (!lockCursor || Cursor.lockState == CursorLockMode.Locked))
            {
                look += Mouse.current.delta.ReadValue() * mouseSensitivity;
            }

            if (Gamepad.current != null)
            {
                look += Gamepad.current.rightStick.ReadValue() * (gamepadSensitivity * Time.unscaledDeltaTime);
            }

            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, pitchLimits.x, pitchLimits.y);

            Vector3 lookaheadTarget = Vector3.zero;
            if (targetBody != null && lookaheadStrength > 0f)
            {
                Vector3 flat = Vector3.ProjectOnPlane(targetBody.linearVelocity, Vector3.up);
                lookaheadTarget = Vector3.ClampMagnitude(flat * lookaheadStrength, lookaheadMaxDistance);
            }
            float lookaheadBlend = 1f - Mathf.Exp(-lookaheadSharpness * Time.unscaledDeltaTime);
            smoothedLookahead = Vector3.Lerp(smoothedLookahead, lookaheadTarget, lookaheadBlend);

            Vector3 pivot = target.position + pivotOffset + smoothedLookahead;
            Quaternion desiredRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = pivot - desiredRotation * Vector3.forward * distance;

            if (shakeStrength > 0.001f)
            {
                float time = Time.unscaledTime * 6.5f;
                float nx = Mathf.PerlinNoise(shakeSeed + time, shakeSeed * 0.5f) - 0.5f;
                float ny = Mathf.PerlinNoise(shakeSeed * 0.5f, shakeSeed + time * 1.13f) - 0.5f;
                Vector3 shake = transform.right * nx + transform.up * ny;
                desiredPosition += shake * (shakeStrength * shakeStrength * 0.35f);
                shakeStrength = Mathf.MoveTowards(shakeStrength, 0f, Time.unscaledDeltaTime * 2.8f);
            }

            if (Physics.Linecast(pivot, desiredPosition, out RaycastHit hit,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                desiredPosition = hit.point + hit.normal * 0.2f;
            }

            float positionBlend = 1f - Mathf.Exp(-positionSharpness * Time.unscaledDeltaTime);
            float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionBlend);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);

            if (cachedCamera != null)
            {
                float fovBlend = 1f - Mathf.Exp(-fovKickSharpness * Time.unscaledDeltaTime);
                smoothedFovKick = Mathf.Lerp(smoothedFovKick, fovKick, fovBlend);
                cachedCamera.fieldOfView = baseFieldOfView + smoothedFovKick;
                fovKick = Mathf.MoveTowards(fovKick, 0f, Time.unscaledDeltaTime * 6f);
            }
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
