using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tomatech.RePalette.Editor
{
    public class RePaletteDatabase : ScriptableObject
    {
        protected ThemeFilterContainerBase themeFilter;
        public ThemeFilterContainerBase ThemeFilter { 
            get
            {
                if(themeFilter)
                    return themeFilter;
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)).ToList();
                themeFilter = subAssets
                    .Select(a=>a as ThemeFilterContainerBase)
                    .FirstOrDefault(a=>a);
                return themeFilter;
            }
            set => themeFilter = value; 
        }

        protected static readonly System.Type[] constraintTypeMap = { typeof(Object), typeof(Texture2D), typeof(ScriptableObject), typeof(TileBase), typeof(AudioClip) };

        public List<string> emptyCategoryGroups = new(); public List<ThemeAssetCategory> themeAssets = new();

        public virtual ThemeAssetCategory WindowSelectedCategory { get; set; }

        public virtual ThemeAssetEntry WindowSelectedEntry { get; set; }

        public virtual List<string> WindowSelectedSubAssets { get; set; }

        public static void ResetDatabaseCheck() => hasChecked = false;
        static bool hasChecked = false;
        static RePaletteDatabase m_Database;

        public static RePaletteDatabase Database
        {
            get
            {
                if (hasChecked)
                    return m_Database;
                if (!m_Database)
                {
                    Resources.LoadAll<RePaletteDatabase>("Settings");
                    var foundPaths = AssetDatabase.FindAssets("t:"+nameof(RePaletteDatabase));
                    if (foundPaths.Length > 0)
                        m_Database = AssetDatabase.LoadAssetAtPath<RePaletteDatabase>(AssetDatabase.GUIDToAssetPath(foundPaths[0]));
                }
                hasChecked = true;
                return m_Database;
            }
        }

        public virtual List<System.Type> ValidConstraints => constraintTypeMap.ToList();

        public ThemeAssetEntry GetAssetEntryFromAddressableKey(string key)
        {
            return themeAssets.Select(c => c.entries.FirstOrDefault(e => e.addressableKey == key)).FirstOrDefault(e => e != null);
        }

        public virtual System.Type GetEntryConstraintType(ThemeAssetEntry e)
        {
            return constraintTypeMap[e.constraintIndex];
        }
    }


    [System.Serializable]
    public class ThemeAssetCategory
    {
        public string path;
        public List<ThemeAssetEntry> entries = new();
        public ThemeAssetCategory(string path)
        {
            this.path = path;
        }
    }



    [System.Serializable]
    public class ThemeAssetEntry
    {
        //TODO: Obtain ReanimatorNode type
        //protected static readonly System.Type[] constraintTypeMap = { typeof(Object), typeof(Texture2D), typeof(ScriptableObject), typeof(TileBase), typeof(AudioClip) };

        public string humanKey;
        public int constraintIndex;
        //public virtual System.Type[] ConstrainableTypes => constraintTypeMap;
        //public virtual System.Type ConstrainedType => constraintTypeMap[constraintIndex];
        public string addressableKey;
        public List<string> enforcedSubAssets;
    }
}