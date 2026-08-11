
using System;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;
using Assets.Scripts;
using Application = UnityEngine.Application;

namespace Assets.Scripts.Droodism.RadiationBelt
{
    public class RadiationBeltConfig
    {
        public const string CONFIG_FOLDER = "/UserData/DroodismConfig/RadiationBeltConfigs/";
        #region parameter
        
        public float renderMetersPerUnit;
        public Vector3 beltTiltAxis;
        public float beltTiltDegrees;
        public float beltSpinSpeedDegPerSec;
        public float beltSpinPhaseDeg;

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
        public float innerHeightScale;
        public int innerParticleCount ; // 粒子数
        public float innerQuality ; // 质量 (越高越薄)
        public float innerBaseIntensity;
        public float innerIntensityEdgeWidth;
        public float innerIntensityExponent;
        public float innerPeakDoseRateRadPerHour;
        
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
        public float outerBaseIntensity;
        public float outerIntensityEdgeWidth;
        public float outerIntensityExponent;
        public float outerPeakDoseRateRadPerHour;
        #endregion
        
        private  static string GetConfigFolderPath()
        {
            string folderPath = Application.persistentDataPath + CONFIG_FOLDER;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }
        private static string GetConfigPath(string planetName)
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

        private static RadiationBeltConfig CreateDefault()
        {
            RadiationBeltConfig defaultCFG = new RadiationBeltConfig();
            defaultCFG.renderMetersPerUnit = 1_000_000f;
            defaultCFG.beltTiltAxis = Vector3.right;
            defaultCFG.beltTiltDegrees = 11.5f;
            defaultCFG.beltSpinSpeedDegPerSec = 0f;
            defaultCFG.beltSpinPhaseDeg = 0f;
            defaultCFG.Enabled = false;
            
            // Kerbalism 'earth' defaults
            // yes i check the RadiationModel from kerbalism
            defaultCFG.innerDist = 0.813f;
            defaultCFG.innerRadius = 0.7000f;
            defaultCFG.innerBorderDist = 0.0001f;
            defaultCFG.innerBorderRadius = 0.915f;
            defaultCFG.innerDeformXY = 0.5720f;
            defaultCFG.innerCompression = 1.01f;
            defaultCFG.innerExtension = 1.00f;
            defaultCFG.innerBorderDeformXY = 0.5f;
            defaultCFG.innerDeform = 0.0f;
            defaultCFG.innerParticleCount = 12000;
            defaultCFG.innerQuality = 50f;
            defaultCFG.innerBaseIntensity = 1.0f;
            defaultCFG.innerIntensityEdgeWidth = 0.08f;
            defaultCFG.innerIntensityExponent = 1.5f;
            defaultCFG.innerPeakDoseRateRadPerHour = 35f;
            
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
            defaultCFG.outerBaseIntensity = 0.7f;
            defaultCFG.outerIntensityEdgeWidth = 0.12f;
            defaultCFG.outerIntensityExponent = 1.4f;
            defaultCFG.outerPeakDoseRateRadPerHour = 0.5f;
            defaultCFG.NormalizeLegacyFields();
            return  defaultCFG;
            
        }

        public void ApplyDefaultPreset()
        {
            beltTiltDegrees = 11.5f;
            beltTiltAxis = Vector3.right;
            beltSpinSpeedDegPerSec = 0f;
            beltSpinPhaseDeg = 0f;

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
            innerBaseIntensity = 1.0f;
            innerIntensityEdgeWidth = 0.08f;
            innerIntensityExponent = 1.5f;
            innerPeakDoseRateRadPerHour = 35f;

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
            outerBaseIntensity = 0.7f;
            outerIntensityEdgeWidth = 0.12f;
            outerIntensityExponent = 1.4f;
            outerPeakDoseRateRadPerHour = 0.5f;
            
            NormalizeLegacyFields();
        }

        public void ApplyGiantPreset()
        {
            beltTiltDegrees = 10.8f;
            beltTiltAxis = Vector3.right;
            beltSpinSpeedDegPerSec = 0f;
            beltSpinPhaseDeg = 0f;
            Enabled = true;

            innerDist = 2.2f;
            innerRadius = 1.0f;
            innerDeformXY = 1.0f;
            innerCompression = 1.05f;
            innerExtension = 0.8f;
            innerBorderDist = 0.8f;
            innerBorderRadius = 1.25f;
            innerBorderDeformXY = 1.0f;
            innerDeform = 0.0f;
            innerHeightScale = 1.0f;
            innerParticleCount = 16000;
            innerQuality = 30.0f;
            innerBaseIntensity = 0.8f;
            innerIntensityEdgeWidth = 0.2f;
            innerIntensityExponent = 1.2f;
            innerPeakDoseRateRadPerHour = 14f;

            outerDist = 6.0f;
            outerRadius = 6.0f;
            outerDeformXY = 1.0f;
            outerCompression = 1.05f;
            outerExtension = 0.7f;
            // Kerbalism giant model uses border_start/end; map to this implementation's subtraction torus.
            outerBorderDist = 3.282f;
            outerBorderRadius = 3.6f;
            outerBorderDeformXY = 1.0f;
            outerDeform = 0.0f;
            outerHeightScale = 1.0f;
            outerParticleCount = 22000;
            outerQuality = 30.0f;
            outerBaseIntensity = 1.2f;
            outerIntensityEdgeWidth = 0.28f;
            outerIntensityExponent = 1.1f;
            outerPeakDoseRateRadPerHour = 1.0f;

            NormalizeLegacyFields();
        }

        public void ApplyMetallicPreset()
        {
            beltTiltDegrees = 7.0f;
            beltTiltAxis = Vector3.right;
            beltSpinSpeedDegPerSec = 0f;
            beltSpinPhaseDeg = 0f;
            Enabled = true;

            innerDist = 1.25f;
            innerRadius = 0.15f;
            innerDeformXY = 1.0f;
            innerCompression = 1.15f;
            innerExtension = 1.0f;
            innerBorderDist = 0.95f;
            innerBorderRadius = 0.25f;
            innerBorderDeformXY = 1.0f;
            innerDeform = 0.05f;
            innerHeightScale = 1.0f;
            innerParticleCount = 12000;
            innerQuality = 50.0f;
            innerBaseIntensity = 1.6f;
            innerIntensityEdgeWidth = 0.05f;
            innerIntensityExponent = 2.0f;
            innerPeakDoseRateRadPerHour = 30f;

            // This model has no outer belt in Kerbalism; keep outer empty.
            outerDist = 1.0f;
            outerRadius = 0.1f;
            outerDeformXY = 1.0f;
            outerCompression = 1.0f;
            outerExtension = 1.0f;
            outerBorderDist = 0.0f;
            outerBorderRadius = 0.1f;
            outerBorderDeformXY = 1.0f;
            outerDeform = 0.0f;
            outerHeightScale = 1.0f;
            outerParticleCount = 0;
            outerQuality = 30.0f;
            outerBaseIntensity = 0.0f;
            outerIntensityEdgeWidth = 0.1f;
            outerIntensityExponent = 1.0f;
            outerPeakDoseRateRadPerHour = 0f;

            NormalizeLegacyFields();
        }

        public void ApplySolidIronPreset()
        {
            beltTiltDegrees = 5.0f;
            beltTiltAxis = Vector3.right;
            beltSpinSpeedDegPerSec = 0f;
            beltSpinPhaseDeg = 0f;
            Enabled = true;

            innerDist = 1.38f;
            innerRadius = 0.2f;
            innerDeformXY = 1.0f;
            innerCompression = 1.1f;
            innerExtension = 1.0f;
            innerBorderDist = 1.1f;
            innerBorderRadius = 0.28f;
            innerBorderDeformXY = 1.0f;
            innerDeform = 0.05f;
            innerHeightScale = 1.0f;
            innerParticleCount = 12000;
            innerQuality = 45.0f;
            innerBaseIntensity = 1.4f;
            innerIntensityEdgeWidth = 0.06f;
            innerIntensityExponent = 1.8f;
            innerPeakDoseRateRadPerHour = 24f;

            outerDist = 1.0f;
            outerRadius = 0.1f;
            outerDeformXY = 1.0f;
            outerCompression = 1.0f;
            outerExtension = 1.0f;
            outerBorderDist = 0.0f;
            outerBorderRadius = 0.1f;
            outerBorderDeformXY = 1.0f;
            outerDeform = 0.0f;
            outerHeightScale = 1.0f;
            outerParticleCount = 0;
            outerQuality = 20.0f;
            outerBaseIntensity = 0.0f;
            outerIntensityEdgeWidth = 0.1f;
            outerIntensityExponent = 1.0f;
            outerPeakDoseRateRadPerHour = 0f;

            NormalizeLegacyFields();
        }

        public void ApplyAnomalyPreset()
        {
            beltTiltDegrees = 25.0f;
            beltTiltAxis = Vector3.right;
            beltSpinSpeedDegPerSec = 0f;
            beltSpinPhaseDeg = 0f;
            Enabled = true;

            // Kerbalism anomaly model is pause-only; approximate as a small polar ring.
            innerDist = 0.765f;
            innerRadius = 0.12f;
            innerDeformXY = 0.45f;
            innerCompression = 1.0f;
            innerExtension = 0.8f;
            innerBorderDist = 0.3f;
            innerBorderRadius = 0.16f;
            innerBorderDeformXY = 0.45f;
            innerDeform = 0.05f;
            innerHeightScale = 1.0f;
            innerParticleCount = 8000;
            innerQuality = 50.0f;
            innerBaseIntensity = 2.0f;
            innerIntensityEdgeWidth = 0.04f;
            innerIntensityExponent = 2.2f;
            innerPeakDoseRateRadPerHour = 46f;

            outerDist = 1.0f;
            outerRadius = 0.1f;
            outerDeformXY = 1.0f;
            outerCompression = 1.0f;
            outerExtension = 1.0f;
            outerBorderDist = 0.0f;
            outerBorderRadius = 0.1f;
            outerBorderDeformXY = 1.0f;
            outerDeform = 0.0f;
            outerHeightScale = 1.0f;
            outerParticleCount = 0;
            outerQuality = 50.0f;
            outerBaseIntensity = 0.0f;
            outerIntensityEdgeWidth = 0.1f;
            outerIntensityExponent = 1.0f;
            outerPeakDoseRateRadPerHour = 0f;

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

        /// <summary>
        /// 以防你们瞎几把填数据,加个函数确保输入合法
        /// </summary>
        private void NormalizeLegacyFields()
        {
            
            if (renderMetersPerUnit <= 0f) renderMetersPerUnit = 1_000_000f;
            
            bool legacyTiltUnset = beltTiltAxis.sqrMagnitude <= 1e-6f && Mathf.Abs(beltTiltDegrees) <= 1e-4f;
            if (legacyTiltUnset) beltTiltDegrees = 11.5f;
            if (beltTiltAxis.sqrMagnitude <= 1e-6f) beltTiltAxis = Vector3.right;
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
            if (innerBaseIntensity < 0f) innerBaseIntensity = 0f;
            if (outerBaseIntensity < 0f) outerBaseIntensity = 0f;
            if (innerIntensityEdgeWidth <= 1e-4f) innerIntensityEdgeWidth = 0.08f;
            if (outerIntensityEdgeWidth <= 1e-4f) outerIntensityEdgeWidth = 0.12f;
            if (innerIntensityExponent <= 1e-4f) innerIntensityExponent = 1.0f;
            if (outerIntensityExponent <= 1e-4f) outerIntensityExponent = 1.0f;
            if (innerPeakDoseRateRadPerHour < 0f) innerPeakDoseRateRadPerHour = 0f;
            if (outerPeakDoseRateRadPerHour < 0f) outerPeakDoseRateRadPerHour = 0f;

            // Legacy xml compatibility:
            // older configs don't contain peak dose rate fields and deserialize as 0.
            // If both are 0, backfill with default environment rates.
            if (innerPeakDoseRateRadPerHour <= 1e-6f && outerPeakDoseRateRadPerHour <= 1e-6f)
            {
                innerPeakDoseRateRadPerHour = 35f;
                outerPeakDoseRateRadPerHour = 0.5f;
            }
        }
        

    }
}