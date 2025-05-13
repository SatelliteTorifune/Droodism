using ModApi.Mods;
using ModApi.Ui.Inspector;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod : GameMod
    {
        
    }
    public class UserInterface
    {
        public void OnBuildFlightViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            Debug.LogErrorFormat("Initialized2");
            var LS = new GroupModel("Life Supoort");

            var fs = Game.Instance.Settings.Game.Flight;

            var ui = Game.Instance.FlightScene.FlightSceneUI;

            request.Model.AddGroup(LS);

            var textButtonModel = new TextButtonModel(
                "Text Button", b => ui.ShowMessage("Text Button Clicked"));
            LS.Add(textButtonModel);

            float thr = 1f;
   
            var currentOxygen = new ProgressBarModel("Current Oxygen", () => thr / 10f);
            LS.Add(currentOxygen);

            var currentWater = new ProgressBarModel(
                "Current Drinking Water", () => 2.4f / 10f);
            LS.Add(currentWater);

            var labelButtonModel = new LabelButtonModel(
                "Label", b => ui.ShowMessage("Label Button Clicked"));
            labelButtonModel.ButtonLabel = "Button Label";
            LS.Add(labelButtonModel);

            var dropdownModel = new DropdownModel(
                "Dropdown",
                () => this._dropdownModelSelection,
                value => this._dropdownModelSelection = value,
                this._dropdownModelOptions);
            LS.Add(dropdownModel);

            LS.Add(new TextButtonModel(
                "Toggle ProgressBar",
                b => currentOxygen.Visible = !currentOxygen.Visible));
        }

        private string _dropdownModelSelection = "Value 1";
        private string[] _dropdownModelOptions = { "A", "B", "B" };
    }
}