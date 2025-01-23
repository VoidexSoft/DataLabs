using UnityEditor;
using UnityEngine;

namespace Voidex.DataLabs.Dashboard
{
    public static class DataLabsEditorUtility
    {
        private const string DataLabsLocatorFileGuid = "cb441f7bdab6b61459aa19cfd0138c36";
        private const string DataLabsItemPath = "DataLabsCoreStorage/";
        private static string m_labsPath;

        public static string GetPathToDataLabsRootFolder()
        {
            if (string.IsNullOrEmpty(m_labsPath)) return GetPathToDataLabsRootFolderCached();

            string assetsFolderPath = Application.dataPath;
            assetsFolderPath = assetsFolderPath.Substring(0, assetsFolderPath.Length - 6);
            bool exists = System.IO.File.Exists(assetsFolderPath + m_labsPath);

            return exists
                ? m_labsPath
                : GetPathToDataLabsRootFolderCached();
        }

        private static string GetPathToDataLabsRootFolderCached()
        {
            m_labsPath = AssetDatabase.GUIDToAssetPath(DataLabsLocatorFileGuid);
            m_labsPath = m_labsPath.Replace("DataLabsLocatorFile.cs", "");
            return m_labsPath;
        }

        public static string GetPathToDataLabsStorageFolder()
        {
            string result = GetPathToDataLabsRootFolder() + DataLabsItemPath;
            return result;
        }

        public static Texture2D GetEditorImage(string title)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{GetPathToDataLabsRootFolder()}/Editor/Icons/{title}.png");
        }
    }
}