using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Voidex.DataLabs.GoogleSheets.Editor;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Voidex.DataLabs.Dashboard
{
    public partial class DataLabsDashboard : EditorWindow
    {
        // static from prefs
        private const string UxmlAssetName = "data_labs_dashboard_uxml";
        protected const string UxmlDashboardContent = "dashboard_content";
        protected const string UxmlSettingsContent = "settings_content";
        public static DataEntity CurrentSelectedAsset
        {
            get
            {
                if (Instance.m_currentSelectedAsset != null)
                {
                    return Instance.m_currentSelectedAsset;
                }

                string currentGuid = DataLabsEditorSettings.GetString(DataLabsEditorSettings.LabsData.CurrentAssetGuid);
                string currentPath = AssetDatabase.GUIDToAssetPath(currentGuid);
                DataEntity asset = AssetDatabase.LoadAssetAtPath<DataEntity>(currentPath);
                Instance.m_currentSelectedAsset = asset;
                return Instance.m_currentSelectedAsset;
            }
            private set
            {
                string currentPath = AssetDatabase.GetAssetPath(value);
                GUID currentGuid = AssetDatabase.GUIDFromAssetPath(currentPath);
                DataLabsEditorSettings.SetString(DataLabsEditorSettings.LabsData.CurrentAssetGuid, currentGuid.ToString());
                Instance.m_currentSelectedAsset = value;
            }
        }

        private DataEntity m_currentSelectedAsset;

        public static IDataGroup CurrentSelectedGroup
        {
            get
            {
                if (Instance.m_currentGroupSelected != null)
                {
                    return Instance.m_currentGroupSelected;
                }

                string currentName = DataLabsEditorSettings.GetString(DataLabsEditorSettings.LabsData.CurrentGroupName);
                GroupFoldableButton button = Instance.GroupColumn.Q<GroupFoldableButton>(currentName);
                if (button == null) return null;

                IDataGroup asset = button.DataGroup;
                if (asset == null) Debug.Log($"Failed to find group asset '{currentName}'.");
                Instance.m_currentGroupSelected = asset;
                return Instance.m_currentGroupSelected;
            }
            private set
            {
                string typeName = value == null
                    ? "NULL GROUP"
                    : value.SourceType.FullName;
                DataLabsEditorSettings.SetString(DataLabsEditorSettings.LabsData.CurrentGroupName, typeName);
                Instance.m_currentGroupSelected = value;
            }
        }

        private IDataGroup m_currentGroupSelected;

        // static dynamic
        public static DataLabsDashboard Instance
        {
            get
            {
                if (m_instance != null) return m_instance;
                Open();
                return m_instance;
            }
            private set => m_instance = value;
        }

        private static DataLabsDashboard m_instance;

        private DataLabsDashboardSettings m_dashboardSettings;

        public DataLabsDashboardSettings DashboardSettings
        {
            get
            {
                if(m_dashboardSettings == null)
                {
                    m_dashboardSettings = new DataLabsDashboardSettings(settingsContent);
                }
                
                return m_dashboardSettings;
            }
            set => m_dashboardSettings = value;
        }

        // static constant
        private static readonly StyleColor ButtonInactive = new StyleColor(Color.gray);
        private static readonly StyleColor ButtonActive = new StyleColor(Color.white);

        // toolbar
        //[SerializeField] protected Historizer Historizer;
        public ToolbarSearchField SearchFieldForGroup; // TODO move these. 
        [SerializeField] public string AssetSearchCache;
        [SerializeField] public string TypeSearchCache;
        [SerializeField] protected string m_filterProperty;
        [SerializeField] protected string m_filterOperator;
        [SerializeField] protected string m_filterValue;
        public static bool SearchTypeIsDirty => Instance != null && Instance.SearchFieldForGroup != null && Instance.SearchFieldForGroup.value != Instance.TypeSearchCache;

        // columns
        public DataGroupColumn GroupColumn; 
        public ColumnOfAssets AssetColumn;
        public AssetInspector InspectorColumn;

        // wrappers for views
        protected VisualElement WrapperForGroupContent;
        protected VisualElement WrapperForAssetList;
        protected VisualElement WrapperForAssetContent;
        protected VisualElement WrapperForInspector;

        protected ToolbarButton AssetNewButton;
        protected ToolbarButton AssetDeleteButton;
        protected ToolbarButton AssetCloneButton;
        protected ToolbarButton AssetRemoveFromGroupButton;

        protected DropdownField AssetFilterPropertyDropdown;
        protected DropdownField AssetFilterOperation;
        protected PropertyField AssetFilterPropertyValueField;
        protected Toolbar AssetFilterBar;
        [SerializeField] public string AssetFilterValueString;
        [SerializeField] public float AssetFilterValueFloat;
        [SerializeField] public int AssetFilterValueInt;
        [SerializeField] protected ListFilter.FilterType AssetFilterType;

        protected ToolbarButton GroupNewButton;
        protected ToolbarButton RefreshButton;

        protected ToolbarButton GroupDelButton;
        protected Button IdSetButton;
        protected IntegerField IdSetField;
        
        //tabs
        protected Button dashboardTab;
        protected Button settingsTab;
        protected VisualElement dashboardContent;
        protected VisualElement settingsContent;
        
        //current tab
        protected VisualElement currentTab;

        [MenuItem("Tools/Voidex/Voidex Attributes %#d", priority = 0)]
        public static void Open()
        {
            if (m_instance != null)
            {
                FocusWindowIfItsOpen(typeof(DataLabsDashboard));
                return;
            }

            Instance = GetWindow<DataLabsDashboard>();
            Instance.titleContent.text = "Voidex Attributes";
            Instance.minSize = new Vector2(850, 400);
            Instance.Show();

            Instance.SetIdStartingPoint(DataLabsEditorSettings.GetInt(DataLabsEditorSettings.LabsData.StartingKeyId));
        }

        private void OnEnable()
        {
            Instance = this;
            DatabaseBuilder.CallbackAfterScriptReload();
            //RebuildFull();

        }

        public void Update()
        {
            if (CurrentSelectedGroup != null && CurrentSelectedGroup.Content != null && SearchTypeIsDirty)
            {
                TypeSearchCache = SearchFieldForGroup.value;
                DataLabsEditorSettings.SetString(DataLabsEditorSettings.LabsData.SearchAssets, AssetSearchCache);
                GroupColumn.Filter(TypeSearchCache);
            }

            if (CurrentSelectedGroup != null && CurrentSelectedGroup.Content != null && GetAssetFilterPropertyName() != m_filterProperty)
            {
                m_filterProperty = GetAssetFilterPropertyName();
                if (m_filterProperty == "*ERROR*") return;

                UpdateAssetFilterPropertyField();
                AssetColumn.ListAssetsBySearch();
            }

            if (CurrentSelectedGroup != null && CurrentSelectedGroup.Content != null && GetAssetFilterOperation().ToString() != m_filterOperator)
            {
                m_filterOperator = GetAssetFilterOperation().ToString();
                UpdateAssetFilterPropertyField();
                AssetColumn.ListAssetsBySearch();
            }

            if (GetAssetFilterPropertyValue() != m_filterValue)
            {
                m_filterValue = GetAssetFilterPropertyValue();
                AssetColumn.ListAssetsBySearch();
            }
        }

        private void LoadUxmlTemplate()
        {
            rootVisualElement.Clear();


            // load uxml and elements
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(UxmlAssetName);
            visualTree.CloneTree(rootVisualElement);

            //tabs
            dashboardTab = rootVisualElement.Q<Button>("DASHBOARD_TAB");
            settingsTab = rootVisualElement.Q<Button>("SETTINGS_TAB");
            dashboardContent = rootVisualElement.Q<VisualElement>(UxmlDashboardContent);
            settingsContent = rootVisualElement.Q<VisualElement>(UxmlSettingsContent);

            
            SetUpUxmlDashBoard();
            SetUpUxmlSettings();
            
            InitializeTab();
        }

        public void SetUpUxmlDashBoard()
        {
            // find important parts and reference them
            WrapperForGroupContent = rootVisualElement.Q<VisualElement>("GC_CONTENT");
            WrapperForAssetContent = rootVisualElement.Q<VisualElement>("AC_CONTENT");
            WrapperForAssetList = rootVisualElement.Q<VisualElement>("ASSET_COLUMN");
            WrapperForInspector = rootVisualElement.Q<VisualElement>("INSPECT_COLUMN");
            SearchFieldForGroup = rootVisualElement.Q<ToolbarSearchField>("GROUP_SEARCH");

            // Historizer = new Historizer();
            // rootVisualElement.Q<VisualElement>("TB_HISTORY").Add(Historizer);


            // init group column buttons
            GroupNewButton = rootVisualElement.Q<ToolbarButton>("GC_NEW");
            GroupNewButton.style.backgroundImage = new StyleBackground(DataLabsEditorUtility.GetEditorImage("cyl_add"));
            GroupNewButton.clicked += CreateNewDataGroupCallback;

            GroupDelButton = rootVisualElement.Q<ToolbarButton>("GC_DEL");
            GroupDelButton.style.backgroundImage = new StyleBackground(DataLabsEditorUtility.GetEditorImage("cyl_del"));
            GroupDelButton.clicked += DeleteSelectedDataGroup;

            RefreshButton = rootVisualElement.Q<ToolbarButton>("GC_RELOAD");
            RefreshButton.style.backgroundImage = new StyleBackground(DataLabsEditorUtility.GetEditorImage("refresh"));
            RefreshButton.clicked += CallbackButtonRefresh;


            // init Asset Column Buttons
            AssetNewButton = WrapperForAssetList.Q<ToolbarButton>("AC_NEW");
            AssetNewButton.style.backgroundImage = new StyleBackground(DataLabsEditorUtility.GetEditorImage("cube_new"));
            AssetNewButton.clicked += CreateNewAssetCallback;

            AssetDeleteButton = WrapperForAssetList.Q<ToolbarButton>("AC_DELETE");
            AssetDeleteButton.style.backgroundImage = new StyleBackground(DataLabsEditorUtility.GetEditorImage("cube_del"));
            AssetDeleteButton.clicked += DeleteSelectedAsset;

            AssetCloneButton = WrapperForAssetList.Q<ToolbarButton>("AC_CLONE");
            AssetCloneButton.style.backgroundImage = new StyleBackground(DataLabsEditorUtility.GetEditorImage("clone"));
            AssetCloneButton.clicked += CloneSelectedAsset;

            AssetRemoveFromGroupButton = WrapperForAssetList.Q<ToolbarButton>("AC_GROUP_REMOVE");
            AssetRemoveFromGroupButton.style.backgroundImage = new StyleBackground(DataLabsEditorUtility.GetEditorImage("cyl_sub"));
            AssetRemoveFromGroupButton.clicked += RemoveAssetFromGroup;


            // init filtering interface
            AssetFilterBar = WrapperForAssetList.Q<Toolbar>("AC_FILTER_TOOLBAR");
            AssetFilterPropertyDropdown = WrapperForAssetList.Q<DropdownField>("AC_FILTER_DROPDOWN");
            AssetFilterPropertyDropdown.Remove(AssetFilterPropertyDropdown.Q<Label>()); // we have to remove the label from the dropdowns

            AssetFilterOperation = AssetFilterBar.Q<DropdownField>("AC_FILTER_OPERATION");
            AssetFilterOperation.choices = ListFilter.FilterOpSymbols;
            AssetFilterOperation.Remove(AssetFilterOperation.Q<Label>());
            IEnumerable<VisualElement> children = AssetFilterOperation.Children();
            children.ElementAt(0).style.maxWidth = 30;
            children.ElementAt(0).style.minWidth = 30;
            children.ElementAt(0).style.width = 30;


            // init footer
            IdSetButton = rootVisualElement.Q<Button>("ID_SET_BUTTON");
            IdSetButton.clicked += SetIdCallback;

            IdSetField = rootVisualElement.Q<IntegerField>("ID_SET_FIELD");
            IdSetField.SetValueWithoutNotify(DataLabsEditorSettings.GetInt(DataLabsEditorSettings.LabsData.StartingKeyId));

            WrapperForGroupContent.Add(GroupColumn);
            WrapperForAssetContent.Add(AssetColumn);
            WrapperForInspector.Add(InspectorColumn);


            // init split pane draggers
            // BUG - basically we have to do this because there is no proper/defined initialization for the drag anchor position.

            SplitView mainSplit = rootVisualElement.Q<SplitView>("MAIN_SPLIT");
            mainSplit.fixedPaneInitialDimension = 549;

            SplitView columnSplit = rootVisualElement.Q<SplitView>("FILTERS_PICK_SPLIT");
            columnSplit.fixedPaneInitialDimension = 250;

            SetIdStartingPoint(DataLabsEditorSettings.GetInt(DataLabsEditorSettings.LabsData.StartingKeyId));
        }
        
        private void SetUpUxmlSettings()
        {
            DashboardSettings.CreateGUI();
        }

        private void CreateGUI()
        {
            RebuildFull();
            
            dashboardTab.RegisterCallback<ClickEvent>(e => ShowDashboardTab());
            settingsTab.RegisterCallback<ClickEvent>(e => ShowSettingsTab());
        }

        public void RebuildFull()
        {
            LoadUxmlTemplate();
            Rebuild(true);
        }

        public void Rebuild(bool fullRebuild = false)
        {
            // search data
            SearchFieldForGroup.SetValueWithoutNotify(DataLabsEditorSettings.GetString(DataLabsEditorSettings.LabsData.SearchGroups));

            TypeSearchCache = SearchFieldForGroup.value;

            // rebuild
            RebuildGroupColumn(fullRebuild);
            RebuildInspectorColumn(fullRebuild);
            RebuildAssetColumn(fullRebuild);
            SetCurrentGroup(CurrentSelectedGroup);

        }

        private void RebuildGroupColumn(bool fullRebuild = false)
        {
            if (fullRebuild || GroupColumn == null)
            {
                WrapperForGroupContent.Clear();
                GroupColumn = new FilterColumnInheritance();
                WrapperForGroupContent.Add(GroupColumn);
            }

            GroupColumn.PanelReload();
        }

        private void RebuildAssetColumn(bool fullRebuild = false)
        {
            if (fullRebuild || AssetColumn == null)
            {
                WrapperForAssetContent.Clear();
                AssetColumn = new ColumnOfAssets();
                WrapperForAssetContent.Add(AssetColumn);
            }

            AssetColumn.PanelReload();
        }

        private void RebuildInspectorColumn(bool fullRebuild = false)
        {
            if (fullRebuild || InspectorColumn == null)
            {
                InspectorColumn?.RemoveFromHierarchy();
                InspectorColumn = new AssetInspector();
                WrapperForInspector.Add(InspectorColumn);
            }

            InspectorColumn.PanelReload();
        }

        // filter property
        public string GetAssetFilterPropertyName()
        {
            if (AssetFilterPropertyDropdown == null || AssetFilterPropertyDropdown.choices == null)
            {
                return "*ERROR*";
            }

            if (AssetFilterPropertyDropdown.index > AssetFilterPropertyDropdown.choices.Count)
            {
                AssetFilterPropertyDropdown.SetValueWithoutNotify(AssetFilterPropertyDropdown.choices[0]);
            }

            return AssetFilterPropertyDropdown.choices[AssetFilterPropertyDropdown.index];
        }

        public string GetAssetFilterPropertyValue()
        {
            return AssetFilterType switch
            {
                ListFilter.FilterType.String => AssetFilterValueString,
                ListFilter.FilterType.Float => AssetFilterValueFloat.ToString(CultureInfo.InvariantCulture),
                ListFilter.FilterType.Int => AssetFilterValueInt.ToString(),
                _ => throw new ArgumentOutOfRangeException(nameof(AssetFilterType), AssetFilterType, null)
            };
        }

        public ListFilter.FilterType GetAssetFilterPropertyType()
        {
            // String
            // Float
            // Int
            // Enum (string list? layer field?)

            string pName = GetAssetFilterPropertyName();
            if (pName == "*ERROR*") return ListFilter.FilterType.String;
            FieldInfo field = CurrentSelectedGroup.SourceType.GetField(pName);

            if (field != null)
            {
                //Debug.Log($"<color=lime>SELECT: `{pName}` - {field.FieldType} </color>");
                if (field.FieldType.IsAssignableFrom(typeof(string))) return ListFilter.FilterType.String;
                if (field.FieldType.IsAssignableFrom(typeof(float))) return ListFilter.FilterType.Float;
                if (field.FieldType.IsAssignableFrom(typeof(int))) return ListFilter.FilterType.Int;
            }
            else
            {
                PropertyInfo property = CurrentSelectedGroup.SourceType.GetProperty(pName);
                //Debug.Log($"<color=lime>SELECT: `{pName}` - {property.PropertyType} </color>");
                if (property.PropertyType.IsAssignableFrom(typeof(string))) return ListFilter.FilterType.String;
                if (property.PropertyType.IsAssignableFrom(typeof(float))) return ListFilter.FilterType.Float;
                if (property.PropertyType.IsAssignableFrom(typeof(int))) return ListFilter.FilterType.Int;
            }

            //Debug.Log("<color=red>Unknown Filter Type.</color>");
            return ListFilter.FilterType.String;
        }

        // filter operation
        public ListFilter.FilterOp GetAssetFilterOperation()
        {
            return (ListFilter.FilterOp) Math.Clamp(AssetFilterOperation.index, 0, int.MaxValue);
        }

        public void SetAssetFilterOperation(ListFilter.FilterOp op)
        {
            AssetFilterOperation.index = (int) op;
        }

        // filter update
        public void ResetAssetFilter()
        {
        }

        public void UpdateAssetFilterChoices()
        {
            // Property Choice
            List<string> results = ListFilter.GetFilterablePropertyNames();
            StringBuilder sb = new StringBuilder();
            foreach (var x in results)
            {
                sb.Append(x + ", ");
            }

            // Title is the fallback, always.
            int titleIndex = results.FindIndex(0, x => x == "Title");

            if (AssetFilterPropertyDropdown.choices != null && AssetFilterPropertyDropdown.choices.Count != results.Count)
                AssetFilterPropertyDropdown.index = results.FindIndex(0, x => x == "Title");

            AssetFilterPropertyDropdown.choices = results;

            int index = results.FindIndex(0, x => x == AssetFilterPropertyDropdown.text);
            if (index > 0)
            {
                AssetFilterPropertyDropdown.index = index;
                AssetFilterPropertyDropdown.value = results[index];
            }
            else
            {
                AssetFilterPropertyDropdown.index = titleIndex;
                AssetFilterPropertyDropdown.value = results[titleIndex];
            }
        }

        public void UpdateAssetFilterPropertyField()
        {
            PropertyField pf = AssetFilterBar.Q<PropertyField>();
            if (pf != null) AssetFilterBar.Remove(pf);

            AssetFilterType = GetAssetFilterPropertyType();
            SerializedProperty property;
            switch (AssetFilterType)
            {
                case ListFilter.FilterType.String:
                    //Debug.Log("Created String Field");
                    property = UnityEditor.Editor.CreateEditor(this).serializedObject.FindProperty("AssetFilterValueString");
                    break;
                case ListFilter.FilterType.Float:
                    //Debug.Log("Created Float Field");
                    property = UnityEditor.Editor.CreateEditor(this).serializedObject.FindProperty("AssetFilterValueFloat");
                    break;
                case ListFilter.FilterType.Int:
                    //Debug.Log("Created Int Field");
                    property = UnityEditor.Editor.CreateEditor(this).serializedObject.FindProperty("AssetFilterValueInt");
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            AssetFilterPropertyValueField = new PropertyField(property, "")
            {
                viewDataKey = "filter_property_field",
                name = "FILTER_PROPERTY_FIELD"
            };
            AssetFilterPropertyValueField.style.minWidth = 100;
            AssetFilterPropertyValueField.style.maxWidth = 200;
            AssetFilterPropertyValueField.bindingPath = property.propertyPath;
            AssetFilterPropertyValueField.Bind(property.serializedObject);

            AssetFilterBar.Add(AssetFilterPropertyValueField);
        }

        public void SetCurrentGroup(IDataGroup group)
        {
            if (group == null) return;
            bool selectionIsACustomGroup = group.GetType() == typeof(DataLabsCustomDataGroup);
            AssetRemoveFromGroupButton.SetEnabled(selectionIsACustomGroup);
            AssetRemoveFromGroupButton.style.unityBackgroundImageTintColor = selectionIsACustomGroup ? ButtonActive : ButtonInactive;

            CurrentSelectedGroup = group;
            UpdateAssetFilterChoices();
            UpdateAssetFilterPropertyField();
            if (selectionIsACustomGroup) GroupColumn.SelectButtonByDisplayTitle(group.Title);
            else GroupColumn.SelectButtonByFullTypeName(group.SourceType.FullName);
            AssetColumn.ListAssetsByGroup(true);
            // TODO RESET FILTER FIELD?
        }

        public void SetCurrentInspectorAsset(DataEntity asset)
        {
            CurrentSelectedAsset = asset;
            InspectorColumn.PanelReload();
            // Historizer.AddAndHistorize();
        }

        public void InspectAssetRemote(Object asset, Type t)
        {
            if (asset == null && t == null) return;
            if (t == null) return;

            if (Instance == null) Open();
            if (Instance != null) Instance.Focus();
            // TODO RESET FILTER FIELD?

            VisualElement button = WrapperForGroupContent.Q<VisualElement>(t.FullName);
            IGroupButton buttonInterface = (IGroupButton) button;
            if (buttonInterface != null)
            {
                buttonInterface.SetAsCurrent();
                GroupColumn.ScrollTo(button);
            }

            if (asset != null) AssetColumn.Pick((DataEntity) asset);
            InspectorColumn.PanelReload();
        }

        /// <summary>
        /// The Dashboard button calls this to create a new asset in the current group.
        /// </summary>
        private void CreateNewAssetCallback()
        {
            if (CurrentSelectedGroup.SourceType.IsAbstract)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Group Error",
                    "Selected Class is abstract! We can't create a new asset in abstract class groups. Choose a valid class and create a new Data Asset, then you can store it in a Custom Group.",
                    "Ok");
                if (confirm) return;
            }

            CreateNewAsset();
        }

        /// <summary>
        /// Create a new asset with the current group Type.
        /// </summary>
        /// <returns></returns>
        public void CreateNewAsset()
        {
            AssetColumn.NewAsset(CurrentSelectedGroup.SourceType);
        }

        /// <summary>
        /// Create a new asset with a specific Type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public DataEntity CreateNewAsset(Type t)
        {
            DataEntity newAsset = AssetColumn.NewAsset(t);
            DatabaseBuilder.Reload();
            Instance.RebuildFull();
            return newAsset;
        }

        public void CloneSelectedAsset()
        {
            AssetColumn.CloneSelection();
        }

        public void DeleteSelectedAsset()
        {
            AssetColumn.DeleteSelection();
        }

        public void SetIdStartingPoint(int id)
        {
            DataLab.Db.SetIdStartingValue(id);
            DataLabsEditorSettings.SetInt(DataLabsEditorSettings.LabsData.StartingKeyId, id);
            if (IdSetField != null) IdSetField.value = id;
            if (DataLab.Db != null) EditorUtility.SetDirty(DataLab.Db);
        }

        private void SetIdCallback()
        {
            SetIdStartingPoint(IdSetField.value);
        }

        public void RemoveAssetFromGroup()
        {
            CurrentSelectedGroup.RemoveEntity(CurrentSelectedAsset.GetDbKey());
            AssetColumn.PanelReload();
        }

        public void CreateNewDataGroupCallback()
        {
            CreateNewDataGroup();
        }

        public void DeleteSelectedDataGroup()
        {
            if (CurrentSelectedGroup == null) return;
            if (CurrentSelectedGroup.GetType() != typeof(DataLabsCustomDataGroup)) return;
            DataLabsCustomDataGroup customGroup = (DataLabsCustomDataGroup) CurrentSelectedGroup;
            if (customGroup == null) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Delete Custom Group",
                $"Are you sure you want to permanently delete '{CurrentSelectedGroup.Title}'?",
                "Delete",
                "Abort");
            if (!confirm) return;

            InspectAssetRemote(null, typeof(object));
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(customGroup));
            CurrentSelectedGroup = null;
            Instance.Rebuild();
        }

        public DataLabsCustomDataGroup CreateNewDataGroup()
        {
            DataLabsCustomDataGroup result = (DataLabsCustomDataGroup) AssetColumn.NewAsset(typeof(DataLabsCustomDataGroup));
            GroupColumn.PanelReload();
            InspectAssetRemote(result, typeof(DataLabsCustomDataGroup));
            return null;
        }

        public void CallbackButtonRefresh()
        {
            RebuildFull();
        }

        #region Tabs

        private void ShowDashboardTab()
        {
            dashboardContent.style.display = DisplayStyle.Flex;
            dashboardContent.style.flexGrow = 1;

            settingsContent.style.display = DisplayStyle.None;
            currentTab = dashboardContent;
        }
        
        private void ShowSettingsTab()
        {
            dashboardContent.style.display = DisplayStyle.None;
            settingsContent.style.display = DisplayStyle.Flex;
            
            settingsContent.style.flexGrow = 1;
            currentTab = settingsContent;
        }
        
        private void InitializeTab()
        {
            if (currentTab == null)
            {
                ShowDashboardTab();
                return;
            }
            
            if (currentTab == dashboardContent)
            {
                ShowDashboardTab();
            }
            else
            {
                ShowSettingsTab();
            }
        }

        #endregion
    }
}