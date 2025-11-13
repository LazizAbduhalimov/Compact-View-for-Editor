using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorUtils.WindowControls
{
    public static class MenuBarManager
    {
        private const string MenuBarButtonName = "MenuBarButton";
        
        // Кеш для меню элементов
        private static List<string> _cachedMenuItems = null;
        private static Dictionary<string, List<string>> _cachedGroupedMenus = null;
        private static bool _menuCacheInitialized = false;
        private static int _lastAssemblyCount = 0;

        static MenuBarManager()
        {
            // Инициализируем кеш меню в фоне
            EditorApplication.delayCall += InitializeMenuCache;
        }

        public static void AddMenuBarToToolbar()
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

                // Ищем левую зону для кнопки MenuBar
                var leftZone = root.Q("ToolbarZoneLeftAlign") ?? root.Q("ToolbarZoneLeftAlign", "ToolbarZone");
                if (leftZone == null || leftZone.Q(MenuBarButtonName) != null) return;

                var menuBarButton = CreateMenuBarButton();
                leftZone.Add(menuBarButton);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to add MenuBar to toolbar: {e.Message}");
            }
        }

        public static void RemoveMenuBarFromToolbar()
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

                var menuBarButton = root.Q(MenuBarButtonName);
                menuBarButton?.RemoveFromHierarchy();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to remove MenuBar button from toolbar: {e.Message}");
            }
        }

        public static bool IsMenuBarInstalled()
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

                return root.Q(MenuBarButtonName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static Button CreateMenuBarButton()
        {
            var menuBarButton = new Button()
            {
                text = "MenuBar ▼",
                name = MenuBarButtonName,
                tooltip = "Show main menu bar",
                style = {
                    minWidth = 75,
                    minHeight = 20,
                    maxHeight = 20,
                    marginLeft = 5,
                    marginRight = 3,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 2,
                    paddingBottom = 2,
                    fontSize = 11,
                    backgroundColor = Color.clear,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    color = EditorGUIUtility.isProSkin ? Color.white : Color.black
                }
            };

            menuBarButton.AddToClassList("unity-toolbar-button");

            var hoverColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.8f, 0.8f, 0.8f);

            menuBarButton.RegisterCallback<MouseEnterEvent>((evt) => {
                menuBarButton.style.backgroundColor = hoverColor;
            });

            menuBarButton.RegisterCallback<MouseLeaveEvent>((evt) => {
                menuBarButton.style.backgroundColor = Color.clear;
            });

            menuBarButton.RegisterCallback<ClickEvent>((evt) => {
                ShowMenuBarDropdown(menuBarButton);
            });

            return menuBarButton;
        }

        private static void ShowMenuBarDropdown(VisualElement button = null)
        {
            var menu = new GenericMenu();

            try
            {
                List<string> menuItems;
                Dictionary<string, List<string>> groupedMenus;

                var currentAssemblyCount = System.AppDomain.CurrentDomain.GetAssemblies().Length;
                bool assembliesChanged = _lastAssemblyCount != 0 && _lastAssemblyCount != currentAssemblyCount;

                if (_menuCacheInitialized && _cachedMenuItems != null && _cachedGroupedMenus != null && !assembliesChanged)
                {
                    menuItems = _cachedMenuItems;
                    groupedMenus = _cachedGroupedMenus;
                }
                else
                {
                    var startTime = System.DateTime.Now;
                    menuItems = GetAllMenuItems();
                    groupedMenus = GroupMenuItems(menuItems);

                    _cachedMenuItems = menuItems;
                    _cachedGroupedMenus = groupedMenus;
                    _menuCacheInitialized = true;
                    _lastAssemblyCount = currentAssemblyCount;
                }

                // Сортируем категории по алфавиту
                var sortedCategories = groupedMenus.OrderBy(x => x.Key).ToList();

                for (int categoryIndex = 0; categoryIndex < sortedCategories.Count; categoryIndex++)
                {
                    var category = sortedCategories[categoryIndex];

                    foreach (var menuItem in category.Value.OrderBy(x => x))
                    {
                        var displayName = menuItem;
                        
                        // Фильтруем потенциально опасные команды
                        if (IsDangerousMenuItem(menuItem))
                        {
                            // Добавляем как отключенный элемент
                            menu.AddDisabledItem(new GUIContent($"{displayName} (Disabled for safety)"));
                            continue;
                        }
                        
                        // Добавляем безопасные элементы
                        menu.AddItem(new GUIContent(displayName), false, () => {
                            try
                            {
                                EditorApplication.ExecuteMenuItem(menuItem);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"Failed to execute menu item '{menuItem}': {e.Message}");
                            }
                        });
                    }

                    if (categoryIndex < sortedCategories.Count - 1)
                    {
                        menu.AddSeparator("");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to build dynamic menu: {e.Message}");
                AddFallbackMenuItems(menu);
            }

            if (button != null)
            {
                var screenPos = button.worldBound;
                menu.DropDown(new Rect(screenPos.x, screenPos.y + screenPos.height, 0, 0));
            }
            else
            {
                menu.ShowAsContext();
            }
        }

        private static void InitializeMenuCache()
        {
            if (!_menuCacheInitialized)
            {
                try
                {
                    var startTime = System.DateTime.Now;

                    _cachedMenuItems = GetAllMenuItems();
                    _cachedGroupedMenus = GroupMenuItems(_cachedMenuItems);
                    _menuCacheInitialized = true;
                    _lastAssemblyCount = System.AppDomain.CurrentDomain.GetAssemblies().Length;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to initialize menu cache: {e.Message}");
                }
            }
        }

        private static List<string> GetAllMenuItems()
        {
            var menuItems = new List<string>();

            try
            {
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                            foreach (var method in methods)
                            {
                                var menuItems_attributes = method.GetCustomAttributes(typeof(MenuItem), false);
                                foreach (MenuItem attribute in menuItems_attributes)
                                {
                                    if (!string.IsNullOrEmpty(attribute.menuItem))
                                    {
                                        menuItems.Add(attribute.menuItem);
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        // Пропускаем проблемные сборки
                    }
                }

                AddStandardUnityMenus(menuItems);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to get menu items via reflection: {e.Message}");
            }

            return menuItems.Distinct().Where(item =>
                !string.IsNullOrEmpty(item) &&
                !item.Contains("%") &&
                !item.StartsWith("internal:", System.StringComparison.OrdinalIgnoreCase) &&
                !item.StartsWith("CONTEXT/", System.StringComparison.OrdinalIgnoreCase) &&
                !item.Contains("---") &&
                !item.Contains("_MenuItem")
            ).ToList();
        }

        private static Dictionary<string, List<string>> GroupMenuItems(List<string> menuItems)
        {
            var groupedMenus = new Dictionary<string, List<string>>();

            foreach (var item in menuItems)
            {
                var parts = item.Split('/');
                if (parts.Length > 0)
                {
                    var mainCategory = parts[0];

                    if (mainCategory.Equals("CONTEXT", System.StringComparison.OrdinalIgnoreCase) ||
                        mainCategory.Equals("Internal", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!groupedMenus.ContainsKey(mainCategory))
                        groupedMenus[mainCategory] = new List<string>();

                    groupedMenus[mainCategory].Add(item);
                }
            }

            return groupedMenus;
        }

        private static void AddStandardUnityMenus(List<string> menuItems)
        {
            var standardMenus = new string[]
            {
                "File/New Scene", "File/Open Scene", "File/Save", "File/Save As...", "File/Save Project",
                "File/Build Settings...", "File/Build And Run", "File/Exit",
                
                "Edit/Undo", "Edit/Redo", "Edit/Cut", "Edit/Copy", "Edit/Paste", "Edit/Duplicate", "Edit/Delete",
                "Edit/Frame Selected", "Edit/Lock View to Selected", "Edit/Find", "Edit/Select All",
                "Edit/Play", "Edit/Pause", "Edit/Step",
                "Edit/Project Settings...", "Edit/Preferences...",
                
                "Assets/Create/Folder", "Assets/Create/C# Script", "Assets/Create/Material", "Assets/Create/Scene",
                "Assets/Show in Explorer", "Assets/Open", "Assets/Delete", "Assets/Refresh",
                "Assets/Import New Asset...", "Assets/Export Package...",
                
                "GameObject/Create Empty", "GameObject/Create Empty Child",
                "GameObject/3D Object/Cube", "GameObject/3D Object/Sphere", "GameObject/3D Object/Capsule",
                "GameObject/Camera", "GameObject/Light/Directional Light",
                
                "Component/Physics/Rigidbody", "Component/Physics/Box Collider",
                "Component/Mesh/Mesh Renderer", "Component/Audio/Audio Source",
                
                "Window/General/Project", "Window/General/Console", "Window/General/Hierarchy",
                "Window/General/Inspector", "Window/General/Scene", "Window/General/Game"
            };

            foreach (var menu in standardMenus)
            {
                if (!menuItems.Contains(menu))
                {
                    menuItems.Add(menu);
                }
            }
        }

        private static void AddFallbackMenuItems(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("File/New Scene"), false, () => EditorApplication.ExecuteMenuItem("File/New Scene"));
            menu.AddItem(new GUIContent("File/Save"), false, () => EditorApplication.ExecuteMenuItem("File/Save"));
            menu.AddItem(new GUIContent("Edit/Undo"), false, () => EditorApplication.ExecuteMenuItem("Edit/Undo"));
            menu.AddItem(new GUIContent("Edit/Redo"), false, () => EditorApplication.ExecuteMenuItem("Edit/Redo"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Window/General/Console"), false, () => EditorApplication.ExecuteMenuItem("Window/General/Console"));
            menu.AddItem(new GUIContent("Window/General/Inspector"), false, () => EditorApplication.ExecuteMenuItem("Window/General/Inspector"));
        }

        private static bool IsDangerousMenuItem(string menuItem)
        {
            if (string.IsNullOrEmpty(menuItem)) return false;
            
            // Список потенциально опасных команд
            var dangerousCommands = new string[]
            {
                "File/Exit",
                "File/Quit",
                "Edit/Play Mode/Exit Play Mode",
                "Window/General/Console/Clear",
                "Assets/Refresh", // Может вызвать длительную операцию
                "Assets/Reimport All", // Очень длительная операция
            };

            return dangerousCommands.Any(dangerous => 
                menuItem.Equals(dangerous, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}