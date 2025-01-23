using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Voidex.DataLabs.Dashboard;
using Voidex.DataLabs.GoogleSheets.Runtime;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public class DataLabsDashboardSettings
    {
        // Internal Data
        private Authentication m_authenticator;
        private ServiceAccountCredential m_credentials;
        private SheetsService m_service;
        private Importer<DataLabImportUtility> m_importer;

        private DataLabsSheetsProfile MainProfile
        {
            get { return GoogleSheetSettings.instance.Profile; }
        }

        private bool HasProfile
        {
            get { return GoogleSheetSettings.instance.Profile != null; }
        }

        private Profile SelectedProfile
        {
            get
            {
                if (GoogleSheetSettings.instance.Profile == null ||
                    m_selectedProfileIndex < 0 ||
                    m_selectedProfileIndex >= GoogleSheetSettings.instance.Profile.Count)
                {
                    return null;
                }

                return GoogleSheetSettings.instance.Profile[m_selectedProfileIndex];
            }
        }

        private int m_selectedProfileIndex = -1;
        private static readonly Color SELECTION_COLOR = new Color32(44, 93, 135, 255);
        private static readonly Color CLEAR_COLOR = new Color(0, 0, 0, 0);
        private static readonly Color DARKEN_COLOR = new Color(0, 0, 0, 0.15f);

        // VisualElements/Controls
        // private TextField m_clientSecretPath;
        private TextField m_credentialsPath;
        private ObjectField m_profile;
        private Button m_profileCreateNewButton;
        private ProgressBar m_progressBar;

        private VisualElement m_profileGroup;

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

        private VisualElement m_root;

        public DataLabsDashboardSettings(VisualElement root)
        {
            m_root = root;
        }

        public void CreateGUI()
        {
            m_authenticator = new Authentication();
            m_importer = new Importer<DataLabImportUtility>();

            // m_clientSecretPath = m_root.Q<TextField>("ClientSecretPath");
            m_credentialsPath = m_root.Q<TextField>("P12FilePath");
            m_credentialsPath.tooltip = "Path to the P12 file containing the Google Sheet service account credentials.";

            m_profile = m_root.Q<ObjectField>("Profile");
            m_profile.tooltip = "The profile to use for importing data.";

            m_profileCreateNewButton = m_root.Q<Button>("ProfileCreateNewButton");
            m_profileCreateNewButton.tooltip = "Create a new profile.";

            m_progressBar = m_root.Q<ProgressBar>("ProgressBar");

            m_profileGroup = m_root.Q("ProfileGroup");

            m_profileList = m_root.Q<ScrollView>("ProfileList");

            m_addButton = m_root.Q<Button>("AddButton");

            m_removeButton = m_root.Q<Button>("RemoveButton");

            m_importAllButton = m_root.Q<Button>("ImportAllButton");
            m_importAllButton.tooltip = "Import all profiles.";

            m_profileSettingsGroup = m_root.Q("ProfileSettingsGroup");

            m_profileName = m_root.Q<TextField>("ProfileName");
            m_profileName.tooltip = "The name of the profile.";

            m_sheetId = m_root.Q<TextField>("SheetID");
            m_sheetId.tooltip = "The ID of the Google Sheet to import data from. You can get this from the URL of the Google Sheet.";

            m_opemInBrowserButton = m_root.Q<Button>("OpenGoogleSheet");

            m_worksheetName = m_root.Q<TextField>("WorksheetName");
            m_worksheetName.tooltip = "The name of the worksheet in the Google Sheet to import data from.";

            m_worksheetNames = m_root.Q<Foldout>("WorksheetNamesFoldout");
            m_worksheetNames.tooltip = "In case you want to import data from multiple worksheets, you can add more worksheet names here.";

            m_addWorksheetButton = m_root.Q<Button>("AddWorksheetButton");
            m_assetType = m_root.Q<DropdownField>("AssetType");
            m_assetDirectory = m_root.Q<TextField>("AssetDirectory");
            m_importButton = m_root.Q<Button>("ImportButton");

            m_oauthWaitMessage = m_root.Q("OAuthWaitMessage");
            m_oauthWaitCancelButton = m_root.Q<Button>("OAuthWaitCancelButton");

            // state setup

            // m_clientSecretPath.value = DataLabsSheetsSettings.instance.ClientSecretPath;
            m_credentialsPath.value = GoogleSheetSettings.instance.CredentialsPath;
            HandleProfileChanged(GoogleSheetSettings.instance.Profile);
            SetOAuthMessageDisplayed(false);
            SetProgressBarDisplayed(false);
            RefreshAssetTypes();

            // Register Callbacks

            m_importer.OnProgressChanged += (s, v) =>
            {
                m_progressBar.value = v;
                m_progressBar.title = s;
            };

            // m_clientSecretPath.RegisterValueChangedCallback(HandleClientSecretPathChanged);
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


            RefreshWorksheetNames();

            m_assetType.RegisterCallback<MouseDownEvent>(OpenSearchTypeWindow, TrickleDown.TrickleDown);
            m_assetType.RegisterValueChangedCallback(UpdateAssetType);
        }

        private void UpdateAssetType(ChangeEvent<string> evt)
        {
            if (SelectedProfile == null)
            {
                return;
            }

            SelectedProfile.AssetType = evt.newValue;
            EditorUtility.SetDirty(MainProfile);
        }

        private void OpenSearchTypeWindow(MouseDownEvent evt)
        {
            if (evt.clickCount == 2)
            {
                SearchableDropdown dropdown = new SearchableDropdown(m_assetType);
                // dropdown.OnValueChanged += x =>
                // {
                //     m_assetType.value = x;
                //     m_assetType.MarkDirtyRepaint();
                // };
                UnityEditor.PopupWindow.Show(m_assetType.worldBound, dropdown);
            }
        }

        private void HandleOpenInBrowser(MouseDownEvent evt)
        {
            if (SelectedProfile == null)
            {
                return;
            }

            Application.OpenURL($"https://docs.google.com/spreadsheets/d/{SelectedProfile.SheetID}");
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

        public void OnProjectChange()
        {
            SetProfileGroupDisplayed(GoogleSheetSettings.instance.Profile != null);
            RefreshAssetTypes();
        }

        public void OnDestroy()
        {
            m_authenticator?.CancelAuthentication();
        }

        private void RefreshProfileList()
        {
            m_profileList.contentContainer.Clear();
            if (GoogleSheetSettings.instance.Profile != null)
            {
                for (int ix = 0; ix < GoogleSheetSettings.instance.Profile.Count; ix++)
                {
                    Label label = new Label(GoogleSheetSettings.instance.Profile[ix].ProfileName);
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

                    m_manipulator = new ContextualMenuManipulator(OnContextMenuPopulate);
                    label.AddManipulator(m_manipulator);

                    int index = ix;
                    label.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button == 0)
                            HandleProfileClicked(index);
                    }, TrickleDown.NoTrickleDown);
                }
            }

            RefreshProfileSettings();
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
                SerializedProperty property = new SerializedObject(MainProfile)
                    .FindProperty("m_profiles")
                    .GetArrayElementAtIndex(m_selectedProfileIndex);

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

        private void DoAuthentication(Action importCallback)
        {
            SetOAuthMessageDisplayed(true);
            // m_authenticator.Authenticate(
            //     DataLabsSheetsSettings.instance,
            //     x => HandleAuthenticationComplete(x, importCallback),
            //     HandleAuthenticationError
            // );

            m_service = m_authenticator.Authenticate(GoogleSheetSettings.instance,
                credential => HandleAuthenticationComplete(credential),
                HandleAuthenticationError);
            m_importer.SetService(m_service);
            importCallback?.Invoke();
        }

        private void DoImport()
        {
            if (SelectedProfile == null)
            {
                return;
            }

            m_importer.SetCredentials(m_credentials);

            m_profileGroup.SetEnabled(false);
            m_progressBar.value = 0f;
            SetProgressBarDisplayed(true);
            m_importer.Import(SelectedProfile, HandleImportComplete, HandleImportErrors);
        }

        private void DoImportAll()
        {
            if (MainProfile == null)
            {
                return;
            }

            m_importer.SetCredentials(m_credentials);
            m_importer.SetService(m_service);
            m_profileGroup.SetEnabled(false);
            m_progressBar.value = 0f;
            SetProgressBarDisplayed(true);
            m_importer.Import(MainProfile, HandleImportComplete, HandleImportErrors);
        }

        private IManipulator? m_manipulator;
        private Profile? m_copiedProfile;

        private void OnContextMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            if (evt.button != 1)
                return;
            evt.menu.ClearItems();

            evt.menu.AppendAction("Copy", (a) => CopyProfile(m_selectedProfileIndex), DropdownMenuAction.AlwaysEnabled);

            if (m_copiedProfile == null)
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
            Profile profileToCopy = GoogleSheetSettings.instance.Profile[index];

            // Create a deep copy of the profile
            Profile profile = new Profile
            {
                ProfileName = profileToCopy.ProfileName,
                SheetID = profileToCopy.SheetID,
                WorksheetNames = profileToCopy.WorksheetNames.ToArray(), // Create a new array to ensure it's a deep copy
                AssetType = profileToCopy.AssetType,
                AssetDirectory = profileToCopy.AssetDirectory
            };

            GoogleSheetSettings.instance.Profile.Profiles.Insert(index, profile);
            m_selectedProfileIndex = index;
            RefreshProfileList();
        }

        public Profile CopyProfile(int index)
        {
            // Get the profile to copy
            Profile profileToCopy = GoogleSheetSettings.instance.Profile[index];

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
            GoogleSheetSettings.instance.Profile.Profiles[index] = copied;

            // Refresh the profile list to reflect the changes
            RefreshProfileList();
        }

        #region Handlers

        // private void HandleClientSecretPathChanged(ChangeEvent<string> ev)
        // {
        //     DataLabsSheetsSettings.instance.ClientSecretPath = ev.newValue;
        // }

        private void HandleCredentialsPathChanged(ChangeEvent<string> ev)
        {
            GoogleSheetSettings.instance.CredentialsPath = ev.newValue;
        }

        private void HandleProfileChanged(DataLabsSheetsProfile newProfile)
        {
            GoogleSheetSettings.instance.Profile = newProfile;
            SetProfileGroupDisplayed(GoogleSheetSettings.instance.Profile != null);
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
            m_authenticator.CancelAuthentication();
            SetOAuthMessageDisplayed(false);
        }


        private void HandleAuthenticationComplete(ServiceAccountCredential credentials)
        {
            SetOAuthMessageDisplayed(false);
            m_credentials = credentials;
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

            GoogleSheetSettings.instance.Profile.AddNewProfile();
            m_selectedProfileIndex = GoogleSheetSettings.instance.Profile.Count - 1;
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
    }
}