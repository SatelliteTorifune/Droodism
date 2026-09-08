using System;
using System.Collections;
using System.Text.RegularExpressions;
using ModApi;
using ModApi.Ui;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts // ===== 耦合点⑤:命名空间,移植到新 Mod 时改成它的命名空间(避免与其他 Mod 冲突)=====
{
    /// <summary>
    /// 通用 Mod 更新检查 + 提醒弹窗系统(移植自 Volken2 的 ModUpdater.cs,已按 Droodism 适配)。
    ///
    /// 【它做什么】
    ///   1. 读取本地版本(Mod.Instance.ModVersion,类型 System.Version,如 0.88);
    ///   2. 双通道获取"网站最新版本":
    ///       通道1:GitHub Releases API 的 tag_name(主);
    ///       通道2:raw 直链 version.txt 兜底(API 限流/断网/还没建过 release 时自动启用);
    ///   3. 网站最新 &gt; 本地版本 且玩家没点过"不再提醒" → 等进主菜单弹三按钮弹窗
    ///      (下载更新 / 稍后再说 / 不再提醒);
    ///   4. "不再提醒"用 PlayerPrefs 记住跳过的版本,以后只有出现更新版本才再次提醒。
    ///
    /// 每次游戏会话只检查一次(static _startedThisSession 防抖)。
    ///
    /// ==================================================================
    /// 【移植到其他 Mod 时,复制本文件后按下面清单逐个检查】(耦合点已用 ★ 标注)
    /// ==================================================================
    ///  ★①  三个 URL 常量(本文件上方):LatestVersionUrl / VersionFileUrl / DownloadUrl
    ///         → 换成新 Mod 的仓库/发布页地址。VersionFileUrl 留空 = 禁用兜底通道。
    ///  ★②  本地版本来源:CheckForUpdate() 里的 `Mod.Instance.ModVersion`
    ///         → 换成新 Mod 自己的版本读取(通常同样是 ModInfo.Version)。
    ///  ★③  日志:Mod.Log(...) → 换成新 Mod 的日志方法,或直接 Debug.Log(...)。
    ///  ★④  弹窗文案:Locale.GetString("Droodism.UI.Update*")
    ///         → 换成新 Mod 的本地化 key;没有本地化就直接写字符串字面量。
    ///  ★⑤  命名空间:namespace Assets.Scripts(本文件顶部)
    ///         → 换成新 Mod 的命名空间。
    ///  ★⑥  主菜单判定:Game.InMenuScene
    ///         → 想在其他场景弹就改这里。
    ///  ★⑦  PlayerPrefs key:SkippedVersionPrefKey(下方)
    ///         → 换个带自己 Mod 名的 key,避免与其他 Mod 互相覆盖"不再提醒"状态。
    ///  ★⑧  触发点:在目标 Mod 的 OnModLoaded 末尾调用
    ///         `new ModUpdater().CheckForUpdate();`
    ///         ——必须在本地版本赋值之后调用(见 Mod.cs 的写法)。
    ///
    /// 【依赖】全部是游戏 ModApi / Unity 自带,无需额外包:
    ///   - ModApi.Ui:IUserInterface.CreateMessageDialog / MessageDialogScript / MessageDialogType
    ///   - UnityEngine:UnityWebRequest / PlayerPrefs / Application.OpenURL / MonoBehaviour
    ///   - System.Version(版本比较)
    ///
    /// 【弹窗文案本地化】
    ///   语言文件 Assets/Content/Languages/{EN-US,ZH-CN}.xml 中的 6 个 key:
    ///   Droodism.UI.UpdateAvailable / UpdateNewVersion / UpdateCurrentVersion /
    ///   UpdateDownload / UpdateLater / UpdateDismiss。
    ///   新 Mod 需要在自己的语言文件里加等价的 key(或直接用字面量,见 ★④)。
    /// </summary>
    public class ModUpdater
    {
        // ==================================================================
        // ★① 网站最新版本来源(移植时改这里)
        // ==================================================================
        // 通道1:GitHub Releases API——返回 JSON,取 "tag_name" 即最新版本号
        //        (如 "0.88" / "v0.88.1",前导 v 会被忽略)。
        // 发版方式:在 GitHub 上 Create a new release,打 tag 如 0.88 并上传 .sr2-mod 资产。
        public const string LatestVersionUrl = "https://api.github.com/repos/SatelliteTorifune/Droodism/releases/latest";

        // 点"下载更新"时打开的页面:Release 列表页(或改成具体某个 release 的页面)。
        public const string DownloadUrl = "https://github.com/SatelliteTorifune/Droodism/releases/latest";

        // 通道2(兜底):GitHub raw 直链的 version.txt(仓库根目录,内容就是版本号,如 "0.88")。
        // 用途:通道1 失败(403 限流 / 网络异常 / 还没建过 release)时自动启用;无 API 限流。
        // 注意:指向 main 分支——发版时记得把 version.txt 同步到 main。
        // 移植:改成新仓库地址;留空("")则禁用兜底通道。
        public const string VersionFileUrl = "https://raw.githubusercontent.com/SatelliteTorifune/Droodism/main/version.txt";

        // ★⑦ 玩家"不再提醒"记住的版本存哪。移植时换带自己 Mod 名的 key,避免互相覆盖。
        private const string SkippedVersionPrefKey = "Droodism.UpdateReminder.SkippedVersion";

        // 防抖:一次游戏会话只检查一次(static 跨实例共享)。
        private static bool _startedThisSession;

        private Version _localVersion;
        private ModUpdaterHost _host;

        /// <summary>
        /// 启动一次更新检查(每次游戏会话最多执行一次,重复调用会被忽略)。
        /// 在 Mod 的 OnModLoaded 末尾调用(★⑧)。
        /// </summary>
        public void CheckForUpdate()
        {
            try
            {
                if (_startedThisSession) return;
                _startedThisSession = true;

                // ★② 本地版本来源:Droodism 读 Mod.Instance.ModVersion
                //    (在 Mod.cs 的 OnModLoaded 里由 ModInfo.Version 赋值)。类型须为 System.Version。
                _localVersion = Mod.Instance.ModVersion;
                if (_localVersion == null)
                {
                    Mod.Log("Droodism: 更新检查跳过——ModVersion 为空"); // ★③
                    return;
                }

                if (string.IsNullOrWhiteSpace(LatestVersionUrl))
                {
                    // 兜底:URL 为空(如调试时临时清空)则只打日志,不弹窗
                    Mod.Log("Droodism: 更新检查——LatestVersionUrl 未配置,当前版本 {0}", _localVersion); // ★③
                    return;
                }

                // 协程宿主:非 MonoBehaviour 类用隐藏 GameObject 跑 UnityWebRequest 协程。
                if (_host == null)
                {
                    var go = new GameObject("DroodismUpdateReminder"); // 名字随意,仅便于排查
                    GameObject.DontDestroyOnLoad(go);
                    _host = go.AddComponent<ModUpdaterHost>();
                    _host.Owner = this;
                }
            }
            catch (Exception ex)
            {
                Mod.Log("Droodism: 更新检查初始化失败: {0}", ex); // ★③
            }
        }

        /// <summary>
        /// 协程主体:双通道获取网站最新版本 → 与本地版本比较 → 需要时等主菜单并弹窗。
        /// 通道1:GitHub Releases API(取 tag_name);通道2:raw 直链 version.txt(API 失败时兜底)。
        /// </summary>
        public IEnumerator FetchRoutine()
        {
            Version latest = null;
            var got = false;

            // 通道1:GitHub Releases API
            yield return TryFetchVersion(LatestVersionUrl, v => { latest = v; got = true; }, () => { });

            // 通道2:API 失败(限流/断网/无 release)时回退到 version.txt
            if (!got)
            {
                Mod.Log("Droodism: 更新检查——API 通道不可用,回退到 version.txt"); // ★③
                yield return TryFetchVersion(VersionFileUrl, v => { latest = v; got = true; }, () => { });
            }

            if (!got || latest == null)
            {
                Mod.Log("Droodism: 更新检查——所有通道均失败,本次跳过提醒"); // ★③
                yield break;
            }

            Mod.Log("Droodism: 更新检查——当前 {0},网站最新 {1}", _localVersion, latest); // ★③
            if (latest <= _localVersion) yield break; // 已是最新,不打扰

            // 用户点过"不再提醒"且该版本已跳过?
            if (Version.TryParse(PlayerPrefs.GetString(SkippedVersionPrefKey, ""), out var skipped)
                && latest <= skipped)
            {
                Mod.Log("Droodism: 更新检查——版本 {0} 已被用户跳过提醒", latest); // ★③
                yield break;
            }

            // ★⑥ 主菜单判定:等进入主菜单再弹(避免在飞行/设计场景打断玩家)。
            //     想在其他场景弹,把 InMenuScene 换成对应的判定即可。
            while (Game.Instance == null || !Game.InMenuScene)
            {
                yield return null;
            }

            ShowUpdateDialog(latest);
        }

        /// <summary>
        /// 抓取指定 URL 并解析版本号。成功时回调 onSuccess(version),失败时回调 onFail。
        /// 两个通道共用的下载+解析原语。
        /// </summary>
        private IEnumerator TryFetchVersion(string url, Action<Version> onSuccess, Action onFail)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                // GitHub 的 URL(API 与 raw)都要求非空 User-Agent,否则返回 403
                request.SetRequestHeader("User-Agent", "DroodismModUpdater/1.0");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Mod.Log("Droodism: 更新检查——请求失败 {0}: {1}", url, request.error); // ★③
                    onFail?.Invoke();
                    yield break;
                }

                if (TryParseLatestVersion(request.downloadHandler.text, out var version))
                {
                    onSuccess?.Invoke(version);
                }
                else
                {
                    Mod.Log("Droodism: 更新检查——无法解析 {0} 的版本,原文: {1}", url, request.downloadHandler.text); // ★③
                    onFail?.Invoke();
                }
            }
        }

        /// <summary>
        /// 从网站响应里解析版本号。兼容:
        /// 纯文本("0.88" / "v0.88.1")、GitHub Releases JSON("tag_name":"v0.88.1")、{"version":"0.88"}。
        /// 通用,移植时无需改动。
        /// </summary>
        private static bool TryParseLatestVersion(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var s = text.Trim();

            // JSON:优先取 "tag_name"(GitHub Releases)或 "version" 字段的值
            if (s.StartsWith("{") || s.StartsWith("["))
            {
                var match = Regex.Match(s, "\"(?:tag_name|version)\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success) s = match.Groups[1].Value.Trim();
            }

            // 去掉前导 v/V,再取第一段形如 "数字.数字..." 的内容(抗 HTML/换行干扰)
            s = Regex.Replace(s, "^[vV]", "");
            s = Regex.Match(s, @"\d+(?:\.\d+){1,3}").Value;

            return Version.TryParse(s, out version);
        }

        /// <summary>
        /// 弹三按钮弹窗:下载更新 / 稍后再说 / 不再提醒。
        /// ★④ 弹窗文案来自本地化 key(Droodism.UI.Update*);没有本地化系统的 Mod
        ///    直接把 Locale.GetString(...) 换成字符串字面量即可。
        /// </summary>
        private void ShowUpdateDialog(Version latest)
        {
            try
            {
                if (Game.Instance?.UserInterface == null) return;

                var dialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.ThreeButtons, null, true);
                if (dialog == null) return;

                // ★④ 下面 6 个文案 key 对应语言文件里的 Droodism.UI.Update*
                dialog.MessageText = string.Format(
                    "{0}\n\n{1}\n{2}",
                    Locale.GetString("Droodism.UI.UpdateAvailable"),
                    string.Format(Locale.GetString("Droodism.UI.UpdateNewVersion"), latest),
                    string.Format(Locale.GetString("Droodism.UI.UpdateCurrentVersion"), _localVersion));
                dialog.OkayButtonText = Locale.GetString("Droodism.UI.UpdateDownload");
                dialog.MiddleButtonText = Locale.GetString("Droodism.UI.UpdateLater");
                dialog.CancelButtonText = Locale.GetString("Droodism.UI.UpdateDismiss");

                // 下载更新
                dialog.OkayClicked += d =>
                {
                    d.Close();
                    if (!string.IsNullOrEmpty(DownloadUrl))
                        Application.OpenURL(DownloadUrl);
                    else
                        Mod.Log("Droodism: 更新下载页 DownloadUrl 未配置"); // ★③
                };

                // 稍后再说:仅关闭,下次启动仍会提醒
                dialog.MiddleClicked += d => d.Close();

                // 不再提醒:记住跳过的版本,出现更新版本前不再弹
                dialog.CancelClicked += d =>
                {
                    PlayerPrefs.SetString(SkippedVersionPrefKey, latest.ToString());
                    PlayerPrefs.Save();
                    d.Close();
                };
            }
            catch (Exception ex)
            {
                Mod.Log("Droodism: 更新提醒弹窗失败: {0}", ex); // ★③
            }
        }

        /// <summary>
        /// 协程宿主:给非 MonoBehaviour 的 ModUpdater 提供跑 UnityWebRequest 协程的载体。
        /// 纯通用,移植时无需改动。
        /// </summary>
        public class ModUpdaterHost : MonoBehaviour
        {
            public ModUpdater Owner;

            private void Start()
            {
                if (Owner != null)
                {
                    StartCoroutine(Owner.FetchRoutine());
                }
            }
        }
    }
}

// ==================================================================
// ★③ 日志说明(移植时替换所有 Mod.Log):
//   Droodism 的 Mod.Log 受设置项 ModSettings.DebugMode 控制(关掉调试模式就不打印)。
//   其他 Mod 若没有类似设施,直接把每个 Mod.Log(...) 换成 Debug.Log(...) 即可,
//   注意 Mod.Log 签名是 (string format, params object[] args),
//   换成 Debug.Log 时用 string.Format(fmt, args) 包一下,或保持 Debug.LogFormat。
// ==================================================================
