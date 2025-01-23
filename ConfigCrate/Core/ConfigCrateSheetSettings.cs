#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Voidex.DataLabs.DataLabs.GoogleSheets.Runtime;
using Voidex.DataLabs.GoogleSheets.Editor;
using FilePathAttribute = UnityEditor.FilePathAttribute;

namespace Voidex.DataLabs.ConfigCrate
{
    [UnityEditor.FilePath("Voidex.DataLabs/DataLabs/GoogleSheets/Configs/ConfigCrateSheetSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    //[CreateAssetMenu(fileName = "ConfigCrateSheetSettings", menuName = "Voidex/DataLabs/GoogleSheets/ConfigCrateSheetSettings", order = 0)]
    public class ConfigCrateSheetSettings : ScriptableSingleton<ConfigCrateSheetSettings>, IGoogleSheetSettings
    {
        public string CredentialsPath
        {
            get { return m_credentialsPath; }
            set
            {
                m_credentialsPath = value;
                Save(true);
            }
        }

        public DataLabsSheetsProfile Profile
        {
            get { return m_profile; }
            set
            {
                m_profile = value;
                Save(true);
            }
        }
        
        public string ServiceAccountEmail
        {
            get { return m_serviceAccountEmail; }
            set
            {
                m_serviceAccountEmail = value;
                Save(true);
            }
        }

        private string m_credentialsPath = "../Credentials/ondi-game-editor-071afcc35871.p12";
        [SerializeField] private DataLabsSheetsProfile m_profile = null;
        
        private string m_serviceAccountEmail = "unity-editor@ondi-game-editor.iam.gserviceaccount.com";
    }
}
#endif