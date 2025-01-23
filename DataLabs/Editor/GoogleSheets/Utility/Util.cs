using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Voidex.DataLabs.Dashboard;

namespace Voidex.DataLabs.GoogleSheets.Editor
{
    public static class Util
    {
        /// <summary>
        /// Generates a hash value from a string input
        /// using the Fowler-Noll-Vol algorithm from
        /// http://www.isthe.com/chongo/tech/comp/fnv/
        /// </summary>
        /// <param name="input">string used for hash</param>
        /// <returns>hash value of the input string</returns>
        public static uint FNVHash(string input)
        {
            uint hash = 2166136261;
            for (int ix = 0; ix < input.Length; ix++)
            {
                hash ^= input[ix];
                hash *= 16777619;
            }

            return hash;
        }

        public static TaskAwaiter GetAwaiter(this AsyncOperation operation)
        {
            var tcs = new TaskCompletionSource<object>();

            operation.completed += x => { tcs.SetResult(null); };
            return ((Task) tcs.Task).GetAwaiter();
        }

        public static bool IsAsset(this Type type)
        {
            return typeof(ScriptableObject).IsAssignableFrom(type) ||
                   typeof(Texture).IsAssignableFrom(type) ||
                   typeof(Sprite).IsAssignableFrom(type) ||
                   typeof(AudioClip).IsAssignableFrom(type) ||
                   typeof(GameObject).IsAssignableFrom(type);
            //|| typeof(AssetReference).IsAssignableFrom(type);
        }

        public static bool IsAssetReference(this Type type)
        {
            return typeof(AssetReference).IsAssignableFrom(type);
        }

         public const string SYMBOL_IGNORE = "#";
    }
}