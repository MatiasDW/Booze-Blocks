using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class WorldBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (UnityEngine.Camera.main == null) return;
            Vector3 direction = transform.position - UnityEngine.Camera.main.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
