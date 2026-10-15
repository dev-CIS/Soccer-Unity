using System;
using FStudio.Events;
using FStudio.UI.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Shared runtime plumbing for every UI Toolkit document in CIS Soccer:
    /// one panel, one theme, layout loading and a few element helpers.
    /// </summary>
    public static class CISUI {
        public const string ResourceRoot = "CISUI/";
        public const string RootClass = "cis-root";

        // Sorting orders of the documents inside the shared panel.
        public const int SortMenu = 0;
        public const int SortMatch = 10;
        public const int SortTouch = 40;
        public const int SortOverlay = 100;

        private static PanelSettings panelSettings;
        private static float lastFocusSoundTime;

        public static PanelSettings PanelSettings {
            get {
                if (panelSettings == null) {
                    // PanelSettings.OnEnable warns about the missing theme before we get the
                    // chance to assign it on the next line; keep that false alarm out of the console.
                    var logEnabled = Debug.unityLogger.logEnabled;
                    Debug.unityLogger.logEnabled = false;
                    try {
                        panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                    } finally {
                        Debug.unityLogger.logEnabled = logEnabled;
                    }

                    panelSettings.name = "CIS Panel Settings";
                    panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ResourceRoot + "CISTheme");
                    panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                    panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                    // Shrink keeps at least 1920x1080 logical pixels on every aspect ratio,
                    // so layouts authored at 1080p always fit.
                    panelSettings.screenMatchMode = PanelScreenMatchMode.Shrink;
                    panelSettings.sortingOrder = 100;
                    panelSettings.clearColor = false;

                    if (panelSettings.themeStyleSheet == null) {
                        Debug.LogError("[CISUI] Theme not found at Resources/" + ResourceRoot + "CISTheme");
                    }
                }

                return panelSettings;
            }
        }

        /// <summary>
        /// Creates a child GameObject with a UIDocument rendering the given layout
        /// from Resources/CISUI/Layouts.
        /// </summary>
        public static UIDocument CreateDocument(Transform parent, string layout, int sortingOrder) {
            var host = new GameObject("UIDocument - " + layout);
            host.SetActive(false);
            host.transform.SetParent(parent, false);

            var document = host.AddComponent<UIDocument>();
            document.panelSettings = PanelSettings;
            document.sortingOrder = sortingOrder;
            document.visualTreeAsset = Resources.Load<VisualTreeAsset>(ResourceRoot + "Layouts/" + layout);

            if (document.visualTreeAsset == null) {
                Debug.LogError("[CISUI] Layout not found: " + layout);
            }

            host.SetActive(true);

            var root = document.rootVisualElement;
            root.AddToClassList(RootClass);
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;

            CISGradients.Apply(root);
            CISNavigation.Attach(root);

            return document;
        }

        public static T Q<T>(VisualElement root, string name) where T : VisualElement {
            var element = root.Q<T>(name);
            if (element == null) {
                Debug.LogWarning($"[CISUI] Element '{name}' ({typeof(T).Name}) not found.");
            }
            return element;
        }

        public static VisualElement Element(string classes, VisualElement parent = null) {
            var element = new VisualElement();
            AddClasses(element, classes);
            element.pickingMode = PickingMode.Ignore;
            parent?.Add(element);
            return element;
        }

        public static Label Text(string text, string classes, VisualElement parent = null) {
            var label = new Label(text);
            AddClasses(label, classes);
            label.pickingMode = PickingMode.Ignore;
            parent?.Add(label);
            return label;
        }

        public static void AddClasses(VisualElement element, string classes) {
            if (string.IsNullOrEmpty(classes)) {
                return;
            }

            foreach (var c in classes.Split(' ')) {
                if (!string.IsNullOrEmpty(c)) {
                    element.AddToClassList(c);
                }
            }
        }

        public static void SetVisible(VisualElement element, bool visible) {
            if (element != null) {
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public static void SetText(Label label, string text) {
            if (label != null) {
                label.text = text;
            }
        }

        /// <summary>
        /// Runs the action on click and on gamepad/keyboard submit.
        /// </summary>
        public static void OnActivate(VisualElement element, Action action) {
            if (element == null) {
                return;
            }

            element.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation();
                PlayClickSound();
                action();
            });

            element.RegisterCallback<NavigationSubmitEvent>(evt => {
                evt.StopPropagation();
                PlayClickSound();
                action();
            });
        }

        /// <summary>
        /// Restarts a one-shot USS animation by toggling a class over two frames.
        /// </summary>
        public static void Pulse(VisualElement element, string className, long holdMs = 0) {
            if (element == null) {
                return;
            }

            element.RemoveFromClassList(className);
            element.schedule.Execute(() => {
                element.AddToClassList(className);
                if (holdMs > 0) {
                    element.schedule.Execute(() => element.RemoveFromClassList(className)).StartingIn(holdMs);
                }
            }).StartingIn(16);
        }

        public static void PlayFocusSound() {
            var now = Time.unscaledTime;
            if (now - lastFocusSoundTime < 0.06f) {
                return;
            }

            lastFocusSoundTime = now;
            EventManager.Trigger(new UIPointerEnterEvent());
        }

        public static void PlayClickSound() {
            EventManager.Trigger(new UIClickEvent());
        }

        public static string Upper(string value) {
            return string.IsNullOrEmpty(value) ? string.Empty : value.ToUpperInvariant();
        }
    }
}
