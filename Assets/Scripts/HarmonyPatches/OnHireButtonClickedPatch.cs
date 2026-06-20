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
using ModApi.Math;
using ModApi.Mods;
using ModApi.Scripts.State.Validation;
using ModApi.Ui;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod : GameMod
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
                    Game.Instance.UserInterface.CreateMessageDialog().MessageText = $"You do not have enough money to hire a new astronaut. You currently have {Units.GetMoneyString(Game.Instance.GameState.AvailableFunds)} and it costs {Units.GetMoneyString((long) hireCostScaled)} to hire a new astronaut.";
                else if (Game.IsCareer && !CareerState.IsDebugMode &&  (int)_sourcesCountProp.GetValue(_sourcesField.GetValue(__instance)) >=  validator.ItemValue("Crew"))
                {
                    Game.Instance.UserInterface.CreateMessageDialog().MessageText = "Your crew is already as large as it can get. You can unlock larger crews in the Tech Tree.";
                }
                //目前为止一切正常,下面开始
                else
                {
                    var dialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.ThreeButtons);
                    dialog.MessageText = "<size=125%>Choose the what kind of Drood you want to hire:</size>";
                    dialog.OkayButtonText = "Pilot";
                    dialog.MiddleButtonText = "Engineer";
                    dialog.CancelButtonText = "Scientist";
                    dialog.OkayClicked += d => OnRoleSelected(__instance, d, DroodType.Pilot, hireCostScaled);
                    dialog.MiddleClicked += d => OnRoleSelected(__instance, d, DroodType.Engineer, hireCostScaled);
                    dialog.CancelClicked += d => OnRoleSelected(__instance, d, DroodType.Scientist, hireCostScaled);
                }
                return false;
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
                    LogWarning($"[Droodism] Could not read crew Id from newly created CrewMember '{crewMember.Name}'");
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
}
