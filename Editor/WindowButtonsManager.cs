using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUtils.WindowControls
{
    public static class WindowButtonsManager
    {
        private const string ContainerName = "WindowControlsContainer";
        private static IntPtr _unityWindowHandle = IntPtr.Zero;

        // Windows API
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        private const int SW_MINIMIZE = 6;
        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;
        private const uint WM_CLOSE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        public static void AddWindowControlsToToolbar()
        {
            try
            {
                var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
                if (toolbarType == null) return;

                var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
                if (toolbars == null || toolbars.Length == 0) return;

                var toolbar = toolbars[0];
                var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                var root = rootField?.GetValue(toolbar) as VisualElement;
                if (root == null) return;

                // Ищем правую зону для кнопок управления окном
                var rightZone = FindRightZone(root);
                if (rightZone == null) return;

                // Избегаем дубликатов
                if (rightZone.Q(ContainerName) != null) return;

                var container = new VisualElement()
                {
                    name = ContainerName,
                    style = {
                        flexDirection = FlexDirection.Row,
                        marginLeft = 3,
                        marginRight = 2
                    }
                };

                // Добавляем вертикальную разделительную линию
                var separator = new VisualElement()
                {
                    style = {
                        width = 1,
                        backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.3f),
                        marginLeft = 4,
                        marginRight = 4,
                        marginTop = 2,
                        marginBottom = 2
                    }
                };
                container.Add(separator);

                // Кнопка минимизации (желтый/оранжевый круг)
                var minimizeButton = CreateCircleButton(new Color(1.0f, 0.8f, 0.0f), "Minimize window", MinimizeWindow);
                container.Add(minimizeButton);

                // Кнопка максимизации/восстановления (зеленый круг)
                var maximizeButton = CreateCircleButton(new Color(0.0f, 0.8f, 0.0f), "Maximize/Restore window", ToggleMaximizeWindow);
                container.Add(maximizeButton);

                // Кнопка закрытия (красный круг)
                var closeButton = CreateCircleButton(new Color(1.0f, 0.3f, 0.3f), "Close window", CloseWindow);
                container.Add(closeButton);

                rightZone.Insert(0, container);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to add window controls to toolbar: {e.Message}");
            }
        }

        public static void RemoveWindowControlsFromToolbar()
        {
            try
            {
                var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
                if (toolbarType == null) return;

                var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
                if (toolbars == null || toolbars.Length == 0) return;

                var toolbar = toolbars[0];
                var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                var root = rootField?.GetValue(toolbar) as VisualElement;
                if (root == null) return;

                var container = root.Q(ContainerName);
                container?.RemoveFromHierarchy();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to remove window controls from toolbar: {e.Message}");
            }
        }

        public static bool IsWindowControlsInstalled()
        {
            try
            {
                var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
                if (toolbarType == null) return false;

                var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
                if (toolbars == null || toolbars.Length == 0) return false;

                var toolbar = toolbars[0];
                var rootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                var root = rootField?.GetValue(toolbar) as VisualElement;
                if (root == null) return false;

                return root.Q(ContainerName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static VisualElement FindRightZone(VisualElement root)
        {
            var rightZone = root.Q("ToolbarZoneRightAlign") ?? root.Q("ToolbarZoneRightAlign", "ToolbarZone");
            if (rightZone != null) return rightZone;

            // Ищем конкретные элементы для позиционирования
            var layoutButton = root.Q("Layout");
            var cloudButton = root.Q("CloudBuild");
            var accountButton = root.Q("Account");

            if (layoutButton != null) return layoutButton.parent;
            if (cloudButton != null) return cloudButton.parent;
            if (accountButton != null) return accountButton.parent;

            return root.Q("ToolbarZoneLeftAlign") ?? root.Q("ToolbarZoneLeftAlign", "ToolbarZone");
        }

        private static VisualElement CreateCircleButton(Color circleColor, string tooltip, System.Action action)
        {
            var button = new VisualElement()
            {
                tooltip = tooltip,
                style = {
                    width = 18,
                    height = 18,
                    marginLeft = 2,
                    marginRight = 2,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopLeftRadius = 9,
                    borderTopRightRadius = 9,
                    borderBottomLeftRadius = 9,
                    borderBottomRightRadius = 9
                }
            };

            // Создаем круглую текстуру программно с anti-aliasing
            var texture = CreateSmoothCircleTexture(32, circleColor); // Увеличиваем размер для лучшего качества
            button.style.backgroundImage = Background.FromTexture2D(texture);

            // Добавляем обработчики событий мыши для кликабельности
            button.RegisterCallback<MouseDownEvent>((evt) => {
                if (evt.button == 0) // Левая кнопка мыши
                {
                    action?.Invoke();
                    evt.StopPropagation();
                }
                button.style.opacity = 0.6f;
            });

            button.RegisterCallback<MouseUpEvent>((evt) => {
                button.style.opacity = 0.8f;
            });

            button.RegisterCallback<MouseEnterEvent>((evt) => {
                button.style.opacity = 0.8f;
            });

            button.RegisterCallback<MouseLeaveEvent>((evt) => {
                button.style.opacity = 1.0f;
            });

            return button;
        }

        private static Texture2D CreateSmoothCircleTexture(int size, Color color)
        {
            var texture = new Texture2D(size, size);
            var center = size * 0.5f;
            var radius = center - 2; // Немного отступ от края

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    
                    if (distance <= radius)
                    {
                        // Внутри круга - полный цвет
                        texture.SetPixel(x, y, color);
                    }
                    else if (distance <= radius + 1)
                    {
                        // Край с anti-aliasing
                        var alpha = 1.0f - (distance - radius);
                        var edgeColor = new Color(color.r, color.g, color.b, color.a * alpha);
                        texture.SetPixel(x, y, edgeColor);
                    }
                    else
                    {
                        // Вне круга - прозрачный
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return texture;
        }
        private static IntPtr GetUnityMainWindow()
        {
            try
            {
                var currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
                var foregroundWindow = GetForegroundWindow();

                GetWindowThreadProcessId(foregroundWindow, out uint windowProcessId);

                if (windowProcessId == currentProcessId)
                {
                    return foregroundWindow;
                }

                var unityProcesses = System.Diagnostics.Process.GetProcessesByName("Unity");
                foreach (var process in unityProcesses)
                {
                    if (process.Id == currentProcessId)
                    {
                        return process.MainWindowHandle;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to get Unity main window: {e.Message}");
            }

            return IntPtr.Zero;
        }

        private static void MinimizeWindow()
        {
            try
            {
                if (_unityWindowHandle == IntPtr.Zero)
                    _unityWindowHandle = GetUnityMainWindow();

                if (_unityWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(_unityWindowHandle, SW_MINIMIZE);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to minimize window: {e.Message}");
            }
        }

        private static void ToggleMaximizeWindow()
        {
            try
            {
                if (_unityWindowHandle == IntPtr.Zero)
                    _unityWindowHandle = GetUnityMainWindow();

                if (_unityWindowHandle != IntPtr.Zero)
                {
                    if (IsWindowMaximized())
                    {
                        ShowWindow(_unityWindowHandle, SW_RESTORE);
                    }
                    else
                    {
                        ShowWindow(_unityWindowHandle, SW_MAXIMIZE);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to toggle maximize window: {e.Message}");
            }
        }

        private static void CloseWindow()
        {
            try
            {
                // Показываем диалог подтверждения
                bool confirmClose = EditorUtility.DisplayDialog(
                    "Закрыть Unity Editor",
                    "Вы действительно хотите закрыть Unity Editor?\n\nУбедитесь, что все изменения сохранены.",
                    "Да, закрыть",
                    "Отмена"
                );

                // Если пользователь подтвердил закрытие
                if (confirmClose)
                {
                    if (_unityWindowHandle == IntPtr.Zero)
                        _unityWindowHandle = GetUnityMainWindow();

                    if (_unityWindowHandle != IntPtr.Zero)
                    {
                        SendMessage(_unityWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to close window: {e.Message}");
            }
        }

        private static bool IsWindowMaximized()
        {
            try
            {
                if (_unityWindowHandle == IntPtr.Zero)
                    _unityWindowHandle = GetUnityMainWindow();

                if (_unityWindowHandle != IntPtr.Zero)
                {
                    var placement = new WINDOWPLACEMENT();
                    placement.length = Marshal.SizeOf(placement);

                    if (GetWindowPlacement(_unityWindowHandle, ref placement))
                    {
                        return placement.showCmd == SW_MAXIMIZE;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to check window state: {e.Message}");
            }

            return false;
        }
    }
}