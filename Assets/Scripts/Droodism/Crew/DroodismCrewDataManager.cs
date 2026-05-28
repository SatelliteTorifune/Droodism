using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using ModApi.GameLoop;
using System.Reflection;
using ModApi.Scenes.Events;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Droodism.Crew
{
    public class DroodismCrewDataManager:MonoBehaviour
    {
        public static DroodismCrewDataManager Instance { get; private set; }

        private readonly List<DroodismCrewData> _members = new List<DroodismCrewData>();
        private int _nextCrewMemberId = 1;
        private string _currentGameSaveName;
        private bool _loaded;
        private Dictionary<int, string> _gameCrewNameById = new Dictionary<int, string>();
        private object _lastKnownGameStateInstance;
        

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Instance = this;
            if (!_loaded)
            {
                LoadForCurrentGameSave();
            }

            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(object sender,SceneEventArgs e)
        {
            try
            {
                var gameState = Game.Instance.GameState;
                if (gameState == null)
                {
                    return;
                }

                var gameSaveName = GetCurrentGameSaveName();
                var sanitized = SanitizeFileName(gameSaveName);

                bool gameStateChanged = !ReferenceEquals(gameState, _lastKnownGameStateInstance);
                bool saveNameChanged = !string.Equals(sanitized, _currentGameSaveName, StringComparison.Ordinal);
                
                _lastKnownGameStateInstance = gameState;

                if (gameStateChanged || saveNameChanged)
                {
                    LoadForSave(gameSaveName);
                    EnsureSyncedWithGameCrewManager(saveNow: true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Droodism] Crew sync on scene load failed: {ex}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (Game.Instance?.SceneManager != null)
                {
                    Game.Instance.SceneManager.SceneLoaded -= OnSceneLoaded;
                }
            }
            catch
            {
                // ignore
            }
        }
        
        public void LoadForCurrentGameSave()
        {
            _loaded = true;
            var gameSaveName = GetCurrentGameSaveName();
            LoadForSave(gameSaveName);
        }

        public void LoadForSave(string gameSaveName)
        {
            if (string.IsNullOrWhiteSpace(gameSaveName))
            {
                gameSaveName = "UnknownSave";
            }

            _currentGameSaveName = SanitizeFileName(gameSaveName);

            _members.Clear();
            _nextCrewMemberId = 1;

            string filePath = GetCrewMembersXmlPath(_currentGameSaveName);
            bool fileExisted = File.Exists(filePath);
            bool parseFailed = false;
            if (!fileExisted)
            {
                // 即使没有 XML，也要尝试从游戏 crew manager 同步条目（保证 name/id 对齐）
                SyncWithGameCrewManager();
                Save(); // 首次进入存档时也把 xml 生成出来，避免你看到“只有加 crew 才保存”
                return;
            }

            try
            {
                var root = XDocument.Load(filePath).Root;
                if (root == null || root.Name != "CrewMembers")
                {
                    return;
                }

                if (int.TryParse((string)root.Attribute("nextId"), out var nextId))
                {
                    _nextCrewMemberId = Math.Max(1, nextId);
                }

                foreach (var memberElement in root.Elements("CrewMember"))
                {
                    var data = new DroodismCrewData(memberElement);
                    // 去重：以 CrewID 为唯一键
                    if (_members.All(m => m.CrewID != data.CrewID))
                    {
                        _members.Add(data);
                    }
                    else
                    {
                        // 如果有重复，更新为最新读取的那条
                        var old = _members.First(m => m.CrewID == data.CrewID);
                        old.CrewName = data.CrewName;
                        old.CrewRole = data.CrewRole;
                        old.RadiationRate = data.RadiationRate;
                    }

                    _nextCrewMemberId = Math.Max(_nextCrewMemberId, data.CrewID + 1);
                }
            }
            catch (Exception)
            {
                // 解析失败就保持空列表与 nextId=1，避免阻断进入游戏
                _members.Clear();
                _nextCrewMemberId = 1;
                parseFailed = true;
            }

            // 最关键：用“游戏 CrewManager”为准，把我们加载的 radiation 迁移到正确的 game crew id 上
            SyncWithGameCrewManager();

            // 解析失败则用同步后的数据落盘，避免缺项/空文件
            if (parseFailed)
            {
                Save();
            }
        }

        public DroodismCrewData CreateCrewMember(string crewName, DroodType crewRole, double lifetimeRadiation = 0)
        {
            // 按你的要求：id/name 必须和游戏一致。
            // 由于我们无法在游戏里“创建 crew”，所以只有当该 crewName 在游戏中已存在时才允许创建条目。
            if (!TryGetGameCrewIdByName(crewName, out var crewId))
            {
                throw new InvalidOperationException($"Cannot create Droodism crew entry for '{crewName}': not found in game CrewManager.");
            }

            var existing = GetCrewMember(crewId);
            if (existing != null)
            {
                // 只更新 radiation（避免 name/id 漂移）
                existing.RadiationRate = lifetimeRadiation;
                Save();
                return existing;
            }

            var member = new DroodismCrewData
            {
                CrewID = crewId,
                CrewName = _gameCrewNameById.TryGetValue(crewId, out var n) ? n : crewName,
                CrewRole = crewRole,
                RadiationRate = lifetimeRadiation
            };

            _members.Add(member);
            _nextCrewMemberId = Math.Max(_nextCrewMemberId, crewId + 1);
            Save();
            return member;
        }
        
        public DroodismCrewData GetCrewMember(int crewId)
        {
            return _members.FirstOrDefault(m => m.CrewID == crewId);
        }

        public void SetLifetimeRadiation(int crewId, double lifetimeRadiation, bool saveImmediately = true)
        {
            var member = GetCrewMember(crewId);
            if (member == null)
            {
                return;
            }

            member.RadiationRate = lifetimeRadiation;
            if (saveImmediately)
            {
                Save();
            }
        }

        
        private DroodType GetRandomDroodPost()
        {
            int i = new System.Random().Next(0, 2);
            return (DroodType)i;
        }

        public void Save()
        {
            if (string.IsNullOrWhiteSpace(_currentGameSaveName))
            {
                _currentGameSaveName = GetCurrentGameSaveName();
            }

            // 保存前保证 name/id 对齐（很轻量：只更新已有 entries 的 CrewName）
            SyncWithGameCrewManager(updateNamesOnly: true);

            string filePath = GetCrewMembersXmlPath(_currentGameSaveName);
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var root = new XElement("CrewMembers",
                new XAttribute("nextId", _nextCrewMemberId));

            foreach (var member in _members)
            {
                if (member == null) continue;
                root.Add(member.GenerateXml());
            }

            new XDocument(root).Save(filePath);
        }
        
        public void EnsureSyncedWithGameCrewManager(bool saveNow = true)
        {
            SyncWithGameCrewManager(updateNamesOnly: false);
            if (saveNow)
            {
                Save();
            }
        }

        private static string GetCrewMembersXmlPath(string gameSaveName)
        {
            var folder = GetConfigFolderPath();
            return Path.Combine(folder, $"{gameSaveName}.xml");
        }

        private static string GetConfigFolderPath()
        {
            return Application.persistentDataPath + "/UserData/DroodismData/CrewMembers/";
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "UnknownSave";
            }

            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(ch, '_');
            }

            return name;
        }

        private static string GetCurrentGameSaveName()
        {
            try
            {
                var gameState = Game.Instance?.GameState;
                if (gameState == null)
                {
                    return "UnknownSave";
                }

                // 优先尝试 Id（如果 ModApi 的 GameState 有该字段）
                var idProp = gameState.GetType().GetProperty("Id");
                if (idProp != null)
                {
                    var id = idProp.GetValue(gameState) as string;
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        return id;
                    }
                }

                // 兜底：使用 RootPath 最后一段目录名作为“存档名”
                var rootPath = gameState.RootPath;
                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    return "UnknownSave";
                }

                rootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return new DirectoryInfo(rootPath).Name;
            }
            catch
            {
                return "UnknownSave";
            }
        }

        private void SyncWithGameCrewManager(bool updateNamesOnly = false)
        {
            // 尝试从游戏的 CrewManager 拉取 id/name
            if (!TryGetGameCrewIdNameLookup(out var lookup))
            {
                return;
            }

            _gameCrewNameById = lookup;

            if (updateNamesOnly)
            {
                foreach (var m in _members)
                {
                    if (m == null) continue;
                    if (_gameCrewNameById.TryGetValue(m.CrewID, out var newName))
                    {
                        m.CrewName = newName;
                    }
                }

                // 保证“当前游戏存在的 crew”都有条目（若之前没生成过 radiation 条目，则写入 0）
                foreach (var kv in _gameCrewNameById)
                {
                    int crewId = kv.Key;
                    string crewName = kv.Value;
                    if (_members.All(m => m != null && m.CrewID != crewId))
                    {
                        _members.Add(new DroodismCrewData
                        {
                            CrewID = crewId,
                            CrewName = crewName,
                            CrewRole = GetRandomDroodPost(),
                            RadiationRate = 0
                        });
                    }
                }
                return;
            }

            // 迁移策略：
            // 1) 若 XML 里存在同 CrewID，则直接保留 RadiationRate
            // 2) 否则若 XML 里存在同 CrewName，则迁移 RadiationRate 到 game 的 CrewID
            // 3) 否则 radiation = 0
            var oldById = _members.ToDictionary(m => m.CrewID, m => m);
            var oldByName = _members
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.CrewName))
                .GroupBy(m => m.CrewName)
                .ToDictionary(g => g.Key, g => g.First());

            var newMembers = new List<DroodismCrewData>();
            int? maxId = null;

            foreach (var kv in _gameCrewNameById)
            {
                int crewId = kv.Key;
                string crewName = kv.Value;

                DroodismCrewData migrated = null;
                if (oldById.TryGetValue(crewId, out var byId))
                {
                    migrated = byId;
                    migrated.CrewName = crewName;
                }
                else if (oldByName.TryGetValue(crewName, out var byName))
                {
                    migrated = byName;
                    migrated.CrewID = crewId;
                    migrated.CrewName = crewName;
                }
                else
                {
                    migrated = new DroodismCrewData
                    {
                        CrewID = crewId,
                        CrewName = crewName,
                        CrewRole = GetRandomDroodPost(),
                        RadiationRate = 0
                    };
                }

                newMembers.Add(migrated);
                maxId = maxId.HasValue ? Math.Max(maxId.Value, crewId) : crewId;
            }

            _members.Clear();
            _members.AddRange(newMembers);
            _nextCrewMemberId = maxId.HasValue ? maxId.Value + 1 : 1;
            
        }

        private bool TryGetGameCrewIdNameLookup(out Dictionary<int, string> lookup)
        {
            lookup = new Dictionary<int, string>();
            try
            {
                var gameState = Game.Instance?.GameState;
                if (gameState == null)
                {
                    return false;
                }

                var crewProp = gameState.GetType().GetProperty("Crew", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var crewObj = crewProp?.GetValue(gameState);
                if (crewObj == null)
                {
                    return false;
                }

                // CrewManager.Members
                var membersProp = crewObj.GetType().GetProperty("Members", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var membersObj = membersProp?.GetValue(crewObj);
                if (membersObj == null)
                {
                    return false;
                }

                foreach (var member in (System.Collections.IEnumerable)membersObj)
                {
                    if (member == null) continue;

                    int id = ReadIntMember(member, "Id", "CrewId", "CrewID", "NodeId");
                    string name = ReadStringMember(member, "Name", "CrewName", "CrewName");

                    if (id > 0 && !string.IsNullOrWhiteSpace(name))
                    {
                        lookup[id] = name;
                    }
                }

                return lookup.Count > 0;
            }
            catch
            {
                lookup.Clear();
                return false;
            }
        }

        private bool TryGetGameCrewIdByName(string crewName, out int crewId)
        {
            crewId = 0;
            if (string.IsNullOrWhiteSpace(crewName))
            {
                return false;
            }

            try
            {
                if (_gameCrewNameById.Count == 0)
                {
                    TryGetGameCrewIdNameLookup(out _);
                }

                var match = _gameCrewNameById.FirstOrDefault(kv => string.Equals(kv.Value, crewName, StringComparison.Ordinal));
                if (match.Key != 0)
                {
                    crewId = match.Key;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static int ReadIntMember(object obj, params string[] candidateNames)
        {
            foreach (var name in candidateNames)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.PropertyType == typeof(int))
                {
                    return (int)prop.GetValue(obj);
                }

                var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(int))
                {
                    return (int)field.GetValue(obj);
                }
            }

            return 0;
        }

        private static string ReadStringMember(object obj, params string[] candidateNames)
        {
            foreach (var name in candidateNames)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    return (string)prop.GetValue(obj);
                }

                var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(string))
                {
                    return (string)field.GetValue(obj);
                }
            }

            return null;
        }
    }
}