using System.Reflection;
using System.Xml.Linq;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Craft.Parts;
using UnityEngine.SceneManagement;
using ModApi.Design.Events;
using ModApi.Scenes.Events;
using HarmonyLib;
using static Assets.Scripts.SearchDrood;
using static ModApi.Common.Game;

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

        List<PartData> droodPartsAdded = new();
        public static Mod Inctance { get; } = GetModInstance<Mod>();
        private CraftScript Craft => Instance.Designer.CraftScript as CraftScript;

        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            var harmony = new Harmony("com.SatelliteTorifune.Droodism");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;

        }

        public override void OnModLoaded()
        {
            
        }
        
        public void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (Instance.SceneManager.InDesignerScene)
            {
                Instance.Designer.CraftLoaded += OnCraftLoaded;
            }

        }
        /// <summary>
        /// 在加载Craft时使用"CheckDrood"方法遍历所有modifier得到零件并添加SupportLife的modifier
        /// When load a craft get all Craft's modifier using "CheckDrood" method and adding a "SupportLife"modifie to the part
        /// </summary>
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
                List<PartModifierScript> modifiers = part.PartScript.Modifiers;
                Debug.LogFormat($"{modifiers.Count}:number of {part.Id}");
                if (modifiers != null)
                {
                    foreach (PartModifierScript _pms in modifiers)
                    {

                        PartModifierData _modifierData = _pms.GetData();

                        if (_modifierData.Name == "EvaData")
                        {
                            isDrood = true;
                        }

                        if (_modifierData.Name == "SupportLifeData")
                        {
                            hasLifeSupport = true;
                        }
                    }
                }

                if (isDrood && !hasLifeSupport)
                {

                    DroodParts.Add(part);
                }

            }

            for (int i = 0; i < DroodParts.Count; i++)
            {
                Debug.LogFormat("{0}", DroodParts[i].Id);
            }

            return DroodParts;

        }

    
    }
    /// <summary>
    /// 在EvaScript的OnModifiersCreated时获取PartData并且添加Modifier
    /// gets the PartData and adding a modifier to Part when "OnModifiersCreated" invoke in EvaScript(using harmony)
    /// </summary>
    [HarmonyPatch(typeof(EvaScript),"OnModifiersCreated")]
    public class EvaModifierCreatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(EvaScript __instance)
        {
            try
            {
                // 获取 EvaScript 所在的 Part的Data
                PartData partData = __instance.PartScript.Data;
                if (partData != null)
                {
                    
                    Debug.Log($"PartData found: {partData}");
                    AddLSModifier(partData);
                    Debug.LogFormat("成功添加了modifier");
                }
                else
                {
                    Debug.LogWarning("PartData not found on GameObject!");
                    
                }
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat($"Error:{e.Message}");
            }

        }

    }
}
