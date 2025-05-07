using UnityEngine.SceneManagement;
using ModApi.Design.Events;
using ModApi.Scenes.Events;

namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi;
    using ModApi.Common;
    using ModApi.Mods;
    using UnityEngine;
    using UnityEngine.PlayerLoop;
    using HarmonyLib;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() : base()
        {
            
        }
        public static Mod Inctance { get; } = GetModInstance<Mod>();
        
        protected override void OnModInitialized()
        {
            Harmony harmony = new Harmony("com.SatelliteTorifune.Droodism");
            harmony.PatchAll();
            Debug.LogFormat("this mod is loaded");
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        } 
        public void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (Game.Instance.SceneManager.InDesignerScene)
            {
                Game.Instance.Designer.PartAdded += OnPartAdded;
             
                Debug.LogErrorFormat("You R entering a scene");
            }
                
        }
        
        public void OnPartAdded (object sender, DesignerPartAddedEventArgs e)
        {
            
            Debug.LogFormat($"the part is added :{e.DesignerPart.Name}:{e.DesignerPart.GenerateXml()}");
            if (e.DesignerPart.Name=="Drood"||e.DesignerPart.Name=="Tourist")
            {
                Debug.Log("THis is human");
            }
            
        } 
    }
}