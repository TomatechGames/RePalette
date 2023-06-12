using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Tomatech.RePalette
{
    [System.Serializable]
    public class ThemeAssetReference<T> where T : Object
    {
        IResourceLocation themeAssetLocation;
        AsyncOperationHandle<IList<T>>? themeAssetHandle;

        [SerializeField]
        string addressableKey;
        [SerializeField]
        string subAssetKey;

        public async Task<T> GetAsset()
        {
            if (RepaletteResourceManager.ThemeFilter == null)
                return null;
            var locationHandle = RepaletteResourceManager.ThemeFilter.GetThemeAssetLocation<T>(addressableKey);
            await locationHandle;
            IResourceLocation targetLocation = locationHandle.Result;
            if (targetLocation == null)
                return null;
            Debug.Log("has theme location");
            if (targetLocation == themeAssetLocation)
                return themeAssetHandle.Value.Result.First(r => subAssetKey == "" || r.name == subAssetKey);
            Debug.Log("theme not cached");
            if (themeAssetHandle != null)
                Addressables.Release(themeAssetHandle.Value);
            var handle = Addressables.LoadAssetAsync<IList<T>>(targetLocation.PrimaryKey);
            await handle.Task;

            if (subAssetKey == "")
            {
                if (handle.Result.Count > 0 && handle.Result.FirstOrDefault(l => l) is T typedMainAsset)
                {
                    Debug.Log("main asset found");
                    themeAssetHandle = handle;
                    themeAssetLocation = targetLocation;
                    return typedMainAsset;
                }
                Debug.Log("main asset not found");
                Debug.Log(string.Join(", ", handle.Result));
                themeAssetHandle = null;
                themeAssetLocation = null;
                return null;
            }

            var filteredHandleResults = handle.Result.Where(r => r.name == subAssetKey).ToList();
            if (handle.Status == AsyncOperationStatus.Succeeded && filteredHandleResults.Count > 0 && filteredHandleResults.FirstOrDefault(l => l) is T typedSubAsset)
            {
                Debug.Log("sub asset found");
                themeAssetHandle = handle;
                themeAssetLocation = targetLocation;
                return typedSubAsset;
            }
            else
            {
                Debug.Log("sub asset not found: "+subAssetKey);
                themeAssetHandle = null;
                themeAssetLocation = null;
            }
            return null;
        }

        public void ReleaseAsset()
        {
            if (themeAssetHandle != null)
                Addressables.Release(themeAssetHandle.Value);
        }
    }
}