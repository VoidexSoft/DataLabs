using System;

namespace Voidex.DataLabs.GoogleSheets.Runtime
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class VerticalContentListAttribute : Attribute
    {
        public string Alias { get; }
        public VerticalContentListAttribute()
        {
            Alias = string.Empty;
        }
        public VerticalContentListAttribute(string alias)
        {
            Alias = alias;
        }
    }
}