using Assets.Scripts.Craft;
using ModApi.Craft.Parts;
using UnityEngine.SceneManagement;
using ModApi.Design.Events;
using ModApi.Scenes.Events;
using static Assets.Scripts.SearchDrood;

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
    public partial class Mod : ModApi.Mods.GameMod
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() : base()
        {
            
        }
        private CraftScript Craft => ModApi.Common.Game.Instance.Designer.CraftScript as CraftScript;
        public static Mod Inctance { get; } = GetModInstance<Mod>();
        
        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            Harmony harmony = new Harmony("com.SatelliteTorifune.Droodism");
            harmony.PatchAll();
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            

        } 
        public void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (Game.Instance.SceneManager.InDesignerScene)
            {
                Game.Instance.Designer.CraftLoaded += OnCraftLoaded;
                Game.Instance.Designer.PartAdded += OnPartAdded;

            }
                
        }
        
        public void OnPartAdded (object sender, DesignerPartAddedEventArgs e)
        {
            if (e.DesignerPart.Name=="Drood"||e.DesignerPart.Name=="Tourist")
            {
                
                List<PartData> droodParts = CheckDrood(Craft);
                foreach (PartData part in droodParts)
                {
                    AddLSModifier(part); 
                }

            }
            
        }
        
        public void OnCraftLoaded()
        {
            List<PartData> droodParts = CheckDrood(Craft);
            foreach (PartData part in droodParts)
            {
                AddLSModifier(part); 
            }
            
            
        }
        /// <summary>
        /// CheckDrood方法接受CraftScript参数,遍历所有modifier,得到含有Eva Modifier的Part的类型为PartData的列表
        /// CheckDrood Method receives CraftScript as a parameter,checks all modifier inside the craft,returns with a list (which type is PartData) of Parts with Eva Modifier
        /// </summary>
        /// <param name="craft"></param>
        public List<PartData> CheckDrood(CraftScript craft)
        {
            List<PartData> DroodParts = new List<PartData>();
            var parts = craft.Data.Assembly.Parts;
            foreach (PartData part in parts)
            {
                bool isDrood = false;
                bool hasLifeSupport = false;
                var modifiers = part.PartScript.Modifiers;
                if (modifiers != null)
                {
                    foreach (PartModifierScript _pms in modifiers)
                    {
                        PartModifierData _modifierData = _pms.GetData();
                        
                        if (_modifierData.Name=="EvaData")
                        {
                            isDrood = true;
                        }
                        if (_modifierData.Name == "SupportLifeData")
                        {
                            hasLifeSupport = true;
                        }
                    }
                }
                if (isDrood&&!hasLifeSupport)
                {
                    
                    DroodParts.Add(part);
                }
            }

            return DroodParts;
        }
        
        



    }
}