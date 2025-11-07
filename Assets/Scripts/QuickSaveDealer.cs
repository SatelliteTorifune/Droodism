using System;
using System.IO;
using Assets.Scripts.Flight;
using ModApi.Mods;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.State;
using Debug = UnityEngine.Debug;
using HarmonyLib;

namespace Assets.Scripts
{
    
 
    public partial class Mod : GameMod
    {
        public static string GetQuickSavePath(string rootPath)
        {
            // 1. 规范化路径格式（统一使用反斜杠）
            string normalizedPath = rootPath.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
        
            // 2. 确保路径不以分隔符结尾
            if (normalizedPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                normalizedPath = normalizedPath.TrimEnd(Path.DirectorySeparatorChar);
            }
        
            // 3. 直接分割路径并替换最后一部分
            string[] pathParts = normalizedPath.Split(Path.DirectorySeparatorChar);
        
            if (pathParts.Length < 1)
                throw new ArgumentException("无效的路径");
        
            // 替换最后一部分为 "QuickSave"
            pathParts[pathParts.Length - 1] = "QuickSave";
        
            // 4. 重新组合路径
            return string.Join(Path.DirectorySeparatorChar.ToString(), pathParts);
        }
       
        static void ProcessXmlFile(string filePath)
        {
            try
            {
                // 加载 XML 文件
                XDocument doc = XDocument.Load(filePath);

                // 查找 partType 包含 "Eva" 的 Part 节点
                var evaParts = doc.Descendants("Part")
                    .Where(p => p.Attribute("partType")?.Value.Contains("Eva") == true);

                bool modified = false;
                foreach (var part in evaParts)
                {
                    // 获取所有 FuelTank 子节点
                    var fuelTanks = part.Elements("FuelTank").ToList();

                    // 移除 fuelType 为 "Oxygen", "Food", 或 "Drinking Water" 的 FuelTank
                    var tanksToRemove = fuelTanks
                        .Where(t => t.Attribute("fuelType")?.Value is "Oxygen" or "Food" or "H2O" or"CO2" or"Wasted Water" or "Solid Waste")
                        .ToList();

                    if (tanksToRemove.Any())
                    {
                        foreach (var tank in tanksToRemove)
                        {
                            tank.Remove();
                        }
                        modified = true;
                    }
                }

                // 如果文件被修改，保存回原文件
                if (modified)
                {
                    doc.Save(filePath);
                }
                
            }
            catch (Exception ex)
            {
              LOG($"Error processing file {filePath}: {ex.Message}");
            }
        }
        /// <summary>
        /// 在快速保存时，移除小蓝人身上的FuelTank Mofifier防止歇逼
        /// remove FuelTank Modifier from Eva parts in QuickSave in case of NullReferenceException
        /// </summary>
        public void OnQuickSave()
        {
            
            string quickSavePath = GetQuickSavePath(Game.Instance.GameState.RootPath);
            LOG("QuickSave: {0},time{1},path{2}", Game.Instance.GameState.RootPath,
                Game.Instance.GameState.GetCurrentTime(), quickSavePath);
            try
            {
                // 获取目录下所有 XML 文件
                string[] xmlFiles = Directory.GetFiles(quickSavePath, "*.xml");
                if (xmlFiles.Length == 0)
                {
                    Console.WriteLine("No XML files found in the specified directory.");
                    return;
                }

                foreach (string filePath in xmlFiles)
                {
                    Console.WriteLine($"Processing file: {filePath}");
                    ProcessXmlFile(filePath);
                }

                Console.WriteLine("All XML files processed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    /// <summary>
    /// 使用HarmonyLib来拦截FlightSceneScript的QuickSave方法，并在其调用后调用Mod的OnQuickSave方法
    /// Use HarmonyLib to intercept the QuickSave method of FlightSceneScript and call Mod's OnQuickSave method after it is called.
    /// </summary>
    [HarmonyPatch(typeof(FlightSceneScript), "QuickSave")]
    public class FlightSceneScript_QuickSave_Patch
    {
        [HarmonyPostfix]
        static void Postfix(FlightSceneScript __instance)
        {
            DroodismCrewMananger.Instance?.OnQuickSave();
            Mod.Instance.OnQuickSave();
        }
    }
    
    
}

