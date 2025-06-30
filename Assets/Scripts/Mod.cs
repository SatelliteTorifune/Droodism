using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Xml.Linq;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Flight;
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
            

        }
        public void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            subPlus();
            Debug.LogFormat($"SceneLoaded事件触发{e.Scene}");
            
            if (ModApi.Common.Game.InDesignerScene)
            {
                Instance.Designer.CraftLoaded += OnCraftLoaded;
                Created += OnPartAdded;
            }

            if (InFlightScene)
            {
                try
                {
                    UpdateDroodCount();
                    Debug.LogFormat("OnSceneLoaded更新Drood数量");
                }
                catch (Exception e1)
                {
                    Debug.LogFormat("你要干啥{0}",e1);
                }
            }
            
        }

        private void subPlus()
        {
            try
            {
                Instance.FlightScene.Initialized += OnInitialized;
                Debug.LogFormat(" Initialized订阅OnInitialized");
                Instance.FlightScene.CraftChanged += OnCraftChanged;
                Debug.LogFormat(" CraftChanged订阅OnCraftChanged");
                Instance.FlightScene.CraftStructureChanged += OnCraftStructureChangedUI;
                Debug.LogFormat(" CraftStructureChanged订阅OnCraftStructureChangedUI");
                Instance.FlightScene.ActiveCommandPodChanged += OnCraftChanged;
                Debug.LogFormat(" ActiveCommandPodChanged订阅OnCraftChanged");
                Instance.FlightScene.ActiveCommandPodStateChanged += OnCraftChanged;
                Debug.LogFormat(" ActiveCommandPodStateChanged订阅OnCraftChanged");
                
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat($"订阅有问题{e}");
            }
            
        }
        
        private void subMinus()
        {
            Instance.FlightScene.Initialized -= OnInitialized;
            Instance.FlightScene.CraftChanged -= OnCraftChanged;
            Instance.FlightScene.CraftStructureChanged -= OnCraftStructureChangedUI;
            Instance.FlightScene.ActiveCommandPodChanged -= OnCraftChanged;
            Instance.FlightScene.ActiveCommandPodStateChanged -= OnCraftChanged;
        }
        /// <summary>
        /// 在加载Craft时使用"CheckDrood"方法遍历所有modifier得到零件并添加SupportLife的modifier
        /// When load a craft get all Craft's modifier using "CheckDrood" method and adding a "SupportLife"modifie to the part
        /// </summary>
        public void OnCraftLoaded()
        {
            
            foreach (PartData part in CheckDrood(Craft))
            {
                AddLsModifier(part);
            }

            foreach (PartData part in  CheckGenerator(Craft))
            {
                AddLSGModifier(part);
            }

            foreach (PartData part in  CheckCommandPod(Craft))
            {
                PatchCommandPod(part);
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
            
            Debug.LogFormat($"{e.Part.Name},id{e.Part.Id}有{e.Part.Modifiers.Count}个modifier,1:{e.PartType.Name}");
            if (e.Part.Name=="Eva"||e.Part.Name == "Eva-Tourist")
            {
                AddLsModifier(e.Part);
            }
            
            if (e.Part.Name == "Generator1")
            {
                AddLSGModifier(e.Part);
            }

            if (e.Part.Name.Contains("Command")||e.Part.Name.Contains("Capusle"))
            {
               PatchCommandPod(e.Part);
            }
            
        }
        
        
    }
    
    [HarmonyPatch(typeof(EvaScript), nameof(EvaScript.OnModifiersCreated))]
    public static class EvaScriptPatch
    {
        /// <summary>
        /// 在 OnModifiersCreated 方法执行后运行的后置补丁。
        /// Postfix patch to run after the OnModifiersCreated method.
        /// </summary>
        /// <param name="__instance">EvaScript 实例。The EvaScript instance.</param>
        public static void Postfix(EvaScript __instance)
        {
            // 仅在飞行场景中执行修补
            // Only execute the patch in the flight scene
            if (!Game.InFlightScene)
            {
                return;
            }

            try
            {
                // 获取零件上所有的 FuelTankScript 组件
                // Get all FuelTankScript components on the part
                var fuelTanks = ((Component)__instance).GetComponents<FuelTankScript>();

                // 查找燃料类型为 "JetPack" 的 FuelTankScript
                // Find the FuelTankScript with fuel type "JetPack"
                var jetPackFuelTank = fuelTanks.FirstOrDefault(tank => tank.FuelType?.Id == "Jetpack");

                // 使用反射设置 _fuelTank 字段的值
                // Use reflection to set the value of the _fuelTank field
                var fuelTankField = AccessTools.Field(typeof(EvaScript), "_fuelTank");
                if (fuelTankField != null)
                {
                    fuelTankField.SetValue(__instance, jetPackFuelTank);
                    if (jetPackFuelTank == null)
                    {
                        Debug.LogWarning("[EvaScriptPatch] No FuelTankScript with fuel type 'JetPack' found. Setting _fuelTank to null.");
                    }
                    else
                    {
                        Debug.Log($"[EvaScriptPatch] Successfully set _fuelTank to JetPack fuel tank.");
                    }
                }
                else
                {
                    Debug.LogError("[EvaScriptPatch] Failed to find _fuelTank field via reflection.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EvaScriptPatch] Error in Postfix patch: {e.Message}");
            }
        }
    }
    
  
    
}
