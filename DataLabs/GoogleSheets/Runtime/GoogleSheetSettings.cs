#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Voidex.DataLabs.DataLabs.GoogleSheets.Runtime;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    [FilePath("Voidex.DataLabs/DataLabs/GoogleSheets/Configs/DataLabsSheetsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class GoogleSheetSettings : ScriptableSingleton<GoogleSheetSettings>, IGoogleSheetSettings
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