
using System;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;
using Assets.Scripts;
using Application = UnityEngine.Application;

namespace Droodism.RadiationBelt
{
    public class RadiationBeltConfig
    {
        public const string CONFIG_FOLDER = "/UserData/DroodismConfig/RadiationBeltConfigs/";
        private const string DEFAULT_CONFIG_NAME = "Default";

        #region parameter
        
        public Vector3 Scale = Vector3.one * 14;

        public bool Enabled;
        public float innerMajorRadius;
        public float innerMinorRadius;
        public float innerOuterCenterX;
        public float innerOuterCenterY;
        public float innerOuterRadiusX;
        public float innerOuterRadiusY;
        public float innerCoreRadius;
        public float innerDeform;
        public float innerCoreOffset;
        public float innerHeightScale;
        public float innerCoreRadiusX;
        public float innerCoreRadiusY;
        public float innerCoreCenterX;
        public float innerCoreCenterY;
        public int innerParticleCount ; // 粒子数
        public float innerQuality ; // 质量 (越高越薄)

        public float outerMajorRadius ;
        public float outerMinorRadius ;
        public float outerBorderStart ; // 内减法渐变
        public float outerBorderEnd ;
        public float outerCoreRadius;
        public float outerCoreOffset;
        public float outerCoreCenterX;
        public float outerCoreCenterY;
        public float outerCoreRadiusX;
        public float outerCoreRadiusY;
        public float outerOuterCenterX;
        public float outerOuterCenterY;
        public float outerOuterRadiusX;
        public float outerOuterRadiusY;
        public float outerCompression; // 太阳侧压缩
        public float outerExtension; // 尾侧拉伸
        public float outerDeform;
        public float outerHeightScale;
        public int outerParticleCount ;
        public float outerQuality;
        
        public Vector3 starDirection = Vector3.left;
        

        #endregion
        
        public static string GetConfigFolderPath()
        {
            string folderPath = Application.persistentDataPath + CONFIG_FOLDER;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }
        public static string GetConfigPath(string planetName)
        {
            return Path.Combine(GetConfigFolderPath(), planetName + ".xml");
        }
        public void SaveToFile(string planetName)
        {
            try
            {
                string filePath = GetConfigPath(planetName);
                string directory = Path.GetDirectoryName(filePath);
            
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(RadiationBeltConfig));
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(stream, this);
                }
                Mod.Log($"Radiation Belt config '{planetName}' saved to: {filePath}");
            }
            catch (System.Exception e)
            {
                Mod.Log($"Failed to save Radiation Belt config '{planetName}': {e.Message}");
            }
        }

        public static RadiationBeltConfig CreateDefault()
        {
            RadiationBeltConfig defaultCFG = new RadiationBeltConfig();
            defaultCFG.Scale = Vector3.one * 14;
            defaultCFG.Enabled = false;
            defaultCFG.innerMajorRadius = 2f;
            defaultCFG.innerDeform = 0.2f;
            defaultCFG.innerCoreRadius = 0.5f;
            defaultCFG.innerCoreOffset = 0.5f;
            defaultCFG.innerParticleCount = 8000;
            defaultCFG.innerQuality = 30f;
            
            defaultCFG.outerHeightScale = 1f;
            defaultCFG.innerHeightScale = 1f;
            
            
            defaultCFG.outerMajorRadius = 5.2f;
            defaultCFG.outerMinorRadius = 1.35f;
            defaultCFG.outerCoreRadius = 1.0f;
            defaultCFG.outerCoreOffset = 0.5f;
            defaultCFG.outerBorderStart = 0.1f;
            defaultCFG.outerBorderEnd = 1.0f;
            defaultCFG.outerCompression = 0.6f;
            defaultCFG.outerExtension = 1.5f; 
            defaultCFG.outerDeform = 0.15f;
            defaultCFG.outerParticleCount = 15000;
            defaultCFG.outerQuality = 40f;
            return  defaultCFG;
            
        }
        public static RadiationBeltConfig LoadFromFile(string planetName)
        {
            string filePath = GetConfigPath(planetName);
        
            if (!File.Exists(filePath))
            {
                Mod.Log($"Config file '{planetName}' not found at {filePath}. Creating default config.");
                RadiationBeltConfig defaultConfig = CreateDefault();
                defaultConfig.SaveToFile(planetName);
                return defaultConfig;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(RadiationBeltConfig));
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    RadiationBeltConfig config = serializer.Deserialize(stream) as RadiationBeltConfig;
                    Mod.Log($"Cloud config '{planetName}' loaded from: {filePath}");
                    return config;
                }
            }
            catch (System.Exception e)
            {
                Mod.Log($"Failed to load Radiation Belt config '{planetName}': {e.Message}.");
                return CreateDefault();
            }
        }
        

    }
}