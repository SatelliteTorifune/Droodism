using Assets.Scripts.Craft.Parts.Modifiers.Eva;
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
    
    public class FirstAddKitScript : PartModifierScript<FirstAidKitData>,IFlightUpdate
    {
        private bool isHealing;
        public void FlightUpdate(in FlightFrameData frame)
        {
            if (isHealing)
            {
                HealingLogic(frame,Game.Instance.FlightScene.ViewManager.GameView.SelectedPart.Data );
            }
        }
        private void HealingLogic(in FlightFrameData data,PartData pd)
        {
            if (pd==null)
            {
                isHealing = false;
                return;
            }
            if (!(pd.PartType.Name==("Eva")||pd.PartType.Name==("Eva-Tourist")))
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.FirstAddKitScript.SelectNotDrood"), false, 3f);
                isHealing = false;
                return;
            }

            if ((pd.PartScript.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.FirstAddKitScript.HealingInterruptedTooFar"), pd.GetModifier<EvaData>().CrewName), false, 3f);
                isHealing = false;
                return;
            }

            if (pd.Damage<=0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.FirstAddKitScript.HealingCompleted"), pd.GetModifier<EvaData>().CrewName), false, 3f);
                isHealing = false;
                return;
            }

            if (Data.HealHp<=0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.FirstAddKitScript.HealingInterruptedNoTools"), pd.GetModifier<EvaData>().CrewName), false, 4f);
                isHealing = false;
                return;
            }

            if (!pd.GetModifier<SupportLifeData>().Script.CanHeal)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.FirstAddKitScript.HealingInterruptedTakingDamage"), pd.GetModifier<EvaData>().CrewName), false, 4f);
                isHealing = false;
                return;
            }

            if (Data.HealHp>0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.FirstAddKitScript.HealingProgress"), pd.GetModifier<EvaData>().CrewName, Units.GetPercentageString(Mathf.Clamp01((100f - pd.Damage) / 100f))), false, 3f);
                float num = (float)data.DeltaTimeWorld * this.Data.HealRate;
                Data.HealHp -=num ;
                pd.Damage -= num;
            }
           
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            model.Add(new TextModel(Locale.GetString("Droodism.FirstAddKitScript.RemainHealingPoints"),(Func<string>) (() => $"{this.Data.HealHp:F1}")));
            model.Add(new TextButtonModel(Locale.GetString("Droodism.FirstAddKitScript.HealDrood"), (Action<TextButtonModel>)(b => { this.OnHealingClick(); })));
        }

        private void OnHealingClick()
        {
            var part = Game.Instance.FlightScene.ViewManager.GameView.SelectedPart;
            if (part==null)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.FirstAddKitScript.NoDroodSelected"), false, 3f);
                isHealing = false;
                return;
            }
            if (!(part.Data.PartType.Name==("Eva")||part.Data.PartType.Name==("Eva-Tourist")))
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Locale.GetString("Droodism.FirstAddKitScript.SelectNotDrood"), false, 3f);
                isHealing = false;
                return;
            }
            if ((part.GameObject.transform.position - this.PartScript.GameObject.transform.position).magnitude > 5f)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.FirstAddKitScript.CrewTooFarToHealing"), part.GetModifier<EvaScript>().Data.CrewName), false, 3f);
                isHealing = false;
                return;
            }
            if (!part.GetModifier<SupportLifeScript>().CanHeal)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.FirstAddKitScript.CrewTakingDamage"), part.GetModifier<EvaScript>().Data.CrewName), false, 4f);
                isHealing = false;
                return;
            }
            if (part.Data.Damage <= 0)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format(Locale.GetString("Droodism.FirstAddKitScript.CrewHealthy"), part.GetModifier<EvaScript>().Data.CrewName), false, 3f);
                isHealing = false;
                return;
            }
            

            if (part.Data.PartType.Name==("Eva")||part.Data.PartType.Name==("Eva-Tourist"))
            {
                isHealing = true;
            }
            
            
        }
    }
}