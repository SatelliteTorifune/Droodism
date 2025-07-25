using HarmonyLib;
using ModApi.Craft;
using ModApi.Craft.Propulsion;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Assets.Scripts
{
    public static class FuelValidation
    {
        public static void ValidateFuelTotal(object validationResult, ICraftScript craftScript, string fuelTypeId, int droodCount,bool isWaste)
        {
            try
            {
                if (craftScript == null) return;
                FuelType fuelType = Game.Instance.PropulsionData.GetFuelType(fuelTypeId);
                double totalFuel = isWaste
                    ? craftScript.FuelSources.FuelSources
                        .Where(fuelSource => fuelSource.FuelType == fuelType)
                        .Sum(fuelSource => fuelSource.TotalCapacity)
                    : craftScript.FuelSources.FuelSources
                        .Where(fuelSource => fuelSource.FuelType == fuelType)
                        .Sum(fuelSource => fuelSource.TotalFuel);
                double threshold = 100;
                Debug.Log("Validating fuel:1 ");
                // 如果燃料总量低于阈值，添加警告

                if (totalFuel < threshold)
                {
                    var validationResultType = validationResult.GetType();
                    Debug.Log("Validating fuel:2 ");
                    var messagesField = AccessTools.Field(validationResultType, "Messages");
                    Debug.Log("Validating fuel:3 ");
                    if (messagesField == null)
                    {
                        Debug.LogError($"Messages field not found in {validationResultType.FullName}");
                        return;
                    }

                    var messages = messagesField.GetValue(validationResult) as System.Collections.IList;
                    Debug.Log("Validating fuel:4 ");
                    if (messages == null)
                    {
                        Debug.LogError($"Failed to cast Messages to IList. Actual type: {messagesField.GetValue(validationResult)?.GetType().FullName}");
                        return;
                    }

                    // 创建 ValidationMessage 实例
                    var validationMessageType = Type.GetType("ModApi.Scripts.State.Validation.ValidationMessage, ModApi");
                    Debug.Log("Validating fuel:5 ");
                    if (validationMessageType == null)
                    {
                        Debug.LogError("ValidationMessage type not found.");
                        return;
                    }

                    object validationMessage = Activator.CreateInstance(validationMessageType);
                    AccessTools.Field(validationMessageType, "Message").SetValue(validationMessage,
                        $"Total {fuelType.Name} is below {threshold:F1} units ({totalFuel:F1} units).");
                    AccessTools.Field(validationMessageType, "Priority").SetValue(validationMessage, 10);
                    AccessTools.Field(validationMessageType, "MessageType").SetValue(validationMessage,
                        Enum.Parse(Type.GetType("ModApi.Scripts.State.Validation.ValidationMessageType, ModApi"), "Warning"));
                    AccessTools.Field(validationMessageType, "PartID").SetValue(validationMessage, 0); // 不关联零件

                    // 添加到 Messages 列表
                    messages.Add(validationMessage);
                    Debug.Log($"Added warning: Total {fuelType.Name} is {totalFuel:F1} units, below threshold {threshold:F1}.");
                

                }
            }
            catch (Exception e)
            {
                Debug.LogFormat("Error validating fuel total: {0}", e);
            }


        }
    }
}