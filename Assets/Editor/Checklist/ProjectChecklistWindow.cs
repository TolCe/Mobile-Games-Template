using System;
using System.Collections.Generic;
using Template.Core;
using Template.Core.Ads;
using UnityEditor;
using UnityEngine;

namespace Template.Editor.Checklist
{
    public class ProjectChecklistWindow : EditorWindow
    {
        private enum CheckStatus
        {
            Pass,
            Warning,
            Error
        }

        private struct CheckResult
        {
            public string Title;
            public CheckStatus Status;
            public string Message;
            public UnityEngine.Object Context;
        }

        private struct Section
        {
            public string Title;
            public Func<IEnumerable<CheckResult>> Run;
        }

        private const string GoogleMobileAdsSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string AdMobTestPublisherId = "ca-app-pub-3940256099942544";
        private const string AdMobAndroidAppIdField = "adMobAndroidAppId";
        private const string AdMobIOSAppIdField = "adMobIOSAppId";
        private const string ProjectLifetimeScopePrefabPath = "Assets/Prefabs/ProjectLifetimeScope.prefab";
        private static readonly char[] AdMobIdSeparators = { '~', '/' };

        private readonly List<Section> sections = new List<Section>();
        private readonly Dictionary<string, List<CheckResult>> results = new Dictionary<string, List<CheckResult>>();
        private Vector2 scroll;

        [MenuItem("Tools/Checklist")]
        public static void Open()
        {
            GetWindow<ProjectChecklistWindow>("Checklist");
        }

        private void OnEnable()
        {
            sections.Clear();
            sections.Add(new Section { Title = "Google AdMob", Run = CheckAdMobAppIds });
            sections.Add(new Section { Title = "AdMob Ad Units", Run = CheckAdUnitIds });
            Refresh();
        }

        private void OnFocus()
        {
            Refresh();
        }

        private void Refresh()
        {
            results.Clear();
            foreach (var section in sections)
            {
                results[section.Title] = new List<CheckResult>(section.Run());
            }
            Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    Refresh();
                }
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var section in sections)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);
                foreach (var result in results[section.Title])
                {
                    DrawResult(result);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawResult(CheckResult result)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(GetStatusIcon(result.Status), GUILayout.Width(20), GUILayout.Height(20));
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(result.Title, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedMiniLabel);
                }
                if (result.Context != null && GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = result.Context;
                    EditorGUIUtility.PingObject(result.Context);
                }
            }
        }

        private static GUIContent GetStatusIcon(CheckStatus status)
        {
            switch (status)
            {
                case CheckStatus.Pass: return EditorGUIUtility.IconContent("TestPassed");
                case CheckStatus.Warning: return EditorGUIUtility.IconContent("console.warnicon.sml");
                default: return EditorGUIUtility.IconContent("console.erroricon.sml");
            }
        }

        private static IEnumerable<CheckResult> CheckAdMobAppIds()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GoogleMobileAdsSettingsPath);
            if (settings == null)
            {
                yield return new CheckResult
                {
                    Title = "AdMob Settings",
                    Status = CheckStatus.Error,
                    Message = $"Settings asset not found at {GoogleMobileAdsSettingsPath}. Open Assets > Google Mobile Ads > Settings to create it."
                };
                yield break;
            }

            yield return CheckAdMobAppId("Android App ID", GetAdMobAppId(settings, AdMobAndroidAppIdField), settings);
            yield return CheckAdMobAppId("iOS App ID", GetAdMobAppId(settings, AdMobIOSAppIdField), settings);
        }

        // GoogleMobileAdsSettings is internal to the GMA plugin, so read its fields through serialization.
        private static string GetAdMobAppId(ScriptableObject settings, string field)
        {
            return settings == null ? null : new SerializedObject(settings).FindProperty(field)?.stringValue;
        }

        private static IEnumerable<CheckResult> CheckAdUnitIds()
        {
            var scope = AssetDatabase.LoadAssetAtPath<ProjectLifetimeScope>(ProjectLifetimeScopePrefabPath);
            var config = scope == null ? null : new SerializedObject(scope).FindProperty("adConfig")?.objectReferenceValue as AdConfig;
            if (config == null)
            {
                yield return new CheckResult
                {
                    Title = "Ad Config",
                    Status = CheckStatus.Error,
                    Message = $"No AdConfig assigned on {ProjectLifetimeScopePrefabPath}. Create one with Create > Template > Ad Config and assign it.",
                    Context = scope
                };
                yield break;
            }

            yield return new CheckResult
            {
                Title = "Ads Enabled",
                Status = config.AdsEnabled ? CheckStatus.Pass : CheckStatus.Warning,
                Message = config.AdsEnabled
                    ? "Ads are on."
                    : "Ads are turned off in AdConfig. No ads will show in this build.",
                Context = config
            };

            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GoogleMobileAdsSettingsPath);
            var androidAppId = GetAdMobAppId(settings, AdMobAndroidAppIdField);
            var iosAppId = GetAdMobAppId(settings, AdMobIOSAppIdField);

            yield return CheckAdUnitId("Android Banner", config.Android.Banner, androidAppId, config);
            yield return CheckAdUnitId("Android Interstitial", config.Android.Interstitial, androidAppId, config);
            yield return CheckAdUnitId("Android Rewarded", config.Android.Rewarded, androidAppId, config);
            yield return CheckAdUnitId("iOS Banner", config.IOS.Banner, iosAppId, config);
            yield return CheckAdUnitId("iOS Interstitial", config.IOS.Interstitial, iosAppId, config);
            yield return CheckAdUnitId("iOS Rewarded", config.IOS.Rewarded, iosAppId, config);
        }

        private static CheckResult CheckAdUnitId(string title, string unitId, string appId, UnityEngine.Object context)
        {
            var result = new CheckResult { Title = title, Context = context };
            var trimmed = unitId?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                result.Status = CheckStatus.Warning;
                result.Message = "Not set. This ad format is turned off on this platform.";
            }
            else if (trimmed.Contains('~'))
            {
                result.Status = CheckStatus.Error;
                result.Message = $"{unitId}\nThis is an App ID. Ad unit IDs look like ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN.";
            }
            else if (trimmed.StartsWith(AdMobTestPublisherId, StringComparison.Ordinal))
            {
                result.Status = CheckStatus.Warning;
                result.Message = $"TEST ID: {unitId}\nReplace with the production ad unit ID before release.";
            }
            else if (!string.IsNullOrWhiteSpace(appId) && GetAdMobPublisher(trimmed) != GetAdMobPublisher(appId.Trim()))
            {
                result.Status = CheckStatus.Error;
                result.Message = $"{unitId}\nBelongs to a different AdMob account than the App ID ({appId}). Ads will not serve.";
            }
            else
            {
                result.Status = CheckStatus.Pass;
                result.Message = $"Production ID: {unitId}";
            }

            return result;
        }

        private static string GetAdMobPublisher(string id)
        {
            var separator = id.IndexOfAny(AdMobIdSeparators);
            return separator < 0 ? id : id.Substring(0, separator);
        }

        private static CheckResult CheckAdMobAppId(string title, string appId, UnityEngine.Object context)
        {
            var result = new CheckResult { Title = title, Context = context };

            if (string.IsNullOrWhiteSpace(appId))
            {
                result.Status = CheckStatus.Error;
                result.Message = "Not set.";
            }
            else if (appId.Trim().StartsWith(AdMobTestPublisherId, StringComparison.Ordinal))
            {
                result.Status = CheckStatus.Warning;
                result.Message = $"TEST ID: {appId}\nReplace with the production App ID before release.";
            }
            else
            {
                result.Status = CheckStatus.Pass;
                result.Message = $"Production ID: {appId}";
            }

            return result;
        }
    }
}
