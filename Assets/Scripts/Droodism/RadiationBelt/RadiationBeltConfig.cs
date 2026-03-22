
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
        
        public Vector3 Scale = Vector3.one * 1;
        public float renderMetersPerUnit;

        public bool Enabled;
        public float innerDist;
        public float innerRadius;
        public float innerBorderDist;
        public float innerBorderRadius;
        public float innerDeformXY;
        public float innerCompression;
        public float innerExtension;
        public float innerBorderDeformXY;
        public float innerDeform;
        public float innerExtention;
        public float innerBorderDeform;
        public float innerHeightScale;
        public int innerParticleCount ; // 粒子数
        public float innerQuality ; // 质量 (越高越薄)
        
        public float outerBorderRadius;
        public float outerRadius;
        public float outerDist;
        public float outerBorderDist;
        public float outerDeformXY;
        public float outerCompression;
        public float outerExtension;
        public float outerBorderDeformXY;
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
            defaultCFG.Scale = Vector3.one;
            defaultCFG.renderMetersPerUnit = 1_000_000f;
            defaultCFG.Enabled = false;
            
            // Kerbalism 'earth' defaults
            defaultCFG.innerDist = 0.813f;
            defaultCFG.innerRadius = 0.7000f;
            defaultCFG.innerBorderDist = 0.0001f;
            defaultCFG.innerBorderRadius = 0.915f;
            defaultCFG.innerDeformXY = 0.5720f;
            defaultCFG.innerCompression = 1.01f;
            defaultCFG.innerExtension = 1.00f;
            defaultCFG.innerBorderDeformXY = 0.5f;
            defaultCFG.innerDeform = 0.0f;
            defaultCFG.innerExtention = defaultCFG.innerExtension; // legacy
            defaultCFG.innerBorderDeform = defaultCFG.innerBorderDeformXY; // legacy
            defaultCFG.innerParticleCount = 12000;
            defaultCFG.innerQuality = 50f;
            
            defaultCFG.outerDist = 2.6338f;
            defaultCFG.outerRadius = 2.48f;
            defaultCFG.outerBorderDist = 1.4412f;
            defaultCFG.outerBorderRadius = 1.4875f;
            defaultCFG.outerDeformXY = 0.7225f;
            defaultCFG.outerCompression = 1.01f;
            defaultCFG.outerExtension = 1.00f;
            defaultCFG.outerBorderDeformXY = 0.7225f;
            defaultCFG.outerHeightScale = 1f;
            defaultCFG.innerHeightScale = 1f;
            defaultCFG.outerDeform = 0.0f;
            defaultCFG.outerParticleCount = 18000;
            defaultCFG.outerQuality = 60f;
            defaultCFG.NormalizeLegacyFields();
            return  defaultCFG;
            
        }

        public void ApplyKerbalismEarthPreset()
        {
            innerDist = 0.813f;
            innerRadius = 0.7000f;
            innerDeformXY = 0.5720f;
            innerCompression = 1.01f;
            innerExtension = 1.00f;
            innerBorderDist = 0.0001f;
            innerBorderRadius = 0.915f;
            innerBorderDeformXY = 0.5f;
            innerDeform = 0.0f;
            innerQuality = 50.0f;

            outerDist = 2.6338f;
            outerRadius = 2.48f;
            outerDeformXY = 0.7225f;
            outerCompression = 1.01f;
            outerExtension = 1.00f;
            outerBorderDist = 1.4412f;
            outerBorderRadius = 1.4875f;
            outerBorderDeformXY = 0.7225f;
            outerDeform = 0.0f;
            outerQuality = 60.0f;

            // 旧字段同步，避免旧逻辑分支读取到不一致数据
            innerExtention = innerExtension;
            innerBorderDeform = innerBorderDeformXY;
            NormalizeLegacyFields();
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
                    config?.NormalizeLegacyFields();
                    //Mod.Log($"Radiation Belt config '{planetName}' loaded from: {filePath}");
                    return config;
                }
            }
            catch (System.Exception e)
            {
                Mod.Log($"Failed to load Radiation Belt config '{planetName}': {e.Message}.");
                return CreateDefault();
            }
        }

        private void NormalizeLegacyFields()
        {
            // 兼容老配置：旧字段有值时用于填充新字段
            if (innerExtension <= 0f && innerExtention > 0f) innerExtension = innerExtention;
            if (innerBorderDeformXY <= 0f && innerBorderDeform > 0f) innerBorderDeformXY = innerBorderDeform;

            // 新字段最终兜底
            if (renderMetersPerUnit <= 0f) renderMetersPerUnit = 1_000_000f;
            if (innerDeformXY <= 0f) innerDeformXY = 1.0f;
            if (innerCompression <= 0f) innerCompression = 1.0f;
            if (innerExtension <= 0f) innerExtension = 1.0f;
            if (innerBorderDeformXY <= 0f) innerBorderDeformXY = 1.0f;

            if (outerDeformXY <= 0f) outerDeformXY = 1.0f;
            if (outerCompression <= 0f) outerCompression = 1.0f;
            if (outerExtension <= 0f) outerExtension = 1.0f;
            if (outerBorderDeformXY <= 0f) outerBorderDeformXY = 1.0f;

            if (innerHeightScale <= 0f) innerHeightScale = 1.0f;
            if (outerHeightScale <= 0f) outerHeightScale = 1.0f;
        }
        

    }
}