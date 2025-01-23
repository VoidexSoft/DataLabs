#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;

namespace Voidex.DataLabs.Editor.GoogleSheets.Internal
{
    public static class AddressablesUtils
    {
        public static AssetReference GetAssetReferenceFromAddress(string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (var group in settings.groups)
            {
                foreach (var entry in group.entries)
                {
                    if (entry.address == address)
                    {
                        return new AssetReference(entry.guid);
                    }
                }
            }

            return null;
        }
        
        public static AssetReferenceT<T> GetAssetReferenceFromAddress<T>(string address) where T : UnityEngine.Object
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (var group in settings.groups)
            {
                foreach (var entry in group.entries)
                {
                    if (entry.address == address)
                    {
                        return new AssetReferenceT<T>(entry.guid);
                    }
                }
            }

            return null;
        }
        
        public static AssetReferenceGameObject GetAssetReferenceGameObjectFromAddress(string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (var group in settings.groups)
            {
                foreach (var entry in group.entries)
                {
                    if (entry.address == address)
                    {
                        return new AssetReferenceGameObject(entry.guid);
                    }
                }
            }

            return null;
        }
    }
#endif
}