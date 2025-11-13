using UnityEditor;
using UnityEngine;

namespace EditorUtils.WindowControls
{
    [InitializeOnLoad]
    public static class WindowControlsCoordinator
    {
        private static bool _attempted;

        static WindowControlsCoordinator()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            var settings = EditorUISettings.Instance;
            if (settings == null) return;
            
            settings.LoadSettings();

            // Показываем элементы toolbar если включен хотя бы один из компонентов
            if (settings.showWindowControls || settings.showMenuBar || settings.enableWindowDrag)
            {
                ShowControls();
            }
        }

        public static void ShowControls()
        {
            EditorApplication.update += TryInstall;
        }

        public static void HideControls()
        {
            _attempted = false;

            var settings = EditorUISettings.Instance;
            if (settings == null) return;

            if (!settings.showWindowControls)
            {
                WindowButtonsManager.RemoveWindowControlsFromToolbar();
            }

            if (!settings.showMenuBar)
            {
                MenuBarManager.RemoveMenuBarFromToolbar();
            }

            if (!settings.enableWindowDrag)
            {
                WindowDragManager.RemoveDragAreaFromToolbar();
            }

            // Отключаем обновление только если все компоненты отключены
            if (!settings.showWindowControls && !settings.showMenuBar && !settings.enableWindowDrag)
            {
                EditorApplication.update -= TryInstall;
            }
            else
            {
                // Если хотя бы один компонент должен быть виден, перезапускаем установку
                _attempted = false;
                EditorApplication.update -= TryInstall;
                EditorApplication.update += TryInstall;
            }
        }

        // Методы для управления отдельными компонентами
        public static void ShowMenuBarButton()
        {
            var settings = EditorUISettings.Instance;
            if (settings?.showMenuBar != true) return;

            _attempted = false;
            EditorApplication.update += TryInstall;
        }

        public static void HideMenuBarButton()
        {
            MenuBarManager.RemoveMenuBarFromToolbar();
        }

        public static void ShowWindowControls()
        {
            var settings = EditorUISettings.Instance;
            if (settings?.showWindowControls != true) return;

            _attempted = false;
            EditorApplication.update += TryInstall;
        }

        public static void HideWindowControls()
        {
            WindowButtonsManager.RemoveWindowControlsFromToolbar();
        }

        public static void ShowDragArea()
        {
            var settings = EditorUISettings.Instance;
            if (settings?.enableWindowDrag != true) return;

            _attempted = false;
            EditorApplication.update += TryInstall;
        }

        public static void HideDragArea()
        {
            WindowDragManager.RemoveDragAreaFromToolbar();
        }

        private static void TryInstall()
        {
            if (_attempted) return;

            try
            {
                var settings = EditorUISettings.Instance;
                if (settings == null || (!settings.showWindowControls && !settings.showMenuBar && !settings.enableWindowDrag)) 
                {
                    EditorApplication.update -= TryInstall;
                    return;
                }

                // Устанавливаем компоненты согласно настройкам
                if (settings.showMenuBar && !MenuBarManager.IsMenuBarInstalled())
                {
                    MenuBarManager.AddMenuBarToToolbar();
                }

                if (settings.showWindowControls && !WindowButtonsManager.IsWindowControlsInstalled())
                {
                    WindowButtonsManager.AddWindowControlsToToolbar();
                }

                if (settings.enableWindowDrag && !WindowDragManager.IsDragAreaInstalled())
                {
                    WindowDragManager.AddDragAreaToToolbar();
                }

                _attempted = true;
                EditorApplication.update -= TryInstall;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"TryInstall error: {e.Message}");
                _attempted = true;
                EditorApplication.update -= TryInstall;
            }
        }

        // Проверки установки компонентов
        public static bool IsInstalled()
        {
            var settings = EditorUISettings.Instance;
            if (settings == null) return false;

            bool menuBarCheck = !settings.showMenuBar || MenuBarManager.IsMenuBarInstalled();
            bool windowControlsCheck = !settings.showWindowControls || WindowButtonsManager.IsWindowControlsInstalled();
            bool dragAreaCheck = !settings.enableWindowDrag || WindowDragManager.IsDragAreaInstalled();

            return menuBarCheck && windowControlsCheck && dragAreaCheck;
        }

        public static bool IsMenuBarInstalled()
        {
            return MenuBarManager.IsMenuBarInstalled();
        }

        public static bool IsWindowControlsInstalled()
        {
            return WindowButtonsManager.IsWindowControlsInstalled();
        }

        public static bool IsDragAreaInstalled()
        {
            return WindowDragManager.IsDragAreaInstalled();
        }
    }
}