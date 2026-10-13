using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace ArrowFlow
{
    [Serializable]
    public class AdsConfiguration
    {
        public string androidAppId, iosAppId;
        public string androidRewardedUnitId, iosRewardedUnitId;
        public string androidBannerUnitId, iosBannerUnitId;
        public string androidInterstitialUnitId, iosInterstitialUnitId;
        public string[] testDeviceIds;

        // Official Google sample inventory, independent of our unpublished AdMob apps.
        public void UseGoogleTestAds()
        {
            androidAppId = "ca-app-pub-3940256099942544~3347511713";
            iosAppId = "ca-app-pub-3940256099942544~1458002511";
            androidRewardedUnitId = "ca-app-pub-3940256099942544/5224354917";
            iosRewardedUnitId = "ca-app-pub-3940256099942544/1712485313";
            androidBannerUnitId = "ca-app-pub-3940256099942544/6300978111";
            iosBannerUnitId = "ca-app-pub-3940256099942544/2934735716";
            androidInterstitialUnitId = "ca-app-pub-3940256099942544/1033173712";
            iosInterstitialUnitId = "ca-app-pub-3940256099942544/4411468910";
        }
    }

    // All ad state and game callbacks are serialized onto Unity's main thread.
    public static class Ads
    {
        static AdsConfiguration config;
        static RewardedAd rewarded;
        static InterstitialAd interstitial;
        static BannerView banner;
        static bool started, consentBusy, consentChecked, initializing, initialized, showing, bannerWanted;
        static bool loadingRewarded, loadingInterstitial, loadingBanner, bannerDisplayed;
        static float consentRetryAt, rewardedRetryAt, interstitialRetryAt, bannerRetryAt;
        static float rewardedLoadedAt, interstitialLoadedAt, lastFullscreenAt;
        static int generation, rewardedFailures, interstitialFailures, bannerFailures, completedLevels;
        public static bool PrivacyOptionsRequired => ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        public static bool Busy => showing || consentBusy;
        public static float BannerInsetPixels => bannerDisplayed && banner != null
            ? banner.GetHeightInPixels() + 12 : 0;
        static bool CanLoad => initialized && !Busy && ConsentInformation.CanRequestAds();
        public static bool RewardedReady => CanLoad && rewarded != null && Time.realtimeSinceStartup - rewardedLoadedAt < 3300 && rewarded.CanShowAd();
        static void OnMain(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);
        static string Unit(string android, string ios)
        {
#if UNITY_IOS
            return ios;
#else
            return android;
#endif
        }
        static float RetryDelay(ref int failures) => Mathf.Min(60, 5 * Mathf.Pow(2, Mathf.Min(failures++, 4)));

        public static void Initialize()
        {
            if (started) return;
            started = true;
            // Consent callbacks are dispatched before MobileAds.Initialize creates its executor.
            MobileAdsEventExecutor.Initialize();
            lastFullscreenAt = Time.realtimeSinceStartup;
            var asset = Resources.Load<TextAsset>("AdsConfiguration");
            config = asset == null ? new AdsConfiguration() : JsonUtility.FromJson<AdsConfiguration>(asset.text);
            config = config ?? new AdsConfiguration();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            config.UseGoogleTestAds();
#endif
            if (config.testDeviceIds != null && config.testDeviceIds.Length > 0)
                MobileAds.SetRequestConfiguration(new RequestConfiguration { TestDeviceIds = new List<string>(config.testDeviceIds) });
            UpdateConsent();
        }
        static void UpdateConsent()
        {
            if (Busy) return;
            consentBusy = true;
            DisplayBanner(false);
            ConsentInformation.Update(new ConsentRequestParameters(), error => OnMain(() =>
            {
                if (error != null) { FinishConsent(error); return; }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => OnMain(() => FinishConsent(formError)));
            }));
        }
        static void FinishConsent(FormError error)
        {
            consentBusy = false;
            consentChecked = error == null;
            consentRetryAt = Time.realtimeSinceStartup + 30;
            if (error != null) Debug.LogWarning("Ad privacy request failed: " + error.Message);
            if (!ConsentInformation.CanRequestAds()) return;
            if (initialized) { LoadAds(); return; }
            if (initializing) return;
            initializing = true;
            MobileAds.Initialize(status => OnMain(() =>
            {
                initializing = false;
                initialized = true;
                LoadAds();
            }));
        }
        public static void ShowPrivacyOptions(Action<string> done)
        {
            if (Busy || !PrivacyOptionsRequired) return;
            consentBusy = true;
            // Invalidate pending requests and cached ads made with previous privacy choices.
            generation++;
            loadingRewarded = loadingInterstitial = loadingBanner = false;
            rewarded?.Destroy(); rewarded = null;
            interstitial?.Destroy(); interstitial = null;
            banner?.Destroy(); banner = null;
            bannerDisplayed = false;
            ConsentForm.ShowPrivacyOptionsForm(error => OnMain(() =>
            {
                FinishConsent(error);
                done?.Invoke(error == null ? null : "Privacy options couldn't open. Please try again.");
            }));
        }
        public static void Tick()
        {
            if (!started || Busy) return;
            if (!consentChecked && Time.realtimeSinceStartup >= consentRetryAt) { UpdateConsent(); return; }
            if (rewarded != null && Time.realtimeSinceStartup - rewardedLoadedAt >= 3300) { rewarded.Destroy(); rewarded = null; }
            if (interstitial != null && Time.realtimeSinceStartup - interstitialLoadedAt >= 3300) { interstitial.Destroy(); interstitial = null; }
            LoadAds();
        }
        static void LoadAds()
        {
            if (!CanLoad) return;
            LoadRewarded();
            LoadInterstitial();
            if (bannerWanted) LoadBanner();
        }
        static void LoadRewarded()
        {
            string id = Unit(config.androidRewardedUnitId, config.iosRewardedUnitId);
            if (loadingRewarded || rewarded != null || Time.realtimeSinceStartup < rewardedRetryAt || string.IsNullOrWhiteSpace(id)) return;
            loadingRewarded = true;
            int requestGeneration = generation;
            RewardedAd.Load(id, new AdRequest(), (ad, error) => OnMain(() =>
            {
                if (requestGeneration != generation) { ad?.Destroy(); return; }
                loadingRewarded = false;
                if (error != null || ad == null)
                {
                    ad?.Destroy();
                    rewardedRetryAt = Time.realtimeSinceStartup + RetryDelay(ref rewardedFailures);
                    Debug.LogWarning("Rewarded ad failed to load: " + error);
                    return;
                }
                if (consentBusy || !ConsentInformation.CanRequestAds()) { ad.Destroy(); return; }
                rewardedFailures = 0; rewarded = ad; rewardedLoadedAt = Time.realtimeSinceStartup;
            }));
        }
        static void LoadInterstitial()
        {
            string id = Unit(config.androidInterstitialUnitId, config.iosInterstitialUnitId);
            if (loadingInterstitial || interstitial != null || Time.realtimeSinceStartup < interstitialRetryAt || string.IsNullOrWhiteSpace(id)) return;
            loadingInterstitial = true;
            int requestGeneration = generation;
            InterstitialAd.Load(id, new AdRequest(), (ad, error) => OnMain(() =>
            {
                if (requestGeneration != generation) { ad?.Destroy(); return; }
                loadingInterstitial = false;
                if (error != null || ad == null)
                {
                    ad?.Destroy();
                    interstitialRetryAt = Time.realtimeSinceStartup + RetryDelay(ref interstitialFailures);
                    Debug.LogWarning("Interstitial ad failed to load: " + error);
                    return;
                }
                if (consentBusy || !ConsentInformation.CanRequestAds()) { ad.Destroy(); return; }
                interstitialFailures = 0; interstitial = ad; interstitialLoadedAt = Time.realtimeSinceStartup;
            }));
        }
        public static void SetBannerVisible(bool visible)
        {
            bannerWanted = visible;
            if (!visible || !CanLoad) DisplayBanner(false);
            else LoadBanner();
        }
        static void DisplayBanner(bool visible)
        {
            if (banner == null || bannerDisplayed == visible) return;
            bannerDisplayed = visible;
            if (visible) banner.Show(); else banner.Hide();
        }
        static void LoadBanner()
        {
            if (banner != null) { if (!loadingBanner) DisplayBanner(true); return; }
            string id = Unit(config.androidBannerUnitId, config.iosBannerUnitId);
            if (Time.realtimeSinceStartup < bannerRetryAt || string.IsNullOrWhiteSpace(id)) return;
            var view = new BannerView(id, AdSize.Banner, AdPosition.Bottom);
            banner = view;
            loadingBanner = true;
            view.Hide();
            view.OnBannerAdLoaded += () => OnMain(() =>
            {
                if (banner != view) return;
                loadingBanner = false; bannerFailures = 0;
                DisplayBanner(bannerWanted && CanLoad);
            });
            view.OnBannerAdLoadFailed += error => OnMain(() =>
            {
                if (banner != view) return;
                loadingBanner = false; view.Destroy(); banner = null; bannerDisplayed = false;
                bannerRetryAt = Time.realtimeSinceStartup + RetryDelay(ref bannerFailures);
                Debug.LogWarning("Banner failed to load: " + error);
            });
            view.LoadAd(new AdRequest());
        }
        public static void LevelCompleted() { completedLevels++; }
        public static void ShowInterstitialBetweenLevels(Action done)
        {
            if (!CanLoad || completedLevels < 3 || Time.realtimeSinceStartup - lastFullscreenAt < 120
                || interstitial == null || Time.realtimeSinceStartup - interstitialLoadedAt >= 3300 || !interstitial.CanShowAd())
            { done?.Invoke(); return; }
            var ad = interstitial; interstitial = null;
            showing = true; DisplayBanner(false); completedLevels = 0;
            bool finished = false;
            Action finish = () =>
            {
                if (finished) return;
                finished = true; showing = false; lastFullscreenAt = Time.realtimeSinceStartup;
                ad.Destroy(); done?.Invoke(); LoadAds();
            };
            ad.OnAdFullScreenContentClosed += () => OnMain(finish);
            ad.OnAdFullScreenContentFailed += error => OnMain(finish);
            try { ad.Show(); }
            catch (Exception error) { Debug.LogWarning("Interstitial couldn't open: " + error.Message); finish(); }
        }
        // Reward delivery is independent of dismissal and only follows the SDK reward event.
        public static void ShowRewarded(Action earnReward, Action<bool> closed)
        {
            Initialize();
            if (!RewardedReady) { closed?.Invoke(false); return; }
            var ad = rewarded; rewarded = null;
            showing = true; DisplayBanner(false);
            bool earned = false, finished = false;
            Action finish = () =>
            {
                if (finished) return;
                finished = true; showing = false; lastFullscreenAt = Time.realtimeSinceStartup;
                ad.Destroy(); closed?.Invoke(earned); LoadAds();
            };
            ad.OnAdFullScreenContentClosed += () => OnMain(finish);
            ad.OnAdFullScreenContentFailed += error => OnMain(finish);
            try
            {
                ad.Show(reward => OnMain(() =>
                {
                    if (earned) return;
                    earned = true; earnReward?.Invoke();
                }));
            }
            catch (Exception error) { Debug.LogWarning("Rewarded ad couldn't open: " + error.Message); finish(); }
        }
    }
}
