using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voidex.DataLabs;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Voidex.DataLabs.Dashboard
{
    public class DatabaseBuilder : IPreprocessBuildWithReport
    {
        private static bool DUMPLOG = false;
        private static StringBuilder dump;

        public int callbackOrder => 100;

        public void OnPreprocessBuild(BuildReport report)
        {
            Reload();
        }

        public static void CallbackAfterScriptReload()
        {
            if (DataLab.Db == null)
            {
                Debug.LogWarning("Is this the first time loading DataLabs? You may want to restart the editor and run the Data Upgrader at 'Tools/Voidex/DataLabs/Data Upgrader'.");
                return;
            }

            Reload();
        }

        /// <summary>
        /// Rebuild lists of static groups, custom groups, and all data entities.
        /// </summary>
        [MenuItem("Tools/Voidex/DataLabs/DB Soft Reload", priority = 10)]
        public static void Reload()
        {
            List<DataEntity> entities = FindDataEntities();
            FindStaticGroups(entities);
            EditorUtility.SetDirty(DataLab.Db);
            AssetDatabase.SaveAssetIfDirty(DataLab.Db);
            AssetDatabase.Refresh();

            // check if any data has a key of int.MinValue, which is default unassigned and suggests a problem or old data.
            if (DataLab.Db.Get(int.MinValue))
            {
                bool pressedOk = EditorUtility.DisplayDialog(
                    "Invalid entries detected",
                    "A entry in the Database with the default state int.MinValue (-2147483648) was found. This may be because DataLabs was just upgraded, and is normal. \n\n" +
                    "Please set the DB Key Starting Value in the DataLabs Dashboard footer and then run '/Tools/Voidex/DataLabs/Data Upgrader' to upgrade any DB keys.",
                    "Ok");
            }
        }

        private static void FindStaticGroups(List<DataEntity> projectAssets)
        {
            if (DUMPLOG)
            {
                dump = new StringBuilder();
                dump.Append("DataLabs DATABASE BUILDER DUMP LOG\n [...]\n");
            }

            DataLab.Db.ClearStaticGroups();
            TypeCache.TypeCollection allTypesAvailable = TypeCache.GetTypesDerivedFrom(typeof(DataEntity));

            Dictionary<Type, List<DataEntity>> content = new Dictionary<Type, List<DataEntity>>();
            foreach (Type type in allTypesAvailable)
            {
                if (type.Name == "None") continue;
                if (!content.ContainsKey(type)) content.Add(type, new List<DataEntity>());
            }

            foreach (DataEntity asset in projectAssets)
            {
                Type assetType = asset.GetType();
                RecursiveGroupPopulation(content, asset, assetType);
            }

            foreach (KeyValuePair<Type, List<DataEntity>> dataGroup in content)
            {
                DataLabsStaticDataGroup group = new DataLabsStaticDataGroup(dataGroup.Key)
                {
                    SourceType = dataGroup.Key,
                    Content = dataGroup.Value
                };
                DataLab.Db.SetStaticGroup(group);
                if (DUMPLOG) dump.Append($" ----{group.SourceType.Name} [{group.Content.Count} entities] \n");
            }

            if (DUMPLOG) Debug.Log(dump);
        }

        private static List<DataEntity> FindDataEntities()
        {
            List<DataEntity> list = new List<DataEntity>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(DataEntity)}");
            list.AddRange(guids.Select(guid => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(DataEntity)) as DataEntity));

            DataLab.Db.ClearData();
            foreach (DataEntity x in list)
            {
                DataLab.Db.Add(x, false);
            }

            EditorUtility.SetDirty(DataLab.Db);
            return list;
        }

        private static void RecursiveGroupPopulation(Dictionary<Type, List<DataEntity>> content, DataEntity asset, Type assetType)
        {
            if (!content.ContainsKey(assetType)) content.Add(assetType, new List<DataEntity>());
            content[assetType].Add(asset);
            if (assetType.BaseType != typeof(ScriptableObject)) RecursiveGroupPopulation(content, asset, assetType.BaseType);
        }

        [MenuItem("Tools/Voidex/DataLabs/Data Key Upgrader (safe)", priority = 100)]
        public static void DataUpgrader()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Upgrade Data",
                "This will find every DataEntity in the project that has a key of int.MinValue and assign it a new one.",
                "Proceed",
                "Abort");
            if (!proceed) return;

            if (DataLab.Db == null)
            {
                Debug.Log("DB Not found. Please restart the editor and open DataLabs Dashboard.");
                return;
            }

            List<DataEntity> data = GetAllDataEntitiesOfTypeInProject(typeof(DataEntity));
            int changed = 0;
            foreach (DataEntity x in data.Where(x => x.GetDbKey() == int.MinValue))
            {
                x.SetDbKey(DataLab.Db.GenerateUniqueId());
                EditorUtility.SetDirty(x);
                changed++;
            }

            Reload();

            EditorUtility.DisplayDialog(
                "Complete",
                $"Changed {changed} entity keys.",
                "Ok");
        }

        [MenuItem("Tools/Voidex/DataLabs/Data Key Reset (danger)", priority = 100)]
        public static void DataReset()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Reset Data Keys",
                "This will find every DataEntity in the project and assign a new DB Key to each one.",
                "Proceed",
                "Abort");
            if (!proceed) return;

            if (DataLab.Db == null)
            {
                Debug.Log("DB Not found. Please restart the editor and open DataLabs Dashboard.");
                return;
            }

            List<DataEntity> data = GetAllDataEntitiesOfTypeInProject(typeof(DataEntity));
            int changed = 0;
            foreach (DataEntity x in data)
            {
                x.SetDbKey(DataLab.Db.GenerateUniqueId());
                EditorUtility.SetDirty(x);
                changed++;
            }

            Reload();

            EditorUtility.DisplayDialog(
                "Complete",
                $"Changed {changed} entity keys.",
                "Ok");
        }

        /// <summary>
        /// Forces a refresh of assets serialization.
        /// </summary>
        [MenuItem("Tools/Voidex/DataLabs/Reimport Assets - By Type (safe)", priority = 100)]
        public static void ReimportAllByType()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Reimport DataLabs Asset Files",
                $"Reimport all of the DataLabs Data Assets?\n\n" +
                $"This reimports all DataEntity type Assets. Won't fix issues related to mismatching class/file names.\n\n This is generally a safe operation.",
                "Proceed",
                "Abort!");

            if (!confirm) return;

            int count = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                string storage = DataLabsEditorUtility.GetPathToDataLabsStorageFolder();
                if (storage[storage.Length - 1] == '/') storage = storage.Remove(storage.Length - 1);
                string[] files = AssetDatabase.FindAssets("t:DataEntity", new[] {storage});
                for (int i = 0; i < files.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Importing...", AssetDatabase.GUIDToAssetPath(files[i]), (float) i / files.Length);
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(files[i]), ImportAssetOptions.ForceUpdate);
                    Debug.Log($"{AssetDatabase.GUIDToAssetPath(files[i])}");
                    count++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "Done reimporting",
                    $"{count} assets were reimported and logged to the console.",
                    "Great");
            }
        }

        /// <summary>
        /// Forces a refresh of assets serialization.
        /// </summary>
        [MenuItem("Tools/Voidex/DataLabs/Reimport Assets - By Name (safe)", priority = 100)]
        public static void ReimportAllByName()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Reimport DataLabs Asset Files",
                "Reimport all of the DataLabs Data Assets?\n\n" +
                "This reimports all files with names including 'Data-' which is the built-in prefix for saved DataLabs Files.\n\n This is generally a safe operation.",
                "Proceed",
                "Abort");

            if (!confirm) return;

            int count = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                string storage = DataLabsEditorUtility.GetPathToDataLabsStorageFolder();
                if (storage[storage.Length - 1] == '/') storage = storage.Remove(storage.Length - 1);
                string[] files = AssetDatabase.FindAssets("Data-", new[] {storage});
                for (int i = 0; i < files.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Importing...", AssetDatabase.GUIDToAssetPath(files[i]), (float) i / files.Length);
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(files[i]), ImportAssetOptions.ForceUpdate);
                    Debug.Log($"{AssetDatabase.GUIDToAssetPath(files[i])}");
                    count++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "Done reimporting",
                    $"{count} assets were reimported and logged to the console.",
                    "Great");
            }
        }
        
        //Check duplicate keys
        [MenuItem("Tools/Voidex/DataLabs/Check Duplicate Keys", priority = 100)]
        public static void CheckDuplicateKeys()
        {
            if (DataLab.Db == null)
            {
                Debug.LogWarning("Is this the first time loading DataLabs? You may want to restart the editor and run the Data Upgrader at 'Tools/Voidex/DataLabs/Data Upgrader'.");
                return;
            }

            List<DataEntity> data = GetAllDataEntitiesOfTypeInProject(typeof(DataEntity));
            Dictionary<int, DataEntity> keys = new Dictionary<int, DataEntity>();
            List<DataEntity> duplicates = new List<DataEntity>();
            foreach (DataEntity x in data)
            {
                if (keys.ContainsKey(x.GetDbKey()))
                {
                    duplicates.Add(x);
                    duplicates.Add(keys[x.GetDbKey()]);
                }
                else
                {
                    keys.Add(x.GetDbKey(), x);
                }
            }

            if (duplicates.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "No duplicates found",
                    "No duplicate keys were found in the project.",
                    "Ok");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Duplicate keys were found in the project.\n\n");
                foreach (DataEntity x in duplicates)
                {
                    sb.Append($"- {x.name} ({x.GetDbKey()})\n");
                }

                EditorUtility.DisplayDialog(
                    "Duplicates found",
                    sb.ToString(),
                    "Ok");
                
            }
        }

        [MenuItem("Tools/Voidex/DataLabs/Cleanup DataLabs (semi-safe)", priority = 100)]
        public static void CleanupStorageFolder()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Cleanup DataLabs",
                "This will check all asset files with the 'Data-' prefix and ensure validity. Invalid files can be deleted. This is primarily for identifying or removing assets which have broken script connections due to class name mismatches or class deletions. \n\n" +
                "You will be able to confirm delete/skip for each file individually.\n\n" +
                "You may NOT want to do this if the data found is broken accidentally and you're trying to restore it. This does not restore data, it validates assets and offers deletion if they are problematic. While this cleans up the project, it does DELETE the data asset file.\n",
                "Proceed",
                "Abort");

            if (!confirm) return;

            int found = 0;
            int deleted = 0;
            int failed = 0;
            int ignored = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                string storage = DataLabsEditorUtility.GetPathToDataLabsStorageFolder();
                if (storage[storage.Length - 1] == '/') storage = storage.Remove(storage.Length - 1);
                string[] files = AssetDatabase.FindAssets("Data-", new[] {storage});
                for (int i = 0; i < files.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Scanning...", AssetDatabase.GUIDToAssetPath(files[i]), (float) i / files.Length);

                    string path = AssetDatabase.GUIDToAssetPath(files[i]);
                    DataEntity dataFile = AssetDatabase.LoadAssetAtPath<DataEntity>(AssetDatabase.GUIDToAssetPath(files[i]));
                    if (dataFile == null)
                    {
                        found++;

                        // how the heck do i get the object if we're literally dealing with objects that don't cast.
                        //EditorGUIUtility.PingObject();

                        bool deleteFaulty = EditorUtility.DisplayDialog(
                            "Faulty file found",
                            $"{path}\n\n" +
                            "This file seems to be broken. Please check:\n\n" +
                            "* File is actually a DataLabs Data file.\n" +
                            "* Class file still exists.\n" +
                            "* Class filename matches class name.\n" +
                            "* Assemblies are not black-listed.\n\n" +
                            "What do you want to do?", "Delete file", "Ignore file");

                        if (deleteFaulty)
                        {
                            bool success = AssetDatabase.DeleteAsset(path);
                            if (success) deleted++;
                            else failed++;
                        }
                        else ignored++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    "Done cleaning.",
                    $"{found} assets were faulty.\n" +
                    $"{deleted} assets were deleted.\n" +
                    $"{failed} assets failed to delete.\n" +
                    $"{ignored} assets were ignored.\n",
                    "Excellent");
            }
        }

        public static List<DataLabsCustomDataGroup> GetAllCustomDataGroupAssets()
        {
            List<DataLabsCustomDataGroup> result = new List<DataLabsCustomDataGroup>();

            foreach (KeyValuePair<int, DataEntity> group in DataLab.Db.Data)
            {
                if (!(group.Value is DataLabsCustomDataGroup value)) continue;

                //Debug.Log($"Added '{group}' to custom group list");
                result.Add(value);
            }

            return result;
        }

        public static List<T> GetAllAssetsInProject<T>() where T : DataEntity
        {
            // The AssetDatabase does not work correctly during callback for assembly reload and script reload.
            // Use a direct method instead during those times.

            List<T> list = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                DataEntity asset = (DataEntity) AssetDatabase.LoadAssetAtPath(assetPath, typeof(T));
                list.Add(asset as T);
            }

            return list;
        }

        public static List<DataEntity> GetAllDataEntitiesOfTypeInProject(Type t)
        {
            // ~8ms per call

            List<DataEntity> list = new List<DataEntity>();
            string[] guids = AssetDatabase.FindAssets($"t:{t.FullName}");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                DataEntity asset = (DataEntity) AssetDatabase.LoadAssetAtPath(assetPath, typeof(DataEntity));
                list.Add(asset);
            }

            return list;
        }
    }
}