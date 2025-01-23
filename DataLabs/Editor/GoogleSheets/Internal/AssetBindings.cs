using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Voidex.DataLabs.GoogleSheets.Editor;
using Voidex.DataLabs.GoogleSheets.Runtime;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public class AssetBindings
    {
        public ImportType ImportType { get; }
        public string PrimaryKey { get; }

        private Type m_assetType;

        private MethodInfo m_importMethod;
        private MethodInfo m_lateImportMethod;


        public AssetBindings(ILogger logger, Profile profile)
        {
            // must be an existing type
            Type assetType = Type.GetType(profile.AssetType, false);
            if (assetType == null)
            {
                logger.LogError($"Profile `{profile.ProfileName}' has a non-existing AssetType. Make sure it uses an existing, fully qualified name.");
                return;
            }

            // must be an UnityEngine.Object type
            if (!assetType.IsAsset())
            {
                logger.LogError($"{assetType.Name} must inherit from ScriptableObject and have a ContentAssetAttribute");
                return;
            }

            // must include a ContentAsset attribute on the class
            ContentAssetAttribute contentAsset = assetType.GetCustomAttribute<ContentAssetAttribute>();
            if (contentAsset == null)
            {
                logger.LogError($"{assetType.Name} must inherit from ScriptableObject and have a ContentAssetAttribute.");
                return;
            }

            m_assetType = assetType;
            ImportType = contentAsset.ImportType;
            PrimaryKey = contentAsset.PrimaryKey;

            if (ImportType == ImportType.Automatic)
            {
                // if automatic import type, find all of the Content fields/properties
                //SetupAutomatic(logger);
            }
            else if (ImportType == ImportType.Manual || ImportType == ImportType.Both)

            {
                // if manual import type, find the required Import and PostImport functions
                SetupManual(logger);
            }
        }

        public void Import(IImportUtility util)
        {
            if (ImportType == ImportType.Manual)
            {
                m_importMethod.Invoke(null, new object[1] {util});
            }
            else if (ImportType == ImportType.Automatic)
            {
                // initially, we ignore UnityEngine.Objects because
                // they are currently in the process of being created
                //ImportContent(util, m_valueAutoFields);
                ImportContent(util /*, m_arrayValueFields*/);
            }else if (ImportType == ImportType.Both)
            {
                ImportContent(util);
                m_importMethod.Invoke(null, new object[1] {util});
            }
        }

        public void LateImport(IImportUtility util)
        {
            if (ImportType == ImportType.Manual || ImportType == ImportType.Both)
            {
                m_lateImportMethod?.Invoke(null, new object[1] {util});
            }
            else 
            {
                // now that all imported assets are likely to
                // be created, we can do UnityEngine.Objects
                //ImportContent(util /*, m_assetAutoFields*/);
            }
        }

        private void SetupManual(ILogger logger)
        {
            m_importMethod = m_assetType.GetMethod("Import", BindingFlags.Public | BindingFlags.Static);
            if (m_importMethod == null)
            {
                logger.LogError($"ContentAsset `{m_assetType.Name}' requires a public static Import function with exactly one parameter of type IImportUtility");
            }
            else
            {
                ParameterInfo[] parameters = m_importMethod.GetParameters();
                if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != typeof(IImportUtility))
                {
                    logger.LogError($"ContentAsset `{m_assetType.Name}' requires a public static Import function with exactly one parameter of type IImportUtility");
                }
            }

            m_lateImportMethod = m_assetType.GetMethod("LateImport", BindingFlags.Public | BindingFlags.Static);
            if (m_lateImportMethod == null)
            {
                //logger.LogError($"ContentAsset `{m_assetType.Name}' requires a public static LateImport function with exactly one parameter of type IImportUtility");
            }
            else
            {
                ParameterInfo[] parameters = m_lateImportMethod.GetParameters();
                if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != typeof(IImportUtility))
                {
                    logger.LogError($"ContentAsset `{m_assetType.Name}' requires a public static LateImport function with exactly one parameter of type IImportUtility");
                }
            }
        }

        private void ImportContent(IImportUtility util)
        {
            if (util.DataSheets.Count == 0)
                return;

            string path = util.BuildAssetPath(m_assetType.Name, util.DataSheets[0].Id.WorksheetName, ".asset", util.AssetDirectory);
            UnityEngine.Object asset = util.FindOrCreateAsset(m_assetType, path);

            SheetReader.SetValues(util: util, assetType: m_assetType, assetInstance: asset);

            // //support multiple worksheets
            // /*Case 1: each field has a ArrayContentAttribute with worksheet name is NOT null or empty.
            //  We will get the field with ArrayContentAttribute with worksheet name and deserialize the data into the field.
            // */
            // var fieldWithAttributes1 = m_assetType.GetFields()
            //     .Where(f => Attribute.IsDefined(f, typeof(ArrayContentAttribute)) /*&& Attribute.IsDefined(f, typeof(DataOnSheetAttribute))*/).ToList();
            //
            // for (var i = 0; i < util.DataSheets.Count; i++)
            // {
            //     if (fieldWithAttributes1 != null)
            //     {
            //         //get the field with DataOnSheetAttribute with worksheet name
            //         var fieldWithArrayAttributes = fieldWithAttributes1
            //             .FirstOrDefault(f => (f.GetCustomAttribute<ArrayContentAttribute>().WorksheetName == util.DataSheets[i].Id.WorksheetName));
            //
            //         if (fieldWithArrayAttributes != null)
            //         {
            //             var rows = util.DataSheets[i].GetRows(PrimaryKey).ToList();
            //             var dataType = SheetReaderUtils.GetElementTypeFromFieldInfo(fieldWithArrayAttributes);
            //             var arrayObject = SheetReader.Deserialize(dataType, rows, fieldWithArrayAttributes, true);
            //
            //             fieldWithArrayAttributes.SetValue(asset, arrayObject as Array);
            //         }
            //     }
            //     else
            //     {
            //         util.LogWarning($"Field with ArrayContentAttribute with worksheet name {util.DataSheets[i].Id.WorksheetName} not found in {m_assetType.Name}");
            //
            //     }
            // }
            //
            // /*Case 2: each field has a ArrayContentAttribute with worksheet name is null or empty.
            //  We will get the field with ArrayContentAttribute with the first worksheet name in the profile.
            //  */
            // var fieldWithAttributes2 = m_assetType.GetFields()
            //     .FirstOrDefault(f => Attribute.IsDefined(f, typeof(ArrayContentAttribute)) && string.IsNullOrEmpty(f.GetCustomAttribute<ArrayContentAttribute>().WorksheetName)/*&& !Attribute.IsDefined(f, typeof(DataOnSheetAttribute))*/);
            //
            // if (fieldWithAttributes2 != null)
            // {
            //     var rows = util.DataSheets[0].GetRows(PrimaryKey).ToList();
            //
            //
            //     var dataType = SheetReaderUtils.GetElementTypeFromFieldInfo(fieldWithAttributes2);
            //     var arrayObject = SheetReader.Deserialize(dataType, rows, fieldWithAttributes2, true);
            //
            //     fieldWithAttributes2.SetValue(asset, arrayObject as Array);
            // }
            //
            // //field with content attribute only
            //
            // var fieldWithContentAttributes = m_assetType.GetFields()
            //     .Where(f => Attribute.IsDefined(f, typeof(ContentAttribute)) /*&& !string.IsNullOrEmpty(f.GetCustomAttribute<ArrayContentAttribute>().WorkSheetName)*/).ToList();
            // var defaultRows = util.DataSheets[0].GetRows(PrimaryKey).ToList();
            //
            // for (int i = 0; i < util.DataSheets.Count; i++)
            // {
            //     var singleWorksheetRows = util.DataSheets[i].GetRows(PrimaryKey).ToList();
            //     foreach (var fieldWithContentAttribute in fieldWithContentAttributes)
            //     {
            //         if (fieldWithContentAttribute.GetCustomAttribute<ContentAttribute>().WorksheetName == util.DataSheets[i].Id.WorksheetName)
            //         {
            //             var contentDataType = fieldWithContentAttribute.FieldType;
            //             bool isArray = false;
            //             if (contentDataType.IsArray)
            //             {
            //                 contentDataType = contentDataType.GetElementType();
            //                 isArray = true;
            //             }
            //
            //             var contentObject = SheetReader.DeserializeContent(asset, contentDataType, fieldWithContentAttribute, singleWorksheetRows, isArray);
            //
            //             fieldWithContentAttribute.SetValue(asset, contentObject);
            //             continue;
            //         }
            //         
            //         if (string.IsNullOrEmpty(fieldWithContentAttribute.GetCustomAttribute<ContentAttribute>().WorksheetName))
            //         {
            //             var contentDataType = fieldWithContentAttribute.FieldType;
            //             bool isArray = false;
            //             if (contentDataType.IsArray)
            //             {
            //                 contentDataType = contentDataType.GetElementType();
            //                 isArray = true;
            //             }
            //             var contentObject = SheetReader.DeserializeContent(asset, contentDataType, fieldWithContentAttribute, defaultRows, isArray);
            //
            //             fieldWithContentAttribute.SetValue(asset, contentObject);
            //         }
            //     }
            // }

            UnityEditor.EditorUtility.SetDirty(asset);

            UnityEngine.Debug.Log("Save asset: " + path);
        }

        /// <summary>
        /// Set fields' values of all fields with ContentAttribute
        /// </summary>
        /// <param name="util"></param>
        /// <param name="assetType"></param>
        /// <param name="assetInstance"></param>
        public static void SetValues(Voidex.DataLabs.GoogleSheets.Editor.IImportUtility util, Type assetType, UnityEngine.Object assetInstance)
        {
            var fieldWithContentAttributes = assetType.GetFields()
                .Where(f => System.Attribute.IsDefined(f, typeof(ContentAttribute)));

            var singleWorksheetRows = util.DataSheets[0].GetRows("db_key").ToList();
            foreach (var fieldWithContentAttribute in fieldWithContentAttributes)
            {
                var contentDataType = fieldWithContentAttribute.FieldType;
                bool isArray = false;
                if (contentDataType.IsArray)
                {
                    contentDataType = contentDataType.GetElementType();
                    isArray = true;
                }

                var contentObject = SheetReader.DeserializeContent(assetInstance, contentDataType, fieldWithContentAttribute, singleWorksheetRows, util, isArray);

                fieldWithContentAttribute.SetValue(assetInstance, contentObject);
            }
        }

        public static void SetValues(List<Row> rows, Voidex.DataLabs.GoogleSheets.Editor.IImportUtility util, Type assetType, UnityEngine.Object assetInstance,
            bool declareOnly = false)
        {
            var flags = declareOnly ? SheetReader.AUTO_BINDING_FLAGS | BindingFlags.DeclaredOnly : SheetReader.AUTO_BINDING_FLAGS;
            var fieldWithContentAttributes = assetType.GetFields(flags)
                .Where(f => System.Attribute.IsDefined(f, typeof(ContentAttribute)));

            var singleWorksheetRows = rows; //util.DataSheets[0].GetRows("db_key").ToList();
            foreach (var fieldWithContentAttribute in fieldWithContentAttributes)
            {
                var contentDataType = fieldWithContentAttribute.FieldType;
                bool isArray = false;
                if (contentDataType.IsArray)
                {
                    contentDataType = contentDataType.GetElementType();
                    isArray = true;
                }

                var contentObject = SheetReader.DeserializeContent(assetInstance, contentDataType, fieldWithContentAttribute, singleWorksheetRows, util, isArray);

                fieldWithContentAttribute.SetValue(assetInstance, contentObject);
            }
        }

        public static List<T> GetAllInstancesWithAttribute<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name); //FindAssets uses tags check documentation for more info
            List<T> a = new List<T>();
            for (int i = 0; i < guids.Length; i++) //probably could get optimized
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset.GetType().GetCustomAttributes(typeof(ContentAssetAttribute), true).Length > 0)
                {
                    a.Add(asset);
                }
            }

            return a;
        }
    }
}