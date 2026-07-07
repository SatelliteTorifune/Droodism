using System;
using System.IO;
using System.Text;
using Assets.Scripts.Droodism.Crew;
using Assets.Scripts.Droodism.RadiationBelt;
using UnityEngine;

namespace Assets.Scripts
{
    public partial class Mod : ModApi.Mods.GameMod
    {
         private void  CheckDefaultPlanetRadiationBeltConfig()
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
                var asset = Mod.ResourceLoader.LoadAsset<TextAsset>("Assets/Resources/DefaultRadiationBeltConfigs/"+planet+".xml");
                if (asset != null)
                {
                    var targetPath = Path.Combine(folderPath, planet+".xml");
                    if (!File.Exists(targetPath))
                    {
                        File.WriteAllText(targetPath, asset.text, Encoding.UTF8);
                    }
                }
            }
            
        }

        private void CheckDefaultBreathablePlanetConfig()
        {
            var folderPath = GetDefaultBreathablePlanetConfigFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            var asset = Mod.ResourceLoader.LoadAsset<TextAsset>("Assets/Resources/BreathablePlanets.xml");
            
            if (asset != null)
            {
                var targetPath = Path.Combine(folderPath, "BreathablePlanets.xml");
                if (!File.Exists(targetPath))
                {
                    File.WriteAllText(targetPath, asset.text, Encoding.UTF8);
                     Debug.LogFormat($"[Droodism] Copied BreathablePlanets.xml to: {targetPath}");
                }
            }
            else
            {
                Debug.LogErrorFormat("[Droodism] Failed to load BreathablePlanets.xml from Resources!");
            }
        }
        private  static string GetRadiationBeltConfigFolderPath()
        {
            string folderPath = Application.persistentDataPath + RadiationBeltConfig.CONFIG_FOLDER;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
            
        }
        private  static string GetDefaultBreathablePlanetConfigFolderPath()
        {
            string folderPath = Application.persistentDataPath + BreathablePlanets.CONFIG_FOLDER;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }

        private void CheckDefaultFlagImage()
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
                defaultImageAsset = Instance.ResourceLoader.LoadAsset<TextAsset>(defaultImageResourcePath);
            }
            catch (Exception e)
            {
                LogError($"[Droodism] Failed to load default flag image asset at '{defaultImageResourcePath}': {e}");
            }
            

            try
            {
                File.WriteAllBytes(targetPath, defaultImageAsset.bytes);
                Log($"[Droodism] Wrote default flag image to: {targetPath}");
            }
            catch (Exception e)
            {
                LogError($"[Droodism] Failed to write default flag image to '{targetPath}': {e}");
            }
        }

        private void CheckLocalizationFiles(string targetLanguage)
        {
            var targetPath = Path.Combine(Application.persistentDataPath, "Languages", targetLanguage, "StringsDroodism.xml");
            if (File.Exists(targetPath))
            {
                return; 
            }
            
            var localizationFile = Mod.ResourceLoader.LoadAsset<TextAsset>("Assets/Resources/LocalizationFile/"+targetLanguage+"/StringsDroodism.xml");
            try
            {
                File.WriteAllBytes(targetPath, localizationFile.bytes);
                Debug.LogFormat($"[Droodism] Wrote {targetLanguage} localization file to: {targetPath}");
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat($"[Droodism] Failed to write {targetLanguage} localization file to '{targetPath}': {e}");
            }
        }
    }
}