using UnityEngine;

namespace BoozeBlocks.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerActionFeedback : MonoBehaviour
    {
        private float visibleUntil;

        public string CurrentMessage { get; private set; } = string.Empty;
        public bool IsVisible => !string.IsNullOrEmpty(CurrentMessage) && Time.unscaledTime < visibleUntil;

        public void Show(string message, float duration = 2.2f)
        {
            CurrentMessage = message ?? string.Empty;
            visibleUntil = Time.unscaledTime + Mathf.Max(0.2f, duration);
        }
    }
}
