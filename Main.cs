using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABI_RC.Core.Savior;
using ABI.CCK.Components;
using BTKUILib.UIObjects;
using HarmonyLib;
using MelonLoader;
using UIExpansionKit.API;
using UIExpansionKit.API.Controls;
using UnityEngine;

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
        public static MelonPreferences_Entry<bool> EnableHandle;
        
        internal static List<CVRVideoPlayer> VideoPlayers = new();

        private IMenuToggle _toggleButton;
        private IMenuButton _respawnButton;

        //Assetbundle Stuff
        private AssetBundle _bundle;
        private GameObject _tabletPrefab;
        
        private const string PrefCategory = "PortableVideoTablet";

        public override void OnApplicationStart()
        {
            Log = LoggerInstance;
            
            Log.Msg("Starting up Portable Video Tablet!");

            var category = MelonPreferences.CreateCategory(PrefCategory, "Portable Video Tablet");
            EnableHandle = category.CreateEntry("EnableHandle", true, "Enable Tablet Handle", "Enables the handle on the video player tablet");

            if (!LoadAssets())
            {
                Log.Error("Assetbundle failed to load! Unable to startup!");
                return;
            }
            
            if(MelonBase.RegisteredMelons.Any(x => x.Info.Name.Equals("BTKUILib")))
                SetupBTKUI();
            else
                Log.Error("You must have BTKUILib installed to use Portable Video Tablet!");
        }

        private void SetupBTKUI()
        {
            //root page stuff
            var rootpage = new Page("Portable Video Tablet", "", true);
            rootpage.MenuTitle = "Portable Video Player";
            var category = rootpage.AddCategory("Controls");
            
            //button stuff
            var respawn = category.AddButton("Respawn tablet", "", "respawn video tablet");
            respawn.OnPress += RespawnTablet;
            var toggletablet = category.AddToggle("Toggle tablet", "Toggle tablet on and off.", false);
            toggletablet.OnValueUpdated += ToggleTablet;
        }

        private void ToggleTablet(bool state)
        {
            
        }

        private void RespawnTablet()
        {
            
        }

        private bool LoadAssets()
        {
            using (var assetStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PortableVideoTablet.tablet"))
            {
                if (assetStream != null)
                {
                    MelonDebug.Msg("[PortableVideoTablet] Loaded TabletAsset");

                    using var tempStream = new MemoryStream((int)assetStream.Length);
                    assetStream.CopyTo(tempStream);
                    
                    _bundle = AssetBundle.LoadFromMemory(tempStream.ToArray(), 0);
                    _bundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }
            }

            if (_bundle == null) return false;

            _tabletPrefab = _bundle.LoadAsset<GameObject>("tablet");
            _tabletPrefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            return true;
        }
        
    }
        
    [HarmonyPatch(typeof(CVRVideoPlayer))]
    class CVRVideoPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void VideoPlayerStart(CVRVideoPlayer __instance)
        {
            if(CheckVR.Instance.disableVideoPlayers) return;
            
            try
            {
                MelonDebug.Msg($"[PortableVideoTablet] Found video player object, {__instance.name} added to list");

                if (Main.VideoPlayers.Any(x => x.GetHashCode() == __instance.GetHashCode())) return;
                
                Main.VideoPlayers.Add(__instance);
            }
            catch (Exception e)
            {
                Main.Log.Error(e);
            }
            
        }

        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        static void VideoPlayerDestroy(CVRVideoPlayer __instance)
        {
            try
            {
                MelonDebug.Msg($"[PortableVideoTablet] Video player on destroy, {__instance.name} is being destroyed");
                
                if(!Main.VideoPlayers.Contains(__instance)) return;

                Main.VideoPlayers.Remove(__instance);
            }
            catch (Exception e)
            {
                Main.Log.Error(e);
            }
        }
    }
    
}
