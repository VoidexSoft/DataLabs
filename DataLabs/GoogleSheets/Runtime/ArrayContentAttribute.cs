using System;

namespace Voidex.DataLabs.GoogleSheets.Runtime
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ArrayContentAttribute : Attribute
    {
        /// <summary>
        /// Name of the worksheet to use for the field or property in the target object.
        /// </summary>
        public string WorksheetName { get; set; }
        
        public ArrayContentAttribute()
        {
        }
        
        public ArrayContentAttribute(string worksheetName)
        {
            WorksheetName = worksheetName;
        }
    }
}