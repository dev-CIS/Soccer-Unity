using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Bridges UI Toolkit touch widgets to the Input System: feeds a virtual
    /// device control (e.g. "&lt;Gamepad&gt;/leftStick") exactly like the uGUI
    /// OnScreenStick / OnScreenButton components did.
    /// </summary>
    public class CISOnScreenControl : OnScreenControl {
        [InputControl]
        [SerializeField] private string path;

        protected override string controlPathInternal {
            get => path;
            set => path = value;
        }

        public static CISOnScreenControl Create(Transform parent, string controlPath) {
            var host = new GameObject("OnScreen " + controlPath);
            host.SetActive(false);
            host.transform.SetParent(parent, false);

            var control = host.AddComponent<CISOnScreenControl>();
            control.controlPath = controlPath;

            return control;
        }

        public void SetActive(bool active) {
            if (gameObject.activeSelf != active) {
                gameObject.SetActive(active);
            }
        }

        public void Press(bool pressed) {
            if (isActiveAndEnabled) {
                SendValueToControl(pressed ? 1f : 0f);
            }
        }

        public void Move(Vector2 value) {
            if (isActiveAndEnabled) {
                SendValueToControl(value);
            }
        }

        public void Release() {
            if (isActiveAndEnabled) {
                SentDefaultValueToControl();
            }
        }
    }
}
