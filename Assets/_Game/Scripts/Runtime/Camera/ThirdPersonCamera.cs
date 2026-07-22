using UnityEngine;
using UnityEngine.InputSystem;

namespace BoozeBlocks.Camera
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField, Min(1f)] private float distance = 8f;
        [SerializeField, Min(0f)] private float followSharpness = 12f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float gamepadSensitivity = 110f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-15f, 65f);
        [SerializeField] private bool lockCursor = true;

        private Transform target;
        private float yaw;
        private float pitch = 22f;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            if (target != null) yaw = target.eulerAngles.y;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            transform.SetPositionAndRotation(pivot - rotation * Vector3.forward * distance, rotation);
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

            Vector3 pivot = target.position + pivotOffset;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = pivot - rotation * Vector3.forward * distance;

            if (Physics.Linecast(pivot, desiredPosition, out RaycastHit hit,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                desiredPosition = hit.point + hit.normal * 0.2f;
            }

            float blend = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
            transform.rotation = rotation;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
