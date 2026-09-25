using System;
using System.IO;
using System.Text;
using Assets.Scripts.Droodism.Crew;
using Assets.Scripts.Droodism.RadiationBelt;
using UnityEngine;

namespace Assets.Scripts
{
    public static class DroodismFilesSetUp
    {
        

        /// <summary>执行全部默认文件配置。</summary>
        public static void SetUp()
        {
            CheckDefaultPlanetRadiationBeltConfig();
            CheckDefaultBreathablePlanetConfig();
            CheckDefaultFlagImage();
        }

        private static void CheckDefaultPlanetRadiationBeltConfig()
        {
            var folderPath = GetRadiationBeltConfigFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            SetUp("Cylero");
            SetUp("Droo");
            SetUp("Earth");
            SetUp("Miros");
            SetUp("Nebra");
            SetUp("Oord");
            SetUp("Orcus");
            SetUp("Sergeaa");
            SetUp("Taurus");
            SetUp("Tydos");
            SetUp("Urados");
            SetUp("Vulco");
            void SetUp(string planet)
            {
                var asset = Mod.Instance.ResourceLoader.LoadAsset<TextAsset>("Assets/Resources/DefaultRadiationBeltConfigs/" + planet + ".xml");
                if (asset != null)
                {
                    var targetPath = Path.Combine(folderPath, planet + ".xml");
                    if (!File.Exists(targetPath))
                    {
                        File.WriteAllText(targetPath, asset.text, Encoding.UTF8);
                    }
                }
            }
        }

        private static void CheckDefaultBreathablePlanetConfig()
        {
            var folderPath = GetDefaultBreathablePlanetConfigFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            var asset = Mod.Instance.ResourceLoader.LoadAsset<TextAsset>("Assets/Resources/BreathablePlanets.xml");

            if (asset != null)
            {
                var targetPath = Path.Combine(folderPath, "BreathablePlanets.xml");
                if (!File.Exists(targetPath))
                {
                    File.WriteAllText(targetPath, asset.text, Encoding.UTF8);
                    Mod.Log("Copied BreathablePlanets.xml to: {0}", targetPath);
                }
            }
            else
            {
                Mod.Log("Failed to load BreathablePlanets.xml from Resources!");
            }
        }

        private static string GetRadiationBeltConfigFolderPath()
        {
            string folderPath = Application.persistentDataPath + RadiationBeltConfig.CONFIG_FOLDER;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }

        private static string GetDefaultBreathablePlanetConfigFolderPath()
        {
            string folderPath = Application.persistentDataPath + BreathablePlanets.CONFIG_FOLDER;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }

        private static void CheckDefaultFlagImage()
        {
            const string defaultImageResourcePath = "Assets/Resources/DefaultFlag1.bytes";
            const string defaultImageFileName = "CustomImage.jpg";

            var folderPath = Path.Combine(Application.persistentDataPath, "UserData", "DroodismConfig", "FlagImage");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var targetPath = Path.Combine(folderPath, defaultImageFileName);
            if (File.Exists(targetPath))
            {
                return;
            }

            TextAsset defaultImageAsset = null;
            try
            {
                defaultImageAsset = Mod.Instance.ResourceLoader.LoadAsset<TextAsset>(defaultImageResourcePath);
            }
            catch (Exception e)
            {
                Mod.Log("Failed to load default flag image asset at '{0}': {1}", defaultImageResourcePath, e);
            }

            try
            {
                File.WriteAllBytes(targetPath, defaultImageAsset.bytes);
                Mod.Log("Wrote default flag image to: {0}", targetPath);
            }
            catch (Exception e)
            {
                Mod.Log("Failed to write default flag image to '{0}': {1}", targetPath, e);
            }
        }
    }
}