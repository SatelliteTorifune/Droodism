using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Design;
using Assets.Scripts.Droodism.Crew;
using Assets.Scripts.Flight.UI;
using Assets.Scripts.State;
using Assets.Scripts.Ui.Crew;
using HarmonyLib;
using ModApi;
using ModApi.Math;
using ModApi.Mods;
using ModApi.Scripts.State.Validation;
using ModApi.Ui;
using UnityEngine;

namespace Assets.Scripts.HarmonyPatches
{
    [HarmonyPatch(typeof(CrewAssignmentDialogScript), "OnHireButtonClicked")]
    class OnHireButtonClickedPatch
    {
        //获取class的private属性啥的
        static readonly FieldInfo _sourcesField = typeof(CrewAssignmentDialogScript).GetField("_sources", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo _parentSourceField = typeof(CrewAssignmentDialogScript).GetField("_parentSource", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo _addCrewMethod = typeof(CrewAssignmentDialogScript).GetMethod("AddCrew", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(UI.Xml.XmlElement), typeof(CrewMember) }, null);
        static readonly PropertyInfo _sourcesCountProp = typeof(System.Collections.ICollection).GetProperty("Count");

        static bool Prefix(CrewAssignmentDialogScript __instance)
        {
            int hireCostScaled = 6000000 * Game.Instance.GameState.Crew.Members.Count<CrewMember>() * Game.Instance.GameState.Crew.Members.Count<CrewMember>();
            IGameStateValidator validator = Game.Instance.GameState.Validator;
            if (Game.IsCareer && !CareerState.IsDebugMode && !Game.Instance.GameState.Validator.IsItemAvailable("Cheats.SkipValidation") && Game.Instance.GameState.AvailableFunds < (long) hireCostScaled)
                Game.Instance.UserInterface.CreateMessageDialog().MessageText = string.Format(
                    Locale.GetString("Crew.Assignment.InsufficientFunds"),
                    Units.GetMoneyString(Game.Instance.GameState.AvailableFunds),
                    Units.GetMoneyString((long) hireCostScaled));
            else if (Game.IsCareer && !CareerState.IsDebugMode &&  (int)_sourcesCountProp.GetValue(_sourcesField.GetValue(__instance)) >=  validator.ItemValue("Crew"))
            {
                Game.Instance.UserInterface.CreateMessageDialog().MessageText = Locale.GetString("Crew.Assignment.CrewFull");
            }
            //先走原版的资金确认提示(花多少钱雇佣),确认后才弹出职业选择
            else
            {
                var confirmDialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.OkayCancel, null, true);
                confirmDialog.MessageText = string.Format(Locale.GetString("Crew.Assignment.HireConfirm"), Units.GetMoneyString((long)hireCostScaled));
                confirmDialog.OkayClicked += d =>
                {
                    d.Close();
                    ShowRoleSelectionDialog(__instance, hireCostScaled);
                };
            }
            return false;
        }

        static void ShowRoleSelectionDialog(CrewAssignmentDialogScript instance, long hireCost)
        {
            var dialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.ThreeButtons);
            dialog.MessageText = $"<size=125%>{Locale.GetString("Droodism.OnHireButtonClickedPatch.ChooseDroodType")}</size>";
            dialog.OkayButtonText = Locale.GetString("Droodism.OnHireButtonClickedPatch.Pilot");
            dialog.MiddleButtonText = Locale.GetString("Droodism.OnHireButtonClickedPatch.Engineer");
            dialog.CancelButtonText = Locale.GetString("Droodism.OnHireButtonClickedPatch.Scientist");
            dialog.OkayClicked += d => OnRoleSelected(instance, d, DroodType.Pilot, hireCost);
            dialog.MiddleClicked += d => OnRoleSelected(instance, d, DroodType.Engineer, hireCost);
            dialog.CancelClicked += d => OnRoleSelected(instance, d, DroodType.Scientist, hireCost);
        }

        static void OnRoleSelected(CrewAssignmentDialogScript instance, MessageDialogScript dialog, DroodType role, long hireCost)
        {
            //原版创建的模式
            dialog.Close();
            CrewMember crewMember = Game.Instance.GameState.Crew.CreateCrewMember();
            Game.Instance.GameState.Career?.SpendMoney(hireCost);
            
            
            int crewId = ReadIntMember(crewMember, "Id", "CrewId", "CrewID", "NodeId");
            if (crewId > 0)
            {
                DroodismCrewDataManager.Instance.RecordCrewMemberRole(crewId, crewMember.Name, role);
            }
            else
            {
                Mod.LogWarning($"[Droodism] Could not read crew Id from newly created CrewMember '{crewMember.Name}'");
                DroodismCrewDataManager.Instance.RecordCrewMemberRole(crewMember.Name, role);
            }

            //利用反射原版写入
            IList sources = (IList)_sourcesField.GetValue(instance);
            var parentSource = _parentSourceField.GetValue(instance);
            var addedSource = _addCrewMethod.Invoke(instance, new object[] { parentSource, crewMember });
            sources.Add(addedSource);
        }

        private static int ReadIntMember(object obj, params string[] candidateNames)
        {
            foreach (var name in candidateNames)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.PropertyType == typeof(int))
                    return (int)prop.GetValue(obj);
                var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(int))
                    return (int)field.GetValue(obj);
            }
            return 0;
        }
    }
}
