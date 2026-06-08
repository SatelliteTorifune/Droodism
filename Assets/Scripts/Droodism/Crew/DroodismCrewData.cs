using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts.Droodism.Crew
{
    [SerializeField]
    public class DroodismCrewData
    {
        #region Parameter
        public string CrewName;
        public int CrewID;
        public DroodType CrewRole;
        public double RadiationRate;
        public long MissionTime;
        #endregion

        public DroodismCrewData()
        {
        }

        public DroodismCrewData(XElement xml)
        {
            if (xml == null)
            {
                return;
            }

            CrewName = xml.Element(nameof(CrewName))?.Value ?? string.Empty;
            if (int.TryParse(xml.Element(nameof(CrewID))?.Value, out var crewId))
            {
                CrewID = crewId;
            }
            
            var roleValue = xml.Element(nameof(CrewRole))?.Value;
            if (!string.IsNullOrWhiteSpace(roleValue))
            {
                if (!int.TryParse(roleValue, out var roleInt))
                {
                    Enum.TryParse(roleValue, out DroodType roleParsed);
                    CrewRole = roleParsed;
                }
                else
                {
                    CrewRole = (DroodType)roleInt;
                }
            }

            if (double.TryParse(xml.Element(nameof(RadiationRate))?.Value, out var radiationRate))
            {
                RadiationRate = radiationRate;
            }

            if (double.TryParse(xml.Element(nameof(MissionTime))?.Value, out var missionTime))
            {
                MissionTime = (long)missionTime;
            }
        }

        public XElement GenerateXml()
        {
            return new XElement("CrewMember",
                new XElement(nameof(CrewName), CrewName ?? string.Empty),
                new XElement(nameof(CrewID), CrewID),
                new XElement(nameof(CrewRole), CrewRole.ToString()),
                new XElement(nameof(RadiationRate), RadiationRate),
                new XElement(nameof(MissionTime), MissionTime));
        }
        
    }
}