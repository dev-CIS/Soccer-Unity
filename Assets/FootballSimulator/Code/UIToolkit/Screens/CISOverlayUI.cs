using FStudio.Events;
using FStudio.UI.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Always-on top layer that survives scene and UI swaps:
    /// match loading curtain, loading toast and the error dialog.
    /// Created on demand by the menu / match UI (see <see cref="Ensure"/>).
    /// </summary>
    public class CISOverlayUI : MonoBehaviour {
        private static CISOverlayUI instance;

        private static readonly string[] Tips = {
            "Split the defence with a through pass: W on keyboard, Y on a gamepad.",
            "Change your team's tactic on the fly with Z / X or the D-pad.",
            "Switch to the player nearest the ball with Q or LB.",
            "Shoot with D on keyboard or X on a gamepad.",
            "Tackle and cross with A on keyboard or B on a gamepad.",
            "Pause with Esc or Start to change the camera and check live stats.",
            "Pick AWAY under 'You Control' to take charge of the visiting side."
        };

        private UIDocument document;

        private CISScreen curtain;
        private CISScreen toast;
        private CISScreen error;

        private Label toastText;
        private Label curtainTip;
        private Label errorText;
        private VisualElement curtainBar;

        private IVisualElementScheduledItem curtainHideJob;
        private IVisualElementScheduledItem tipJob;
        private IVisualElementScheduledItem barJob;

        private int tipIndex;
        private float barPosition;

        public static void Ensure() {
            if (instance != null) {
                return;
            }

            var host = new GameObject("CIS Overlay UI");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<CISOverlayUI>();
        }

        private void Awake() {
            if (instance != null && instance != this) {
                Destroy(gameObject);
                return;
            }

            instance = this;

            CISSettings.ApplyAll();

            document = CISUI.CreateDocument(transform, "Overlay", CISUI.SortOverlay);
            var root = document.rootVisualElement;

            curtain = new CISScreen(CISUI.Q<VisualElement>(root, "curtain"), false);
            curtain.Root.pickingMode = PickingMode.Position; // block input while loading.

            toast = new CISScreen(CISUI.Q<VisualElement>(root, "toast"), false);

            error = new CISScreen(CISUI.Q<VisualElement>(root, "error"), true) {
                OnCancel = CloseError
            };

            toastText = CISUI.Q<Label>(root, "toast-text");
            curtainTip = CISUI.Q<Label>(root, "curtain-tip");
            errorText = CISUI.Q<Label>(root, "error-text");
            curtainBar = CISUI.Q<VisualElement>(root, "curtain-bar");

            var ok = CISUI.Q<CISButton>(root, "error-ok");
            if (ok != null) {
                ok.clicked += CloseError;
                error.DefaultFocus = ok;
            }
        }

        private void OnEnable() {
            EventManager.Subscribe<LoadingEvent>(OnLoading);
            EventManager.Subscribe<BigLoadingEvent>(OnBigLoading);
            EventManager.Subscribe<ErrorEvent>(OnError);
        }

        private void OnDisable() {
            EventManager.UnSubscribe<LoadingEvent>(OnLoading);
            EventManager.UnSubscribe<BigLoadingEvent>(OnBigLoading);
            EventManager.UnSubscribe<ErrorEvent>(OnError);
        }

        private void OnDestroy() {
            if (instance == this) {
                instance = null;
            }
        }

        private void Update() {
            if (!CISNavigation.HasOpenScope) {
                return;
            }

            // Restore focus for pad / keyboard users after the mouse cleared it.
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            var navigationInput =
                (keyboard != null && keyboard.anyKey.wasPressedThisFrame) ||
                (gamepad != null && gamepad.wasUpdatedThisFrame);

            if (navigationInput) {
                CISNavigation.EnsureFocus();
            }
        }

        private void OnLoading(LoadingEvent loadingEvent) {
            if (loadingEvent == null) {
                toast.Hide();
                return;
            }

            toastText.text = Humanize(loadingEvent.Message);
            toast.Show();
        }

        private void OnBigLoading(BigLoadingEvent bigLoadingEvent) {
            curtainHideJob?.Pause();

            if (bigLoadingEvent != null) {
                ShowCurtain();
                return;
            }

            // keep the curtain a moment so the stadium can settle before revealing it.
            curtainHideJob = curtain.Root.schedule.Execute(HideCurtain);
            curtainHideJob.StartingIn(1000);
        }

        private void ShowCurtain() {
            tipIndex = Random.Range(0, Tips.Length);
            curtainTip.text = Tips[tipIndex];
            curtainTip.RemoveFromClassList("curtain__tip--out");

            tipJob?.Pause();
            tipJob = curtain.Root.schedule.Execute(NextTip).StartingIn(4500).Every(4500);

            barJob?.Pause();
            barJob = curtain.Root.schedule.Execute(() => {
                barPosition += 0.012f;
                if (barPosition > 1.35f) {
                    barPosition = -0.35f;
                }
                curtainBar.style.left = Length.Percent(barPosition * 100f);
            }).Every(16);

            curtain.Show();
        }

        private void HideCurtain() {
            tipJob?.Pause();
            barJob?.Pause();
            curtain.Hide();
        }

        private void NextTip() {
            curtainTip.AddToClassList("curtain__tip--out");
            curtainTip.schedule.Execute(() => {
                tipIndex = (tipIndex + 1) % Tips.Length;
                curtainTip.text = Tips[tipIndex];
                curtainTip.RemoveFromClassList("curtain__tip--out");
            }).StartingIn(320);
        }

        private void OnError(ErrorEvent errorEvent) {
            if (errorEvent == null) {
                error.Hide();
                return;
            }

            errorText.text = errorEvent.Error;
            error.Show();
        }

        private void CloseError() {
            EventManager.Trigger<ErrorEvent>(null);
        }

        private static string Humanize(string message) {
            if (string.IsNullOrEmpty(message)) {
                return "LOADING";
            }

            return message.Replace('_', ' ').ToUpperInvariant();
        }
    }
}
