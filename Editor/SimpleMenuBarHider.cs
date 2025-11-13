using System;
using EditorUtils.WindowControls;
using UnityEditor;
using UnityEngine;

namespace EditorUtils
{
    [InitializeOnLoad]
    public static class MenuBarHider
    {
        private static IntPtr _unityWindowHandle = IntPtr.Zero;
        private static bool _isMenuBarHidden = false;
        private static bool _shouldMonitorMenuBar = false;

        static MenuBarHider()
        {
            EditorApplication.delayCall += InitializeMenuBarHiding;
            EditorApplication.quitting += () =>
            {
                var settings = EditorUISettings.Instance;
                settings.SaveSettings();
                StopMenuBarMonitoring();
            };
        }

        private static void InitializeMenuBarHiding()
        {
            _unityWindowHandle = GetUnityMainWindow();
            var settings = EditorUISettings.Instance;
            settings.LoadSettings();
            
            if (settings.hideMenuBar) 
            {
                HideMenuBar();
                StartMenuBarMonitoring();
            }
            if (settings.hideTitleBar) HideTitleBar();
            if (settings.showWindowControls) WindowControlsCoordinator.ShowControls();
            if (settings.hideStatusBar) StatusBarHider.HideStatusBar();
        }
        
        private static void StartMenuBarMonitoring()
        {
            if (!_shouldMonitorMenuBar)
            {
                _shouldMonitorMenuBar = true;
                EditorApplication.update += MonitorMenuBarState;
            }
        }
        
        private static void StopMenuBarMonitoring()
        {
            if (_shouldMonitorMenuBar)
            {
                _shouldMonitorMenuBar = false;
                EditorApplication.update -= MonitorMenuBarState;
            }
        }
        
        private static void MonitorMenuBarState()
        {
            if (!_shouldMonitorMenuBar || !_isMenuBarHidden) return;
            
            try
            {
                if (_unityWindowHandle != IntPtr.Zero)
                {
                    var currentMenu = GetMenu(_unityWindowHandle);
                    if (currentMenu != IntPtr.Zero)
                    {
                        // Меню бар появился снова - скрываем его
                        SetMenu(_unityWindowHandle, IntPtr.Zero);
                        DrawMenuBar(_unityWindowHandle); // Принудительная перерисовка
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error during menu bar monitoring: {e.Message}");
            }
        }

        private static IntPtr _originalMenu = IntPtr.Zero;
        
        public static void HideMenuBar()
        {
            try
            {
                if (_unityWindowHandle == IntPtr.Zero)
                    _unityWindowHandle = GetUnityMainWindow();

                if (_unityWindowHandle != IntPtr.Zero)
                {
                    _originalMenu = GetMenu(_unityWindowHandle);
                    
                    if (_originalMenu != IntPtr.Zero)
                    {
                        SetMenu(_unityWindowHandle, IntPtr.Zero);
                        DrawMenuBar(_unityWindowHandle);
                        _isMenuBarHidden = true;
                        StartMenuBarMonitoring(); // Начинаем мониторинг
                    }
                    else
                    {
                        Debug.LogWarning("Menu bar is already hidden or not found");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to hide menu bar: {e.Message}");
            }
        }

        public static void ShowMenuBar()
        {
            try
            {
                StopMenuBarMonitoring(); // Останавливаем мониторинг
                
                if (_unityWindowHandle != IntPtr.Zero)
                {
                    if (_originalMenu != IntPtr.Zero)
                    {
                        SetMenu(_unityWindowHandle, _originalMenu);
                    }
                    else
                    {
                        SetWindowPos(_unityWindowHandle, IntPtr.Zero, 0, 0, 0, 0, 
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
                    }
                    
                    DrawMenuBar(_unityWindowHandle);
                    InvalidateRect(_unityWindowHandle, IntPtr.Zero, true);
                    UpdateWindow(_unityWindowHandle);
                    
                    _isMenuBarHidden = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to show menu bar: {e.Message}");
            }
        }

        public static void HideTitleBar()
        {
            try
            {
                if (_unityWindowHandle == IntPtr.Zero)
                    _unityWindowHandle = GetUnityMainWindow();

                if (_unityWindowHandle != IntPtr.Zero)
                {
                    // Убираем заголовок окна через Windows API
                    int style = GetWindowLong(_unityWindowHandle, GWL_STYLE);
                    SetWindowLong(_unityWindowHandle, GWL_STYLE, style & ~WS_CAPTION);
                    
                    // Обновляем окно
                    SetWindowPos(_unityWindowHandle, IntPtr.Zero, 0, 0, 0, 0, 
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
                    
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to hide title bar: {e.Message}");
            }
        }

        public static void ShowTitleBar()
        {
            try
            {
                if (_unityWindowHandle != IntPtr.Zero)
                {
                    // Восстанавливаем заголовок окна
                    int style = GetWindowLong(_unityWindowHandle, GWL_STYLE);
                    SetWindowLong(_unityWindowHandle, GWL_STYLE, style | WS_CAPTION);
                    
                    SetWindowPos(_unityWindowHandle, IntPtr.Zero, 0, 0, 0, 0, 
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
                    
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to show title bar: {e.Message}");
            }
        }

        public static bool IsMenuBarHidden => _isMenuBarHidden;

        private static IntPtr GetUnityMainWindow()
        {
            try
            {
                IntPtr activeWindow = GetActiveWindow();
                if (activeWindow != IntPtr.Zero)
                {
                    return activeWindow;
                }
                
                return GetForegroundWindow();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to get Unity window handle: {e.Message}");
                return IntPtr.Zero;
            }
        }

        // Windows API константы
        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        // Windows API imports
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetMenu(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetMenu(IntPtr hWnd, IntPtr hMenu);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DrawMenuBar(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
    }
}