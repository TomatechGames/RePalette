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
            var locationHandle = RepaletteResourceManager.ThemeFilter.GetThemeAssetLocation<T>(addressableKey);
            await locationHandle;
            IResourceLocation targetLocation = locationHandle.Result;
            if (targetLocation == themeAssetLocation)
                return themeAssetHandle.Value.Result.First(r => r.name == subAssetKey);
            if (targetLocation == null)
                return null;
            Debug.Log("has theme location");
            if (themeAssetHandle != null)
                Addressables.Release(themeAssetHandle.Value);
            var handle = Addressables.LoadAssetAsync<IList<T>>(targetLocation.PrimaryKey);
            await handle.Task;
            var filteredHandleResults = handle.Result.Where(r => r.name == subAssetKey).ToList();
            if (handle.Status == AsyncOperationStatus.Succeeded && filteredHandleResults.Count > 0)
            {
                Debug.Log("asset found");
                themeAssetHandle = handle;
                themeAssetLocation = targetLocation;
                return filteredHandleResults[0];
            }
            else
            {
                Debug.Log("asset not found: "+subAssetKey);
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