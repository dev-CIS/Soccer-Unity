using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FStudio.UIToolkit {
    /// <summary>
    /// Console-style spatial navigation for UI Toolkit.
    /// Open navigable screens form a stack; the top one owns focus, Cancel and
    /// directional input moves focus to the nearest focusable element on screen.
    /// Mouse hover moves focus too, so pad and mouse share one highlight.
    /// </summary>
    public static class CISNavigation {
        private static readonly List<CISScreen> scopes = new List<CISScreen>();
        private static readonly List<VisualElement> candidates = new List<VisualElement>();
        private static readonly Dictionary<CISScreen, VisualElement> lastFocused = new Dictionary<CISScreen, VisualElement>();

        private static bool suppressFocusSound;

        public const string SkipClass = "cis-nav-skip";

        public static CISScreen Top {
            get {
                for (int i = scopes.Count - 1; i >= 0; i--) {
                    var scope = scopes[i];
                    if (scope.Root == null || scope.Root.panel == null || !scope.IsOpen) {
                        scopes.RemoveAt(i);
                        continue;
                    }
                    return scope;
                }
                return null;
            }
        }

        public static void Attach(VisualElement documentRoot) {
            documentRoot.RegisterCallback<NavigationMoveEvent>(OnMove);
            documentRoot.RegisterCallback<NavigationCancelEvent>(OnCancel);
            documentRoot.RegisterCallback<PointerOverEvent>(OnPointerOver);
            documentRoot.RegisterCallback<FocusInEvent>(OnFocusIn);
        }

        internal static void Push(CISScreen screen) {
            scopes.Remove(screen);
            scopes.Add(screen);
        }

        internal static void Pop(CISScreen screen) {
            scopes.Remove(screen);

            // give focus back to whatever is now on top.
            var top = Top;
            if (top != null && top.Root != null) {
                top.Root.schedule.Execute(() => {
                    if (Top == top && FocusedElement(top.Root) == null) {
                        FocusScreen(top);
                    }
                }).StartingIn(32);
            }
        }

        /// <summary>
        /// Focuses the preferred element, else the screen's remembered element,
        /// its default, or its first focusable.
        /// </summary>
        public static void FocusScreen(CISScreen screen, VisualElement preferred = null) {
            if (screen == null || screen.Root == null || screen.Root.panel == null) {
                return;
            }

            VisualElement target = null;

            if (preferred != null && IsNavigable(preferred, screen.Root)) {
                target = preferred;
            } else if (lastFocused.TryGetValue(screen, out var remembered) && IsNavigable(remembered, screen.Root)) {
                target = remembered;
            } else if (screen.DefaultFocus != null && IsNavigable(screen.DefaultFocus, screen.Root)) {
                target = screen.DefaultFocus;
            } else {
                Collect(screen.Root);
                if (candidates.Count > 0) {
                    target = candidates[0];
                }
            }

            if (target != null) {
                suppressFocusSound = true;
                target.Focus();
                suppressFocusSound = false;
            }
        }

        /// <summary>
        /// Called every frame by the overlay: if a navigable screen is open but
        /// nothing has focus (e.g. after clicking empty space), restore it.
        /// </summary>
        public static void EnsureFocus() {
            var top = Top;
            if (top == null) {
                return;
            }

            if (FocusedElement(top.Root) == null) {
                FocusScreen(top);
            }
        }

        public static bool HasOpenScope => Top != null;

        private static VisualElement FocusedElement(VisualElement anyElementInPanel) {
            return anyElementInPanel?.panel?.focusController?.focusedElement as VisualElement;
        }

        private static void OnFocusIn(FocusInEvent evt) {
            var top = Top;
            var target = evt.target as VisualElement;

            if (top != null && target != null && top.Root.Contains(target)) {
                lastFocused[top] = target;
            }

            if (!suppressFocusSound) {
                CISUI.PlayFocusSound();
            }
        }

        private static void OnCancel(NavigationCancelEvent evt) {
            var top = Top;
            if (top == null) {
                return;
            }

            evt.StopPropagation();
            evt.PreventDefault();

            if (top.OnCancel != null) {
                CISUI.PlayClickSound();
                top.OnCancel.Invoke();
            }
        }

        private static void OnPointerOver(PointerOverEvent evt) {
            if (evt.pointerType == UnityEngine.UIElements.PointerType.touch) {
                return;
            }

            var top = Top;
            if (top == null) {
                return;
            }

            var element = evt.target as VisualElement;
            while (element != null && element != top.Root) {
                if (IsNavigable(element, top.Root)) {
                    if (FocusedElement(element) != element) {
                        element.Focus();
                    }
                    return;
                }
                element = element.parent;
            }
        }

        private static void OnMove(NavigationMoveEvent evt) {
            var top = Top;
            if (top == null) {
                return;
            }

            evt.StopPropagation();
            evt.PreventDefault();

            var current = FocusedElement(top.Root);

            if (current == null || !top.Root.Contains(current)) {
                FocusScreen(top);
                return;
            }

            Vector2 direction;
            switch (evt.direction) {
                case NavigationMoveEvent.Direction.Left: direction = Vector2.left; break;
                case NavigationMoveEvent.Direction.Right: direction = Vector2.right; break;
                case NavigationMoveEvent.Direction.Up: direction = Vector2.down; break; // UI space: y grows downwards.
                case NavigationMoveEvent.Direction.Down: direction = Vector2.up; break;
                default: return;
            }

            var next = FindNeighbour(top.Root, current, direction);
            next?.Focus();
        }

        private static VisualElement FindNeighbour(VisualElement scope, VisualElement current, Vector2 direction) {
            Collect(scope);

            var from = current.worldBound;
            var horizontal = Mathf.Abs(direction.x) > 0.5f;

            VisualElement best = null;
            float bestScore = float.MaxValue;

            foreach (var candidate in candidates) {
                if (candidate == current) {
                    continue;
                }

                var to = candidate.worldBound;
                if (to.width <= 0 || to.height <= 0) {
                    continue;
                }

                var delta = to.center - from.center;
                var along = Vector2.Dot(delta, direction);

                if (along <= 1f) {
                    continue;
                }

                float perpendicular;
                if (horizontal) {
                    var overlap = Mathf.Min(from.yMax, to.yMax) - Mathf.Max(from.yMin, to.yMin);
                    perpendicular = overlap > 0 ? 0 : Mathf.Abs(delta.y);
                } else {
                    var overlap = Mathf.Min(from.xMax, to.xMax) - Mathf.Max(from.xMin, to.xMin);
                    perpendicular = overlap > 0 ? 0 : Mathf.Abs(delta.x);
                }

                var score = along + perpendicular * 2.5f;

                if (score < bestScore) {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static void Collect(VisualElement scope) {
            candidates.Clear();
            scope.Query<VisualElement>().ForEach(element => {
                if (element != scope && IsNavigable(element, scope)) {
                    candidates.Add(element);
                }
            });
        }

        private static bool IsNavigable(VisualElement element, VisualElement scope) {
            if (element == null || !element.focusable || !element.enabledInHierarchy || element.panel == null) {
                return false;
            }

            if (element.ClassListContains(SkipClass)) {
                return false;
            }

            for (var e = element; e != null; e = e.parent) {
                var style = e.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden) {
                    return false;
                }

                if (e == scope) {
                    return true;
                }
            }

            // not inside the scope.
            return false;
        }
    }
}
