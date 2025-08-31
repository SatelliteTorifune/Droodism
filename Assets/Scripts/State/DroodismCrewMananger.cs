using System.IO;
using ModApi.Math;
using UnityEngine;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Scenes.Events;
using System.Xml.Serialization;
using System.Collections.Generic;
using Assets.Packages.DevConsole;
using HarmonyLib;
using Assets.Scripts.Flight;
using ModApi.Ui;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Flight.UI;
using UI.Xml;

namespace Assets.Scripts.State
{
    public class DroodismCrewMananger : MonoBehaviour
    {
        public static DroodismCrewMananger Instance { get; private set; }
        public string DroodismDataFilePath { get; private set; }

       
        private List<CrewData> _crewDataList;
        private XmlSerializer _xmlSerializer;
        private CrewDatabase _crewDB;
        private bool _loadTempData = false;
        
        public const int XmlVersion = 1;

        private void Awake()
        {
            Instance = this;
            _crewDataList = new List<CrewData>();
            _xmlSerializer = new XmlSerializer(typeof(CrewDatabase));

            DroodismDataFilePath = Application.persistentDataPath + "/UserData/DroodismData/";
            Directory.CreateDirectory(DroodismDataFilePath);
        }

        private void Start()
        {
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;

            if (!File.Exists(DroodismDataFilePath + "DroodismCrewData.xml")) {
                SaveEventXml();
            }
        }

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight") {

                if (!_loadTempData) {
                    LoadEventXml();
                }
                
                _loadTempData = false;

                LoadEventsFromDatabase();

                Game.Instance.FlightScene.FlightEnded += OnFlightEnded;
            }
        }
        
        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
            switch (e.ExitReason)
            {
                case FlightSceneExitReason.SaveAndDestroy:
                    goto case FlightSceneExitReason.SaveAndExit;
                case FlightSceneExitReason.SaveAndRecover:
                    goto case FlightSceneExitReason.SaveAndExit;
                case FlightSceneExitReason.UndoAndExit:
                    goto case FlightSceneExitReason.Retry;
                case FlightSceneExitReason.Retry:
                        break;
                case FlightSceneExitReason.SaveAndExit:
                    SaveEventsToDatabase();
                    SaveEventXml();
                    _loadTempData = false;
                    break;
                case FlightSceneExitReason.CraftNodeChanged:
                    SaveEventsToDatabase();
                    _loadTempData = true;
                    break;
                case FlightSceneExitReason.QuickLoad:
                    _loadTempData = true;
                    break;
                default:
                    break;
            }
        }

        public void OnQuickSave()
        {
            SaveEventsToDatabase();
            _loadTempData = true;
        }

        private void SaveEventXml()
        {
            FileStream stream = new FileStream(DroodismDataFilePath + "DroodismCrewData.xml", FileMode.Create);
            _xmlSerializer.Serialize(stream, _crewDB);
            stream.Close();
        }

        private void LoadEventXml()
        {
            FileStream stream = new FileStream(DroodismDataFilePath + "DroodismCrewData.xml", FileMode.Open);
            try {
                _crewDB = _xmlSerializer.Deserialize(stream) as CrewDatabase;
            }
            catch (System.Exception e) {
                Debug.LogError("Failed to load events from xml: " + e.Message);
            }

            _crewDB ??= new CrewDatabase { xmlVersion = XmlVersion };
            stream.Close();

            if (_crewDB.xmlVersion != XmlVersion) {
                Debug.LogWarning("Mismatched event xml version, surely it'll be fine :clueless:");
            }
        }

        private void SaveEventsToDatabase()
        {
            string currentGameStateId = Game.Instance.GameState.Id;
            CrewGameState crewList = _crewDB.lists.Find((CrewGameState state) => { return state.gameStateId == currentGameStateId; });

            if (crewList == null)
            {
                crewList = new CrewGameState();
                crewList.gameStateId = currentGameStateId;
                _crewDB.lists.Add(crewList);
            }

            crewList.events.Clear();
            foreach (var item in _crewDataList)
            {
                crewList.events.Add(new CrewData
                {
                    Name = item.Name,
                    id =item.id,
                    MissionTimeTotal = item.MissionTimeTotal,
                    Level=item.Level,
                    role=item.role,
                });
            }
        }

        private void LoadEventsFromDatabase()
        {
            string currentGameStateId = Game.Instance.GameState.Id;
            CrewGameState crewList = _crewDB.lists.Find((CrewGameState state) => { return state.gameStateId == currentGameStateId; });

            if (crewList == null) {
                return;
            }

            _crewDataList.Clear();
            foreach (var item in crewList.events)
            {
                _crewDataList.Add(new CrewData
                {
                    Name = item.Name,
                    id =item.id,
                    MissionTimeTotal = item.MissionTimeTotal,
                    Level=item.Level,
                    role=item.role,
                });
            }
        }

       
    }
    

    public class CrewDatabase
    {
        [XmlAttribute]
        public int xmlVersion;
        [XmlArray("GameStates")]
        public List<CrewGameState> lists = new List<CrewGameState>();
    }

    public class CrewGameState
    {
        [XmlAttribute]
        public string gameStateId;
        [XmlArray("Crew")]
        public List<CrewData> events = new List<CrewData>();
    }

    public class CrewData
    {
        [XmlAttribute]
        public string Name;
        [XmlAttribute]
        public int id;
        [XmlAttribute]
        public long MissionTimeTotal;
        [XmlAttribute]
        public int Level;
        [XmlAttribute]
        public DroodType role;
    }

    
}