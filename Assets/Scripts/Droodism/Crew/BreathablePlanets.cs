using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts.Droodism.Crew
{
    public class BreathablePlanets
    {
        public const string CONFIG_FOLDER = "/UserData/DroodismConfig/Planets";
        [XmlElement("BreathablePlanet")]
        public string[] BreathablePlanet;
        
        private static string GetConfigFolderPath()
        {
            string folderPath = Application.persistentDataPath + CONFIG_FOLDER;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return folderPath;
        }
        private static string GetConfigPath()
        {
            return Path.Combine(GetConfigFolderPath(),"BreathablePlanets.xml");
        }

        public static BreathablePlanets LoadFromFile()
        {
            string filePath = GetConfigPath();
            Debug.Log($"[BreathablePlanets] Attempting to load from: {filePath}");
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[BreathablePlanets] File does not exist at: {filePath}");
                throw new FileNotFoundException($"BreathablePlanets config not found at {filePath}");
            }
            XmlSerializer serializer = new XmlSerializer(typeof(BreathablePlanets));
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                BreathablePlanets config = serializer.Deserialize(stream) as BreathablePlanets;
                Debug.Log($"[BreathablePlanets] Loaded successfully. Planet count: {config?.BreathablePlanet?.Length ?? -1}");
                if (config?.BreathablePlanet != null)
                {
                    Debug.Log($"[BreathablePlanets] Planets: {string.Join(", ", config.BreathablePlanet)}");
                }
                return config;
            }
        }
        
       
    }
}