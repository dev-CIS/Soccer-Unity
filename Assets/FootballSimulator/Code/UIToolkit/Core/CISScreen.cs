using System;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// A full-screen or modal layer with an animated show/hide (driven by the
    /// .cis-screen / .cis-screen--in USS classes) and its own navigation scope.
    /// </summary>
    public class CISScreen {
        private const long TransitionMs = 300;

        public readonly VisualElement Root;

        /// <summary>
        /// Navigable screens take part in gamepad/keyboard focus navigation.
        /// Passive screens (HUD, banners) never steal focus.
        /// </summary>
        public readonly bool Navigable;

        public VisualElement DefaultFocus;

        /// <summary>
        /// Invoked on Cancel (B / Esc) while this screen is the top navigation scope.
        /// </summary>
        public Action OnCancel;

        public bool IsOpen { get; private set; }

        public event Action Shown;
        public event Action Hidden;

        private IVisualElementScheduledItem pending;
        private VisualElement requestedFocus;

        /// <param name="fullScreen">
        /// Full-screen layers get the .cis-screen class (absolute fill + fade/slide).
        /// Pass false for HUD widgets that position and animate themselves
        /// through their own "&lt;block&gt;.cis-screen--in" rules.
        /// </param>
        public CISScreen(VisualElement root, bool navigable, bool fullScreen = true) {
            Root = root;
            Navigable = navigable;

            if (Root == null) {
                return;
            }

            if (fullScreen) {
                Root.AddToClassList("cis-screen");
            }
            Root.style.display = DisplayStyle.None;

            if (!navigable) {
                Root.pickingMode = PickingMode.Ignore;
            }
        }

        /// <param name="focus">
        /// Element to focus this time. When omitted the screen restores the
        /// element that was focused last time it was open, then DefaultFocus.
        /// </param>
        public void Show(VisualElement focus = null) {
            if (Root == null) {
                return;
            }

            requestedFocus = focus;

            pending?.Pause();

            var wasOpen = IsOpen;
            IsOpen = true;

            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();

            if (Navigable) {
                CISNavigation.Push(this);
            }

            pending = Root.schedule.Execute(() => {
                Root.AddToClassList("cis-screen--in");

                if (Navigable) {
                    CISNavigation.FocusScreen(this, requestedFocus);
                    requestedFocus = null;
                }
            });
            pending.StartingIn(16);

            if (!wasOpen) {
                Shown?.Invoke();
            }
        }

        public void Hide() {
            if (Root == null || !IsOpen) {
                return;
            }

            IsOpen = false;

            if (Navigable) {
                CISNavigation.Pop(this);
            }

            BlurInside();

            Root.RemoveFromClassList("cis-screen--in");

            pending?.Pause();
            pending = Root.schedule.Execute(() => {
                if (!IsOpen) {
                    Root.style.display = DisplayStyle.None;
                }
            });
            pending.StartingIn(TransitionMs);

            Hidden?.Invoke();
        }

        public void HideImmediate() {
            if (Root == null) {
                return;
            }

            var wasOpen = IsOpen;
            IsOpen = false;

            if (Navigable) {
                CISNavigation.Pop(this);
            }

            BlurInside();

            pending?.Pause();
            Root.RemoveFromClassList("cis-screen--in");
            Root.style.display = DisplayStyle.None;

            if (wasOpen) {
                Hidden?.Invoke();
            }
        }

        private void BlurInside() {
            var focused = Root.panel?.focusController?.focusedElement as VisualElement;
            if (focused != null && (focused == Root || Root.Contains(focused))) {
                focused.Blur();
            }
        }
    }
}
