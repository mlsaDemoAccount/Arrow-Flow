using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArrowFlow.Editor
{
    // Run before Google's manifest/plist processors. JSON is the source of truth for IDs.
    public sealed class AdsBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android && report.summary.platform != BuildTarget.iOS) return;
            var config = JsonUtility.FromJson<AdsConfiguration>(File.ReadAllText("Assets/Resources/AdsConfiguration.json"));
            if (config == null) throw new BuildFailedException("AdsConfiguration.json is invalid.");
            bool android = report.summary.platform == BuildTarget.Android;
            bool development = (report.summary.options & BuildOptions.Development) != 0;
            Validate(android ? config.androidAppId : config.iosAppId, true, development);
            Validate(android ? config.androidRewardedUnitId : config.iosRewardedUnitId, false, development);
            Validate(android ? config.androidBannerUnitId : config.iosBannerUnitId, false, development);
            Validate(android ? config.androidInterstitialUnitId : config.iosInterstitialUnitId, false, development);
            var asset = AssetDatabase.LoadAssetAtPath<Object>("Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset");
            if (asset == null) throw new BuildFailedException("Google Mobile Ads settings asset is missing.");
            var settings = new SerializedObject(asset);
            settings.FindProperty("adMobAndroidAppId").stringValue = config.androidAppId;
            settings.FindProperty("adMobIOSAppId").stringValue = config.iosAppId;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
        static void Validate(string id, bool app, bool development)
        {
            string pattern = app ? @"^ca-app-pub-\d{16}~\d{10}$" : @"^ca-app-pub-\d{16}/\d{10}$";
            if (id == null || !Regex.IsMatch(id, pattern))
                throw new BuildFailedException("Set valid AdMob " + (app ? "App IDs" : "ad unit IDs") + " in Assets/Resources/AdsConfiguration.json.");
            if (!development && id.StartsWith("ca-app-pub-3940256099942544"))
                throw new BuildFailedException("Release build still uses Google test ad IDs. Supply production IDs in AdsConfiguration.json, or enable Development Build to test ads.");
        }
    }
}
