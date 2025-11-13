using UnityEditor;
using UnityEngine;

namespace EditorUtils
{
    public class EditorUISettings : ScriptableObject
    {
        [Header("Window Appearance")]
        [Tooltip("Hide Unity window title bar to save vertical space")]
        public bool hideTitleBar = false;
        
        [Tooltip("Hide Unity menu bar (File, Edit, Assets, etc.)")]
        public bool hideMenuBar = false;
        
        [Header("Custom Controls")]
        [Tooltip("Show window control buttons (minimize, maximize, close) in toolbar")]
        public bool showWindowControls = false;
        
        [Tooltip("Show MenuBar button in toolbar to access all Unity menus")]
        public bool showMenuBar = true;
        
        [Tooltip("Enable dragging Unity window by clicking and dragging toolbar area")]
        public bool enableWindowDrag = false;
        
        [Header("Status Bar")]
        [Tooltip("Hide Unity status bar at the bottom")]
        public bool hideStatusBar = false;

        public static EditorUISettings Instance => _instance = _instance != null ? _instance : CreateOrLoadSettings();
        private static EditorUISettings _instance;

        private string HideTitleBarEditorPrefsKey => "EditorUISettings_HideTitleBar";
        private string HideMenuBarEditorPrefsKey => "EditorUISettings_HideMenuBar";
        private string ShowWindowControlsEditorPrefsKey => "EditorUISettings_ShowWindowControls";
        private string ShowMenuBarButtonEditorPrefsKey => "EditorUISettings_ShowMenuBarButton";
        private string HideStatusBarEditorPrefsKey => "EditorUISettings_HideStatusBar";
        private string EnableWindowDragEditorPrefsKey => "EditorUISettings_EnableWindowDrag";

        public void SaveSettings()
        {
            EditorPrefs.SetBool(HideTitleBarEditorPrefsKey, hideTitleBar);
            EditorPrefs.SetBool(HideMenuBarEditorPrefsKey, hideMenuBar);
            EditorPrefs.SetBool(ShowWindowControlsEditorPrefsKey, showWindowControls);
            EditorPrefs.SetBool(ShowMenuBarButtonEditorPrefsKey, showMenuBar);
            EditorPrefs.SetBool(HideStatusBarEditorPrefsKey, hideStatusBar);
            EditorPrefs.SetBool(EnableWindowDragEditorPrefsKey, enableWindowDrag);
        }

        public void LoadSettings()
        {
            hideTitleBar = EditorPrefs.GetBool(HideTitleBarEditorPrefsKey, false);
            hideMenuBar = EditorPrefs.GetBool(HideMenuBarEditorPrefsKey, false);
            showWindowControls = EditorPrefs.GetBool(ShowWindowControlsEditorPrefsKey, false);
            showMenuBar = EditorPrefs.GetBool(ShowMenuBarButtonEditorPrefsKey, true);
            hideStatusBar = EditorPrefs.GetBool(HideStatusBarEditorPrefsKey, false);
            enableWindowDrag = EditorPrefs.GetBool(EnableWindowDragEditorPrefsKey, true);
        }

        private static EditorUISettings CreateOrLoadSettings()
        {
            // Сначала ищем существующие настройки во всем проекте
            string[] assets = AssetDatabase.FindAssets("t:EditorUISettings");
            if (assets.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(assets[0]);
                var existingSettings = AssetDatabase.LoadAssetAtPath<EditorUISettings>(path);
                if (existingSettings != null)
                {
                    existingSettings.LoadSettings();
                    return existingSettings;
                }
            }

            // Создаем новые настройки в подходящем месте
            var settings = CreateInstance<EditorUISettings>();
            settings.LoadSettings(); // Загружаем из EditorPrefs при первом создании
            
            // Создаем в специальной папке пакета
            string targetPath = "Assets/CompactEditorView/EditorUISettings.asset";

            try
            {
                // Создаем папку если её нет
                string directory = System.IO.Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
                {
                    AssetDatabase.CreateFolder("Assets", "CompactEditorView");
                }

                AssetDatabase.CreateAsset(settings, targetPath);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"EditorUISettings created at: {targetPath}");
                return settings;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not create EditorUISettings at {targetPath}: {e.Message}");
                
                // Если не удалось создать файл настроек, работаем только с EditorPrefs
                Debug.LogWarning("Could not create EditorUISettings asset file. Using EditorPrefs only.");
                return settings;
            }
        }
    }
}