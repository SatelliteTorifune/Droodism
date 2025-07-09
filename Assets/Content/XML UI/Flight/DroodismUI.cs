using System;
using System.Linq;
using System.Xml.Linq;
using ModApi.Common;
using ModApi.Ui;
using UnityEngine;

public static class DroodismUI
{
    private const string droodismBottomId = "toggle-droodism-bottom";
    public static void Initialize()
    {
        var userInterface = Game.Instance.UserInterface;
        userInterface.AddBuildUserInterfaceXmlAction(
            UserInterfaceIds.Flight.NavPanel, 
            OnBuildFlightUI);
    }
    
    private static void OnBuildFlightUI(BuildUserInterfaceXmlRequest request)
    {
        Debug.Log("Building Droodism UI1");
        var ns = XmlLayoutConstants.XmlNamespace;
        var viewButton = request.XmlDocument
            .Descendants(ns + "ContentButton")
            .First(x => (string)x.Attribute("id") == "toggle-flight-inspector");
        Debug.Log("Building Droodism UI2");
        viewButton.Parent.Add(
            new XElement(
                ns + "ContentButton",
                new XAttribute("id", droodismBottomId),
                new XAttribute("class", "panel-button audio-btn-click"),
                //new XAttribute("OnClick", "OnToggleDroodismUIClicked(this)"),
                new XAttribute("tooltip", "Toggle Droodism UI."),
                new XAttribute("name", "NavPanel.ToggleDroodismInspector"),
                new XElement(
                    ns + "Image",
                    new XAttribute("class", "panel-button-icon"),
                    new XAttribute("sprite", "Droodism/Sprites/DroodsimUIIcon"))));
        Debug.Log("Building Droodism UI3");
       
        try
        {
            Debug.Log("尝试构建");
            request.AddOnLayoutRebuiltAction(xmlLayoutController =>
            {   Debug.Log("Building Droodism UI4");
                var button = xmlLayoutController.XmlLayout.GetElementById(droodismBottomId);
                button.AddOnClickEvent(OnToggleDroodismUIClicked);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"出事了{e}");
        }
        
    }

    static void OnToggleDroodismUIClicked()
    {
        Debug.LogFormat("Clicked Droodism UI");
    }
    
}
