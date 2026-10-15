using FStudio.Data;
using FStudio.MatchEngine.Cameras;
using FStudio.MatchEngine.Enums;
using Shared.Responses;
using UnityEngine;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Persistent match + game settings. Uses the same PlayerPrefs keys as the
    /// previous uGUI panels so existing saves carry over.
    /// </summary>
    public static class CISSettings {
        private const string KeyAILevel = "SETTING_AILEVEL";
        private const string KeyDayTime = "SETTING_DAYTIME";
        private const string KeySide = "SETTING_SIDE_CIS";
        private const string KeyQuality = "SETTING_QUALITY";
        private const string KeyCamera = "SETTING_CAMERA";
        private const string KeyVolume = "SETTING_VOLUME_CIS";

        public static readonly string[] CameraTypes = { "Broadcast", "Stadium", "Tele", "StadiumHigh" };
        public static readonly string[] CameraLabels = { "BROADCAST", "STADIUM", "TELE", "HIGH STADIUM" };

        public static readonly string[] AILevelLabels = { "AMATEUR", "SEMI-PRO", "PROFESSIONAL", "WORLD CLASS", "LEGENDARY" };
        public static readonly string[] DayTimeLabels = { "MORNING", "AFTERNOON", "NIGHT" };
        public static readonly string[] SideLabels = { "SPECTATE", "HOME", "AWAY" };

        private const int DefaultQuality =
#if UNITY_STANDALONE
            2;
#else
            1;
#endif

        private const int DefaultCamera = 1;

        public static AILevel AILevel {
            get => (AILevel)Mathf.Clamp(PlayerPrefs.GetInt(KeyAILevel, (int)AILevel.Legendary), 0, AILevelLabels.Length - 1);
            set => PlayerPrefs.SetInt(KeyAILevel, (int)value);
        }

        public static DayTimes DayTime {
            get => (DayTimes)Mathf.Clamp(PlayerPrefs.GetInt(KeyDayTime, (int)DayTimes.Night), 0, DayTimeLabels.Length - 1);
            set => PlayerPrefs.SetInt(KeyDayTime, (int)value);
        }

        /// <summary>Which team the player controls. Defaults to Home.</summary>
        public static MatchCreateRequest.UserTeam Side {
            get => (MatchCreateRequest.UserTeam)Mathf.Clamp(PlayerPrefs.GetInt(KeySide, (int)MatchCreateRequest.UserTeam.Home), 0, SideLabels.Length - 1);
            set => PlayerPrefs.SetInt(KeySide, (int)value);
        }

        public static int Quality {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, DefaultQuality), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            set {
                PlayerPrefs.SetInt(KeyQuality, value);
                ApplyGraphics();
            }
        }

        public static int Camera {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KeyCamera, DefaultCamera), 0, CameraTypes.Length - 1);
            set {
                PlayerPrefs.SetInt(KeyCamera, value);
                ApplyCamera();
            }
        }

        /// <summary>Master volume step, 0..10.</summary>
        public static int Volume {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KeyVolume, 8), 0, 10);
            set {
                PlayerPrefs.SetInt(KeyVolume, value);
                ApplyVolume();
            }
        }

        public static string[] QualityLabels {
            get {
                var names = QualitySettings.names;
                var labels = new string[names.Length];
                for (int i = 0; i < names.Length; i++) {
                    labels[i] = CISUI.Upper(names[i]);
                }
                return labels;
            }
        }

        public static string[] VolumeLabels {
            get {
                var labels = new string[11];
                for (int i = 0; i <= 10; i++) {
                    labels[i] = i == 0 ? "MUTED" : (i * 10) + "%";
                }
                return labels;
            }
        }

        public static void ApplyAll() {
            ApplyGraphics();
            ApplyVolume();
        }

        public static void ApplyGraphics() {
            var quality = Quality;
            QualitySettings.SetQualityLevel(quality);
            // same shader layer formula the match engine used before.
            Shader.SetGlobalFloat("_SHADER_LAYER_COUNT", 2 + quality * 3 + quality);
        }

        public static void ApplyVolume() {
            AudioListener.volume = Volume / 10f;
        }

        public static async void ApplyCamera() {
            var cameraSystem = CameraSystem.Current;
            if (cameraSystem == null) {
                return;
            }

            try {
                await cameraSystem.SwitchCamera(CameraTypes[Camera]);
            } catch (System.Exception exception) {
                Debug.LogWarning("[CISSettings] Could not switch camera: " + exception.Message);
            }
        }
    }
}
