using Assets.Scripts.Flight.GameView;
using Assets.Scripts.Flight.GameView.UI;
using Assets.Scripts.Flight.MapView;
using Assets.Scripts.Flight.Sim;
using ModApi.Flight.GameView;
using ModApi.Flight.UI;
using System;
using System.Collections.Generic;
using UI.Xml;
using UnityEngine.Profiling;

namespace Assets.Scripts.Flight.UI
{
    public class PatchNav:FlightPanelController
    {
        private const string SelectedClass = "panel-button-icon-toggled";
        /// <summary>The analog sticks button</summary>
        private XmlElement _analogSticksButton;
        /// <summary>The flight inspector button</summary>
        private XmlElement _flightInspectorButton;
        /// <summary>The flight log button</summary>
        private XmlElement _flightLogButton;
        /// <summary>The flight scene UI</summary>
        private IFlightSceneUI _flightSceneUi;
        /// <summary>The game view</summary>
        private IGameView _gameView;
        /// <summary>The game view interface</summary>
        private GameViewInterfaceScript _gameViewInterface;
        /// <summary>The lock heading button</summary>
        private XmlElement _lockHeadingButton;
        /// <summary>The lock vector buttons</summary>
        private Dictionary<NavSphereIndicatorType, XmlElement> _lockVectorButtons = new Dictionary<NavSphereIndicatorType, XmlElement>();
        /// <summary>The nav sphere</summary>
        private INavSphere _navSphere;
        /// <summary>The translation button</summary>
        private XmlElement _translationButton;
        /// <summary>The visible button</summary>
        private XmlElement _visibleButton;
        public void LayoutRebuilt(ParseXmlResult parseResult)
        {
            this._lockHeadingButton = this.xmlLayout.GetElementById("nav-sphere-lock");
            this._visibleButton = this.xmlLayout.GetElementById("nav-sphere-visible");
            this._translationButton = this.xmlLayout.GetElementById("nav-sphere-translation");
            this._flightInspectorButton = this.xmlLayout.GetElementById("toggle-flight-inspector");
            this._flightLogButton = this.xmlLayout.GetElementById("toggle-flight-log");
            this._analogSticksButton = this.xmlLayout.GetElementById("toggle-analog-sticks");
            this._lockVectorButtons.Clear();
            foreach (XmlElement xmlElement in this.xmlLayout.GetElementsByClass("panel-button"))
            {
                if (xmlElement.name.StartsWith("NavSpherePanel.Lock"))
                {
                    NavSphereIndicatorType type;
                    if (Enum.TryParse<NavSphereIndicatorType>(xmlElement.name.Substring(19), out type))
                    {
                        this._lockVectorButtons.Add(type, xmlElement);
                        xmlElement.AddOnClickEvent((Action) (() => this._navSphere.ToggleLock(type)), true);
                    }
                }
            }
        }
    }
}