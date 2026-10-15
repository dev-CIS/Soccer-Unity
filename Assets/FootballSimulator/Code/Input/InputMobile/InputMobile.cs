using System.Collections.Generic;
using FStudio.MatchEngine;
using FStudio.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.Input {
    /// <summary>
    /// Mobile touch controls (UI Toolkit). A floating joystick plus action
    /// buttons that feed the same virtual gamepad controls the old uGUI
    /// on-screen stick / buttons used, so the match input code is unchanged.
    /// </summary>
    internal class InputMobile : MonoBehaviour {
        private const string MATCH_ENGINE_ACTION_MAP = "MatchEngine";
        private const string PressedClass = "touch__btn--pressed";
        private const float StickRadius = 90f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Load() {
            var host = new GameObject("InputMobile");
            DontDestroyOnLoad(host);
            host.AddComponent<InputMobile>();
        }

        private VisualElement controls;
        private VisualElement pauseButton;
        private VisualElement stickZone;
        private VisualElement stickBase;
        private VisualElement stickKnob;

        private readonly List<CISOnScreenControl> mainControls = new List<CISOnScreenControl>();
        private readonly List<System.Action> releaseActions = new List<System.Action>();

        private CISOnScreenControl stick;
        private CISOnScreenControl pauseControl;

        private int stickPointer = -1;
        private Vector2 stickOrigin;

        private bool mainVisible = true;
        private bool cancelVisible = true;

        private void Awake() {
            var document = CISUI.CreateDocument(transform, "TouchControls", CISUI.SortTouch);
            var root = document.rootVisualElement;

            controls = root.Q("touch-controls");
            pauseButton = root.Q("touch-pause");
            stickZone = root.Q("touch-stick-zone");
            stickBase = root.Q("touch-stick-base");
            stickKnob = root.Q("touch-stick-knob");

            stick = CISOnScreenControl.Create(transform, "<Gamepad>/leftStick");
            mainControls.Add(stick);
            BindStick();

            BindButton(root.Q("touch-a"), "<XInputController>/buttonSouth", true);
            BindButton(root.Q("touch-b"), "<XInputController>/buttonEast", true);
            BindButton(root.Q("touch-x"), "<XInputController>/buttonWest", true);
            BindButton(root.Q("touch-y"), "<XInputController>/buttonNorth", true);
            BindButton(root.Q("touch-lb"), "<XInputController>/leftShoulder", true);
            BindButton(root.Q("touch-tactic-down"), "<XInputController>/dpad/left", true);
            BindButton(root.Q("touch-tactic-up"), "<XInputController>/dpad/right", true);
            pauseControl = BindButton(pauseButton, "<XInputController>/start", false);

            Apply(false, false);
        }

        private void Update() {
            var mainEnabled = false;
            var cancelEnabled = false;

#if UNITY_ANDROID || UNITY_IOS
            try {
                var matchEngineActive = MatchManager.Current != null;
                var hasUserTeam = MatchManager.Current?.UserTeam != null;

                var activeSchema = GameInput.ActiveSchema;
                var inMatch = GameInput.ActiveMap == MATCH_ENGINE_ACTION_MAP &&
                    activeSchema != "GamePad" &&
                    activeSchema != "XBoxController" &&
                    matchEngineActive;

                mainEnabled = inMatch && hasUserTeam;
                cancelEnabled = inMatch;
            } catch (System.Exception) {
                // no PlayerInput yet.
            }
#endif

            if (mainEnabled != mainVisible || cancelEnabled != cancelVisible) {
                Apply(mainEnabled, cancelEnabled);
            }
        }

        private void Apply(bool main, bool cancel) {
            mainVisible = main;
            cancelVisible = cancel;

            foreach (var release in releaseActions) {
                release();
            }
            ReleaseStick();

            CISUI.SetVisible(controls, main);
            CISUI.SetVisible(pauseButton, cancel);

            foreach (var control in mainControls) {
                control.SetActive(main);
            }

            pauseControl.SetActive(cancel);
        }

        // ------------------------------------------------------------------ buttons

        private CISOnScreenControl BindButton(VisualElement element, string controlPath, bool isMainControl) {
            var control = CISOnScreenControl.Create(transform, controlPath);

            if (isMainControl) {
                mainControls.Add(control);
            }

            if (element == null) {
                return control;
            }

            var pointer = -1;

            void Release() {
                if (pointer == -1) {
                    return;
                }

                if (element.HasPointerCapture(pointer)) {
                    element.ReleasePointer(pointer);
                }

                pointer = -1;
                element.RemoveFromClassList(PressedClass);
                control.Press(false);
            }

            element.RegisterCallback<PointerDownEvent>(evt => {
                if (pointer != -1) {
                    return;
                }

                pointer = evt.pointerId;
                element.CapturePointer(pointer);
                element.AddToClassList(PressedClass);
                control.Press(true);
                evt.StopPropagation();
            });

            element.RegisterCallback<PointerUpEvent>(evt => {
                if (evt.pointerId == pointer) {
                    Release();
                }
            });

            element.RegisterCallback<PointerCancelEvent>(evt => {
                if (evt.pointerId == pointer) {
                    Release();
                }
            });

            element.RegisterCallback<PointerCaptureOutEvent>(evt => Release());

            releaseActions.Add(Release);

            return control;
        }

        // ------------------------------------------------------------------ joystick

        private void BindStick() {
            if (stickZone == null) {
                return;
            }

            stickZone.RegisterCallback<PointerDownEvent>(evt => {
                if (stickPointer != -1) {
                    return;
                }

                stickPointer = evt.pointerId;
                stickZone.CapturePointer(stickPointer);

                // the stick re-centres under the thumb.
                stickOrigin = evt.localPosition;
                var half = stickBase.resolvedStyle.width * 0.5f;
                stickBase.style.left = stickOrigin.x - half;
                stickBase.style.top = stickOrigin.y - half;
                stickBase.style.bottom = StyleKeyword.Auto;
                stickBase.AddToClassList("touch__stick-base--active");

                MoveStick(Vector2.zero);
                evt.StopPropagation();
            });

            stickZone.RegisterCallback<PointerMoveEvent>(evt => {
                if (evt.pointerId != stickPointer) {
                    return;
                }

                MoveStick((Vector2)evt.localPosition - stickOrigin);
            });

            stickZone.RegisterCallback<PointerUpEvent>(evt => {
                if (evt.pointerId == stickPointer) {
                    ReleaseStick();
                }
            });

            stickZone.RegisterCallback<PointerCancelEvent>(evt => {
                if (evt.pointerId == stickPointer) {
                    ReleaseStick();
                }
            });

            stickZone.RegisterCallback<PointerCaptureOutEvent>(evt => ReleaseStick());
        }

        private void MoveStick(Vector2 delta) {
            delta = Vector2.ClampMagnitude(delta, StickRadius);
            stickKnob.style.translate = new Translate(delta.x, delta.y, 0);

            // UI space grows downwards, stick "up" is positive.
            stick.Move(new Vector2(delta.x, -delta.y) / StickRadius);
        }

        private void ReleaseStick() {
            if (stickPointer != -1 && stickZone.HasPointerCapture(stickPointer)) {
                stickZone.ReleasePointer(stickPointer);
            }

            stickPointer = -1;

            stickKnob.style.translate = new Translate(0, 0, 0);
            stickBase.style.left = StyleKeyword.Null;
            stickBase.style.top = StyleKeyword.Null;
            stickBase.style.bottom = StyleKeyword.Null;
            stickBase.RemoveFromClassList("touch__stick-base--active");

            stick.Release();
        }
    }
}
