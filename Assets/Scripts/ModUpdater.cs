using System;
using System.Collections;
using System.Text.RegularExpressions;
using ModApi;
using ModApi.Ui;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts
{
    /// 通用 Mod 更新检查 + 提醒弹窗:三通道取"网站最新版本"(GitHub Releases API → raw version.txt → 官方 mod 页版本号),
    /// 网站最新 &gt; 本地且玩家没点过"不再提醒" → 进主菜单后弹三按钮弹窗(下载更新 / 稍后再说 / 不再提醒)。
    /// **不阻塞主线程**:网络等待全在协程里(UnityWebRequest + 总看门狗 20s,单请求 5s);每次游戏会话只检查一次(static 防抖)。
    /// 第三通道是为 GitHub 不可达(如国内网络)准备的兜底:simplerockets.com 是 mod 分发页本身,能装到 mod 的玩家就能访问。
    /// **版本比较用十进制语义**:0.5 &lt; 0.51 &lt; 0.68 &lt; 0.7 &lt; 0.71;禁止直接 System.Version 比较
    /// (它按段比整数,会把 0.68 误判成 "68 &gt; 7" 而大于 0.7)。
    public class ModUpdater
    {
        // 通道1:GitHub Releases API,取 JSON 里的 "tag_name" 即最新版本号(前导 v 会被忽略)。
        public const string LatestVersionUrl =
            "https://api.github.com/repos/SatelliteTorifune/Droodism/releases/latest";

        public const string DownloadUrl =
            "https://github.com/SatelliteTorifune/Droodism/releases/latest";

        // 通道2(兜底):仓库根目录 version.txt 的 raw 直链,内容就是版本号;留空("")则禁用兜底通道。
        public const string VersionFileUrl =
            "https://raw.githubusercontent.com/SatelliteTorifune/Droodism/main/version.txt";

        // 通道3(国内兜底):官方 mod 页,从 HTML 的 GENERAL INFO 里读 "Mod Version:";
        // 页面手工维护,发布时**必须**同步更新,否则只能访问该通道的玩家收不到更新提示。
        public const string ModPageUrl =
            "https://www.simplerockets.com/Mods/View/308057/Droodism";

        public const string ModPageDownloadUrl =
            "https://www.simplerockets.com/Mods/Download/308057?p=Desktop";

        private const int RequestTimeoutSeconds = 5;   // 单请求超时;三条通道合计仍在总看门狗内

        private const string SkippedVersionPrefKey = "Droodism.UpdateReminder.SkippedVersion";   // "不再提醒"记住的版本(key 带 Mod 名防冲突)
        private const float MenuPollInterval = 0.5f;   // 秒:等主菜单的轮询间隔(见 FetchRoutine)

        private static bool _startedThisSession;   // 一次游戏会话只检查一次(static 跨实例)

        private Version _localVersion;
        private ModUpdaterHost _host;

        /// 启动一次更新检查(每次游戏会话最多一次,重复调用忽略):不发起也不等待任何网络请求。
        public void CheckForUpdate()
        {
            try
            {
                if (_startedThisSession) return;
                _startedThisSession = true;

                _localVersion = Mod.Instance.ModVersion;
                if (_localVersion == null)
                {
                    Mod.Log("更新检查跳过——ModVersion 为空");
                    return;
                }

                if (string.IsNullOrWhiteSpace(LatestVersionUrl))
                {
                    // URL 被清空(调试)则只打日志,不弹窗
                    Mod.Log("更新检查——LatestVersionUrl 未配置,当前版本 {0}", _localVersion);
                    return;
                }

                // 非 MonoBehaviour,要一个隐藏 GameObject 当协程宿主
                if (_host == null)
                {
                    var go = new GameObject("DroodismUpdateReminder");
                    GameObject.DontDestroyOnLoad(go);
                    _host = go.AddComponent<ModUpdaterHost>();
                    _host.Owner = this;
                }
            }
            catch (Exception ex)
            {
                Mod.Log("更新检查初始化失败: {0}", ex);
            }
        }

        public IEnumerator FetchRoutine()
        {
            // 总看门狗:整个"等最新版本号"的过程必须在 deadline 前结束(Time.realtimeSinceStartup 不受暂停影响)。
            // UnityWebRequest.timeout 只管单个请求,这里管整体等待。3 通道 × 5s 单请求 = 15s,留余量。
            const float totalTimeoutSeconds = 20f;
            var deadline = Time.realtimeSinceStartup + totalTimeoutSeconds;

            Version latest = null;
            var got = false;
            var fromModPage = false;

            yield return TryFetchVersion(LatestVersionUrl, deadline, v => { latest = v; got = true; }, () => { });

            // 通道2(兜底):API 限流 / 断网 / 无 release 时回退 version.txt,前提是还没到看门狗期限
            if (!got && Time.realtimeSinceStartup < deadline)
            {
                Mod.Log("更新检查——API 通道不可用,回退到 version.txt");
                yield return TryFetchVersion(VersionFileUrl, deadline, v => { latest = v; got = true; }, () => { });
            }

            // 通道3(国内兜底):GitHub 不可达时读官方 mod 页的 GENERAL INFO;页面手工维护,可能滞后,只作兜底
            if (!got && Time.realtimeSinceStartup < deadline && !string.IsNullOrWhiteSpace(ModPageUrl))
            {
                Mod.Log("更新检查——GitHub 通道不可用,回退到官方 mod 页");
                yield return TryFetchModPageVersion(deadline, v => { latest = v; got = true; fromModPage = true; }, () => { });
            }

            if (!got || latest == null)
            {
                if (Time.realtimeSinceStartup >= deadline)
                    Mod.Log("更新检查——等待版本号超时(>{0}s),本次跳过提醒", totalTimeoutSeconds);
                else
                    Mod.Log("更新检查——所有通道均失败,本次跳过提醒");
                yield break;
            }

            Mod.Log("更新检查——当前 {0},网站最新 {1}(来源:{2})",
                _localVersion, latest, fromModPage ? "官方 mod 页" : "GitHub");
            if (CompareVersions(latest, _localVersion) <= 0) yield break; // 已是最新,不打扰

            if (Version.TryParse(PlayerPrefs.GetString(SkippedVersionPrefKey, ""), out var skipped)
                && CompareVersions(latest, skipped) <= 0)
            {
                Mod.Log("更新检查——版本 {0} 已被用户跳过提醒", latest);
                yield break;
            }

            // 等进入主菜单再弹,避免在飞行/设计场景打断玩家(想换场景就改 InMenuScene 判定)。
            // 不要改回 yield return null:这条协程挂在常驻宿主上,每帧判定会在所有场景里一直空转
            while (!IsInMenuScene())
            {
                yield return new WaitForSecondsRealtime(MenuPollInterval);
            }

            ShowUpdateDialog(latest, fromModPage);
        }

        /// <summary>是否已在主菜单;取不到游戏实例时返回 false(继续等)。</summary>
        private static bool IsInMenuScene()
        {
            try
            {
                var game = Game.Instance;
                return game != null && game.SceneManager != null && game.SceneManager.InMenuScene;
            }
            catch { return false; }
        }

        /// 抓取 URL 并解析版本号;超过 deadline 主动 Abort(onSuccess / onFail 由各通道共用)。
        private IEnumerator TryFetchVersion(string url, float deadline, Action<Version> onSuccess, Action onFail)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = RequestTimeoutSeconds;
                // GitHub 的 URL(API 与 raw)都要求非空 User-Agent,否则返回 403
                request.SetRequestHeader("User-Agent", "DroodismUpdater/1.0");

                // 逐帧轮询 isDone(而非 `yield return request.SendWebRequest()`)才能每帧检查看门狗并主动 Abort;不阻塞主线程。
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (Time.realtimeSinceStartup >= deadline)
                    {
                        request.Abort();
                        Mod.Log("更新检查——总等待超时,已中断请求 {0}", url);
                        onFail?.Invoke();
                        yield break;
                    }
                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Mod.Log("更新检查——请求失败 {0}: {1}", url, request.error);
                    onFail?.Invoke();
                    yield break;
                }

                if (TryParseLatestVersion(request.downloadHandler.text, out var version))
                {
                    onSuccess?.Invoke(version);
                }
                else
                {
                    Mod.Log("更新检查——无法解析 {0} 的版本,原文: {1}", url, request.downloadHandler.text);
                    onFail?.Invoke();
                }
            }
        }

        /// <summary>通道3:抓官方 mod 页并解析 GENERAL INFO 里的 "Mod Version:";失败静默交给上层跳过提醒。</summary>
        private IEnumerator TryFetchModPageVersion(float deadline, Action<Version> onSuccess, Action onFail)
        {
            using (var request = UnityWebRequest.Get(ModPageUrl))
            {
                request.timeout = RequestTimeoutSeconds;
                request.SetRequestHeader("User-Agent", "DroodismUpdater/1.0");

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (Time.realtimeSinceStartup >= deadline)
                    {
                        request.Abort();
                        Mod.Log("更新检查——mod 页等待超时,已中断请求 {0}", ModPageUrl);
                        onFail?.Invoke();
                        yield break;
                    }
                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Mod.Log("更新检查——mod 页请求失败 {0}: {1}", ModPageUrl, request.error);
                    onFail?.Invoke();
                    yield break;
                }

                if (TryParseModPageVersion(request.downloadHandler.text, out var version))
                {
                    onSuccess?.Invoke(version);
                }
                else
                {
                    Mod.Log("更新检查——mod 页里没找到 GENERAL INFO/Mod Version(页面可能改版),本次跳过");
                    onFail?.Invoke();
                }
            }
        }

        /// <summary>从官方 mod 页 HTML 解析 Mod Version。只认 GENERAL INFO 区块,避免描述里的 "Droodism (0.62-alpha)" 或评论误匹配。</summary>
        private static bool TryParseModPageVersion(string html, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(html)) return false;

            int info = html.IndexOf("GENERAL INFO", StringComparison.OrdinalIgnoreCase);
            string scope = info >= 0 ? html.Substring(info, Math.Min(3000, html.Length - info)) : html;

            var match = Regex.Match(scope,
                "<span[^>]*class=\"[^\"]*spec-label[^\"]*\"[^>]*>\\s*Mod Version:\\s*</span>" +
                "\\s*<span[^>]*class=\"[^\"]*spec-value[^\"]*\"[^>]*>\\s*([^<]+?)\\s*</span>",
                RegexOptions.IgnoreCase);
            if (!match.Success) return false;

            // 复用常规解析:兼容 "0.62" / "v0.62" / "0.62-alpha" 之类写法
            return TryParseLatestVersion(match.Groups[1].Value, out version);
        }

        /// 解析版本号:兼容纯文本("0.7" / "v0.7.1")、Releases JSON 的 "tag_name" 与 {"version":"0.7"}。
        private static bool TryParseLatestVersion(string text, out Version version)
        { 
            version = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var s = text.Trim();

            if (s.StartsWith("{") || s.StartsWith("["))
            {
                var match = Regex.Match(s, "\"(?:tag_name|version)\"\\s*:\\s*\"([^\"]+)\"");
                if (match.Success) s = match.Groups[1].Value.Trim();
            }

            // 去掉前导 v/V,再取第一段形如 "数字.数字..." 的内容(抗 HTML / 换行干扰)
            s = Regex.Replace(s, "^[vV]", "");
            s = Regex.Match(s, @"\d+(?:\.\d+){1,3}").Value;

            return Version.TryParse(s, out version);
        }

        /// <summary>
        /// 版本比较(本项目契约):版本号按"十进制小数"语义排大小 —— 0.5 &lt; 0.51 &lt; 0.62 &lt; 0.68 &lt; 0.7 &lt; 0.71。
        /// 禁止直接用 System.Version 比较:它按"段"比整数,会得出 0.68 &gt; 0.7(段号 68 &gt; 7),把旧版本误判成新版本。
        /// 仅当出现三段及以上(如 1.2.3)时,才回退到 System.Version 语义。
        /// </summary>
        private static int CompareVersions(Version a, Version b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            if (TryParseDecimalVersion(a.ToString(), out var da) &&
                TryParseDecimalVersion(b.ToString(), out var db))
            {
                return da.CompareTo(db);
            }

            return a.CompareTo(b);
        }

        /// <summary>两段(可带尾零段)版本号解析为 decimal:0.62 / v0.7 / 0.7.0 → 数值;三段非零(如 1.2.3)返回 false 交给上层回退。</summary>
        private static bool TryParseDecimalVersion(string text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrEmpty(text)) return false;

            var match = Regex.Match(text.Trim(), @"^[vV]?(\d+(?:\.\d+){0,3})$");
            if (!match.Success) return false;

            // 去掉尾部为 0 的段:0.7.0 → 0.7、0.68.0 → 0.68(尾零不改变十进制数值)
            var parts = match.Groups[1].Value.Split('.');
            var count = parts.Length;
            while (count > 1 && parts[count - 1] == "0") count--;

            var normalized = count == parts.Length
                ? match.Groups[1].Value
                : string.Join(".", parts, 0, count);

            return decimal.TryParse(normalized,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private void ShowUpdateDialog(Version latest, bool fromModPage)
        {
            try
            {
                if (Game.Instance?.UserInterface == null) return;

                var dialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.ThreeButtons, null, true);
                if (dialog == null) return;

                dialog.MessageText = string.Format(
                    "{0}\n\n{1}\n{2}",
                    Locale.GetString("Droodism.UI.UpdateAvailable"),
                    string.Format(Locale.GetString("Droodism.UI.UpdateNewVersion"), latest),
                    string.Format(Locale.GetString("Droodism.UI.UpdateCurrentVersion"), _localVersion));
                dialog.OkayButtonText = Locale.GetString("Droodism.UI.UpdateDownload");
                dialog.MiddleButtonText = Locale.GetString("Droodism.UI.UpdateLater");
                dialog.CancelButtonText = Locale.GetString("Droodism.UI.UpdateDismiss");

                // 从官方 mod 页发现更新 = 玩家网络大概到不了 GitHub,下载按钮改去 mod 页(同一域名,能装 mod 就能下载)
                string downloadUrl = fromModPage ? ModPageDownloadUrl : DownloadUrl;

                dialog.OkayClicked += d =>
                {
                    d.Close();
                    if (!string.IsNullOrEmpty(downloadUrl))
                        Application.OpenURL(downloadUrl);
                    else
                        Mod.Log("更新下载页 DownloadUrl 未配置");
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
                Mod.Log("更新提醒弹窗失败: {0}", ex);
            }
        }

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

            // 宿主被销毁(场景切换/重载)时立刻停协程,不留悬挂的网络等待
            private void OnDestroy()
            {
                StopAllCoroutines();
            }
        }
    }
}

// 注:Mod.Log 受设置项 ModSettings.DebugMode 控制(关掉调试模式就不打印),签名为 (string format, params object[] args)。
