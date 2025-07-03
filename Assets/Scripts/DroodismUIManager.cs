using UI.Xml;
using ModApi.Ui;
using UnityEngine;
using ModApi.Math;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Packages.DevConsole;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Flight.UI;
using HarmonyLib;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Scenes.Events;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
    public class DroodismUIManager : MonoBehaviour
    {
        /*
        public static DroodismUIManager Instance { get; private set; }
        public DroodismUI droodismUI;
        public const string droodismUIPanelButtonId = "droodismUI-Bottom";
        private void Start()
        {
            //Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                droodismUI = Game.Instance.UserInterface.BuildUserInterfaceFromResource<DroodismUI>(
                "Assets/Content/XML UI/Flight/DroodismUI",
                (script, controller) => script.OnLayoutRebuilt(controller));
            }
        }
        public static void OnBuildNavPanelUI(BuildUserInterfaceXmlRequest request)
        {
            var nameSpace = XmlLayoutConstants.XmlNamespace;
            var translationButton = request.XmlDocument
                .Descendants(nameSpace + "ContentButton")
                .First(x => (string)x.Attribute("id") == "nav-sphere-translation");

            string iconPath = "Assets/Content/XML UI/Sprites/DroodsimUIIcon.png";

            translationButton.Parent.Add(
                new XElement(
                    nameSpace + "ContentButton",
                    new XAttribute("id", droodismUIPanelButtonId),
                    new XAttribute("class", "panel-button audio-btn-click"),
                    new XAttribute("tooltip", "Droodism UI1"),
                    new XAttribute("name", "NavPanel.DroodismUI-Button"),
                    new XElement(
                        nameSpace + "Image",
                        new XAttribute("class", "panel-button-icon"),
                        new XAttribute("sprite", iconPath))));
        }
        public void OnToggleEPPanelState() {
            if (droodismUI == null) {
                return;
            }

            droodismUI.OnTogglePanelState();
        }*/
    }

    /*
    [HarmonyPatch(typeof(NavPanelController), "LayoutRebuilt")]
    class LayoutRebuilt_Patch
    {
        static bool Prefix(NavPanelController __instance)
        {
            __instance.xmlLayout.GetElementById(DroodismUIManager.droodismUIPanelButtonId).AddOnClickEvent(DroodismUIManager.Instance.OnToggleEPPanelState);
            return true;
        }
    }
    */


}