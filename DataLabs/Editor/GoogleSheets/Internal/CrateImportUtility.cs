using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Sheets.v4.Data;
using UnityEditor;
using UnityEngine;
using Voidex.DataLabs.Dashboard;
using Voidex.DataLabs.GoogleSheets.Editor;
using ILogger = Voidex.DataLabs.GoogleSheets.Editor.ILogger;

namespace Voidex.DataLabs.DataLabs.Editor.GoogleSheets.Internal
{
    public class CrateImportUtility : IImportUtility
    {
        private enum DirectoryType
        {
            Empty,
            TrailingSlash,
            NoSlash
        }

        public List<DataSheet> DataSheets { get; private set; }
        public Spreadsheet Spreadsheet { get; private set; }
        public string PrimaryKey { get; private set; }
        public string AssetDirectory { get; private set; }

        public bool HasErrors
        {
            get { return m_state.HasErrors; }
        }
        
        private const string FilePrefix = "Data-";

        private ImportState m_state;
        private DirectoryType m_directoryType;


        public CrateImportUtility(ImportState state)
        {
            m_state = state;
        }

        public void Reset(List<DataSheet> dataSheets, Spreadsheet spreadsheet, string primaryKey, string assetDirectory)
        {
            DataSheets = dataSheets;
            Spreadsheet = spreadsheet;
            PrimaryKey = primaryKey;
            AssetDirectory = assetDirectory.Trim();
            if (string.IsNullOrEmpty(AssetDirectory))
            {
                m_directoryType = DirectoryType.Empty;
            }
            else
            {
                char lastChar = AssetDirectory[^1];
                if (lastChar == '\\' || lastChar == '/')
                {
                    m_directoryType = DirectoryType.TrailingSlash;
                }
                else
                {
                    m_directoryType = DirectoryType.NoSlash;
                }
            }
        }


        public string BuildAssetPath(string classTitle, string worksheetName, string assetExtension = ".asset", string assetDirectory = "")
        {
            string fileName = $"{FilePrefix}{classTitle}-{Spreadsheet.Properties.Title}-{worksheetName}{assetExtension}";
            string assetPathAndName = Path.Combine(assetDirectory, fileName);
            return assetPathAndName;
        }

        public T FindOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            return FindOrCreateAsset(typeof(T), path) as T;
        }

        public ScriptableObject FindOrCreateAsset(Type type, string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, type);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance(type);
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(asset, path);
                // var dataEntity = asset as DataEntity;
                // dataEntity.Title = DataSheet.Id.WorksheetName;
                // DataLab.Db.Add(dataEntity, true);
                // DataLabsStaticDataGroup group = DataLab.Db.GetStaticGroup(type);
                // group.AddEntity(dataEntity);
                
                
                EditorUtility.SetDirty(asset);
            }

            return asset as ScriptableObject;
        }

        public bool FindAssetByName<T>(string name, out T asset) where T : UnityEngine.Object
        {
            bool success = FindAssetByName(typeof(T), name, out UnityEngine.Object obj);
            asset = obj as T;
            return success;
        }

        public bool FindAssetByName(Type type, string name, out UnityEngine.Object asset)
        {
            string[] guids = AssetDatabase.FindAssets($"{name} t:{type.FullName}");
            if (guids == null || guids.Length <= 0)
            {
                asset = null;
                return false;
            }

            asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), type);
            return true;
        }

        public void LogError(string error)
        {
            ((ILogger) m_state).LogError(error);
        }

        public void LogError(Exception error)
        {
            ((ILogger) m_state).LogError(error);
        }

        public void LogWarning(string warning)
        {
            ((ILogger) m_state).LogWarning(warning);
        }
    }
}