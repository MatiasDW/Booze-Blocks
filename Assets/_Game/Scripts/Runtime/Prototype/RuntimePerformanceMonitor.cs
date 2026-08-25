using UnityEngine;
using UnityEngine.InputSystem;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class RuntimePerformanceMonitor : MonoBehaviour
    {
        private const int SampleCapacity = 600;
        private readonly float[] frameTimes = new float[SampleCapacity];
        private int sampleCount;
        private int sampleIndex;
        private float refreshAt;
        private GUIStyle style;

        public float AverageFps { get; private set; }
        public float WorstFrameMilliseconds { get; private set; }
        public float SlowFramePercent { get; private set; }
        public float ManagedMemoryMegabytes { get; private set; }
        public bool IsOverlayVisible { get; private set; }

        private void Update()
        {
            float frameTime = Mathf.Max(0.00001f, Time.unscaledDeltaTime);
            frameTimes[sampleIndex] = frameTime;
            sampleIndex = (sampleIndex + 1) % SampleCapacity;
            sampleCount = Mathf.Min(sampleCount + 1, SampleCapacity);

            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
                IsOverlayVisible = !IsOverlayVisible;
            if (Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + 0.5f;
            Recalculate();
        }

        public void SetOverlayVisible(bool visible)
        {
            IsOverlayVisible = visible;
        }

        private void Recalculate()
        {
            if (sampleCount == 0) return;
            float total = 0f;
            float worst = 0f;
            int slowFrames = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                float frameTime = frameTimes[i];
                total += frameTime;
                worst = Mathf.Max(worst, frameTime);
                if (frameTime > 1f / 30f) slowFrames++;
            }
            AverageFps = sampleCount / total;
            WorstFrameMilliseconds = worst * 1000f;
            SlowFramePercent = slowFrames * 100f / sampleCount;
            ManagedMemoryMegabytes = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        }

        private void OnGUI()
        {
            if (!IsOverlayVisible) return;
            style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            Rect panel = new Rect(Screen.width - 250f, 16f, 234f, 106f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, 215f, 90f),
                $"PERFORMANCE (F3)\nFPS promedio: {AverageFps:0}\nPeor frame: {WorstFrameMilliseconds:0.0} ms\nFrames >33 ms: {SlowFramePercent:0.0}%\nMemoria C#: {ManagedMemoryMegabytes:0.0} MB", style);
        }
    }
}
