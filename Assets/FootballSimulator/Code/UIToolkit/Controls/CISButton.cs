using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Button with an optional vector icon and a display-font label.
    /// It drops Unity's default .unity-button styling so only the CIS theme applies.
    /// </summary>
    public class CISButton : Button {
        public new class UxmlFactory : UxmlFactory<CISButton, UxmlTraits> { }

        public new class UxmlTraits : Button.UxmlTraits {
            private readonly UxmlStringAttributeDescription labelAttribute =
                new UxmlStringAttributeDescription { name = "label", defaultValue = "BUTTON" };

            private readonly UxmlEnumAttributeDescription<CISIcon.Kind> iconAttribute =
                new UxmlEnumAttributeDescription<CISIcon.Kind> { name = "icon", defaultValue = CISIcon.Kind.None };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                var button = (CISButton)ve;
                button.text = string.Empty;
                button.label = labelAttribute.GetValueFromBag(bag, cc);
                button.icon = iconAttribute.GetValueFromBag(bag, cc);
            }
        }

        private readonly CISIcon iconElement;
        private readonly Label labelElement;

        public string label {
            get => labelElement.text;
            set => labelElement.text = value;
        }

        public CISIcon.Kind icon {
            get => iconElement.kind;
            set {
                iconElement.kind = value;
                iconElement.style.display = value == CISIcon.Kind.None ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public CISButton() {
            RemoveFromClassList(ussClassName);
            AddToClassList("cis-btn");

            iconElement = new CISIcon(CISIcon.Kind.None);
            iconElement.style.display = DisplayStyle.None;
            Add(iconElement);

            labelElement = CISUI.Text("BUTTON", "cis-btn__label", this);

            clicked += CISUI.PlayClickSound;
        }
    }
}
