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
            XmlSerializer serializer = new XmlSerializer(typeof(BreathablePlanets));
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                return serializer.Deserialize(stream) as BreathablePlanets;
            }
        }
        
       
    }
}