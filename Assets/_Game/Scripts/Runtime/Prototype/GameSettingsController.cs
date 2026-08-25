using BoozeBlocks.Camera;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class GameSettingsController : MonoBehaviour
    {
        private static readonly int[] FrameRateOptions = { 30, 60, 120, -1 };
        private ThirdPersonCamera cameraController;
        private Resolution[] resolutions = System.Array.Empty<Resolution>();
        private int resolutionIndex;

        public int QualityIndex { get; private set; }
        public bool FullScreen { get; private set; }
        public bool VSync { get; private set; }
        public int FrameRateIndex { get; private set; }
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float EffectsVolume { get; private set; }
        public float MouseSensitivity { get; private set; }
        public bool CameraShake { get; private set; }
        public bool Subtitles { get; private set; }

        public string QualityName => QualitySettings.names.Length == 0
            ? "Predeterminada"
            : QualitySettings.names[Mathf.Clamp(QualityIndex, 0, QualitySettings.names.Length - 1)];
        public string ResolutionName => resolutions.Length == 0
            ? $"{Screen.width} x {Screen.height}"
            : $"{resolutions[resolutionIndex].width} x {resolutions[resolutionIndex].height}";
        public string FrameRateName => FrameRateOptions[FrameRateIndex] < 0
            ? "Sin limite"
            : $"{FrameRateOptions[FrameRateIndex]} FPS";

        public void Configure(ThirdPersonCamera thirdPersonCamera)
        {
            cameraController = thirdPersonCamera;
            resolutions = Screen.resolutions ?? System.Array.Empty<Resolution>();
            resolutionIndex = FindCurrentResolution();
            QualityIndex = Mathf.Clamp(PlayerPrefs.GetInt("settings.quality", QualitySettings.GetQualityLevel()),
                0, Mathf.Max(0, QualitySettings.names.Length - 1));
            FullScreen = PlayerPrefs.GetInt("settings.fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            VSync = PlayerPrefs.GetInt("settings.vsync", 1) == 1;
            FrameRateIndex = Mathf.Clamp(PlayerPrefs.GetInt("settings.framerate", 1), 0,
                FrameRateOptions.Length - 1);
            MasterVolume = PlayerPrefs.GetFloat("settings.master", 0.85f);
            MusicVolume = PlayerPrefs.GetFloat("settings.music", 0.70f);
            EffectsVolume = PlayerPrefs.GetFloat("settings.effects", 0.90f);
            MouseSensitivity = PlayerPrefs.GetFloat("settings.sensitivity", 0.12f);
            CameraShake = PlayerPrefs.GetInt("settings.cameraShake", 1) == 1;
            Subtitles = PlayerPrefs.GetInt("settings.subtitles", 1) == 1;
            Apply(false);
        }

        public void CycleQuality(int direction)
        {
            if (QualitySettings.names.Length == 0) return;
            QualityIndex = Wrap(QualityIndex + direction, QualitySettings.names.Length);
            QualitySettings.SetQualityLevel(QualityIndex, true);
            Save();
        }

        public void CycleResolution(int direction)
        {
            if (resolutions.Length == 0) return;
            resolutionIndex = Wrap(resolutionIndex + direction, resolutions.Length);
        }

        public void ToggleFullScreen()
        {
            FullScreen = !FullScreen;
        }

        public void ToggleVSync()
        {
            VSync = !VSync;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Save();
        }

        public void CycleFrameRate(int direction)
        {
            FrameRateIndex = Wrap(FrameRateIndex + direction, FrameRateOptions.Length);
            Application.targetFrameRate = FrameRateOptions[FrameRateIndex];
            Save();
        }

        public void SetMasterVolume(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(MasterVolume, clamped)) return;
            MasterVolume = clamped;
            AudioListener.volume = MasterVolume;
            Save();
        }

        public void SetMusicVolume(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(MusicVolume, clamped)) return;
            MusicVolume = clamped;
            Save();
        }

        public void SetEffectsVolume(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(EffectsVolume, clamped)) return;
            EffectsVolume = clamped;
            Save();
        }

        public void SetMouseSensitivity(float value)
        {
            float clamped = Mathf.Clamp(value, 0.04f, 0.30f);
            if (Mathf.Approximately(MouseSensitivity, clamped)) return;
            MouseSensitivity = clamped;
            cameraController?.SetMouseSensitivity(MouseSensitivity);
            Save();
        }

        public void ToggleCameraShake()
        {
            CameraShake = !CameraShake;
            cameraController?.SetShakeEnabled(CameraShake);
            Save();
        }

        public void ToggleSubtitles()
        {
            Subtitles = !Subtitles;
            Save();
        }

        public void Apply(bool applyResolution = true)
        {
            if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(QualityIndex, true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FrameRateOptions[FrameRateIndex];
            AudioListener.volume = MasterVolume;
            cameraController?.SetMouseSensitivity(MouseSensitivity);
            cameraController?.SetShakeEnabled(CameraShake);
            if (applyResolution && resolutions.Length > 0)
            {
                Resolution resolution = resolutions[resolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, FullScreen);
            }
            Save();
        }

        public void ResetDefaults()
        {
            QualityIndex = Mathf.Clamp(2, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            FullScreen = true;
            VSync = true;
            FrameRateIndex = 1;
            MasterVolume = 0.85f;
            MusicVolume = 0.70f;
            EffectsVolume = 0.90f;
            MouseSensitivity = 0.12f;
            CameraShake = true;
            Subtitles = true;
            resolutionIndex = FindCurrentResolution();
            Apply(false);
        }

        private int FindCurrentResolution()
        {
            int bestIndex = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < resolutions.Length; i++)
            {
                int distance = Mathf.Abs(resolutions[i].width - Screen.width) +
                               Mathf.Abs(resolutions[i].height - Screen.height);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestIndex = i;
            }
            return bestIndex;
        }

        private void Save()
        {
            PlayerPrefs.SetInt("settings.quality", QualityIndex);
            PlayerPrefs.SetInt("settings.fullscreen", FullScreen ? 1 : 0);
            PlayerPrefs.SetInt("settings.vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt("settings.framerate", FrameRateIndex);
            PlayerPrefs.SetFloat("settings.master", MasterVolume);
            PlayerPrefs.SetFloat("settings.music", MusicVolume);
            PlayerPrefs.SetFloat("settings.effects", EffectsVolume);
            PlayerPrefs.SetFloat("settings.sensitivity", MouseSensitivity);
            PlayerPrefs.SetInt("settings.cameraShake", CameraShake ? 1 : 0);
            PlayerPrefs.SetInt("settings.subtitles", Subtitles ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static int Wrap(int value, int count)
        {
            return (value % count + count) % count;
        }
    }
}
