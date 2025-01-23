using System;
using UnityEngine;

namespace Voidex.DataLabs
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.GenericParameter | AttributeTargets.Property)]
    public class SpritePreviewAttribute : PropertyAttribute
    {
        public SpritePreviewAttribute()
        {
        }
    }
}