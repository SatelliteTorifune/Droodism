using Assets.Scripts.Flight;
using Assets.Scripts.Vizzy.UI;
using HarmonyLib;
using ModApi;
using ModApi.Ui;
using UnityEngine;

namespace Assets.Scripts.HarmonyPatches
{
    /// <summary>
    /// 修复飞行中编辑 Vizzy 时点击 Apply 按钮导致的 NullReferenceException。
    /// 原方法 OnButtonApplyClicked 在飞行中调用 Game.Instance.Designer.DesignerUi.ShowMessage 会崩溃，
    /// 因为飞行场景下 Designer 为 null。
    /// </summary>
    [HarmonyPatch(typeof(VizzyUIController), "OnButtonApplyClicked")]
    public class OnButtonApplyClickedPatch
    {
        [HarmonyPrefix]
        static bool Prefix(VizzyUIController __instance)
        {
            // 执行核心逻辑：保存程序并关闭 UI
            __instance.VizzyUI.ApplyChangesToCraft();
            __instance.VizzyUI.Close();

            string message = Locale.GetString("Vizzy.UI.ProgramSaved.Message");

            // 根据当前场景选择合适的 UI 显示消息
            if (Game.Instance.Designer != null)
            {
                // 在设计师中，使用 DesignerUi
                Game.Instance.Designer.DesignerUi.ShowMessage(message, 7f);
            }
            else if (Game.Instance.FlightScene != null)
            {
                // 在飞行中，使用 FlightSceneUI
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(message, false, 7f);
            }

            // 跳过原始方法，避免 NullReferenceException
            return false;
        }
    }

    /// <summary>
    /// 修复飞行中编辑 Vizzy 时点击 Cancel 按钮导致的 NullReferenceException。
    /// 原方法 OnButtonCancelClicked 的回调中调用 Game.Instance.Designer.DesignerUi.ShowMessage 会崩溃，
    /// 因为飞行场景下 Designer 为 null。
    /// </summary>
    [HarmonyPatch(typeof(VizzyUIController), "OnButtonCancelClicked")]
    public class OnButtonCancelClickedPatch
    {
        [HarmonyPrefix]
        static bool Prefix(VizzyUIController __instance)
        {
            MessageDialogScript dialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.OkayCancel, null, true);
            dialog.UseDangerButtonStyle = true;
            dialog.MessageText = Locale.GetString("Vizzy.UI.DiscardChanges.Confirm");
            dialog.OkayClicked += delegate (MessageDialogScript d)
            {
                dialog.Close();
                __instance.VizzyUI.Close();

                string message = Locale.GetString("Vizzy.UI.ProgramDiscarded.Message");

                // 根据当前场景选择合适的 UI 显示消息
                if (Game.Instance.Designer != null)
                {
                    Game.Instance.Designer.DesignerUi.ShowMessage(message, 7f);
                }
                else if (Game.Instance.FlightScene != null)
                {
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage(message, false, 7f);
                }
            };

            // 跳过原始方法，避免 NullReferenceException
            return false;
        }
    }
}