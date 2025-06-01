using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Craft.Parts;
using UnityEngine.SceneManagement;
using ModApi.Design.Events;
using ModApi.Scenes.Events;
using ModApi.Craft.Parts.Events;
using HarmonyLib;
using ModApi.Ui.Inspector;
using static ModApi.Common.Game;
using static ModApi.Craft.Parts.PartData;

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
        public static Mod Inctance { get; } = GetModInstance<Mod>();
        private CraftScript Craft => Instance.Designer.CraftScript as CraftScript;
        

        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            var harmony = new Harmony("com.SatelliteTorifune.Droodism");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.FlightView,OnBuildFlightViewInspectorPanel);
            Debug.Log("OnBuildFlightViewInspectorPanel called when OnModInitialized");
           

        }
        public void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            
            if (Instance.SceneManager.InDesignerScene)
            {
                Instance.Designer.CraftLoaded += OnCraftLoaded;
                Created += OnPartAdded;
                subMinus();
            }

            if (Instance.SceneManager.InFlightScene)
            {
                subPlus();
                
            }
            else
            {
                subMinus();
            }
            

        }

        private void subPlus()
        {
            Instance.FlightScene.Initialized += OnInitialized;
            Instance.FlightScene.CraftChanged += OnCraftChanged;
            Instance.FlightScene.CraftStructureChanged += OnCraftStructureChanged;
            Instance.FlightScene.ActiveCommandPodChanged += OnCraftChanged;
            Instance.FlightScene.ActiveCommandPodStateChanged += OnCraftChanged;
        }
        
        private void subMinus()
        {
            Instance.FlightScene.Initialized -= OnInitialized;
            Instance.FlightScene.CraftChanged -= OnCraftChanged;
            Instance.FlightScene.CraftStructureChanged -= OnCraftStructureChanged;
            Instance.FlightScene.ActiveCommandPodChanged -= OnCraftChanged;
            Instance.FlightScene.ActiveCommandPodStateChanged -= OnCraftChanged;
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
                AddLsModifier(part);
            }

        }
        /// <summary>
        /// 在part添加时检测如果是Drood则添加SupportLife modifier和其他属性
        /// Adding SupportLife modifier when the added part is Drood
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnPartAdded(object sender,CreatedPartEventArgs e)
        {
            if (e.Part.Name=="Eva")
            {
                Debug.LogFormat($"这是Drood,有{e.Part.Modifiers.Count}个modifier");
                AddLsModifier(e.Part);
            }

            if (e.Part.Name == "Eva-Tourist")
            {
                Debug.LogFormat($"这是游客,有{e.Part.Modifiers.Count}个modifier"); 
                AddLsModifier(e.Part);
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
                Debug.LogFormat("DroodParts的 ID 是{0}", DroodParts[i].Id);
            }

            return DroodParts;

        }
        
        /// <summary>
        /// AddLSModifier方法接受PartData参数,为此part添加SupportLife和FuelTank的modifier
        /// AddLSModifier Method receive ParaData as a parameter,adding this part with SupportLife and FuelTank Modifier
        /// </summary>
        /// <param name="part"></param>
        public static void AddLsModifier(PartData part)
        {
            if (!(part != null))
                return;
            SupportLifeData _supportLifeData = part.GetModifier<SupportLifeData>();
            if (_supportLifeData==null)
            {
                _supportLifeData = PartModifierData.CreateFromDefaultXml<SupportLifeData>(part);
                _supportLifeData.PartPropertiesEnabled = false;
                _supportLifeData.InspectorEnabled = true;
            }
            
        }
        
        
    }
    //何意味?
    /*[HarmonyPatch]
    public class HarmonyPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FuelType), "Initialize")]
        private static void AddingStaticFuelType(ref List<FuelType> fuels)
        {
            try
            {
                IniOxygen();
                if (!fuels.Any(f => f.Id == Oxygen.Id))
                {
                    fuels.Add(Oxygen);
                    Debug.Log($"已将自定义 FuelType ({Oxygen.Name}) 添加到 fuels 列表！");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"添加自定义 FuelType 失败: {e}");
            }
        }
    }*/
    
  
    
}
