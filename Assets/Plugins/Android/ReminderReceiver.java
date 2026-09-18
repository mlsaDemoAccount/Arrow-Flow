package com.arrowflowstudio.reminders;
import android.app.*;
import android.content.*;
import android.os.Build;
import java.util.Calendar;
public class ReminderReceiver extends BroadcastReceiver {
 private static final String CHANNEL="daily_puzzles";
 public static boolean allowed(Context context){return ((NotificationManager)context.getSystemService(Context.NOTIFICATION_SERVICE)).areNotificationsEnabled();}
 public static void setEnabled(Context context,boolean enabled){
  context.getSharedPreferences("arrowflow_reminders",0).edit().putBoolean("enabled",enabled).apply();
  AlarmManager alarms=(AlarmManager)context.getSystemService(Context.ALARM_SERVICE);
  PendingIntent pending=PendingIntent.getBroadcast(context,817,new Intent(context,ReminderReceiver.class),PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
  alarms.cancel(pending);
  if(!enabled){((NotificationManager)context.getSystemService(Context.NOTIFICATION_SERVICE)).cancel(817);return;}
  Calendar next=Calendar.getInstance();next.set(Calendar.HOUR_OF_DAY,18);next.set(Calendar.MINUTE,0);next.set(Calendar.SECOND,0);next.set(Calendar.MILLISECOND,0);if(next.getTimeInMillis()<=System.currentTimeMillis())next.add(Calendar.DATE,1);
  // Inexact alarms avoid restricted exact-alarm permissions; the OS may delay delivery.
  alarms.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP,next.getTimeInMillis(),pending);
 }
 @Override public void onReceive(Context context,Intent intent){
  if(!context.getSharedPreferences("arrowflow_reminders",0).getBoolean("enabled",false))return;
  if(Intent.ACTION_BOOT_COMPLETED.equals(intent.getAction())){setEnabled(context,true);return;}
  NotificationManager nm=(NotificationManager)context.getSystemService(Context.NOTIFICATION_SERVICE);
  if(allowed(context)){
   if(Build.VERSION.SDK_INT>=26)nm.createNotificationChannel(new NotificationChannel(CHANNEL,"Daily puzzles",NotificationManager.IMPORTANCE_DEFAULT));
   Intent launch=context.getPackageManager().getLaunchIntentForPackage(context.getPackageName());
   PendingIntent open=PendingIntent.getActivity(context,818,launch,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
   Notification.Builder b=Build.VERSION.SDK_INT>=26?new Notification.Builder(context,CHANNEL):new Notification.Builder(context);
   b.setSmallIcon(context.getApplicationInfo().icon).setContentTitle("A little space for your mind").setContentText("Your daily Arrow Flow puzzle is waiting.").setContentIntent(open).setAutoCancel(true);
   nm.notify(817,b.build());
  }setEnabled(context,true);
 }
}
