#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using StringConverter = Voidex.DataLabs.GoogleSheets.Editor.StringConverter;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public static class SheetReaderUtils
    {
        private static readonly HashSet<Type> s_numericTypes = new() {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),  
            typeof(uint), typeof(float)
        };
        
        public static Type GetElementTypeFromFieldInfo(FieldInfo tmp)
        {
            string fullName = string.Empty;
            if (tmp.FieldType.IsArray)
            {
                if (tmp.FieldType.FullName != null)
                    fullName = tmp.FieldType.FullName.Substring(0, tmp.FieldType.FullName.Length - 2);
            }
            else
            {
                fullName = tmp.FieldType.FullName;
            }

            return GetType(fullName);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="strFullyQualifiedName"></param>
        /// <returns></returns>
        public static Type GetType(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type == null)
            {
                
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(strFullyQualifiedName);
                    if (type != null)
                        break;
                }
            }

            if (type == null)
            {
                throw new Exception("Type is null: " + strFullyQualifiedName);
            }

            return type;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsValidKeyFormat(string key)
        {
            return key.Equals(key.ToLower());
        }

        /// <summary>
        /// Use to check variable and array variables is Primitive or not.
        /// Can't use IsClass or IsPrimitive because Array is always a class.
        /// Want to check the real type of element in array
        /// </summary>
        /// <param name="tmp"></param>
        /// <param name="isCustomPrimitiveArray"></param>
        /// <returns></returns>
        public static bool IsPrimitive(FieldInfo tmp, bool isCustomPrimitiveArray)
        {
            Type type = tmp.FieldType.IsArray && isCustomPrimitiveArray ? GetElementTypeFromFieldInfo(tmp) : tmp.FieldType;

            return IsPrimitive(type);
        }

        public static bool IsPrimitive(Type type)
        {
            return StringConverter.IsSimpleConvertable(type) || type.IsEnum;
        }
    
        public static bool IsNumeric(this Type myType)
        {
            return s_numericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }

        public static string ConvertSnakeCaseToCamelCase(string snakeCase)
        {
            var strings = snakeCase.Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
            var result = strings[0];
            for (int i = 1; i < strings.Length; i++)
            {
                var currentString = strings[i];
                result += char.ToUpperInvariant(currentString[0]) +
                          currentString.Substring(1, currentString.Length - 1);
            }

            return result;
        }

        public static IEnumerable<string> GetAllArrayField(string className)
        {
            return GetType(className)
                ?.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(info => info.FieldType.IsArray).Select(info => info.Name);
        }
        
        public static IEnumerable<string> GetAllFields(string className)
        {
            return GetType(className)
                ?.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(info => info.Name);
        }

        public static IEnumerable<string> GetAllMethod(string className)
        {
            return GetType(className)
                ?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                             BindingFlags.DeclaredOnly).Select(method => method.Name);
        }
        
        public static object GetDefaultValue(Type enumType)
        {
            var attribute = enumType.GetCustomAttribute<DefaultValueAttribute>(inherit: false);
            if (attribute != null)
                return attribute.Value;

            var innerType = enumType.GetEnumUnderlyingType();
            var zero = Activator.CreateInstance(innerType);
            if (enumType.IsEnumDefined(zero))
                return zero;

            var values = enumType.GetEnumValues();
            return values.GetValue(0);
        }
        
        // public static void SetNestedFields(object target, Dictionary<string, List<AssetBindings.ContentBinding>> bindings, object obj)
        // {
        //     foreach (KeyValuePair<string, List<AssetBindings.ContentBinding>> item in bindings)
        //     {
        //         foreach (AssetBindings.ContentBinding binding in item.Value)
        //         {
        //             var array = obj as Array;
        //             binding.SetValue(target, array);
        //         }
        //     }
        //
        //     foreach (FieldInfo field in target.GetType().GetFields())
        //     {
        //         if (field.FieldType.IsPrimitive || field.FieldType == typeof(string))
        //             continue;
        //
        //         object fieldValue = field.GetValue(target);
        //         if (fieldValue != null)
        //             SetNestedFields(fieldValue, bindings, obj);
        //     }
        // }
    }
}
#endif
