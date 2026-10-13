#import <UserNotifications/UserNotifications.h>
extern "C" void UnitySendMessage(const char*, const char*, const char*);
extern "C" void ArrowFlowReminders(bool enabled) {
 UNUserNotificationCenter* center=[UNUserNotificationCenter currentNotificationCenter];
 [center removePendingNotificationRequestsWithIdentifiers:@[@"arrowflow.daily"]];
 [center removeDeliveredNotificationsWithIdentifiers:@[@"arrowflow.daily"]];
 if(!enabled){UnitySendMessage("Arrow Attack","OnReminderPermission","0");return;}
 [center requestAuthorizationWithOptions:(UNAuthorizationOptionAlert|UNAuthorizationOptionSound) completionHandler:^(BOOL granted,NSError* error){
  if(!granted){dispatch_async(dispatch_get_main_queue(),^{UnitySendMessage("Arrow Attack","OnReminderPermission","denied");});return;}
  UNMutableNotificationContent* content=[UNMutableNotificationContent new];content.title=@"A little space for your mind";content.body=@"Your daily Arrow Attack puzzle is waiting.";content.sound=[UNNotificationSound defaultSound];
  NSDateComponents* time=[NSDateComponents new];time.hour=18;time.minute=0;
  UNCalendarNotificationTrigger* trigger=[UNCalendarNotificationTrigger triggerWithDateMatchingComponents:time repeats:YES];
  UNNotificationRequest* request=[UNNotificationRequest requestWithIdentifier:@"arrowflow.daily" content:content trigger:trigger];
  [center addNotificationRequest:request withCompletionHandler:^(NSError* failure){dispatch_async(dispatch_get_main_queue(),^{UnitySendMessage("Arrow Attack","OnReminderPermission",failure?"denied":"1");});}];
 }];
}
