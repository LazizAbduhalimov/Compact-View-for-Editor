using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUtils.WindowControls
{
    public static class WindowDragManager
    {
        private const string DragAreaName = "DragArea";
        private static IntPtr _unityWindowHandle = IntPtr.Zero;

        // Windows API для перетаскивания окна
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern IntPtr SendMessageForDrag(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_NCLBUTTONDOWN = 0xA1;
        private const uint HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static void AddDragAreaToToolbar()
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

                CreateDragArea(root);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to add drag area to toolbar: {e.Message}");
            }
        }

        public static void RemoveDragAreaFromToolbar()
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

                var dragArea = root.Q(DragAreaName);
                dragArea?.RemoveFromHierarchy();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to remove drag area from toolbar: {e.Message}");
            }
        }

        public static bool IsDragAreaInstalled()
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

                return root.Q(DragAreaName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static void CreateDragArea(VisualElement root)
        {
            try
            {
                if (root == null || root.Q(DragAreaName) != null) return;

                // Создаем невидимый маркер что обработчик добавлен
                var marker = new VisualElement()
                {
                    name = DragAreaName,
                    style = {
                        position = Position.Absolute,
                        width = 0,
                        height = 0,
                        opacity = 0
                    }
                };
                root.Add(marker);

                // Добавляем обработчик к root toolbar
                root.RegisterCallback<MouseDownEvent>((evt) => {
                    try
                    {
                        // Проверяем что клик не по кнопке или другому интерактивному элементу
                        var target = evt.target as VisualElement;
                        if (target != null && IsEmptyToolbarArea(target, root))
                        {
                            if (evt.button == 0) // Левая кнопка мыши
                            {
                                StartWindowDrag();
                                evt.StopPropagation();
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Drag handler error: {e.Message}");
                    }
                });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"CreateDragArea error: {e.Message}");
            }
        }

        private static bool IsEmptyToolbarArea(VisualElement target, VisualElement root)
        {
            if (target == null || root == null) return false;

            // Проверяем что это не кнопка, не поле ввода и не другой интерактивный элемент
            if (target is Button || target is TextField || target is Toggle ||
                target.ClassListContains("unity-button") ||
                target.ClassListContains("unity-toolbar-button") ||
                (!string.IsNullOrEmpty(target.name) && target.name.Contains("Button")) ||
                (!string.IsNullOrEmpty(target.name) && target.name.Contains("Field")))
            {
                return false;
            }

            // Проверяем что это root или пустая зона
            return target == root ||
                   (!string.IsNullOrEmpty(target.name) && target.name.Contains("Zone")) ||
                   (!string.IsNullOrEmpty(target.name) && target.name.Contains("Toolbar")) ||
                   string.IsNullOrEmpty(target.name);
        }

        private static void StartWindowDrag()
        {
            try
            {
                if (_unityWindowHandle == IntPtr.Zero)
                    _unityWindowHandle = GetUnityMainWindow();

                if (_unityWindowHandle != IntPtr.Zero)
                {
                    // Освобождаем захват мыши и отправляем сообщение о перетаскивании
                    ReleaseCapture();
                    SendMessageForDrag(_unityWindowHandle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to start window drag: {e.Message}");
            }
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
    }
}