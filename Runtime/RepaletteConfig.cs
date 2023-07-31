using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tomatech.RePalette
{
    public static partial class RepaletteConfig
    {
        public static void Log(object logObj)
        {
            if (UseDebug)
            {
                UnityEngine.Debug.Log(logObj);
            }
        }
        //public static void LogWarning(object logObj)
        //{
        //    if (UseDebug)
        //    {
        //        UnityEngine.Debug.LogWarning(logObj);
        //    }
        //}
        //public static void LogError(object logObj)
        //{
        //    if (UseDebug)
        //    {
        //        UnityEngine.Debug.LogError(logObj);
        //    }
        //}

        static ConfigData configData = new();
        internal static bool UseDebug
        {
            get
            {
                OverrideConfig(configData);
                return configData.useDebug;
            }
        }

        static partial void OverrideConfig(ConfigData useDebug);

        class ConfigData
        {
            public bool useDebug;
        }
    }
}
