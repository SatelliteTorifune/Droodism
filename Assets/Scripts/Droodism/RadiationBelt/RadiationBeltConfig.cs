
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
        public float innerDist ; // 主半径
        public float innerRadius ; // 管半径
        public float innerDeform;
        public float innerHeightScale;
        public int innerParticleCount ; // 粒子数
        public float innerQuality ; // 质量 (越高越薄)

        public float outerDist ;
        public float outerRadius ;
        public float outerBorderStart ; // 内减法渐变
        public float outerBorderEnd ;
        public float outerCompression; // 太阳侧压缩
        public float outerExtension; // 尾侧拉伸
        public float outerDeform;
        public float outerHeightScale;
        public int outerParticleCount ;
        public float outerQuality;
        
        public Vector3 starDirection = Vector3.left;
        

        #endregion
        public float GetInnerDist(Vector3 pciPos)   // pciPos 是行星本地坐标 (PCI)
        {
            Vector3 p = pciPos;
            float dot = Vector3.Dot(p.normalized, starDirection);
            float deformFactor = Mathf.Lerp(outerCompression, outerExtension, (dot + 1f) / 2f);
            p /= deformFactor;

            p += Mathf.Sin(p.magnitude * 5f) * innerDeform * p.normalized;
            p.y *= innerHeightScale;  // 高度缩放，控制圆度

            Vector2 q = new Vector2(new Vector2(p.x, p.z).magnitude - innerDist, p.y);
            return q.magnitude - innerRadius;
        }

        public float GetOuterDist(Vector3 pciPos)
        {
            Vector3 p = pciPos;
            float dot = Vector3.Dot(p.normalized, starDirection);
            float deformFactor = Mathf.Lerp(outerCompression, outerExtension, (dot + 1f) / 2f);
            p /= deformFactor;

            p += Mathf.Sin(p.magnitude * 5f) * outerDeform * p.normalized;
            p.y *= outerHeightScale;

            Vector2 q = new Vector2(new Vector2(p.x, p.z).magnitude - outerDist, p.y);
            float outer = q.magnitude - outerRadius;

            Vector2 q_sub = new Vector2(new Vector2(p.x, p.z).magnitude - outerDist * 0.8f, p.y);
            float subtract = q_sub.magnitude - outerRadius * 0.7f;

            float border = Mathf.Lerp(outerBorderStart, outerBorderEnd, Mathf.Clamp01(p.magnitude / outerDist));

            return Mathf.Max(outer, -subtract - border);
        }

        // ====================== 判断 craft 是否在辐射带内 ======================
        /// <summary>
        /// craft 是否在内带（用 PCI 坐标判断）
        /// </summary>
        public bool IsInInnerBelt(Vector3d craftWorldPos, Transform planetTransform)
        {
            Vector3 pci = planetTransform.InverseTransformPoint((Vector3)craftWorldPos);
            return GetInnerDist(pci) <= 0f;
        }

        public bool IsInOuterBelt(Vector3d craftWorldPos, Transform planetTransform)
        {
            Vector3 pci = planetTransform.InverseTransformPoint((Vector3)craftWorldPos);
            return GetOuterDist(pci) <= 0f;
        }

        /// <summary>
        /// 返回辐射强度因子（0~1），内带强、外带弱
        /// </summary>
        public float GetRadiationIntensity(Vector3d craftWorldPos, Transform planetTransform)
        {
            Vector3 pci = planetTransform.InverseTransformPoint((Vector3)craftWorldPos);
            float dInner = GetInnerDist(pci);
            float dOuter = GetOuterDist(pci);

            if (dInner <= 0f)
                return Mathf.Clamp01(1f - dInner / 0.1f);  // 内带强度高
            if (dOuter <= 0f)
                return 0.4f * Mathf.Clamp01(1f - dOuter / 0.3f);  // 外带强度中
            return 0f;
        }
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
            defaultCFG.innerDist = 2f;
            defaultCFG.innerRadius = 0.5f;
            defaultCFG.innerDeform = 0.2f;
            defaultCFG.innerParticleCount = 8000;
            defaultCFG.innerQuality = 30f;
            defaultCFG.outerHeightScale = 1f;
            defaultCFG.innerHeightScale = 1f;
            defaultCFG.outerDist = 5f;
            defaultCFG.outerRadius = 1.5f;
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