using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEditor.Progress;

namespace Tomatech.RePalette.Editor
{
    public class RePaletteEditorWindow : EditorWindow
    {
        static RePaletteDatabase Database => RePaletteDatabase.Database;
        [SerializeField]
        VisualTreeAsset m_UXML;
        [SerializeField]
        Texture2D windowIcon;
        [SerializeField]
        Texture2D okTexture;
        [SerializeField]
        Texture2D warningTexture;

        [MenuItem("Window/Tomatech/RePalette Theme Manager")]
        static void CreateMenu()
        {
            var window = GetWindow<RePaletteEditorWindow>();
            window.titleContent = new GUIContent("RePalette");
        }

        private void Awake()
        {
            titleContent.image = windowIcon;
        }

        private void OnEnable()
        {
            titleContent.image = windowIcon;
        }

        string selectedPath = "";
        string FollowupSelectedPath => selectedPath == "" ? selectedPath : selectedPath + "/";
        bool IsGroup(string item) => !RePaletteDatabase.Database.themeAssets.Exists(e => e.path == FollowupSelectedPath + item);
        List<string> GetGroupChildren() => RePaletteDatabase.Database.themeAssets
                    .Select(c => c.path)
                    .Union(RePaletteDatabase.Database.emptyCategoryGroups)
                    .Where(p => selectedPath == "" ? true : p.StartsWith(selectedPath + "/"))
                    .Select(p => p.Split("/")[selectedPath.Split("/").Length - (selectedPath == "" ? 1 : 0)])
                    .GroupBy(x => x)
                    .Select(x => x.Key)
                    .ToList();


        string TryIncrementName(string desiredName, List<string> siblingNames)
        {
            if (!siblingNames.Contains(desiredName))
                return desiredName;
            string[] splitName = desiredName.Split(' ');
            bool hasSourceNumber = int.TryParse(splitName[^1], out int sourceNumber);
            if (hasSourceNumber)
                desiredName = string.Join(" ", splitName[^2]);
            else
                sourceNumber = 1;
            while (siblingNames.Contains(desiredName + " " + sourceNumber))
            {
                sourceNumber++;
            }
            return desiredName + " " + sourceNumber;
        }

        void MassRenamePath(string oldPath, string newPath)
        {
            RePaletteDatabase.Database.themeAssets
                .Where(g => g.path.StartsWith(oldPath))
                .ToList()
                .ForEach(g => g.path = newPath + g.path[(oldPath.Length)..]);
            var toReplace = RePaletteDatabase.Database.emptyCategoryGroups
                .Where(g => g.StartsWith(oldPath))
                .ToList()
                .Select(g => newPath + g[(oldPath.Length)..]);
            RePaletteDatabase.Database.emptyCategoryGroups.RemoveAll(g => g.StartsWith(oldPath));
            RePaletteDatabase.Database.emptyCategoryGroups.AddRange(toReplace);
        }
        struct OrderedMenuBuilder
        {
            public int Priority { get; private set; }
            public EventCallback<ContextualMenuPopulateEvent> MenuBuilder { get; private set; }
            public OrderedMenuBuilder(EventCallback<ContextualMenuPopulateEvent> menuBuilder)
            {
                Priority = 0;
                MenuBuilder = menuBuilder;
            }
            public OrderedMenuBuilder(EventCallback<ContextualMenuPopulateEvent> menuBuilder, int priority)
            {
                Priority = priority;
                MenuBuilder = menuBuilder;
            }
        }
        List<OrderedMenuBuilder> contextMenuBuffer = new();
        void AppendCurrentContextMenu(VisualElement element, params OrderedMenuBuilder[] orderedMenuBuilders)
        {
            element.AddManipulator(new ContextualMenuManipulator(e =>
            {
                if (e.target != e.currentTarget)
                    return;
                var orderedMenuBuffer = contextMenuBuffer.OrderBy(orderdEvt => orderdEvt.Priority).Select(orderdEvt => orderdEvt.MenuBuilder).ToList();
                orderedMenuBuffer.ForEach(eventBuilder =>
                {
                    eventBuilder(e);
                });
                contextMenuBuffer.Clear();
                e.StopImmediatePropagation();
            }));
            foreach (var orderedMenuBuilder in orderedMenuBuilders)
            {
                if (orderedMenuBuilder.MenuBuilder is not null)
                    element.RegisterCallback<ContextualMenuPopulateEvent>(_ =>
                    contextMenuBuffer.Add(orderedMenuBuilder), TrickleDown.TrickleDown
                    );
            }
        }

        internal void CreateGUIInternal() => CreateGUI();

        private void CreateGUI()
        {
            titleContent.image = windowIcon;
            contextMenuBuffer = new();
            minSize = new Vector2(550, 150);
            rootVisualElement.Clear();
            m_UXML.CloneTree(rootVisualElement);
            rootVisualElement.Query("Dummy").ForEach(e => e.RemoveFromHierarchy());
            rootVisualElement.Q<Button>("DebugReload").clicked += () => {
                RePaletteDatabase.ResetDatabaseCheck();
                CreateGUI();
            };
            
            // if the database is missing, show prompt in window
            if (!Database)
            {
                rootVisualElement.Q("BrowserRoot").style.display = DisplayStyle.None;
                rootVisualElement.Q("NoDatabasePanel").style.display = DisplayStyle.Flex;
                rootVisualElement.Q<Button>("NoDatabaseButton").clicked += () =>
                {
                    //EditorUtility.SetDirty(this);
                    //TODO: Database subclass selector
                    var newDatabase = CreateInstance<RePaletteDatabase>();
                    AssetDatabase.CreateAsset(newDatabase, "Assets/Settings/RePalette Database.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    RePaletteDatabase.ResetDatabaseCheck();
                    CreateGUI();
                };
            }
            else
            {
                var mainToolbar = rootVisualElement.Q<Toolbar>("MainToolbar");
                if (Database.ThemeFilter)
                {
                    var filterProp = new PropertyField();
                    filterProp.BindProperty(new SerializedObject(Database.ThemeFilter).FindProperty(Database.ThemeFilter.EditorWindowFilterName));
                    filterProp.RegisterValueChangeCallback(e => {
                        PopulateAssetPanel();
                        PopulateSubAssetPanel();
                    });
                    filterProp.AddToClassList("theme-manager-filter");
                    filterProp.style.minWidth = 100;
                    filterProp.style.maxWidth = 400;
                    filterProp.style.flexGrow = 0.5f;
                    mainToolbar.Insert(0, filterProp);
                }
                else
                {
                    var createFilterButton = new Button() { text= "Create Filter" };
                    createFilterButton.clicked += () =>
                    {
                        var validFilterTypes = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .Where(t=>t.IsSubclassOf(typeof(ThemeFilterContainerBase)) && t.Name!= "ThemeFilterContainer`1")
                            .ToList();
                        EditorUtility.DisplayCustomMenu(createFilterButton.worldBound, validFilterTypes.Select(t => new GUIContent(t.Name)).ToArray(), -1, (_,__,i) =>
                        {
                            var newFilter = CreateInstance(validFilterTypes[i]) as ThemeFilterContainerBase;
                            newFilter.name = "RePalette Database Filter";
                            Database.ThemeFilter = newFilter;
                            AssetDatabase.AddObjectToAsset(newFilter, Database);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            CreateGUI();
                        }, null);
                    };
                    mainToolbar.Insert(0, createFilterButton);
                }

                #region Create Category Panel
                var categoryPanel = rootVisualElement.Q("CategoryPanel");
                var categoryList = rootVisualElement.Q<ListView>("CategoryList");

                void CategoryCreationMenuBuilder(DropdownMenu menu)
                {
                    menu.AppendAction("Create Category", a =>
                    {
                        Database.emptyCategoryGroups.RemoveAll(g => selectedPath.StartsWith(g));
                        Database.themeAssets.Add(new ThemeAssetCategory(FollowupSelectedPath + TryIncrementName("New Category", GetGroupChildren())));
                        CreateGUI();
                    });
                    menu.AppendAction("Create Group", a =>
                    {
                        Database.emptyCategoryGroups.RemoveAll(g => selectedPath.StartsWith(g));
                        Database.emptyCategoryGroups.Add(FollowupSelectedPath + "New Group");
                        CreateGUI();
                    });
                }

                categoryList.makeItem = () =>
                {
                    var itemRoot = new VisualElement();
                    var itemLabel = new Label();
                    itemRoot.Add(itemLabel);
                    itemRoot.AddToClassList("unity-button");
                    itemLabel.AddToClassList("theme-manager-label");
                    itemLabel.pickingMode = PickingMode.Ignore;
                    AppendCurrentContextMenu(itemRoot, new OrderedMenuBuilder(e =>
                    {
                        e.menu.AppendSeparator();
                        e.menu.AppendAction("Rename " + (IsGroup(itemLabel.text) ? "Group" : "Category"), a =>
                        {
                            Rect rootRect = itemRoot.worldBound;
                            rootRect.y -= rootRect.height;
                            UnityEditor.PopupWindow.Show(rootRect, new RePaletteRenamePopupWindow(r =>
                            {
                                var siblings = GetGroupChildren();
                                siblings.Remove(itemLabel.text);
                                var finalRename = TryIncrementName(r, siblings);
                                MassRenamePath(FollowupSelectedPath + itemLabel.text, FollowupSelectedPath + finalRename);
                                PopulateCategoryPanel();
                            }, itemLabel.text, rootRect.width));
                        });
                        e.menu.AppendAction("Delete " + (IsGroup(itemLabel.text) ? "Group" : "Category") + "/Confirm Deletion?", e =>
                        {
                            if (!IsGroup(itemLabel.text) || EditorUtility.DisplayDialog(
                                "Warning",
                                $"This will delete a total of {RePaletteDatabase.Database.themeAssets.Where(c => c.path.StartsWith(FollowupSelectedPath + itemLabel.text)).Count()} categories. Do you wish to continue?", "Yes", "NO, $H!7, GO BACK"))
                            {
                                Database.themeAssets.RemoveAll(c => c.path.StartsWith(FollowupSelectedPath + itemLabel.text));
                                Database.emptyCategoryGroups.RemoveAll(c => c.StartsWith(FollowupSelectedPath + itemLabel.text));
                                PopulateCategoryPanel();
                            }
                        });
                    }));
                    return itemRoot;
                };

                categoryList.bindItem = (e, i) =>
                {
                    var list = categoryList.itemsSource as IList<string>;
                    e.ClearClassList();
                    e.AddToClassList("unity-button");
                    if (IsGroup(list[i]))
                    {
                        e.AddToClassList("theme-manager-group-button");
                    }
                    else
                    {
                        e.AddToClassList("theme-manager-category-button");
                    }
                    e.Q<Label>().text = list[i];
                };

                categoryPanel.Q<ToolbarButton>("CategoryBackButton").clicked += () =>
                {
                    selectedPath = string.Join('/', selectedPath.Split('/')[..^1]);
                    PopulateCategoryPanel();
                };
                CategoryCreationMenuBuilder(categoryPanel.Q<ToolbarMenu>("CreateCategoryButton").menu);

                var catListPanel = categoryPanel.Q("CategoryListPanel");
                AppendCurrentContextMenu(catListPanel, new OrderedMenuBuilder(e => CategoryCreationMenuBuilder(e.menu)));

                categoryList.selectionChanged += l =>
                {
                    Database.WindowSelectedEntry = null;
                    Database.WindowSelectedSubAssets = null;
                    PopulateSubAssetPanel();
                    if (categoryList.selectedIndex == -1)
                    {
                        Database.WindowSelectedCategory = null;
                        PopulateAssetPanel();
                        return;
                    }

                    var list = categoryList.itemsSource as IList<string>;
                    var indexedPath = list[categoryList.selectedIndex];
                    if (IsGroup(indexedPath))
                    {
                        selectedPath = FollowupSelectedPath + indexedPath;
                        PopulateCategoryPanel();
                    }
                    else
                    {
                        Database.WindowSelectedCategory = Database.themeAssets.FirstOrDefault(c => c.path == FollowupSelectedPath + indexedPath);
                    }
                    PopulateAssetPanel();
                };

                #endregion

                #region Create Asset Panel

                var assetPanel = rootVisualElement.Q<VisualElement>("AssetPanel");
                assetPanel.Q("AssetList").name = "AssetList_Old";
                var assetListColumns = new Columns
                {
                    new() { name = "NameCol", title="Name", stretchable=true},
                    new() { name = "ConstraintCol", title="Constrained Type", stretchable=true},
                    new() { name = "ObjectCol", title="Object", stretchable=true},
                    new() { name = "ResolvedObjectCol", title="Resolved Object", stretchable=true},
                    new() { name = "KeyCol", title="Key", stretchable=true},
                };
                var assetList = new MultiColumnListView(assetListColumns)
                {
                    name = "AssetList",
                    showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                    style = {
                        flexGrow=1,
                        flexShrink=1,
                    }
                };
                assetPanel.Add(assetList);
                assetList.Q("KeyCol").style.borderRightWidth = 0;
                assetListColumns["NameCol"].optional = false;

                Action createAssetSlotAction = () =>
                {
                    (assetList.itemsSource as IList<ThemeAssetEntry>).Add(new ThemeAssetEntry() { humanKey = "Bruh Sequel", addressableKey = "69420" });
                    PopulateAssetPanel();
                };

                assetListColumns["NameCol"].makeCell = () =>
                {
                    var nameLabel = new Label() { style = { flexGrow = 1, paddingLeft = 5 } };
                    nameLabel.AddToClassList("theme-manager-label");
                    AppendCurrentContextMenu(nameLabel, new OrderedMenuBuilder(e =>
                    {
                        e.menu.AppendSeparator();
                        e.menu.AppendAction("Rename Asset", a =>
                        {
                            Rect rootRect = nameLabel.worldBound;
                            rootRect.y -= rootRect.height;
                            UnityEditor.PopupWindow.Show(rootRect, new RePaletteRenamePopupWindow(r =>
                            {
                                var siblings = Database.WindowSelectedCategory.entries.Select(e => e.humanKey).ToList();
                                siblings.Remove(nameLabel.text);
                                (nameLabel.parent.parent.userData as ThemeAssetEntry).humanKey = TryIncrementName(r, siblings);
                                assetList.RefreshItems();
                            }, nameLabel.text, rootRect.width));
                        });
                    }, 5));
                    return nameLabel;
                };
                assetListColumns["ConstraintCol"].makeCell = () =>
                {
                    var constraintField = new PopupField<Type>();
                    constraintField.choices = Database.ValidConstraints;
                    AppendCurrentContextMenu(constraintField);
                    return constraintField;
                };
                assetListColumns["ObjectCol"].makeCell = () =>
                {
                    var objectField = new ObjectField();
                    AppendCurrentContextMenu(objectField);
                    objectField.RegisterValueChangedCallback( evt =>
                    {
                        if (!Database.ThemeFilter)
                            return;
                        Debug.Log(evt.previousValue + "  ->  " + evt.newValue);
                        var settings = AddressableAssetSettingsDefaultObject.Settings;
                        var themeKey = Database.ThemeFilter.EditorWindowFilter.ThemeKey;

                        if (!settings.GetLabels().Contains(themeKey))
                            settings.AddLabel(themeKey, false);

                        AddressableAssetGroup g = settings.FindGroup("RePalette Assets");

                        if (evt.previousValue)
                        {
                            var oldGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(evt.previousValue));
                            var oldEntry = g.entries.ToList().FirstOrDefault(e => e.guid == oldGUID);
                            if (oldEntry != null)
                            {
                                settings.SetDirty(
                                    AddressableAssetSettings.ModificationEvent.EntryRemoved,
                                    settings.FindAssetEntry(oldGUID),
                                    true);
                                settings.RemoveAssetEntry(oldGUID, false);
                            }
                        }

                        if (evt.newValue)
                        {
                            var newGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(evt.newValue));
                            var newEntry = g.entries.ToList().FirstOrDefault(e => e.guid == newGUID);
                            bool newExists = newEntry != null;
                            newEntry = settings.CreateOrMoveEntry(newGUID, g);
                            newEntry.labels.Add(Database.ThemeFilter.EditorWindowFilter.ThemeKey);
                            newEntry.address = objectField.userData as string;
                            settings.SetDirty(
                                newExists ?
                                    AddressableAssetSettings.ModificationEvent.EntryMoved :
                                    AddressableAssetSettings.ModificationEvent.EntryCreated,
                                newEntry,
                                true);
                        }

                        AssetDatabase.SaveAssets();
                        assetList.RefreshItems();
                    });
                    return objectField;
                };
                assetListColumns["ResolvedObjectCol"].makeCell = () =>
                {
                    var objectField = new ObjectField();
                    AppendCurrentContextMenu(objectField);
                    objectField.SetEnabled(false);
                    return objectField;
                };
                assetListColumns["KeyCol"].makeCell = () =>
                {
                    var keyLabel = new Label() { style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.BoldAndItalic, paddingLeft = 5 } };
                    keyLabel.AddToClassList("theme-manager-label");
                    AppendCurrentContextMenu(keyLabel, new OrderedMenuBuilder(e =>
                    {
                        e.menu.AppendSeparator();
                        e.menu.AppendAction("Rename Key", a =>
                        {
                            Rect rootRect = keyLabel.worldBound;
                            rootRect.y -= rootRect.height;
                            UnityEditor.PopupWindow.Show(rootRect, new RePaletteRenamePopupWindow(r =>
                            {
                                var siblings = Database.WindowSelectedCategory.entries.Select(e => e.addressableKey).ToList();
                                if (!siblings.Contains(r) && EditorUtility.DisplayDialog(
                                "Warning",
                                "All references to this asset slot will be lost. Proceed?", "Yes", "NO, $H!7, GO BACK"))
                                {
                                    (keyLabel.parent.parent.userData as ThemeAssetEntry).addressableKey = r;
                                    assetList.RefreshItems();
                                }
                            }, keyLabel.text, rootRect.width));
                        });
                    }, 5));
                    return keyLabel;
                };

                assetListColumns["NameCol"].bindCell = (e, i) =>
                {
                    var labelE = (e as Label);
                    var indexValue = (assetList.itemsSource as IList<ThemeAssetEntry>)[i];
                    labelE.text = indexValue.humanKey;

                    var listRow = labelE.parent.parent;
                    listRow.userData = (assetList.itemsSource as IList<ThemeAssetEntry>)[i];

                    if (labelE.userData != null)
                        return;
                    labelE.userData = false;

                    //use this space to make right-click actions for the row
                    AppendCurrentContextMenu(listRow, new OrderedMenuBuilder(e =>
                    {
                        e.menu.AppendSeparator();
                        e.menu.AppendAction("Delete Asset Slot/Confirm Deletion?", e =>
                        {
                            if (EditorUtility.DisplayDialog(
                                "Warning",
                                "All references to this asset slot will be lost. Proceed?", "Yes", "NO, $H!7, GO BACK"))
                            {
                                (assetList.itemsSource as IList<ThemeAssetEntry>).Remove(listRow.userData as ThemeAssetEntry);
                                PopulateAssetPanel();
                            }
                        });
                    }, 10),
                    new OrderedMenuBuilder(e =>
                    {
                        e.menu.AppendSeparator();
                        e.menu.AppendAction("Create Asset Slot", a => createAssetSlotAction());
                    }, 0));
                    //listRow.RegisterCallback<ChangeEvent<UnityEngine.Object>>(e=>e.StopImmediatePropagation(), TrickleDown.TrickleDown);
                };
                assetListColumns["ConstraintCol"].bindCell = (e, i) =>
                {
                    var enumE = (e as PopupField<Type>);
                    var indexValue = (assetList.itemsSource as IList<ThemeAssetEntry>)[i];


                    EventCallback<ChangeEvent<Type>> changeEvent = evt =>
                    {
                        indexValue.constraintIndex = Database.ValidConstraints.IndexOf(evt.newValue);
                        assetList.RefreshItems();
                    };

                    enumE.UnregisterValueChangedCallback(enumE.userData as EventCallback<ChangeEvent<Type>>);
                    enumE.index = indexValue.constraintIndex;
                    enumE.RegisterValueChangedCallback(changeEvent);
                    enumE.userData = changeEvent;
                };
                assetListColumns["ObjectCol"].bindCell = (e, i) =>
                {
                    var objectE = (e as ObjectField);
                    var indexValue = (assetList.itemsSource as IList<ThemeAssetEntry>)[i];


                    //objectE.UnregisterValueChangedCallback(objectE.userData as EventCallback<ChangeEvent<UnityEngine.Object>>);
                    objectE.objectType = Database.GetEntryConstraintType(indexValue);
                    objectE.SetValueWithoutNotify(TryGetAsset(indexValue.addressableKey, Database, true, out string themeKey));
                    //objectE.value = ;
                    objectE.userData = indexValue.addressableKey;

                    objectE.tooltip = themeKey;


                };
                assetListColumns["ResolvedObjectCol"].bindCell = (e, i) =>
                {
                    var objectE = (e as ObjectField);
                    var indexValue = (assetList.itemsSource as IList<ThemeAssetEntry>)[i];
                    objectE.objectType = Database.GetEntryConstraintType(indexValue);
                    objectE.value = TryGetAsset(indexValue.addressableKey, Database, false, out string themeKey);
                    objectE.tooltip = string.Join(", ", themeKey);
                };
                assetListColumns["KeyCol"].bindCell = (e, i) =>
                {
                    var labelE = (e as Label);
                    var indexValue = (assetList.itemsSource as IList<ThemeAssetEntry>)[i];
                    labelE.text = indexValue.addressableKey;
                };

                //assetList.AddManipulator(new ContextualMenuManipulator(e =>
                //{
                //    e.menu.AppendAction("Create Asset Slot", a=>createAssetSlotAction());
                //}));
                assetPanel.Q<ToolbarButton>("CreateAssetButton").clicked += createAssetSlotAction;
                assetPanel.Q<ToolbarButton>("AssetBackButton").clicked += () =>
                {
                    Database.WindowSelectedCategory = null;
                    PopulateCategoryPanel();
                };

                assetList.selectionChanged += l =>
                {
                    if (assetList.selectedIndex == -1)
                    {
                        Database.WindowSelectedEntry = null;
                        Database.WindowSelectedSubAssets = null;
                        PopulateSubAssetPanel();
                        return;
                    }

                    var list = assetList.itemsSource as IList<ThemeAssetEntry>;
                    Database.WindowSelectedEntry = list[assetList.selectedIndex];
                    PopulateSubAssetPanel();
                };

                #endregion

                #region Create Sub Asset Panel

                var subAssetList = rootVisualElement.Q<ListView>("SubAssetList");
                subAssetList.style.display = DisplayStyle.Flex;

                subAssetList.makeItem = () =>
                {
                    var itemRoot = new VisualElement() { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };

                    var statusIcon = new VisualElement() { name = "Icon", style = { width = 22, flexGrow = 0, flexShrink = 0 } };

                    var nameLabel = new Label() { style = { flexGrow = 1, paddingLeft = 5 } };
                    nameLabel.AddToClassList("theme-manager-label");

                    var checkbox = new Toggle();
                    checkbox.RegisterValueChangedCallback(e =>
                    {
                        if (e.newValue)
                        {
                            if (!Database.WindowSelectedEntry.enforcedSubAssets.Contains(nameLabel.text))
                                Database.WindowSelectedEntry.enforcedSubAssets.Add(nameLabel.text);
                        }
                        else
                        {
                            if (Database.WindowSelectedEntry.enforcedSubAssets.Contains(nameLabel.text))
                                Database.WindowSelectedEntry.enforcedSubAssets.Remove(nameLabel.text);
                        }
                        subAssetList.RefreshItems();
                    });

                    itemRoot.Add(statusIcon);
                    itemRoot.Add(nameLabel);
                    itemRoot.Add(checkbox);

                    return itemRoot;
                };
                subAssetList.bindItem = (e, i) =>
                {
                    string subAssetName = (subAssetList.itemsSource as IList<string>)[i];
                    var icon = e.Q("Icon");
                    var label = e.Q<Label>();
                    label.text = subAssetName;
                    var toggle = e.Q<Toggle>();
                    toggle.value = Database.WindowSelectedEntry.enforcedSubAssets!=null && Database.WindowSelectedEntry.enforcedSubAssets.Contains(subAssetName);
                    int fontStyleInt = 0;
                    icon.style.backgroundImage = null;
                    icon.tooltip = "";
                    if (toggle.value)
                    {
                        icon.style.backgroundImage = okTexture;
                        fontStyleInt += 1;
                    }
                    if (Database.WindowSelectedSubAssets==null || !Database.WindowSelectedSubAssets.Contains(subAssetName))
                    {
                        fontStyleInt += 2;
                    }
                    if (fontStyleInt == 3)
                    {
                        icon.style.backgroundImage = warningTexture;
                        icon.tooltip = "The enforced sub-asset is not present in the selected object";
                    }

                    label.style.unityFontStyleAndWeight = (FontStyle)fontStyleInt;
                };

                #endregion

                PopulateCategoryPanel();
            }
        }

        void PopulateCategoryPanel()
        {
            var categoryPanel = rootVisualElement.Q("CategoryPanel");
            var categoryList = rootVisualElement.Q<ListView>("CategoryList");

            List<string> groupChildren = GetGroupChildren();
            //Debug.Log(selectedPath + ": " + string.Join(", ", groupChildren));


            groupChildren.Sort((x, y) =>
            {
                return (IsGroup(x), IsGroup(y)) switch
                {
                    (true, true) or (false, false) => x.CompareTo(y),
                    (false, true) => 1,
                    (true, false) => -1
                };
            });

            categoryList.style.display = groupChildren.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            categoryPanel.Q("EmptyLabel").style.display = groupChildren.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            categoryPanel.Q<Label>("CategoryHeader").text = selectedPath == "" ? "Theme Manager Root" : selectedPath.Split('/')[^1];
            categoryPanel.Q<ToolbarButton>("CategoryBackButton").SetEnabled(selectedPath != "");

            categoryList.itemsSource = groupChildren;
            if (Database.WindowSelectedCategory != null)
            {
                var possibleSelectedCategory = groupChildren.FirstOrDefault(s => Database.WindowSelectedCategory.path == FollowupSelectedPath + s);
                if (possibleSelectedCategory != null)
                    categoryList.selectedIndex = groupChildren.IndexOf(possibleSelectedCategory);
                else
                {
                    Database.WindowSelectedCategory = null;
                    categoryList.selectedIndex = -1;
                    PopulateAssetPanel();
                }
            }
            else
                categoryList.selectedIndex = -1;
            categoryList.RefreshItems();
        }

        void PopulateAssetPanel()
        {
            var assetPanel = rootVisualElement.Q<VisualElement>("AssetPanel");
            var assetList = rootVisualElement.Q<MultiColumnListView>("AssetList");
            var cels = assetList.Query(null, "unity-multi-column-view__row-container").ToList();

            var assetHeader = assetPanel.Q<Label>("AssetHeader");
            assetHeader.text = Database.WindowSelectedCategory is null ? "No Category Selected" : Database.WindowSelectedCategory.path.Split("/")[^1];
            assetList.Q("unity-content-viewport").style.visibility = Database.WindowSelectedCategory is null || Database.WindowSelectedCategory.entries.Count == 0 ? Visibility.Hidden : Visibility.Visible;
            assetHeader.style.unityFontStyleAndWeight = Database.WindowSelectedCategory is null ? FontStyle.Italic : FontStyle.Bold;

            if (Database.WindowSelectedCategory is null)
                assetList.itemsSource = null;
            else
                assetList.itemsSource = Database.WindowSelectedCategory.entries;

            //EventCallback<ChangeEvent<UnityEngine.Object>> changeEvent = e => {
            //    e.StopImmediatePropagation();
            //    (e.currentTarget as VisualElement).UnregisterCallback((e.currentTarget as VisualElement).userData as EventCallback<ChangeEvent<UnityEngine.Object>>);
            //    };
            //cels.ForEach(e => {
            //    e.Q<ObjectField>().parent.userData = changeEvent;
            //    e.Q<ObjectField>().parent.RegisterCallback(changeEvent, TrickleDown.TrickleDown);
            //    });
            assetList.RefreshItems();
        }

        void PopulateSubAssetPanel()
        {
            var subAssetPanel = rootVisualElement.Q("SubAssetPanel");
            var subAssetList = rootVisualElement.Q<ListView>("SubAssetList");
            var subAssetHeader = subAssetPanel.Q<Label>("SubAssetHeader");

            List<string> subAssetListItems = new();
            if (Database.WindowSelectedEntry != null)
            {
                if (Database.WindowSelectedEntry.enforcedSubAssets != null)
                    subAssetListItems.AddRange(Database.WindowSelectedEntry.enforcedSubAssets.ToList());
                var selectedObject = TryGetAsset(Database.WindowSelectedEntry.addressableKey, Database, true);
                if (selectedObject != null)
                {
                    Database.WindowSelectedSubAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(selectedObject))
                            .Where(a => a != selectedObject)
                            .Select(a => a.name)
                            .ToList();
                    subAssetListItems.AddRange(Database.WindowSelectedSubAssets.Where(n => !subAssetListItems.Contains(n)));
                }
                else
                    Database.WindowSelectedSubAssets = null;
                subAssetHeader.text = Database.WindowSelectedEntry.humanKey;
                subAssetHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                subAssetList.style.display = DisplayStyle.Flex;
            }
            if (subAssetListItems.Count==0)
            {
                subAssetList.style.display = DisplayStyle.None;
                subAssetHeader.text = "No Asset Selected";
                subAssetHeader.style.unityFontStyleAndWeight = FontStyle.Italic;
            }
            else
            {
                subAssetListItems.Sort();
            }

            subAssetList.itemsSource = subAssetListItems;
            subAssetList.RefreshItems();
        }

        static UnityEngine.Object TryGetAsset(string primaryKey, RePaletteDatabase database, bool onlyDirect) => TryGetAsset(primaryKey, database, onlyDirect, out string _);
        static UnityEngine.Object TryGetAsset(string primaryKey, RePaletteDatabase database, bool onlyDirect, out string themeKey)
        {
            if (!database.ThemeFilter)
            {
                themeKey = null;
                return null;
            }
            UnityEngine.Object result;
            themeKey = database.ThemeFilter.EditorWindowFilter.ThemeKey;
            result = LoadAddressInEditor<UnityEngine.Object>(primaryKey, database.ThemeFilter.EditorWindowFilter.ThemeKey);
            if (!result && !onlyDirect)
            {
                themeKey = database.ThemeFilter.EditorWindowFilter.GetInheritedThemeKeys(themeKeys => GetAddressEntry(primaryKey, themeKeys) != null);
                if (themeKey!=null)
                    result = LoadAddressInEditor<UnityEngine.Object>(primaryKey, themeKey);
                else
                    result = null;
            }
            if (!result)
                themeKey = null;

            return result;
        }

        //Sourced from oxysofts: https://forum.unity.com/threads/how-to-use-addressable-system-in-editor-script.715163/
        static AddressableAssetEntry GetAddressEntry(string address, params string[] labels)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            List<AddressableAssetEntry> allEntries = new(settings.groups.SelectMany(g => g.entries));
            AddressableAssetEntry foundEntry = allEntries.FirstOrDefault(e => e.address == address && labels.Where(l => !e.labels.Contains(l)).Count() == 0);
            return foundEntry;
        }
        static TValue LoadAddressInEditor<TValue>(string address, params string[] labels)
                where TValue : UnityEngine.Object
        {
            var foundEntry = GetAddressEntry(address, labels);
            return foundEntry != null
                       ? AssetDatabase.LoadAssetAtPath<TValue>(foundEntry.AssetPath)
                       : null;
        }
    }
}
