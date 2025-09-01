using System;
using System.IO;
using ModApi.Math;
using UnityEngine;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Scenes.Events;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UI.Xml;
using Random = System.Random;


namespace Assets.Scripts.State
{
    public class DroodismCrewMananger : MonoBehaviour
    {
        public static DroodismCrewMananger Instance { get; private set; }
        public string DroodismDataFilePath { get; private set; }


        public List<CrewData> _crewDataList { get; private set; }
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
            OnSceneLoaded(null, new SceneEventArgs("Menu"));
            if (!File.Exists(DroodismDataFilePath + "DroodismCrewData.xml"))
            {
                SaveXml();
            }
        }

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (!_loadTempData) 
            {
                LoadXml();
            }
                
            _loadTempData = false;

            LoadCrewDataFromDatabase();
            if (e.Scene == "Flight") {

                Game.Instance.FlightScene.FlightEnded += OnFlightEnded;
            }

            if (e.Scene == "Menu")
            {
                //return;
                Debug.LogFormat("从OnSceneLoaded调用SaveCrewDataToDatabase");
                SaveCrewDataToDatabase();
                SaveXml(); 
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
                    SaveCrewDataToDatabase();
                    SaveXml();
                    _loadTempData = false;
                    break;
                case FlightSceneExitReason.CraftNodeChanged:
                    SaveCrewDataToDatabase();
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
            SaveCrewDataToDatabase();
            _loadTempData = true;
        }

        public void SaveXml()
        {
            Debug.Log("DroodismCrewManager.SaveXml() called");
            FileStream stream = new FileStream(DroodismDataFilePath + "DroodismCrewData.xml", FileMode.Create);
            _xmlSerializer.Serialize(stream, _crewDB);
            stream.Close();
        }

        private void LoadXml()
        {
            Debug.Log("DroodimCrewManager.LoadXml()");
            FileStream stream = new FileStream(DroodismDataFilePath + "DroodismCrewData.xml", FileMode.Open);
            try {
                _crewDB = _xmlSerializer.Deserialize(stream) as CrewDatabase;
            }
            catch (Exception e) {
                Debug.LogError("Failed to load events from xml: " + e.Message);
            }

            _crewDB ??= new CrewDatabase { xmlVersion = XmlVersion };
            stream.Close();

            if (_crewDB.xmlVersion != XmlVersion) {
                Debug.LogWarning("Mismatched event xml version, surely it'll be fine :clueless:");
            }
        }

        public void SaveCrewDataToDatabase()
        {
            Debug.Log("DroodismCrewManager.SaveCrewDataToDatabase() called");
            string currentGameStateId = Game.Instance.GameState.Id;
            CrewGameState crewList = _crewDB.lists.Find((CrewGameState state) => { return state.gameStateId == currentGameStateId; });

            if (crewList == null)
            {
                crewList = new CrewGameState();
                crewList.gameStateId = currentGameStateId;
                _crewDB.lists.Add(crewList);
            }

            crewList.crew.Clear();
            foreach (var item in _crewDataList)
            {
                Debug.LogFormat($"SaveCrewDataToDatabase : {item.Name}");
                crewList.crew.Add(new CrewData
                {
                    Name = item.Name,
                    id =item.id,
                    MissionTimeTotal = item.MissionTimeTotal,
                    Level=item.Level,
                    role=item.role,
                });
            }
        }

        private void LoadCrewDataFromDatabase()
        {
            Debug.Log("DroodimCrewManager.LoadCrewDataFromDatabase");
            string currentGameStateId = Game.Instance.GameState.Id;
            CrewGameState crewList = _crewDB.lists.Find((CrewGameState state) => { return state.gameStateId == currentGameStateId; });

            if (crewList == null) {
                return;
            }

            _crewDataList.Clear();
            foreach (var item in crewList.crew)
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
        
        public void CreateCrewData(string _name, int _id, long _missionTimeTotal, int _level, DroodType _role)
        
        {
            if (_id==0)
            {
                return;
            }
            CrewData tempCrewData = new CrewData
            {
                Name=_name,
                id=_id,
                MissionTimeTotal = _missionTimeTotal,
                Level = _level,
                role=_role
                
            };
            
            _crewDataList.Add(tempCrewData);
            _crewDataList.Sort((CrewData a, CrewData b) => a.id.CompareTo(b.id));
        }

        public void CreateCrewDataFromGameStates()
        {
            List<string> crewNameListTemp = new List<string>();
            foreach (var cd in _crewDataList)
            {
                crewNameListTemp.Add(cd.Name);
            }
            foreach (var crewMember in Game.Instance.GameState.Crew.Members)
            {
                if (crewMember.State==CrewMemberState.Available&&!crewNameListTemp.Contains(crewMember.Name))
                {
                    CreateCrewData(crewMember.Name,crewMember.Id,0,1,DroodType.Pilot);
                }
            }
        }

        /*
        public CrewData GetCrewData(CrewMember crewMember)
        {
            
            if(crewMember.Id >= 0 && crewMember.Id < _crewDataList.Count) {
                return _crewDataList[crewMember.Id];
            }
            Debug.LogWarning("Droodism DroodismCrewManger.GetCrewData(int): Invalid Input,Unable to find CrewData,try searching by name");
            foreach (var crewData in _crewDataList)
            {
                if (crewData.Name==crewMember.Name)
                {
                    return crewData;
                }
            }
            Debug.LogWarning("Droodism DroodismCrewManger.GetCrewData(string):Unable to find CrewData,retuning empty CrewData");
            return new CrewData();
        }*/
        public void EditCrewData(int id, CrewData crewData)
        {
            if(id >= 0 && id < _crewDataList.Count) 
            {
                _crewDataList[id]=crewData;
                Debug.LogFormat("DroodismCrewManager.EditCrewData 成功执行");
            }
        }

        public void CreateCrewData(CrewData crewData)
        {
            if (crewData.id!=0)
            {
                _crewDataList.Add(crewData);
            }
        }

        
        public DroodType GetDroodType(string crewName)
        {
            if (crewName.StartsWith("Yuri G")||crewName.StartsWith("Sally R"))
            {
                return DroodType.Pilot;
            }
            Random random = new Random(StringToBinaryInt(crewName));
            int randomInt = random.Next(0, 2);
            Debug.LogFormat("随机数生成了,{0},种子{1}",randomInt,StringToBinaryInt(crewName));
            return randomInt==0?DroodType.Engineer:randomInt==1?DroodType.Scientist:DroodType.Pilot;
            
            int StringToBinaryInt(string input)
            {
                // 使用UTF-8编码将字符串转换为字节数组
                byte[] bytes = Encoding.UTF8.GetBytes(input);

                // 检查字节数组是否为空
                if (bytes.Length == 0)
                {
                    throw new ArgumentException("Input string is empty.");
                }

                // 取字节数组的最后一个字节
                byte lastByte = bytes[bytes.Length - 1];

                // 将最后一个字节转换为int
                return Convert.ToInt32(lastByte);
            }
            

        }
        
        /*
        //逼养的泛型约束到底是谁在用
        public void EditCrewData<T>(T key, CrewData _crewData) where T : IComparable
        {
            if (key is int id)
            {
                var crew = GetCrewData(id);
                if (crew != null)
                {
                    // 编辑逻辑
                }
            }
            else if (key is string name)
            {
                var crew = GetCrewData(name);
                if (crew != null)
                {
                    // 编辑逻辑
                }
            }
            else
            {
                Debug.LogWarning(
                    "Droodism DroodismCrewManger.EditCrewData(): Invalid key type. Only int and string are allowed.");
            }
        }*/
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
        public List<CrewData> crew = new List<CrewData>();
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