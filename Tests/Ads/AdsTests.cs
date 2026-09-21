using System;
using System.Collections.Generic;
using System.Reflection;
using ArrowFlow;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;

// Deterministic SDK doubles exercise the production Ads.cs state machine without a device.
namespace UnityEngine {
 public static class Time { public static float realtimeSinceStartup; }
 public class TextAsset { public string text; }
 public static class Resources { public static T Load<T>(string name) where T:class => new TextAsset() as T; }
 public static class JsonUtility { public static T FromJson<T>(string json) => (T)(object)new AdsConfiguration { androidRewardedUnitId="reward",iosRewardedUnitId="ios-reward",androidBannerUnitId="banner",iosBannerUnitId="ios-banner",androidInterstitialUnitId="interstitial",iosInterstitialUnitId="ios-interstitial" }; }
 public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Pow(float a,float b)=>(float)Math.Pow(a,b); }
 public static class Debug { public static void LogWarning(object text) {} }
}
namespace GoogleMobileAds.Common {
 public static class MobileAdsEventExecutor { public static Queue<Action> Queue=new(); public static void ExecuteInUpdate(Action a)=>Queue.Enqueue(a); public static void Drain(){while(Queue.Count>0)Queue.Dequeue()();} }
}
namespace GoogleMobileAds.Ump.Api {
 public enum PrivacyOptionsRequirementStatus { Unknown,Required,NotRequired }
 public class ConsentRequestParameters {}
 public class FormError { public string Message="error"; }
 public static class ConsentInformation {
  public static bool Allowed; public static int Updates; public static Action<FormError> Callback;
  public static PrivacyOptionsRequirementStatus PrivacyOptionsRequirementStatus=>PrivacyOptionsRequirementStatus.Required;
  public static bool CanRequestAds()=>Allowed;
  public static void Update(ConsentRequestParameters p,Action<FormError> a){Updates++;Callback=a;}
 }
 public static class ConsentForm {
  public static Action<FormError> PrivacyCallback;
  public static void LoadAndShowConsentFormIfRequired(Action<FormError> a)=>a(null);
  public static void ShowPrivacyOptionsForm(Action<FormError> a)=>PrivacyCallback=a;
 }
}
namespace GoogleMobileAds.Api {
 public class RequestConfiguration { public List<string> TestDeviceIds; }
 public class AdRequest {} public class Reward {} public class LoadAdError {} public class AdError {}
 public static class MobileAds {
  public static int Initializations;
  public static void SetRequestConfiguration(RequestConfiguration c){}
  public static void Initialize(Action<object> a){Initializations++;a(new object());}
 }
 public class RewardedAd {
  public static List<Action<RewardedAd,LoadAdError>> Requests=new(); public static string RequestedId;
  public event Action OnAdFullScreenContentClosed; public event Action<AdError> OnAdFullScreenContentFailed;
  public bool Destroyed,Shown; public Action<Reward> Reward;
  public static void Load(string id,AdRequest r,Action<RewardedAd,LoadAdError>a){RequestedId=id;Requests.Add(a);}
  public bool CanShowAd()=>!Destroyed&&!Shown;
  public void Show(Action<Reward>a){Shown=true;Reward=a;}
  public void Destroy()=>Destroyed=true;
  public void Close()=>OnAdFullScreenContentClosed?.Invoke(); public void Fail()=>OnAdFullScreenContentFailed?.Invoke(new());
 }
 public class InterstitialAd {
  public static List<Action<InterstitialAd,LoadAdError>> Requests=new();
  public event Action OnAdFullScreenContentClosed; public event Action<AdError> OnAdFullScreenContentFailed;
  public bool Destroyed,Shown;
  public static void Load(string id,AdRequest r,Action<InterstitialAd,LoadAdError>a)=>Requests.Add(a);
  public bool CanShowAd()=>!Destroyed&&!Shown; public void Show()=>Shown=true; public void Destroy()=>Destroyed=true;
  public void Close()=>OnAdFullScreenContentClosed?.Invoke(); public void Fail()=>OnAdFullScreenContentFailed?.Invoke(new());
 }
 public class AdSize { public static AdSize Banner=new(); } public enum AdPosition { Bottom }
 public class BannerView {
  public static BannerView Last; public bool Visible,Destroyed; public int Shows;
  public event Action OnBannerAdLoaded; public event Action<LoadAdError> OnBannerAdLoadFailed;
  public BannerView(string id,AdSize size,AdPosition pos){Last=this;}
  public void Hide()=>Visible=false; public void Show(){Visible=true;Shows++;} public void Destroy()=>Destroyed=true;
  public void LoadAd(AdRequest r){} public float GetHeightInPixels()=>50;
  public void Loaded()=>OnBannerAdLoaded?.Invoke(); public void Fail()=>OnBannerAdLoadFailed?.Invoke(new());
 }
}
class Program {
 static int checks;
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
 static void Drain()=>MobileAdsEventExecutor.Drain();
 static void Reset(){
  foreach(var f in typeof(Ads).GetFields(BindingFlags.Static|BindingFlags.NonPublic)) f.SetValue(null,f.FieldType.IsValueType?Activator.CreateInstance(f.FieldType):null);
  MobileAdsEventExecutor.Queue.Clear(); RewardedAd.Requests.Clear(); InterstitialAd.Requests.Clear(); MobileAds.Initializations=0; ConsentInformation.Allowed=false; ConsentInformation.Updates=0; UnityEngine.Time.realtimeSinceStartup=0; BannerView.Last=null;
 }
 static void Start(){Reset();Ads.Initialize();ConsentInformation.Allowed=true;ConsentInformation.Callback(null);Drain();}
 static RewardedAd Ready(){var ad=new RewardedAd();RewardedAd.Requests[^1](ad,null);Drain();return ad;}
 static void Main(){
  Reset();Ads.Initialize();Ads.Tick();Check(MobileAds.Initializations==0 && RewardedAd.Requests.Count==0,"Consent must precede initialization and requests");
  ConsentInformation.Callback(new FormError());Drain();UnityEngine.Time.realtimeSinceStartup=29;Ads.Tick();Check(ConsentInformation.Updates==1,"Consent backoff");UnityEngine.Time.realtimeSinceStartup=30;Ads.Tick();Check(ConsentInformation.Updates==2,"Consent retries");
  ConsentInformation.Allowed=true;ConsentInformation.Callback(null);Drain();Check(MobileAds.Initializations==1 && RewardedAd.Requests.Count==1,"Initialize once after consent");Ads.Initialize();Ads.Tick();Check(MobileAds.Initializations==1 && RewardedAd.Requests.Count==1,"No duplicate initialization or loads");
  var ad=Ready();int rewards=0,closes=0;bool earned=false;Ads.ShowRewarded(()=>rewards++,v=>{closes++;earned=v;});ad.Reward(new());Check(rewards==0,"Reward callback is marshaled");ad.Reward(new());ad.Close();ad.Fail();Drain();Check(rewards==1 && closes==1 && earned,"Reward and completion each exactly once");Check(ad.Destroyed && RewardedAd.Requests.Count==2,"Dispose and preload replacement");
  ad=Ready();Ads.ShowRewarded(()=>rewards++,v=>{closes++;earned=v;});ad.Close();Drain();Check(rewards==1 && !earned,"Skipping gives no reward");
  Ads.ShowRewarded(()=>rewards++,v=>earned=v);Check(rewards==1 && !earned,"No-fill gives no reward");
  Start();RewardedAd.Requests[0](null,new());Drain();UnityEngine.Time.realtimeSinceStartup=4;Ads.Tick();Check(RewardedAd.Requests.Count==1,"Load failure backoff");UnityEngine.Time.realtimeSinceStartup=5;Ads.Tick();Check(RewardedAd.Requests.Count==2,"Load failure retry");
  ad=Ready();UnityEngine.Time.realtimeSinceStartup=3306;Ads.Tick();Check(ad.Destroyed && RewardedAd.Requests.Count==3,"Expired rewarded ad replaced");
  Start();var pending=RewardedAd.Requests[0];Ads.ShowPrivacyOptions(_=>{});ad=new();pending(ad,null);Drain();Check(ad.Destroyed && !Ads.RewardedReady,"Privacy change invalidates in-flight ad");ConsentInformation.Allowed=false;ConsentForm.PrivacyCallback(null);Drain();Ads.Tick();Check(RewardedAd.Requests.Count==1,"No loads when consent disallows ads");
  Start();Ads.SetBannerVisible(true);var banner=BannerView.Last;Ads.SetBannerVisible(false);banner.Loaded();Drain();Check(!banner.Visible && Ads.BannerInsetPixels==0,"Late banner load stays hidden during gameplay");Ads.SetBannerVisible(true);Ads.Tick();Ads.Tick();Check(banner.Visible && banner.Shows==1 && Ads.BannerInsetPixels==62,"Menu banner has reserved space and no repeated show calls");Ads.SetBannerVisible(false);Check(!banner.Visible,"Hide menu banner");
  Start();var inter=new InterstitialAd();InterstitialAd.Requests[0](inter,null);Drain();int continued=0;for(int i=0;i<3;i++)Ads.LevelCompleted();Ads.ShowInterstitialBetweenLevels(()=>continued++);Check(!inter.Shown && continued==1,"No interstitial in first two minutes");UnityEngine.Time.realtimeSinceStartup=120;Ads.ShowInterstitialBetweenLevels(()=>continued++);Check(inter.Shown && continued==1,"Interstitial only at eligible break");inter.Close();inter.Fail();Drain();Check(continued==2 && inter.Destroyed,"Interstitial continues exactly once");
  var next=new InterstitialAd();InterstitialAd.Requests[^1](next,null);Drain();UnityEngine.Time.realtimeSinceStartup=300;Ads.ShowInterstitialBetweenLevels(()=>continued++);Check(!next.Shown,"Three more levels needed after interstitial");
  Console.WriteLine($"PASS: {checks} ad lifecycle assertions");
 }
}
