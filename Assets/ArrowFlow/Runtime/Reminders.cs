using UnityEngine;
using System.Runtime.InteropServices;

namespace ArrowFlow
{
    public static class Reminders
    {
#if UNITY_IOS && !UNITY_EDITOR
 [DllImport("__Internal")] static extern void ArrowFlowReminders(bool enabled);
#endif
        public static void Set(bool enabled)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
  using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))using(var bridge=new AndroidJavaClass("com.arrowflowstudio.reminders.ReminderReceiver")){
   if(!enabled){bridge.CallStatic("setEnabled",activity,false);GameApp.Instance.OnReminderPermission("0");return;}
   using(var version=new AndroidJavaClass("android.os.Build$VERSION"))if(version.GetStatic<int>("SDK_INT")>=33&&!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS")){
    var callbacks=new UnityEngine.Android.PermissionCallbacks();callbacks.PermissionGranted+=s=>Set(true);callbacks.PermissionDenied+=s=>GameApp.Instance.OnReminderPermission("denied");UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS",callbacks);return;
   }
   bool allowed=bridge.CallStatic<bool>("allowed",activity);bridge.CallStatic("setEnabled",activity,allowed);GameApp.Instance.OnReminderPermission(allowed?"1":"denied");
  }
#elif UNITY_IOS && !UNITY_EDITOR
  ArrowFlowReminders(enabled);
#else
            GameApp.Instance.OnReminderPermission("desktop");
#endif
        }
    }
}


