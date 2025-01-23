using System;
using UnityEngine;

namespace Voidex.DataLabs
{
    /// <summary>
    /// <para>Use on Fields and Properties to expose them to the Dashboard filtering system.</para>
    /// <para>Supports string, float and int types.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DataLabsFilterableAttribute : PropertyAttribute
    {
    }
}