using System;

namespace Voidex.DataLabs.GoogleSheets.Runtime
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class PrimitiveInlinedArrayContent : Attribute
    {
        public readonly string CustomSeparator;

        public PrimitiveInlinedArrayContent(string customSeparator = "*")
        {
            this.CustomSeparator = customSeparator;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ColumnNameAttribute : Attribute
    {
        #region Properties

        public string ColumnName { get; set; }

        #endregion Properties
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IgnoreColumnAttribute : Attribute
    {
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ColumnNameFormatAttribute : Attribute
    {
        #region Properties

        public string ColumnFormat { get; set; }

        #endregion Properties
    }
}