using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Voidex.DataLabs.ConfigCrate;
using Voidex.DataLabs.DataLabs.Editor.GoogleSheets;
using Voidex.DataLabs.DataLabs.Editor.GoogleSheets.Internal;
using Voidex.DataLabs.GoogleSheets.Editor;
using Voidex.DataLabs.GoogleSheets.Runtime;

namespace Voidex.ConfigCrate.Configurations
{
    public class ConfigCrateWindow : EditorWindow
    {
        private GoogleSheetImporter<CrateImportUtility> m_importer;
        private TabbedController m_tabbedController;

        private const string UxmlAssetName = "config_crate_window";

        private DataLabsSheetsProfile MainProfile
        {
            get { return ConfigCrateSheetSettings.instance.Profile; }
        }

        private bool HasProfile
        {
            get { return ConfigCrateSheetSettings.instance.Profile != null; }
        }

        private int m_selectedProfileIndex = -1;

        private Profile SelectedProfile
        {
            get
            {
                if (filteredProfiles != null)
                {
                    if (m_selectedProfileIndex < 0 || m_selectedProfileIndex >= filteredProfiles.Count)
                    {
                        return null;
                    }

                    return filteredProfiles[m_selectedProfileIndex];
                }
                else
                {
                    if (ConfigCrateSheetSettings.instance.Profile == null ||
                        m_selectedProfileIndex < 0 ||
                        m_selectedProfileIndex >= ConfigCrateSheetSettings.instance.Profile.Count)
                    {
                        return null;
                    }

                    return ConfigCrateSheetSettings.instance.Profile[m_selectedProfileIndex];
                }
            }
        }

        private TextField m_credentialsPath;
        private ObjectField m_profile;
        private Label m_nullSheetProfileWarning;
        private Button m_profileCreateNewButton;
        private ProgressBar m_progressBar;

        private VisualElement m_profileGroup;

        private TextField m_searchField;
        private ScrollView m_profileList;
        private Button m_addButton;
        private Button m_removeButton;
        private Button m_importAllButton;

        private VisualElement m_profileSettingsGroup;
        private TextField m_profileName;
        private TextField m_sheetId;
        private Button m_opemInBrowserButton;

        private TextField m_worksheetName;
        private Foldout m_worksheetNames;
        private Button m_addWorksheetButton;
        private DropdownField m_assetType;
        private TextField m_assetDirectory;
        private Button m_importButton;

        private VisualElement m_oauthWaitMessage;
        private Button m_oauthWaitCancelButton;
        
        private List<Profile>? filteredProfiles;


        private static readonly Color SELECTION_COLOR = new Color32(44, 93, 135, 255);
        private static readonly Color CLEAR_COLOR = new Color(0, 0, 0, 0);
        private static readonly Color DARKEN_COLOR = new Color(0, 0, 0, 0.15f);


        [MenuItem("Tools/Voidex/Config Crate Window #&c")]
        private static void OpenWindow()
        {
            var window = GetWindow<ConfigCrateWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1280, 720);
        }

        private void OnEnable()
        {
            m_importer = new GoogleSheetImporter<CrateImportUtility>();
            m_tabbedController = new TabbedController(rootVisualElement);

            m_importer.OnAuthenticationComplete += HandleAuthenticationComplete;
            m_importer.OnAuthenticationError += HandleAuthenticationError;
            m_importer.OnAuthenticationStart += AuthenticatedStart;

            m_importer.OnImportStart += OnImportStart;

            m_importer.OnImportComplete += HandleImportComplete;
            m_importer.OnImportErrors += HandleImportErrors;
        }

        private void OnDisable()
        {
            m_importer.OnAuthenticationComplete -= HandleAuthenticationComplete;
            m_importer.OnImportComplete -= HandleImportComplete;
            m_importer.OnImportErrors -= HandleImportErrors;
            m_importer.OnAuthenticationError -= HandleAuthenticationError;
            m_importer.OnImportStart -= OnImportStart;

            m_importer.OnAuthenticationStart -= AuthenticatedStart;
        }


        public void CreateGUI()
        {
            rootVisualElement.Clear();

            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(UxmlAssetName);
            visualTree.CloneTree(rootVisualElement);

            m_credentialsPath = rootVisualElement.Q<TextField>("P12FilePath");
            m_credentialsPath.tooltip = "Path to the P12 file containing the Google Sheet service account credentials.";

            m_profile = rootVisualElement.Q<ObjectField>("Profile");
            m_profile.tooltip = "The profile to use for importing data.";

            m_profileCreateNewButton = rootVisualElement.Q<Button>("ProfileCreateNewButton");
            m_profileCreateNewButton.tooltip = "Create a new profile.";
            
            m_nullSheetProfileWarning = rootVisualElement.Q<Label>("NullSheetProfileWarning");

            m_progressBar = rootVisualElement.Q<ProgressBar>("ProgressBar");

            m_profileGroup = rootVisualElement.Q("ProfileGroup");
            m_searchField = rootVisualElement.Q<TextField>("Search");

            m_profileList = rootVisualElement.Q<ScrollView>("ProfileList");

            m_addButton = rootVisualElement.Q<Button>("AddButton");

            m_removeButton = rootVisualElement.Q<Button>("RemoveButton");

            m_importAllButton = rootVisualElement.Q<Button>("ImportAllButton");
            m_importAllButton.tooltip = "Import all profiles.";

            m_profileSettingsGroup = rootVisualElement.Q("ProfileSettingsGroup");

            m_profileName = rootVisualElement.Q<TextField>("ProfileName");
            m_profileName.tooltip = "The name of the profile.";

            m_sheetId = rootVisualElement.Q<TextField>("SheetID");
            m_sheetId.tooltip = "The ID of the Google Sheet to import data from. You can get this from the URL of the Google Sheet.";

            m_opemInBrowserButton = rootVisualElement.Q<Button>("OpenGoogleSheet");

            m_worksheetName = rootVisualElement.Q<TextField>("WorksheetName");
            m_worksheetName.tooltip =
                "The name of the worksheet in the Google Sheet to import data from. You can use multiple worksheet names by clicking the Add Worksheet button.";

            m_worksheetNames = rootVisualElement.Q<Foldout>("WorksheetNamesFoldout");
            m_worksheetNames.tooltip = "In case you want to import data from multiple worksheets, you can add more worksheet names here.";

            m_addWorksheetButton = rootVisualElement.Q<Button>("AddWorksheetButton");
            m_assetType = rootVisualElement.Q<DropdownField>("AssetType");
            m_assetDirectory = rootVisualElement.Q<TextField>("AssetDirectory");
            m_importButton = rootVisualElement.Q<Button>("ImportButton");

            m_oauthWaitMessage = rootVisualElement.Q("OAuthWaitMessage");
            m_oauthWaitCancelButton = rootVisualElement.Q<Button>("OAuthWaitCancelButton");

            // state setup

            m_credentialsPath.value = ConfigCrateSheetSettings.instance.CredentialsPath;
            HandleProfileChanged(ConfigCrateSheetSettings.instance.Profile);
            SetOAuthMessageDisplayed(false);
            SetProgressBarDisplayed(false);
            RefreshAssetTypes();

            // Register Callbacks

            m_importer.Importer.OnProgressChanged += (s, v) =>
            {
                m_progressBar.value = v;
                m_progressBar.title = s;
            };

            m_credentialsPath.RegisterValueChangedCallback(HandleCredentialsPathChanged);
            m_profile.RegisterValueChangedCallback(x => HandleProfileChanged(x.newValue as DataLabsSheetsProfile));

            m_profileName.RegisterValueChangedCallback(HandleProfileNameChanged);

            m_addButton.RegisterCallback<MouseDownEvent>(HandleAddProfileButton, TrickleDown.TrickleDown);
            m_removeButton.RegisterCallback<MouseDownEvent>(HandleRemoveProfileButton, TrickleDown.TrickleDown);
            m_profileCreateNewButton.RegisterCallback<MouseDownEvent>(HandleProfileCreateNewButton, TrickleDown.TrickleDown);
            m_importAllButton.RegisterCallback<MouseDownEvent>(HandleImportAllButton, TrickleDown.TrickleDown);
            m_importButton.RegisterCallback<MouseDownEvent>(HandleImportButton, TrickleDown.TrickleDown);
            m_oauthWaitCancelButton.RegisterCallback<MouseDownEvent>(HandleOAuthWaitCancelButton, TrickleDown.TrickleDown);
            m_addWorksheetButton.RegisterCallback<MouseDownEvent>(HandleAddWorksheetButton, TrickleDown.TrickleDown);
            m_opemInBrowserButton.RegisterCallback<MouseDownEvent>(HandleOpenInBrowser, TrickleDown.TrickleDown);

            //search
            m_searchField.RegisterValueChangedCallback(HandleSearchFieldChanged);
            
            m_tabbedController.RegisterTabCallbacks();
            
            if (ConfigCrateSheetSettings.instance.Profile == null)
            {
                m_tabbedController.SelectTab("SettingsTab");
                m_nullSheetProfileWarning.style.display = DisplayStyle.Flex;
            }else
            {
                m_nullSheetProfileWarning.style.display = DisplayStyle.None;
            }
        }

        private void HandleSearchFieldChanged(ChangeEvent<string> evt)
        {
            string searchText = evt.newValue.ToLower();
            if (string.IsNullOrEmpty(searchText))
            {
                m_addButton.SetEnabled(true);
                m_removeButton.SetEnabled(true);
            }
            else
            {
                m_addButton.SetEnabled(false);
                m_removeButton.SetEnabled(false);
            }

            // Filter the profiles based on the search text
            filteredProfiles = ConfigCrateSheetSettings.instance.Profile
                .Where(profile => profile.ProfileName.ToLower().Contains(searchText))
                .ToList();

            // Clear the profile list
            m_profileList.contentContainer.Clear();

            foreach (var profile in filteredProfiles)
            {
                Label label = new Label(profile.ProfileName);
                m_profileList.contentContainer.Add(label);

                // Attach the click event handler to the label
                int index = filteredProfiles.IndexOf(profile);
                label.RegisterCallback<MouseDownEvent>(x => HandleProfileClicked(index), TrickleDown.NoTrickleDown);
                m_manipulator = new ContextualMenuManipulator(OnContextMenuPopulate);
                label.AddManipulator(m_manipulator);
            }
            
            if (filteredProfiles.Count == 0)
            {
                Label label = new Label("No profiles found");
                m_profileList.contentContainer.Add(label);
            }
            
            // Reset the selected profile index to 0
            m_selectedProfileIndex = 0;
            RefreshProfileList();
            
            if(string.IsNullOrEmpty(searchText))
            {
                filteredProfiles = ConfigCrateSheetSettings.instance.Profile.Profiles;
                m_selectedProfileIndex = 0;
                RefreshProfileList();
            }
        }
        
        #region WorkSheetNames

        private void HandleAddWorksheetButton(MouseDownEvent evt)
        {
            if (SelectedProfile == null)
            {
                return;
            }

            SelectedProfile.WorksheetNames = SelectedProfile.WorksheetNames ?? new string[0];
            Array.Resize(ref SelectedProfile.WorksheetNames, SelectedProfile.WorksheetNames.Length + 1);
            SelectedProfile.WorksheetNames[^1] = "Sheet1";
            //create new text field
            TextField textField = new TextField();
            textField.value = "Sheet1";
            textField.RegisterValueChangedCallback(x => HandleWorksheetNameChanged(x, textField));
            //add a remove(-) button under textField
            Button button = new Button(() => { RemoveWorksheetName(textField); });
            textField.Add(button);
            button.text = "-";

            m_worksheetNames.contentContainer.Add(textField);

            RefreshProfileSettings();

            //save the profile asset
            EditorUtility.SetDirty(MainProfile);
        }

        private void RefreshWorksheetNames()
        {
            if (SelectedProfile == null)
            {
                return;
            }
            
            m_worksheetName.RegisterValueChangedCallback(x => HandleWorksheetNameChanged(x, m_worksheetName));

            m_worksheetName.value = SelectedProfile.WorksheetNames[0];
            //start from 1 because the first worksheet name is already added
            for (var i = 1; i < SelectedProfile.WorksheetNames.Length; i++)
            {
                TextField textField = new TextField();
                textField.value = SelectedProfile.WorksheetNames[i];
                textField.RegisterValueChangedCallback(x => HandleWorksheetNameChanged(x, textField));
                //add a remove(-) button under textField
                Button button = new Button(() => { RemoveWorksheetName(textField); });
                textField.Add(button);
                button.text = "-";
                m_worksheetNames.contentContainer.Add(textField);
            }
        }

        private void HandleWorksheetNameChanged(ChangeEvent<string> evt, TextField textField)
        {
            if (SelectedProfile == null)
            {
                return;
            }

            int index = Array.IndexOf(SelectedProfile.WorksheetNames, evt.previousValue);
            if (index == -1)
            {
                return;
            }

            SelectedProfile.WorksheetNames[index] = evt.newValue;

            //save the profile asset
            EditorUtility.SetDirty(MainProfile);
        }

        private void RemoveWorksheetName(TextField textField)
        {
            if (SelectedProfile == null)
            {
                return;
            }

            int index = Array.IndexOf(SelectedProfile.WorksheetNames, textField.value);
            if (index == -1)
            {
                return;
            }

            List<string> worksheetNames = new List<string>(SelectedProfile.WorksheetNames);
            worksheetNames.RemoveAt(index);
            SelectedProfile.WorksheetNames = worksheetNames.ToArray();
            m_worksheetNames.contentContainer.Remove(textField);

            RefreshProfileSettings();

            //save the profile asset
            EditorUtility.SetDirty(MainProfile);
        }

        #endregion

        public void OnProjectChange()
        {
            SetProfileGroupDisplayed(ConfigCrateSheetSettings.instance.Profile != null);
            RefreshAssetTypes();
        }

        private void RefreshProfileList()
        {
            m_profileList.contentContainer.Clear();
            
            if(ConfigCrateSheetSettings.instance.Profile == null)
            {
                return;
            }
            
            var profiles = filteredProfiles ?? ConfigCrateSheetSettings.instance.Profile.Profiles;
            
            if (profiles != null)
            {
                for (int ix = 0; ix < profiles.Count; ix++)
                {
                    Label label = new Label(profiles[ix].ProfileName);
                    if (ix == m_selectedProfileIndex)
                    {
                        label.style.backgroundColor = SELECTION_COLOR;
                    }
                    else
                    {
                        label.style.backgroundColor = ((ix % 2) == 0) ? CLEAR_COLOR : DARKEN_COLOR;
                    }

                    label.style.paddingTop = 2;
                    label.style.paddingBottom = 2;
                    label.style.paddingLeft = 8;
                    m_profileList.contentContainer.Add(label);
                    int index = ix;
                    
                    m_manipulator = new ContextualMenuManipulator(OnContextMenuPopulate);
                    label.AddManipulator(m_manipulator);
                    
                    label.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if(evt.button == 0)
                            HandleProfileClicked(index);
                    }, TrickleDown.NoTrickleDown);
                }
            }

            RefreshProfileSettings();
        }

        private IManipulator? m_manipulator;
        private Profile? m_copiedProfile;
        private void OnContextMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            if(evt.button != 1)
                return;
            evt.menu.ClearItems();
            
            evt.menu.AppendAction("Copy", (a) => CopyProfile(m_selectedProfileIndex), DropdownMenuAction.AlwaysEnabled);

            if(m_copiedProfile == null)
            {
                evt.menu.AppendAction("Paste", (a) => { }, DropdownMenuAction.AlwaysDisabled);
            }
            else
            {
                evt.menu.AppendAction("Paste", (a) => PasteProfile(m_copiedProfile, m_selectedProfileIndex), DropdownMenuAction.AlwaysEnabled);
            }
            
            //duplicate
            evt.menu.AppendAction("Duplicate", (a) => DuplicateProfile(m_selectedProfileIndex), DropdownMenuAction.AlwaysEnabled);
        }
        
        public void DuplicateProfile(int index)
        {
            // Get the profile to copy
            Profile profileToCopy = ConfigCrateSheetSettings.instance.Profile[index];

            // Create a deep copy of the profile
            Profile profile = new Profile
            {
                ProfileName = profileToCopy.ProfileName,
                SheetID = profileToCopy.SheetID,
                WorksheetNames = profileToCopy.WorksheetNames.ToArray(), // Create a new array to ensure it's a deep copy
                AssetType = profileToCopy.AssetType,
                AssetDirectory = profileToCopy.AssetDirectory
            };

            ConfigCrateSheetSettings.instance.Profile.Profiles.Insert(index, profile);
            m_selectedProfileIndex = index;
            RefreshProfileList();
        }
        
        public Profile CopyProfile(int index)
        {
            // Get the profile to copy
            Profile profileToCopy = ConfigCrateSheetSettings.instance.Profile[index];

            // Create a deep copy of the profile
            m_copiedProfile = new Profile
            {
                ProfileName = profileToCopy.ProfileName,
                SheetID = profileToCopy.SheetID,
                WorksheetNames = profileToCopy.WorksheetNames.ToArray(), // Create a new array to ensure it's a deep copy
                AssetType = profileToCopy.AssetType,
                AssetDirectory = profileToCopy.AssetDirectory
            };

            return m_copiedProfile;
        }

        public void PasteProfile(Profile? copied, int index)
        {
            if (copied == null)
            {
                return;
            }
            //paste the copied profile to the selected item
            ConfigCrateSheetSettings.instance.Profile.Profiles[index] = copied;

            // Refresh the profile list to reflect the changes
            RefreshProfileList();
        }

        private void RefreshProfileSettings()
        {
            Profile profile = SelectedProfile;

            SetProfileSettingsDisplayed(profile != null);
            if (profile == null)
            {
                m_profileName.Unbind();
                m_sheetId.Unbind();
                //m_worksheetName.Unbind();
                m_worksheetNames.Unbind();
                m_assetType.Unbind();
                m_assetDirectory.Unbind();
            }
            else
            {
                SerializedProperty property ;
                if (filteredProfiles != null && filteredProfiles.Count > 0)
                {
                    property = new SerializedObject(MainProfile)
                        .FindProperty("m_profiles")
                        .GetArrayElementAtIndex(ConfigCrateSheetSettings.instance.Profile.Profiles.IndexOf(profile));
                }
                else
                {
                    property = new SerializedObject(MainProfile)
                        .FindProperty("m_profiles")
                        .GetArrayElementAtIndex(m_selectedProfileIndex);
                }

                m_profileName.BindProperty(property.FindPropertyRelative("ProfileName"));
                m_sheetId.BindProperty(property.FindPropertyRelative("SheetID"));
                //m_worksheetName.BindProperty(property.FindPropertyRelative("WorksheetName"));
                m_worksheetNames.BindProperty(property.FindPropertyRelative("WorksheetNames"));
                m_assetType.BindProperty(property.FindPropertyRelative("AssetType"));
                m_assetDirectory.BindProperty(property.FindPropertyRelative("AssetDirectory"));

                RefreshWorksheetNames();
            }
        }

        private void RefreshAssetTypes()
        {
            List<string> assetTypes = new List<string>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (TypeInfo typeInfo in assembly.DefinedTypes)
                {
                    Type type = typeInfo.AsType();
                    ContentAssetAttribute contentAsset = type.GetCustomAttribute<ContentAssetAttribute>();
                    if (type.IsAsset() && contentAsset != null)
                    {
                        assetTypes.Add($"{typeInfo.FullName}, {assembly.GetName().Name}");
                    }
                }
            }

            assetTypes.Sort(StringComparer.Ordinal);
            m_assetType.choices = assetTypes;
        }

        public void OnDestroy()
        {
            m_importer.Authenticator?.CancelAuthentication();
        }

        private void SetProfileGroupDisplayed(bool isDisplayed)
        {
            m_profileGroup.style.display = isDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetOAuthMessageDisplayed(bool isDisplayed)
        {
            m_oauthWaitMessage.style.display = isDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
            m_profileGroup.SetEnabled(!isDisplayed);
        }

        private void SetProfileSettingsDisplayed(bool isDisplayed)
        {
            m_profileSettingsGroup.style.display = isDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetProgressBarDisplayed(bool isDisplayed)
        {
            m_progressBar.style.display = isDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HandleOpenInBrowser(MouseDownEvent evt)
        {
            if (SelectedProfile == null)
            {
                return;
            }

            Application.OpenURL($"https://docs.google.com/spreadsheets/d/{SelectedProfile.SheetID}");
        }

        #region Authentication & Import

        void OnImportStart()
        {
            m_profileGroup.SetEnabled(false);
            m_progressBar.value = 0f;
            SetProgressBarDisplayed(true);
        }

        void AuthenticatedStart()
        {
            SetOAuthMessageDisplayed(true);
        }

        private void DoAuthentication(Action importCallback)
        {
            m_importer.DoAuthentication(importCallback);
        }

        private void DoImport()
        {
            if (SelectedProfile == null)
            {
                return;
            }
            m_importer.DoImport(SelectedProfile);
        }

        private void DoImportAll()
        {
            if (MainProfile == null)
            {
                return;
            }
            m_importer.DoImportAll(MainProfile.Profiles);
        }

        private void HandleCredentialsPathChanged(ChangeEvent<string> ev)
        {
            ConfigCrateSheetSettings.instance.CredentialsPath = ev.newValue;
        }

        private void HandleProfileChanged(DataLabsSheetsProfile newProfile)
        {
            if (newProfile == null)
            {
                m_nullSheetProfileWarning.style.display = DisplayStyle.Flex;
            }else
            {
                m_nullSheetProfileWarning.style.display = DisplayStyle.None;
            }
            
            ConfigCrateSheetSettings.instance.Profile = newProfile;
            SetProfileGroupDisplayed(ConfigCrateSheetSettings.instance.Profile != null);
            m_profile.value = newProfile;
            m_selectedProfileIndex = 0;


            RefreshProfileList();
        }

        private void HandleProfileCreateNewButton(MouseDownEvent ev)
        {
            DataLabsSheetsProfile asset = ScriptableObject.CreateInstance<DataLabsSheetsProfile>();
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath("Assets/Voidex.DataLabs/DataLabs/GoogleSheets/Configs/DataLabsSheetsProfile.asset"));
            AssetDatabase.Refresh();
            m_profile.value = asset;
        }


        private void HandleImportAllButton(MouseDownEvent ev)
        {
            DoAuthentication(DoImportAll);
        }

        private void HandleImportButton(MouseDownEvent ev)
        {
            DoAuthentication(DoImport);
        }

        private void HandleOAuthWaitCancelButton(MouseDownEvent ev)
        {
            m_importer.Authenticator.CancelAuthentication();
            SetOAuthMessageDisplayed(false);
        }


        private void HandleAuthenticationComplete()
        {
            SetOAuthMessageDisplayed(false);
        }

        private void HandleAuthenticationError(string error)
        {
            Debug.LogError(error);
            SetOAuthMessageDisplayed(false);
        }

        private void HandleProfileNameChanged(ChangeEvent<string> ev)
        {
            m_profileList.contentContainer.Query<Label>().AtIndex(m_selectedProfileIndex).text = ev.newValue;
        }

        private void HandleAddProfileButton(MouseDownEvent ev)
        {
            if (!HasProfile)
            {
                return;
            }
            

            ConfigCrateSheetSettings.instance.Profile.AddNewProfile();
            m_selectedProfileIndex = ConfigCrateSheetSettings.instance.Profile.Count - 1;
            RefreshProfileList();
        }

        private void HandleRemoveProfileButton(MouseDownEvent ev)
        {
            if (!HasProfile || SelectedProfile == null)
            {
                return;
            }

            MainProfile.RemoveProfile(m_selectedProfileIndex);
            m_selectedProfileIndex = -1;
            RefreshProfileList();
        }

        private void HandleProfileClicked(int index)
        {
            m_selectedProfileIndex = index;
            RefreshProfileList();
        }


        private void HandleImportComplete()
        {
            m_profileGroup.SetEnabled(true);
            SetProgressBarDisplayed(false);
        }

        private void HandleImportErrors(IEnumerable<Exception> errors)
        {
            m_profileGroup.SetEnabled(true);
            SetProgressBarDisplayed(false);
            foreach (Exception error in errors)
            {
                Debug.LogException(error);
            }
        }

        #endregion


        private void OnFocus()
        {
            Repaint();
        }
    }
}