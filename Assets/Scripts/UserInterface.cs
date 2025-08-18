using System;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;
using ModApi.Flight;
using ModApi.Math;
using ModApi.Mods;
using ModApi.Ui.Inspector;
using UnityEngine;
using Debug = UnityEngine.Debug;
using TextButtonModel = ModApi.Ui.Inspector.TextButtonModel;

namespace Assets.Scripts
{
    
    public partial class Mod : GameMod
    {
        
        // Method called when the mod is initialized
        private void OnInitialized(IFlightScene flightScene)
        {
            那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花();
            // Update the drood count when the mod is initialized
            UpdateDroodCount();
            Debug.Log("OnInitialized called UpdateDroodCount");
            那个傻逼操你妈你妈大b人人插左插插右插插插的你妈b开花();
        }

        // Method called when the craft changes
        private void OnCraftChanged(ICraftNode craftNode)
        {
            
            /* Update the drood count when the craft changes
            UpdateDroodCount();
            Debug.Log("OnCraftChanged called UpdateDroodCount");*/
        }

        // Method called when the craft structure changes in the UI
        private void OnCraftStructureChangedUI()
        {
            /* Update the drood count when the craft structure changes in the UI
            UpdateDroodCount();
            Debug.Log("OnCraftStructureChangedUI calledUpdateDroodCount");*/
        }

        // Method to update the count of droods, astronauts, and tourists on the craft
        public void UpdateDroodCount()
        {
            return;
        }
    }
}
