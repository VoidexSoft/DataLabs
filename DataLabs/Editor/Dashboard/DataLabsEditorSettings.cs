using UnityEditor;
using UnityEngine;

namespace Voidex.DataLabs.Dashboard
{
    public static class DataLabsEditorSettings
    {
        public enum LabsData
        {
            CurrentAssetGuid, // guid of the current selected asset
            CurrentGroupName, // guid of the current selected group 
            BreadcrumbBarGuids, // guids
            SelectedAssetGuids, // guids
            SearchGroups, // the content of the search group field
            SearchAssets, // the content of the search asset field
            StartingKeyId, // the content of the search asset field
        }

        public static int GetInt(LabsData data)
        {
            int result = EditorPrefs.GetInt(data.ToString());
            return result;
        }

        public static string GetString(LabsData data)
        {
            string result = EditorPrefs.GetString(data.ToString());
            return result;
        }

        public static void SetInt(LabsData data, int value)
        {
            EditorPrefs.SetInt(data.ToString(), value);
        }

        public static void SetString(LabsData data, string value)
        {
            EditorPrefs.SetString(data.ToString(), value);
        }
    }
}