using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Tomatech.RePalette
{
    public class BasicThemeFilterContainer : ThemeFilterContainer<BasicThemeFilter> { }

    [System.Serializable]
    public class BasicThemeFilter : ThemeFilterBase
    {
        public string themeKey;

        public override string ThemeKey => themeKey;
    }
}
