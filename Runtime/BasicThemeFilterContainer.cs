using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Tomatech.RePalette
{
    public abstract class ThemeFilterBase
    {
        public abstract string ThemeKey { get; }
        public virtual string GetInheritedThemeKeys(System.Func<string, bool> validator) => validator(ThemeKey) ? ThemeKey : null;
        public Task<IResourceLocation> GetThemeAssetLocation<TObject>(string objectKey) where TObject : Object
        {
            return GetThemeAssetLocation(objectKey, typeof(TObject));
        }
        public virtual async Task<IResourceLocation> GetThemeAssetLocation(string objectKey, System.Type typeFilter)
        {
            var keyList = new List<string> { objectKey, ThemeKey};
            var locationHandle = Addressables.LoadResourceLocationsAsync(keyList, Addressables.MergeMode.Intersection, typeFilter).Task;
            await locationHandle;
            if (locationHandle.Result.Count == 0)
                return null;
            return locationHandle.Result[0];
        }
    }

    public abstract class ThemeFilterContainerBase : ScriptableObject
    {
        public abstract string EditorWindowFilterName { get; }
        public abstract ThemeFilterBase EditorWindowFilter { get; }
    }

    public class ThemeFilterContainer<TFilter> : ThemeFilterContainerBase where TFilter : ThemeFilterBase, new()
    {
        public override string EditorWindowFilterName => nameof(editorFilter);
        public override ThemeFilterBase EditorWindowFilter => editorFilter;
        public TFilter editorFilter = new();
    }

    public class BasicThemeFilterContainer : ThemeFilterContainer<BasicThemeFilter> { }

    [System.Serializable]
    public class BasicThemeFilter : ThemeFilterBase
    {
        public string themeKey;

        public override string ThemeKey => themeKey;
    }
}
