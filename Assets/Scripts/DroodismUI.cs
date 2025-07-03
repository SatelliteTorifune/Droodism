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
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Scenes.Events;

namespace Assets.Scripts
{
    public class DroodismUI:MonoBehaviour
    {
        private XmlLayoutController controller;
        private XmlElement mainPanel;
        private XmlElement listItemTemplate;
        private XmlSerializer _xmlSerializer;
        private bool _mainPanelVisible = false;
        
        /*
        public void OnLayoutRebuilt(IXmlLayoutController layoutController)
        {
            controller = (XmlLayoutController)layoutController;
            mainPanel = controller.xmlLayout.GetElementById("DroodismUI-Panel");
        }
        
        public void OnTogglePanelState() 
        { 
            _mainPanelVisible = !_mainPanelVisible;
        }
        public void SetUIVisibility(bool state)
        {
            mainPanel.SetActive(state && _mainPanelVisible);
        }
        private void ShowMessage(string message) 
        { 
            Game.Instance.FlightScene?.FlightSceneUI.ShowMessage(message); 
        }
        */
        
    }
}