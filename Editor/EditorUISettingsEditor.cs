using UnityEditor;
using UnityEngine;

namespace EditorUtils
{
    [CustomEditor(typeof(EditorUISettings))]
    public class EditorUISettingsEditor : Editor
    {
        [MenuItem("Tools/Editor UI Settings")]
        public static void OpenOrCreateSettings()
        {
            // Сначала ищем существующие настройки
            string[] assets = AssetDatabase.FindAssets("t:EditorUISettings");
            if (assets.Length > 0)
            {
                // Настройки найдены - показываем их
                string path = AssetDatabase.GUIDToAssetPath(assets[0]);
                var settings = AssetDatabase.LoadAssetAtPath<EditorUISettings>(path);
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
                return;
            }

            // Настройки не найдены - создаем новые
            var newSettings = ScriptableObject.CreateInstance<EditorUISettings>();
            newSettings.LoadSettings(); // Загружаем из EditorPrefs

            // Создаем в специальной папке пакета
            string targetPath = "Assets/CompactEditorView/EditorUISettings.asset";

            try
            {
                // Создаем папку если её нет
                if (!AssetDatabase.IsValidFolder("Assets/CompactEditorView"))
                {
                    AssetDatabase.CreateFolder("Assets", "CompactEditorView");
                }

                AssetDatabase.CreateAsset(newSettings, targetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Selection.activeObject = newSettings;
                EditorGUIUtility.PingObject(newSettings);
                
                Debug.Log($"EditorUISettings created at: {targetPath}");
                return;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not create EditorUISettings at {targetPath}: {e.Message}");
                
                // Если не удалось создать файл, просто показываем объект в памяти
                Selection.activeObject = newSettings;
                Debug.LogWarning("Could not create EditorUISettings asset file. Object created in memory only.");
            }
        }

        public override void OnInspectorGUI()
        {
            var settings = target as EditorUISettings;
            
            bool prevHideTitleBar = settings.hideTitleBar;
            bool prevHideMenuBar = settings.hideMenuBar;
            bool prevShowWindowControls = settings.showWindowControls;
            bool prevShowMenuBar = settings.showMenuBar;
            bool prevHideStatusBar = settings.hideStatusBar;
            bool prevEnableWindowDrag = settings.enableWindowDrag;

            DrawDefaultInspector();
            
            if (prevHideTitleBar != settings.hideTitleBar)
            {
                settings.SaveSettings();
                if (settings.hideTitleBar)
                {
                    MenuBarHider.HideTitleBar();
                }
                else
                {
                    MenuBarHider.ShowTitleBar();
                }
            }
            
            if (prevHideMenuBar != settings.hideMenuBar)
            {
                settings.SaveSettings();
                if (settings.hideMenuBar)
                {
                    MenuBarHider.HideMenuBar();
                }
                else
                {
                    MenuBarHider.ShowMenuBar();
                }
            }
            
            if (prevShowWindowControls != settings.showWindowControls)
            {
                settings.SaveSettings();
                if (settings.showWindowControls)
                {
                    EditorUtils.WindowControls.WindowControlsCoordinator.ShowWindowControls();
                }
                else
                {
                    EditorUtils.WindowControls.WindowControlsCoordinator.HideWindowControls();
                }
            }
            
            if (prevShowMenuBar != settings.showMenuBar)
            {
                settings.SaveSettings();
                if (settings.showMenuBar)
                {
                    EditorUtils.WindowControls.WindowControlsCoordinator.ShowMenuBarButton();
                }
                else
                {
                    EditorUtils.WindowControls.WindowControlsCoordinator.HideMenuBarButton();
                }
            }
            
            if (prevHideStatusBar != settings.hideStatusBar)
            {
                settings.SaveSettings();
                if (settings.hideStatusBar)
                {
                    StatusBarHider.HideStatusBar();
                }
                else
                {
                    StatusBarHider.ShowStatusBar();
                }
                if (settings.hideTitleBar)
                {
                    MenuBarHider.ShowTitleBar();
                    MenuBarHider.HideTitleBar();
                }
                else
                {
                    MenuBarHider.HideTitleBar();
                    MenuBarHider.ShowTitleBar();
                }   
            }
            
            if (prevEnableWindowDrag != settings.enableWindowDrag)
            {
                settings.SaveSettings();
                if (settings.enableWindowDrag)
                {
                    EditorUtils.WindowControls.WindowControlsCoordinator.ShowDragArea();
                }
                else
                {
                    EditorUtils.WindowControls.WindowControlsCoordinator.HideDragArea();
                }
            }
        }
    }
}