using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Droodism.Crew;
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
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Select Part is not a Drood", false, 3f);
                isDoing = false;
                return;
            }
            
            if ((pd.PartScript.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Resupply Process Is Interrupted: {pd.GetModifier<EvaData>().CrewName} is too far away from this Tool box", false, 3f);
                isDoing = false;
                return;
            }
            
            if (pd.GetModifier<SupportLifeData>().UtilizationFactor>300)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Resupply Process Completed for {pd.GetModifier<EvaData>().CrewName}.", false, 3f);
                isDoing = false;
                return;
            }

            if (Data.ToolPoint<0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Resupply Process Is Interrupted:Not Enough Healing Tools for {pd.GetModifier<EvaData>().CrewName}", false, 4f);
                isDoing = false;
                return;
            }
            
            if (Data.ToolPoint>0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Resupply {pd.GetModifier<EvaData>().CrewName} : Progress {Units.GetPercentageString(Mathf.Clamp01(pd.GetModifier<SupportLifeData>().UtilizationFactor / 300f))}", false, 3f);
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
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("No Drood selected", false, 3f);
                isDoing = false;
                return;
            }
            if ((part.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Selected Crew {part.GetModifier<EvaScript>().Data.CrewName} is too far away to resupply.", false, 3f);
                isDoing = false;
                return;
            }
            if (!(part.Data.PartType.Name==("Eva")||part.Data.PartType.Name==("Eva-Tourist")))
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Select Part is not a Drood", false, 3f);
                isDoing = false;
                return;
            }

            if (part.GetModifier<SupportLifeScript>().Data.DroodismCrewData==null)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Select Crew {part.GetModifier<EvaScript>().Data.CrewName} is not an Enginner", false, 3f);
                isDoing = false;
                return; 
            }

            if (part.GetModifier<SupportLifeScript>().Data.DroodismCrewData.CrewRole!=DroodType.Engineer)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Select Crew {part.GetModifier<EvaScript>().Data.CrewName} is not an Enginner", false, 3f);
                isDoing = false;
                return;
            }
            if (part.GetModifier<SupportLifeScript>().Data.UtilizationFactor >= 300)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Selected Engineer {part.GetModifier<EvaScript>().Data.CrewName} has full set of tool.", false, 3f);
                isDoing = false;
                return;
            }

            isDoing = true;
        }
    }
}