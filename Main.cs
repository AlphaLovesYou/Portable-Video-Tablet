using System;
using ABI.CCK.Components;
using HarmonyLib;
using MelonLoader;

namespace PortableVideoTablet
{
    public static class BuildInfo
    {
        public const string Name = "Portable Video Tablet";
        public const string Author = "Young Alpha"; 
        public const string Version = "1.0.0";
      
    }
    public class Main:MelonMod
    {
        public static MelonLogger.Instance Log;
        public override void OnApplicationStart()
        {
            Log = LoggerInstance;
        }
    }
        
    [HarmonyPatch(typeof(CVRVideoPlayer))]
    class CVRVideoPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void VideoPlayerStart(CVRVideoPlayer __instance)
        {
            try
            {
                
            }
            catch (Exception e)
            {
                Main.Log.Error(e);
            }
        }
    }
}
