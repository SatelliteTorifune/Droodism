using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Flight.UI;
using ModApi.Ui;
using UnityEngine;
using HarmonyLib;
using ModApi.Flight;
using ModApi.Math;
using ModApi.Scenes.Events;
using Rewired;
using UnityEngine.Serialization;
using Game = ModApi.Common.Game;

namespace Assets.Scripts
{


    public class DroodismUIManager : MonoBehaviour
    {

        public const string droodismBottomId = "toggle-droodism-ui-bottom";
        public NewDroodismUI newDroodismUIIntance;

        public static DroodismUIManager Instance;

        private void Awake()
        {
            Instance = this;
        }
        private void Start()
        {
            log("Start");
            Instance = this;
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Flight.NavPanel, OnBuildFlightUI);

        }

        private void Update()
        {   
            if (!Game.Instance.SceneManager.InFlightScene) 
            {
                return;
            }
            UpdateDroodismUIPanel();
        }
        private void UpdateDroodismUIPanel() 
        {
            if (newDroodismUIIntance == null) 
            {
                return;
            }
            newDroodismUIIntance.SetUIVisibility(Game.Instance.FlightScene.FlightSceneUI.Visible);
            newDroodismUIIntance.UpdateFuelPercentageItemTemplate();

        }

        public void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                newDroodismUIIntance = Game.Instance.UserInterface.BuildUserInterfaceFromResource<NewDroodismUI>(
                    "Droodism/Flight/DroodismInspectPanel",
                    (script, controller) => script.OnLayoutRebuilt(controller));
                log("OnSceneLoaded");
            }
        }

        private static void OnBuildFlightUI(BuildUserInterfaceXmlRequest request)
        {
            var ns = XmlLayoutConstants.XmlNamespace;
            var inspectButton = request.XmlDocument
                .Descendants(ns + "ContentButton")
                .First(x => (string)x.Attribute("id") == "toggle-flight-inspector");
            inspectButton.Parent.Add(
                new XElement(
                    ns + "ContentButton",
                    new XAttribute("id", droodismBottomId),
                    new XAttribute("class", "panel-button audio-btn-click"),
                    new XAttribute("tooltip", "Toggle Droodism UI."),
                    new XAttribute("name", "NavPanel.ToggleDroodismInspector"),
                    new XElement(
                        ns + "Image",
                        new XAttribute("class", "panel-button-icon"),
                        new XAttribute("sprite", "Droodism/Sprites/DroodsimUIIcon"))));
        }

        public  void OnToggleDroodismInspectorPanelState()
        {
            log("OnToggleDroodismInspectorPanelState");
            newDroodismUIIntance.OnTogglePanelState();
        }
        

        private static void log(string message)
        {
            Debug.LogFormat("DroodismUIManager:"+message);
        }

    }
    
}
