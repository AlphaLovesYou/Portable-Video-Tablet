using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI.CCK.Components;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Objects;
using HarmonyLib;
using MelonLoader;
using UIExpansionKit.API;
using UIExpansionKit.API.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

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
        private GameObject _tabletInstantiate;
        private MultiSelection _videoPlayer;
        private MeshRenderer _screenRender;
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
     

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
        //BTKUI stuff
        private void SetupBTKUI()
        {
            //Root page creation
            var rootpage = new Page("PortableVideoTablet", "Video Tablet", true,"");
            rootpage.MenuTitle = "Portable Video Player";
            var category = rootpage.AddCategory("Controls");
            
            
            //Tablet control buttons
            var respawn = category.AddButton("Respawn tablet", "", "respawn video tablet");
            respawn.OnPress += RespawnTablet;
            
            var toggleTablet = category.AddToggle("Toggle tablet", "Toggle tablet on and off.", false);
            toggleTablet.OnValueUpdated += ToggleTablet;
            
            _videoPlayer = new MultiSelection("Video Players", null, 0);
            _videoPlayer.OnOptionUpdated += OnVideoSelected;
            var players = category.AddButton("Video Players", "", "Change currently displayed video player.");
            players.OnPress += VideoPlayerSelect;
        }

        private void OnVideoSelected(int option)
        {
            try
            {
                var videoPlayer = VideoPlayers[option];

                _screenRender.material.SetTexture(MainTex,videoPlayer.ProjectionTexture);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        //button controlls
        private void ToggleTablet(bool state)
        {
            if (_tabletInstantiate == null)
            {
               _tabletInstantiate= Object.Instantiate(_tabletPrefab);
               _screenRender = _tabletInstantiate.transform.Find("video").GetComponent<MeshRenderer>();
               Object.DontDestroyOnLoad(_tabletInstantiate);
            }
            _tabletInstantiate.SetActive(state);
            RespawnTablet();
        }

        private void RespawnTablet()
        {
            //if this shit works first try im buying another pair of thigh highs
            var camera = MetaPort.Instance.isUsingVr
                ? PlayerSetup.Instance.vrCamera.transform
                : PlayerSetup.Instance.desktopCamera.transform;
            Vector3 position;
            position = camera.position +
                       camera.forward;
            _tabletInstantiate.transform.position = position;
            _tabletInstantiate.transform.LookAt(camera);
            //im going to propose to whoever made LookAt and give them the best head of their life.
        }

        private void VideoPlayerSelect()
        {
            var playerList = VideoPlayers.Select(videoPlayer => videoPlayer.name).ToList();

            _videoPlayer.Options = playerList.ToArray();
            _videoPlayer.SelectedOption = 0;
            QuickMenuAPI.OpenMultiSelect(_videoPlayer);
        }
        
        //Asset loading things
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
    //Harmony things
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
