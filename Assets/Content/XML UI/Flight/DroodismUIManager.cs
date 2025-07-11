using System;
using System.Linq;
using System.Xml.Linq;
using ModApi.Common;
using ModApi.Ui;
using UnityEngine;

public static class  DroodismUIManager
{

    public const string droodismBottomId = "toggle-droodism-bottom";
    public static void Initialize()
    {
        var userInterface = Game.Instance.UserInterface;
        userInterface.AddBuildUserInterfaceXmlAction(
            UserInterfaceIds.Flight.NavPanel, 
            OnBuildFlightUI);
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
    public static void OnToggleDroodismInspectorPanelState() 
    {
        Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Droodism Inspector Panel",true,10f);
    }
    

}
