using System;
using Assets.Scripts.Design;
using HarmonyLib;
using ModApi.Mods;
using ModApi.Ui.Inspector;

namespace Assets.Scripts
{
    public partial class Mod : GameMod
    {


        [HarmonyPatch(typeof(CraftPerformanceAnalysis), "RefreshInspectorPanel", new Type[] { typeof(bool) })]
        public class CraftPerformanceAnalysisPatch
        {
            // Postfix 补丁，在 RefreshInspectorPanel 方法执行后运行,添加一个新的info组,哈基j没给我留接口,那我只能harmony启动了
            [HarmonyPostfix]
            public static void Postfix(CraftPerformanceAnalysis __instance, bool immediate)
            {
                try
                {



                    if (!immediate) return;
                    var inspectorPanelField = AccessTools.Field(typeof(CraftPerformanceAnalysis), "_inspectorPanel");
                    var inspectorPanel = inspectorPanelField.GetValue(__instance) as IInspectorPanel;

                    if (inspectorPanel == null)
                    {
                        LogError("InspectorPanel is null, cannot add TEXT group.");
                        return;
                    }

                    var inspectorModel = inspectorPanel.Model;
                    if (inspectorModel == null)
                    {
                        LogError("InspectorModel is null, cannot add TEXT group.");
                        return;
                    }

                    GroupModel textGroup = new GroupModel("<color=green>Life Support Resources Info");

                    textGroup.Add<TextModel>(new TextModel("Drood Count",
                        () => Scripts.Mod.Instance.GetDroodCountInDesigner()));
                    foreach (var var in fuelTypes)
                    {
                        AddStuff(var);
                    }

                    void AddStuff(String fuelType)
                    {
                        bool isWaste = fuelType.Contains("Waste") || fuelType == "CO2";
                        string name = "";
                        switch (fuelType)
                        {
                            case "H2O":
                                name = "Water";
                                break;
                            case "CO2":
                                name = "Carbon Dioxide";
                                break;

                            default:
                                name = fuelType;
                                break;

                        }

                        textGroup.Add<TextModel>(new TextModel(name + (isWaste ? " Capacity" : " Amount"),
                            () => GetFuelAmountInDesigner(fuelType, isWaste)));
                    }

                    // 将新组添加到 InspectorModel
                    inspectorModel.AddGroup(textGroup);
                }
                catch (Exception e)
                {
                    Log("Droodism.CraftPerformanceAnalysis failed", e);
                }
            }
        }
    }
}