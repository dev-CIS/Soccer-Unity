using System;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// FIFA style option row: LABEL ........ &lt; VALUE &gt;
    /// Left/Right (pad, keys) or the arrow buttons cycle through the options.
    /// </summary>
    public class CISSpinner : VisualElement {
        public new class UxmlFactory : UxmlFactory<CISSpinner, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits {
            private readonly UxmlStringAttributeDescription labelAttribute =
                new UxmlStringAttributeDescription { name = "label", defaultValue = "OPTION" };

            private readonly UxmlBoolAttributeDescription pipsAttribute =
                new UxmlBoolAttributeDescription { name = "show-pips", defaultValue = false };

            public UxmlTraits() {
                // UXML would otherwise reset focusable to false.
                focusable.defaultValue = true;
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                var spinner = (CISSpinner)ve;
                spinner.label = labelAttribute.GetValueFromBag(bag, cc);
                spinner.showPips = pipsAttribute.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>Raised with the new index when the user changes the value.</summary>
        public event Action<int> changed;

        /// <summary>
        /// Raised on submit (A / Enter). When nobody listens, submit steps to the next option.
        /// </summary>
        public event Action submitted;

        private readonly Label labelElement;
        private readonly Label valueElement;
        private readonly VisualElement pipsElement;

        private string[] options = new string[0];
        private int index;
        private bool m_ShowPips;

        public string label {
            get => labelElement.text;
            set => labelElement.text = value;
        }

        public bool showPips {
            get => m_ShowPips;
            set {
                m_ShowPips = value;
                Refresh();
            }
        }

        public int Index => index;

        public CISSpinner() {
            AddToClassList("cis-spinner");
            focusable = true;

            labelElement = CISUI.Text("OPTION", "cis-spinner__label", this);

            var control = CISUI.Element("cis-spinner__control", this);

            var left = new CISIcon(CISIcon.Kind.ChevronLeft);
            left.AddToClassList("cis-spinner__arrow");
            left.pickingMode = PickingMode.Position;
            control.Add(left);

            var valueWrap = CISUI.Element("cis-spinner__value-wrap", control);
            valueElement = CISUI.Text("-", "cis-spinner__value", valueWrap);
            pipsElement = CISUI.Element("cis-spinner__pips", valueWrap);

            var right = new CISIcon(CISIcon.Kind.ChevronRight);
            right.AddToClassList("cis-spinner__arrow");
            right.pickingMode = PickingMode.Position;
            control.Add(right);

            left.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation();
                Focus();
                Step(-1);
            });

            right.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation();
                Focus();
                Step(1);
            });

            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);

            RegisterCallback<NavigationSubmitEvent>(evt => {
                evt.StopPropagation();

                if (submitted != null) {
                    CISUI.PlayClickSound();
                    submitted.Invoke();
                } else {
                    Step(1);
                }
            });
        }

        public void SetOptions(string[] newOptions, int selected) {
            options = newOptions ?? new string[0];
            index = options.Length == 0 ? 0 : Math.Max(0, Math.Min(selected, options.Length - 1));
            Refresh();
        }

        public void SetIndexWithoutNotify(int selected) {
            if (options.Length == 0) {
                return;
            }

            index = Math.Max(0, Math.Min(selected, options.Length - 1));
            Refresh();
        }

        private void OnNavigationMove(NavigationMoveEvent evt) {
            if (evt.direction == NavigationMoveEvent.Direction.Left) {
                Step(-1);
            } else if (evt.direction == NavigationMoveEvent.Direction.Right) {
                Step(1);
            } else {
                return;
            }

            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void Step(int delta) {
            if (options.Length == 0) {
                return;
            }

            index = (index + delta + options.Length) % options.Length;
            Refresh();

            CISUI.PlayClickSound();
            CISUI.Pulse(valueElement, "cis-spinner__value--pop", 120);

            changed?.Invoke(index);
        }

        private void Refresh() {
            if (valueElement == null) {
                return;
            }

            valueElement.text = options.Length > 0 ? options[index] : "-";

            pipsElement.Clear();
            pipsElement.style.display = m_ShowPips && options.Length > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            if (!m_ShowPips) {
                return;
            }

            for (int i = 0; i < options.Length; i++) {
                var pip = CISUI.Element("cis-spinner__pip", pipsElement);
                if (i <= index) {
                    pip.AddToClassList("cis-spinner__pip--on");
                }
            }
        }
    }
}
