using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

namespace ArrowFlow
{
    [Serializable]
    public class AdsConfiguration
    {
        public string androidAppId, androidRewardedUnitId, iosAppId, iosRewardedUnitId;
        public string[] testDeviceIds;
    }

    // Rewarded-video wrapper around the Google Mobile Ads Unity Plugin.
    // Ad unit IDs live in Assets/Resources/AdsConfiguration.json, same pattern as TournamentConfiguration.json.
    // Ships with Google's public test ad unit IDs so the game shows real test ads before real AdMob IDs are added.
    // The AdMob App ID itself is set separately in the Unity editor under Assets > Google Mobile Ads > Settings;
    // the values kept in AdsConfiguration.json are there for reference alongside the ad unit IDs.
    public static class Ads
    {
        static AdsConfiguration config;
        static RewardedAd rewarded;
        static bool initialized, loading;

        static AdsConfiguration Config
        {
            get
            {
                if (config == null)
                {
                    var asset = Resources.Load<TextAsset>("AdsConfiguration");
                    config = asset == null ? new AdsConfiguration() : JsonUtility.FromJson<AdsConfiguration>(asset.text);
                }
                return config;
            }
        }

        static string RewardedUnitId
        {
            get
            {
#if UNITY_IOS
                return Config.iosRewardedUnitId;
#else
                return Config.androidRewardedUnitId;
#endif
            }
        }

        public static bool RewardedReady { get { return rewarded != null && rewarded.CanShowAd(); } }

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            var ids = Config.testDeviceIds;
            if (ids != null && ids.Length > 0)
                MobileAds.SetRequestConfiguration(new RequestConfiguration { TestDeviceIds = new List<string>(ids) });
            MobileAds.Initialize(status => LoadRewarded());
        }

        static void LoadRewarded()
        {
            string unitId = RewardedUnitId;
            if (loading || string.IsNullOrEmpty(unitId)) return;
            loading = true;
            if (rewarded != null) { rewarded.Destroy(); rewarded = null; }
            RewardedAd.Load(unitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
            {
                loading = false;
                if (error != null || ad == null) { Debug.LogWarning("Rewarded ad failed to load: " + error); return; }
                rewarded = ad;
                rewarded.OnAdFullScreenContentClosed += LoadRewarded;
                rewarded.OnAdFullScreenContentFailed += _ => LoadRewarded();
            });
        }

        // Calls done exactly once. The bool is true only if the player actually earned the reward by
        // watching an ad through to completion. GameApp grants the life refill either way, so nobody
        // is ever blocked on ad inventory or a bad connection - the flag only changes the status message.
        public static void ShowRewarded(Action<bool> done)
        {
            if (!initialized) Initialize();
            var ad = rewarded;
            if (ad != null && ad.CanShowAd())
            {
                bool earned = false, finished = false;
                ad.OnAdFullScreenContentClosed += () => { if (!finished) { finished = true; done(earned); } };
                ad.OnAdFullScreenContentFailed += _ => { if (!finished) { finished = true; done(false); } };
                ad.Show(reward => earned = true);
            }
            else
            {
                LoadRewarded();
                done(false);
            }
        }
    }
}
