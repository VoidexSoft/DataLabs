#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using Voidex.DataLabs.GoogleSheets.Runtime;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public static class SheetReader
    {
        public const BindingFlags AUTO_BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty;
        
        public static object Deserialize(Type type, List<Row> rows, IImportUtility util, FieldInfo fieldInfo, bool isArray = true)
        {
            if (isArray)
            {
                //Support ArrayContentAttribute
                return CreateArray(type, rows, util, fieldInfo, true);
            }
            else
            {
                return CreateSingle(type, rows, util, fieldInfo);
            }
        }

        public static object DeserializeContent(object target, Type type, FieldInfo info, List<Row> rows, IImportUtility util, bool isArray = false, string format = "{0}")
        {
            object result = null;
            var table = CreateTable(rows);

            (bool primitiveArray, _) = GetPrimitiveArraySeparator(info);
            bool isPrimitive = SheetReaderUtils.IsPrimitive(info, primitiveArray); //primitive inline array
            //--------------------------------------------------------------------------------
            //4 cases that ContentAttribute supports
            //Case primitive inlined array on the target
            if (primitiveArray && isPrimitive)
            {
                var cols = rows[0].Values.ToArray();

                if (isPrimitive)
                {
                    string columnName = GetFieldColumnName(info, format);
                    if (table.TryGetValue(columnName, out var idx))
                    {
                        SetValuePrimitive(target, info, idx < cols.Length ? cols[idx] : string.Empty);
                        result = info.GetValue(target);
                    }
                    else
                    {
                        SetValuePrimitive(target, info, idx < cols.Length ? cols[idx] : string.Empty);
                        result = info.GetValue(target);
                        Debug.LogWarning("Key is not exist: " + columnName);
                    }
                }
            }
            //Case primitive on the target
            else if (!primitiveArray && isPrimitive)
            {
                //get the row that primary key is not empty
                string columnName = GetFieldColumnName(info, format);
                var cols = rows[0].Values.ToArray();
                if (table.TryGetValue(columnName, out var idx))
                {
                    //get the first row that primary key is not empty
                    //var row = rows.FirstOrDefault(r => !r.Values.ElementAt(idx).Equals(string.Empty));
                    SetValuePrimitive(target, info, idx < cols.Length ? cols[idx] : string.Empty);
                    result = info.GetValue(target);
                }
                else
                {
                    Debug.LogError("Key is not exist: " + columnName);
                }
            }
            //Case single object on the target (class, struct, record)
            else if (!isArray && !isPrimitive)
            {
                result = CreateSingle(type, rows, util, fieldInfo: info);
            }
            //primitive array on the target
            else if (isArray && !isPrimitive)
            {
                result = CreateArray(type, rows, util, info, false);
            }
            //Case array object on the target


            return result;
        }

        private static Dictionary<string, int> CreateTable(List<Row> rows)
        {
            Dictionary<string, int> table = new Dictionary<string, int>();

            var header = rows[0].Keys.ToArray();
            for (int i = 0; i < header.Count(); i++)
            {
                string id = header[i];
                if (SheetReaderUtils.IsValidKeyFormat(id))
                {
                    var camelId = SheetReaderUtils.ConvertSnakeCaseToCamelCase(id);

                    if (!table.ContainsKey(camelId))
                    {
                        table.Add(camelId, i);
                    }
                    else
                    {
                        throw new Exception("Key is duplicate: " + id);
                    }
                }
                else
                {
                    throw new Exception("Key is not valid: " + id);
                }
            }

            return table;
        }

        private static object CreateSingle(Type type, List<Row> rows, IImportUtility util, FieldInfo fieldInfo)
        {
            var table = CreateTable(rows);

            if (type.IsAsset())
            {
                return CreateUnityObject(0, 0, rows, util, table, type, fieldInfo);
            }else if (type.IsAssetReference())
            {
                return CreateAddressableReference(0, 0, rows, util, table, type, fieldInfo);
            }

            return Create(0, 0, rows, util, table, type);
        }

        private static object CreateArray(Type type, List<Row> rows, IImportUtility util, FieldInfo fieldInfo, bool hasWrapper)
        {
            var table = CreateTable(rows);
            string fieldSetValue = fieldInfo.Name;

            List<int> startRows = hasWrapper ? GetWrapperObjectIndices(0, rows) : CountNumberElement(0, 0, 0, rows);

            Array arrayValue = Array.CreateInstance(type, startRows.Count);

            var isPrimitive = SheetReaderUtils.IsPrimitive(type);
            if (isPrimitive)
            {
                if (table.TryGetValue(fieldSetValue, out var idx))
                {
                    for (int i = 0; i < arrayValue.Length; i++)
                    {
                        object rowData = GetPrimitiveValue(type, rows[startRows[i]].Values.ElementAt(idx));
                        arrayValue.SetValue(rowData, i);
                    }
                }
                else
                {
                    throw new Exception($"Not found field to set value: {fieldSetValue}");
                }
            }
            else
            {
                for (int i = 0; i < arrayValue.Length; i++)
                {
                    if (type.IsAsset())
                    {
                        object rowData = CreateUnityObject(startRows[i], 0, rows, util, table, type, fieldInfo);
                        arrayValue.SetValue(rowData, i);
                    }
                    else if (type.IsAssetReference())
                    {
                        object rowData = CreateAddressableReference(startRows[i], 0, rows, util, table, type, fieldInfo);
                        arrayValue.SetValue(rowData, i);
                    }
                    else
                    {
                        object rowData = isPrimitive ? GetPrimitiveValue(type, rows[startRows[i]].Values.ElementAt(0)) : Create(startRows[i], 0, rows, util, table, type);
                        arrayValue.SetValue(rowData, i);
                    }
                    // object rowData = isPrimitive ? 
                    //     GetPrimitiveValue(type, rows[startRows[i]].Values.ElementAt(0)) : 
                    //     Create(startRows[i], 0, rows, table, type);
                    // arrayValue.SetValue(rowData, i);
                }
            }

            return arrayValue;
        }

        static UnityEngine.Object FindAssetByName(Type type, string name)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{name} t:{type.Name}");
            if (guids == null || guids.Length <= 0)
            {
                return null;
            }

            return UnityEditor.AssetDatabase.LoadAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]), type);
        }

        static object CreateUnityObject(int index, int parentIndex, List<Row> rows, IImportUtility util, Dictionary<string, int> table, Type type, FieldInfo wrapperField,
            string format = "{0}")
        {
            // Check if the type is a Unity object
            if (!type.IsAsset())
            {
                throw new ArgumentException("Type must be a Unity object");
            }

            // single unity object on the target
            if (!type.IsArray)
            {
                var columnName = GetFieldColumnName(wrapperField, format);
                if (!table.TryGetValue(columnName, out var idx))
                {
                    throw new Exception($"Key is not exist: {columnName}");
                }

                UnityEngine.Object singleObject = FindAssetByName(type, rows[index].Values.ElementAt(idx));
                // If the object is a ScriptableObject, we need to deserialize the rơw data into the object
                if (typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    SetValues(util, type, singleObject);
                    UnityEditor.EditorUtility.SetDirty(singleObject);
                }

                return singleObject;
            }


            // Array unity object on the target
            var wrapperColumnName = GetFieldColumnName(wrapperField, format);
            if (!table.TryGetValue(wrapperColumnName, out var wrapperIndex))
            {
                throw new Exception($"Key is not exist: {wrapperColumnName}");
            }


            string objectName = rows[index].Values.ElementAt(wrapperIndex);

            object variable = FindAssetByName(type, objectName);

            if (variable == null)
            {
                throw new Exception($"Unity object of type '{type.Name}' and name '{objectName}' not found");
            }

            FieldInfo[] fieldInfo = type.GetFields(AUTO_BINDING_FLAGS);

            //var cols = rows[index].Values.ToArray();
            foreach (FieldInfo info in fieldInfo)
            {
                var ignoredAttributes = info.GetCustomAttributes(typeof(IgnoreColumnAttribute), true);
                if (ignoredAttributes.Length > 0) continue;

                if (info.FieldType.IsAsset())
                {
                    return CreateUnityObject(index, parentIndex, rows, util, table, info.FieldType, info);
                }
                else if (info.FieldType.IsAssetReference())
                {
                    return CreateAddressableReference(index, parentIndex, rows, util, table, info.FieldType, info);
                }
                else
                {
                    return Create(index, parentIndex, rows, util, table, type, format);
                }
            }

            return variable;
        }

        static object CreateAddressableReference(int index, int parentIndex, List<Row> rows, IImportUtility util, Dictionary<string, int> table, Type type, FieldInfo wrapperField,
            string format = "{0}")
        {
            // Check if the type is a Unity object
            if (!type.IsAssetReference())
            {
                throw new ArgumentException("Type must be a Unity object");
            }

            // single unity object on the target
            if (!type.IsArray)
            {
                var columnName = GetFieldColumnName(wrapperField, format);
                if (!table.TryGetValue(columnName, out var idx))
                {
                    throw new Exception($"Key is not exist: {columnName}");
                }

                object assetRef = StringConverter.Convert(type, rows[index].Values.ElementAt(idx));
                return assetRef;
            }
            else
            {
                return CreateArray(type, rows, util, wrapperField, false);
            }
        }

        static object Create(int index, int parentIndex, List<Row> rows, IImportUtility util, Dictionary<string, int> table, Type type, string format = "{0}")
        {
            object variable = null; // = Activator.CreateInstance(type);
            if (!type.IsAsset())
            {
                variable = Activator.CreateInstance(type);
            }

            FieldInfo[] fieldInfo = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var cols = rows[index].Values.ToArray();
            foreach (FieldInfo info in fieldInfo)
            {
                var ignoredAttributes = info.GetCustomAttributes(typeof(IgnoreColumnAttribute), true);
                if (ignoredAttributes.Length > 0) continue;
                (bool primitiveArray, _) = GetPrimitiveArraySeparator(info);
                bool isPrimitive = SheetReaderUtils.IsPrimitive(info, primitiveArray);
                if (isPrimitive)
                {
                    string columnName = GetFieldColumnName(info, format);
                    if (table.TryGetValue(columnName, out var idx))
                    {
                        SetValuePrimitive(variable, info, idx < cols.Length ? cols[idx] : string.Empty);
                    }
                    else
                    {
                        Debug.LogError("Key is not exist: " + columnName);
                    }
                }
                else
                {
                    var fieldFormat = GetColumnFormat(info);

                    if (info.FieldType.IsArray)
                    {
                        var elementType = SheetReaderUtils.GetElementTypeFromFieldInfo(info);

                        string columnName = GetFieldColumnName(info, format);

                        int objectIndex;

                        var isElementPrimitive = SheetReaderUtils.IsPrimitive(elementType);

                        if (isElementPrimitive)
                        {
                            if (table.TryGetValue(columnName, out var value))
                            {
                                objectIndex = value;
                                Assert.IsTrue(objectIndex < cols.Length);
                            }
                            else
                            {
                                throw new Exception($"Key is not exist: {columnName}");
                            }
                        }
                        else
                        {
                            objectIndex = GetObjectIndex(elementType, table, fieldFormat);
                        }

                        var startRows = CountNumberElement(index, objectIndex, parentIndex, rows);

                        Array arrayValue = Array.CreateInstance(elementType, startRows.Count);

                        for (int i = 0; i < arrayValue.Length; i++)
                        {
                            if (isElementPrimitive)
                            {
                                var value = rows[startRows[i]].Values.ElementAt(objectIndex);
                                var primitiveValue = GetPrimitiveValue(elementType, value);
                                arrayValue.SetValue(primitiveValue, i);
                            }
                            else
                            {
                                var value = Create(startRows[i], objectIndex, rows, util, table, elementType, fieldFormat);
                                arrayValue.SetValue(value, i);
                            }
                        }


                        info.SetValue(variable, arrayValue);
                    }
                    else
                    {
                        var typeName = info.FieldType.FullName;
                        if (typeName == null)
                        {
                            throw new Exception("Full name is nil");
                        }

                        Type elementType = SheetReaderUtils.GetType(typeName);
                        //Type elementType = tmp.FieldType;

                        var objectIndex = GetObjectIndex(elementType, table);

                        var value = Create(index, objectIndex, rows, util, table, elementType, fieldFormat);

                        info.SetValue(variable, value);
                    }
                }
            }

            return variable;
        }

        static void SetValuePrimitive(object variable, FieldInfo fieldInfo, string value)
        {
            var type = fieldInfo.FieldType;

            if (string.IsNullOrEmpty(value))
            {
                value = GetDefaultValue(fieldInfo);
            }

            if (type.IsArray)
            {
                (_, string customSeparator) = GetPrimitiveArraySeparator(fieldInfo);
                var arrayValue = StringConverter.Convert(type, value, customSeparator);

                fieldInfo.SetValue(variable, arrayValue);
            }
            else
            {
                var primitiveValue = GetPrimitiveValue(type, value);
                fieldInfo.SetValue(variable, primitiveValue);
            }
        }

        private static object GetPrimitiveValue(Type type, string value)
        {
            var converted = StringConverter.Convert(type, value);
            if (converted == null)
            {
                Debug.LogWarning($"Failed to convert value '{value}' to type '{type.Name}'");
            }

            return converted;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        private static int GetObjectIndex(Type type, Dictionary<string, int> table, string format = "{0}")
        {
            int minIndex = int.MaxValue;
            FieldInfo[] fieldInfo = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo tmp in fieldInfo)
            {
                var fieldName = GetFieldColumnName(tmp, format);
                if (table.TryGetValue(fieldName, out var idx))
                {
                    if (idx < minIndex)
                        minIndex = idx;
                }
                else
                {
                    //Debug.Log("Miss " + tmp.Name);
                }
            }

            return minIndex;
        }

        //element count of the created array, and the start row index of the array
        //rowIndex: the row index of the current object
        //objectIndex: the column index of the current object
        //parentIndex: the row index of the first element of the current object.
        /*
        TODO:
        1. Find the first column for the object and pass it to objectIndex
        2. Find the first row for the object and pass it to parentIndex

         */
        private static List<int> CountNumberElement(int rowIndex, int objectIndex, int parentIndex,
            List<Row> rows)
        {
            //int count = 0;
            var startRows = new List<int>();

            for (int i = rowIndex; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count > objectIndex || !row.Values.ElementAt(objectIndex).Equals("#"))
                {
                    if (objectIndex == parentIndex)
                    {
                        //count++;
                        startRows.Add(i);
                    }
                    else if (row.Values.Count() > parentIndex && string.IsNullOrEmpty(row.Values.ElementAt(parentIndex)) || i == rowIndex)
                    {
                        //count++;
                        startRows.Add(i);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return (startRows);
        }

        private static List<int> GetWrapperObjectIndices(int rowIndex, List<Row> rows)
        {
            //int count = 0;
            var startRows = new List<int>();

            for (int i = rowIndex; i < rows.Count; i++)
            {
                var row = rows[i];
                //if any row has primary key is not empty, then count it
                if (!string.IsNullOrEmpty(row.PrimaryValue))
                {
                    //count++;
                    startRows.Add(i);
                }
            }

            return startRows;
        }

        private static string GetFieldColumnName(FieldInfo fieldInfo, string format)
        {
            object[] attributes = fieldInfo.GetCustomAttributes(typeof(ColumnNameAttribute), true);

            if (attributes.Length > 0)
            {
                ColumnNameAttribute columnNameAttribute = (ColumnNameAttribute) attributes[0];
                return SheetReaderUtils.ConvertSnakeCaseToCamelCase(string.Format(format, columnNameAttribute.ColumnName));
            }

            var r = SheetReaderUtils.ConvertSnakeCaseToCamelCase(string.Format(format, fieldInfo.Name));
            return r;
        }

        private static string GetColumnFormat(FieldInfo fieldInfo)
        {
            object[] attributes = fieldInfo.GetCustomAttributes(typeof(ColumnNameFormatAttribute), true);
            if (attributes.Length > 0)
            {
                ColumnNameFormatAttribute columnNameFormatAttribute = (ColumnNameFormatAttribute) attributes[0];
                return columnNameFormatAttribute.ColumnFormat;
            }

            return "{0}";
        }

        private static (bool, string) GetPrimitiveArraySeparator(FieldInfo fieldInfo)
        {
            object[] attributes = fieldInfo.GetCustomAttributes(typeof(PrimitiveInlinedArrayContent), true);
            if (attributes.Length > 0)
            {
                PrimitiveInlinedArrayContent primitiveInlinedArrayContent = (PrimitiveInlinedArrayContent) attributes[0];

                return (true, primitiveInlinedArrayContent.CustomSeparator);
            }

            var defaultSeparator = "*";

            return (false, defaultSeparator);
        }

        private static string GetDefaultValue(FieldInfo fieldInfo)
        {
            object[] attributes = fieldInfo.GetCustomAttributes(typeof(DefaultValueAttribute), true);
            if (attributes.Length > 0)
            {
                DefaultValueAttribute defaultValueAttribute = (DefaultValueAttribute) attributes[0];

                return defaultValueAttribute.Value.ToString();
            }

            var type = fieldInfo.FieldType;

            if (type == typeof(string))
            {
                return string.Empty;
            }

            if (type.IsNumeric())
            {
                return "0";
            }

            if (type == typeof(bool))
            {
                return "FALSE";
            }

            if (type.IsEnum)
            {
                return SheetReaderUtils.GetDefaultValue(type).ToString();
            }

            throw new Exception($"{type.FullName} {fieldInfo.Name} is not support default value. Current support (string, numeric, enum, true/false");
        }

        public static void SetValues(Voidex.DataLabs.GoogleSheets.Editor.IImportUtility util, Type assetType, UnityEngine.Object assetInstance)
        {
            //support multiple worksheets
            /*Case 1: each field has a ArrayContentAttribute with worksheet name is NOT null or empty.
             We will get the field with ArrayContentAttribute with worksheet name and deserialize the data into the field.
            */
            var fieldWithAttributes1 = assetType.GetFields(AUTO_BINDING_FLAGS)
                .Where(f => Attribute.IsDefined(f, typeof(ArrayContentAttribute))).ToList();

            for (var i = 0; i < util.DataSheets.Count; i++)
            {
                if (fieldWithAttributes1 != null)
                {
                    //get the field with DataOnSheetAttribute with worksheet name
                    var fieldWithArrayAttributes = fieldWithAttributes1
                        .FirstOrDefault(f => (f.GetCustomAttribute<ArrayContentAttribute>().WorksheetName == util.DataSheets[i].Id.WorksheetName));

                    if (fieldWithArrayAttributes != null)
                    {
                        var rows = util.DataSheets[i].GetRows(util.PrimaryKey).ToList();
                        var dataType = SheetReaderUtils.GetElementTypeFromFieldInfo(fieldWithArrayAttributes);
                        var arrayObject = SheetReader.Deserialize(dataType, rows, util, fieldWithArrayAttributes, true);

                        fieldWithArrayAttributes.SetValue(assetInstance, arrayObject as Array);
                    }
                }
                else
                {
                    util.LogWarning($"Field with ArrayContentAttribute with worksheet name {util.DataSheets[i].Id.WorksheetName} not found in {assetType.Name}");
                }
            }

            /*Case 2: each field has a ArrayContentAttribute with worksheet name is null or empty.
             We will get the field with ArrayContentAttribute with the first worksheet name in the profile.
             */
            var fieldWithAttributes2 = assetType.GetFields(AUTO_BINDING_FLAGS)
                .FirstOrDefault(f => Attribute.IsDefined(f, typeof(ArrayContentAttribute)) && string.IsNullOrEmpty(f.GetCustomAttribute<ArrayContentAttribute>().WorksheetName));

            if (fieldWithAttributes2 != null)
            {
                var rows = util.DataSheets[0].GetRows(DataSheet.DEFAULT_PRIMARY_KEY).ToList();


                var dataType = SheetReaderUtils.GetElementTypeFromFieldInfo(fieldWithAttributes2);
                var arrayObject = SheetReader.Deserialize(dataType, rows, util, fieldWithAttributes2, true);

                fieldWithAttributes2.SetValue(assetInstance, arrayObject as Array);
            }

            //field with content attribute only

            var fieldWithContentAttributes = assetType.GetFields(AUTO_BINDING_FLAGS)
                .Where(f => Attribute.IsDefined(f, typeof(ContentAttribute))).ToList();
            var defaultRows = util.DataSheets[0].GetRows(DataSheet.DEFAULT_PRIMARY_KEY).ToList();

            for (int i = 0; i < util.DataSheets.Count; i++)
            {
                var singleWorksheetRows = util.DataSheets[i].GetRows(DataSheet.DEFAULT_PRIMARY_KEY).ToList();
                foreach (var fieldWithContentAttribute in fieldWithContentAttributes)
                {
                    if (fieldWithContentAttribute.GetCustomAttribute<ContentAttribute>().WorksheetName == util.DataSheets[i].Id.WorksheetName)
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
                        continue;
                    }

                    if (string.IsNullOrEmpty(fieldWithContentAttribute.GetCustomAttribute<ContentAttribute>().WorksheetName))
                    {
                        var contentDataType = fieldWithContentAttribute.FieldType;
                        bool isArray = false;
                        if (contentDataType.IsArray)
                        {
                            contentDataType = contentDataType.GetElementType();
                            isArray = true;
                        }

                        var contentObject = SheetReader.DeserializeContent(assetInstance, contentDataType, fieldWithContentAttribute, defaultRows, util, isArray);

                        fieldWithContentAttribute.SetValue(assetInstance, contentObject);
                    }
                }
            }
        }
    }
}
#endif