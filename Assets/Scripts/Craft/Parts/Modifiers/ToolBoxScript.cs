using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Droodism.Crew;
using ModApi;
using ModApi.GameLoop;
using ModApi.Math;
using ModApi.Ui.Inspector;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class ToolBoxScript : PartModifierScript<ToolBoxData>,IFlightUpdate
    {
        private bool isDoing;
        public void FlightUpdate(in FlightFrameData frame)
        {
            if (isDoing)
            {
                ReSupplyLogic(frame,Game.Instance.FlightScene.ViewManager.GameView.SelectedPart.Data );
            }
        }
        private void ReSupplyLogic(in FlightFrameData data,PartData pd)
        {
            if (pd==null)
            {
                isDoing = false;
                return;
            }
            if (!(pd.PartType.Name==("Eva")||pd.PartType.Name==("Eva-Tourist")))
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.ToolBox.SelectNotDrood"), false, 3f);
                isDoing = false;
                return;
            }

            if ((pd.PartScript.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.ToolBox.ResupplyInterruptedTooFar"), pd.GetModifier<EvaData>().CrewName), false, 3f);
                isDoing = false;
                return;
            }

            if (pd.GetModifier<SupportLifeData>().UtilizationFactor>300)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.ToolBox.ResupplyCompleted"), pd.GetModifier<EvaData>().CrewName), false, 3f);
                isDoing = false;
                return;
            }

            if (Data.ToolPoint<0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.ToolBox.ResupplyInterruptedNoTools"), pd.GetModifier<EvaData>().CrewName), false, 4f);
                isDoing = false;
                return;
            }

            if (Data.ToolPoint>0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.ToolBox.ResupplyProgress"), pd.GetModifier<EvaData>().CrewName, Units.GetPercentageString(Mathf.Clamp01(pd.GetModifier<SupportLifeData>().UtilizationFactor / 300f))), false, 3f);
                float num = (float)data.DeltaTimeWorld * 3;
                Data.ToolPoint -=num ;
                pd.GetModifier<SupportLifeData>().UtilizationFactor += num;
            }
           
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            model.Add(new TextModel("<color=yellow>Remain Tool Kits",(Func<string>) (() => $"{this.Data.ToolPoint:F1}")));
            model.Add(new TextButtonModel("<color=green>Refill Engineer Tool Kits", (Action<TextButtonModel>)(b => { this.OnFixingClick(); })));
        }

        private void OnFixingClick()
        {
            var part = Game.Instance.FlightScene.ViewManager.GameView.SelectedPart;
            if (part==null)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.ToolBox.NoDroodSelected"), false, 3f);
                isDoing = false;
                return;
            }
            if ((part.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.ToolBox.CrewTooFarToResupply"), part.GetModifier<EvaScript>().Data.CrewName), false, 3f);
                isDoing = false;
                return;
            }
            if (!(part.Data.PartType.Name==("Eva")||part.Data.PartType.Name==("Eva-Tourist")))
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.ToolBox.SelectNotDrood"), false, 3f);
                isDoing = false;
                return;
            }

            if (part.GetModifier<SupportLifeScript>().Data.DroodismCrewData==null)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.ToolBox.CrewNotEngineer"), part.GetModifier<EvaScript>().Data.CrewName), false, 3f);
                isDoing = false;
                return;
            }

            if (part.GetModifier<SupportLifeScript>().Data.DroodismCrewData.CrewRole!=DroodType.Engineer)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.ToolBox.CrewNotEngineer"), part.GetModifier<EvaScript>().Data.CrewName), false, 3f);
                isDoing = false;
                return;
            }
            if (part.GetModifier<SupportLifeScript>().Data.UtilizationFactor >= 300)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.ToolBox.EngineerHasFullTools"), part.GetModifier<EvaScript>().Data.CrewName), false, 3f);
                isDoing = false;
                return;
            }

            isDoing = true;
        }
    }
}