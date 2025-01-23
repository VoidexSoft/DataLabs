using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Voidex.DataLabs.Dashboard;
using Voidex.DataLabs;
using UnityEngine;

namespace Voidex.DataLabs.Dashboard
{
    public static class ListFilter
    {
        // *** PROPERTY SELECTION ***
        // Property Name

        // *** TYPES ***
        // (#) int
        // (#) float
        // (A) string
        // (A) enum

        // *** OPERATIONS ***
        // Includes / Exact (default, no operator)
        // Greater Than >
        // Less Than <
        // Between - (require lhs/rhs vals, only numerical properties)

        public enum FilterType
        {
            String,
            Float,
            Int
        }

        public enum FilterOp
        {
            Contains,
            GreaterThan,
            LessThan
        }

        public static List<string> FilterOpSymbols = new List<string>
        {
            "=",
            ">",
            "<"
        };

        public static List<string> GetFilterablePropertyNames()
        {
            Type targetType = DataLabsDashboard.CurrentSelectedGroup.SourceType;
            List<string> results = (from field in targetType.GetFields() where Attribute.IsDefined(field, typeof(DataLabsFilterableAttribute)) select field.Name).ToList();
            results.AddRange(from prop in targetType.GetProperties() where Attribute.IsDefined(prop, typeof(DataLabsFilterableAttribute)) select prop.Name);
            results.AddRange(from method in targetType.GetMethods() where Attribute.IsDefined(method, typeof(DataLabsFilterableAttribute)) select method.Name);
            return results;
        }

        /// <summary>
        /// Filters the current Group based on class field/property criteria.
        /// </summary>
        /// <returns>A list of DataEntity assets from the selected Group that meet the filter criteria.</returns>
        public static List<DataEntity> FilterList()
        {
            string input = DataLabsDashboard.Instance.GetAssetFilterPropertyValue();

            // if the list is too small, just return the list.
            if (DataLabsDashboard.CurrentSelectedGroup.Content.Count < 2 || string.IsNullOrEmpty(input)) return DataLabsDashboard.CurrentSelectedGroup.Content;

            List<DataEntity> filteredListResult = new List<DataEntity>();
            Type targetType = DataLabsDashboard.CurrentSelectedGroup.SourceType;
            FilterOp operation = DataLabsDashboard.Instance.GetAssetFilterOperation();
            FilterType filterType = DataLabsDashboard.Instance.GetAssetFilterPropertyType();

            // ********* FIGURE OUT THE OPERATOR ********* //
            if (filterType == FilterType.Float)
            {
                foreach (DataEntity asset in DataLabsDashboard.CurrentSelectedGroup.Content)
                {
                    FieldInfo field = targetType.GetField(DataLabsDashboard.Instance.GetAssetFilterPropertyName());
                    object val;
                    if (field == null)
                    {
                        PropertyInfo property = targetType.GetProperty(DataLabsDashboard.Instance.GetAssetFilterPropertyName());
                        if (property == null) continue; // it's not a field or a property.
                        val = property.GetValue(asset);
                    }
                    else val = field.GetValue(asset);

                    float assetValue = Convert.ToSingle(val);
                    float targetValue = DataLabsDashboard.Instance.AssetFilterValueFloat;

                    switch (operation)
                    {
                        case FilterOp.Contains:
                            if (Mathf.Approximately(assetValue, targetValue)) filteredListResult.Add(asset);
                            break;
                        case FilterOp.GreaterThan:
                            if (assetValue > targetValue) filteredListResult.Add(asset);
                            break;
                        case FilterOp.LessThan:
                            if (assetValue < targetValue) filteredListResult.Add(asset);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else if (filterType == FilterType.Int)
            {
                foreach (DataEntity asset in DataLabsDashboard.CurrentSelectedGroup.Content)
                {
                    FieldInfo field = targetType.GetField(DataLabsDashboard.Instance.GetAssetFilterPropertyName());
                    object val;
                    if (field == null)
                    {
                        PropertyInfo property = targetType.GetProperty(DataLabsDashboard.Instance.GetAssetFilterPropertyName());
                        if (property == null) continue; // it's not a field or a property.
                        val = property.GetValue(asset);
                    }
                    else val = field.GetValue(asset);

                    int assetValue = Convert.ToInt32(val);
                    int targetValue = DataLabsDashboard.Instance.AssetFilterValueInt;

                    switch (operation)
                    {
                        case FilterOp.Contains:
                            if (assetValue == targetValue) filteredListResult.Add(asset);
                            break;
                        case FilterOp.GreaterThan:
                            if (assetValue > targetValue) filteredListResult.Add(asset);
                            break;
                        case FilterOp.LessThan:
                            if (assetValue < targetValue) filteredListResult.Add(asset);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else if (filterType == FilterType.String)
            {
                foreach (DataEntity asset in DataLabsDashboard.CurrentSelectedGroup.Content)
                {
                    FieldInfo field = targetType.GetField(DataLabsDashboard.Instance.GetAssetFilterPropertyName());
                    object val;
                    if (field == null)
                    {
                        PropertyInfo property = targetType.GetProperty(DataLabsDashboard.Instance.GetAssetFilterPropertyName());
                        if (property == null) continue; // it's not a field or a property.
                        val = property.GetValue(asset);
                    }
                    else val = field.GetValue(asset);

                    string assetValue = Convert.ToString(val);
                    string targetValue = DataLabsDashboard.Instance.AssetFilterValueString;

                    if (assetValue.ToLower().Contains(targetValue.ToLower())) filteredListResult.Add(asset);
                }
            }

            return filteredListResult;
        }
    }
}