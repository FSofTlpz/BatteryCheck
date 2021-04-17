using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;

namespace BatteryCheck.Droid {

   /// <summary>
   /// zum einfacheren Umgang mit den Notifications
   /// </summary>
   class NotificationHelper {

      private static Context appcontext = global::Android.App.Application.Context;

      const string NOTIFICATION_INFO = "Battery";
      const string NOTIFICATION_MINALARM = "MinAlarm";
      const string NOTIFICATION_MAXALARM = "MaxAlarm";


      private static NotificationManager notificationManager = null;


      class AppNotificationChannel {

         public readonly List<int> Icon = new List<int>() {
            Resource.Mipmap.ni0,
            Resource.Mipmap.ni1,
            Resource.Mipmap.ni2,
            Resource.Mipmap.ni3,
            Resource.Mipmap.ni4,
            Resource.Mipmap.ni5,
            Resource.Mipmap.ni6,
            Resource.Mipmap.ni7,
            Resource.Mipmap.ni8,
         };

         /// <summary>
         /// Name des Kanals (öffentlich sichtbar)
         /// </summary>
         public string ChannelName { get; protected set; }

         /// <summary>
         /// ID des Kanals
         /// </summary>
         public string ChannelID { get; protected set; }

         /// <summary>
         /// ID für die (einzige) Notification dieses Kanals
         /// </summary>
         public int NotificationID { get; protected set; }

         /// <summary>
         /// Builder u.a. zum Erzeugen einer Notification
         /// </summary>
         public NotificationCompat.Builder NotificationBuilder { get; protected set; }


         public AppNotificationChannel(string channelName, string channelID, int notificationID) {
            ChannelName = channelName;
            ChannelID = channelID;
            NotificationID = notificationID;
         }

         /// <summary>
         /// erzeugt einen <see cref="NotificationChannel"/> und registriert ihn im OS
         /// </summary>
         /// <param name="notificationManager"></param>
         /// <returns></returns>
         public NotificationChannel CreateAndRegisterChannel(NotificationManager notificationManager) {
            if (notificationManager != null) {
               notificationManager.DeleteNotificationChannel(ChannelID);

               NotificationChannel notificationChannel = createNotificationChannel(ChannelName, ChannelName);

               notificationManager.CreateNotificationChannel(notificationChannel);  // Kanal wird im System registriert
               return notificationChannel;
            }
            return null;
         }

         NotificationChannel createNotificationChannel(string notificationchannel, string channelname) {
            NotificationImportance notificationImportance = NotificationImportance.High;
            if (notificationchannel == NOTIFICATION_INFO)
               notificationImportance = NotificationImportance.Low;

            NotificationChannel notificationChannel = new NotificationChannel(channels[notificationchannel].ChannelID, channelname, notificationImportance);

            /* NotificationImportance:
               IMPORTANCE_MAX          Unused.
               IMPORTANCE_HIGH         Higher notification importance: shows everywhere, makes noise and peeks. May use full screen intents.
               IMPORTANCE_DEFAULT      Default notification importance: shows everywhere, makes noise, but does not visually intrude.
               IMPORTANCE_LOW          Low notification importance: Shows in the shade, and potentially in the status bar (see shouldHideSilentStatusBarIcons()), 
                                       but is not audibly intrusive.
               IMPORTANCE_MIN          Min notification importance: only shows in the shade, below the fold. This should not be used with Service#startForeground(int, Notification) 
                                       since a foreground service is supposed to be something the user cares about so it does not make semantic sense to mark its notification as 
                                       minimum importance. If you do this as of Android version Build.VERSION_CODES.O, the system will show a higher-priority notification about 
                                       your app running in the background.
               IMPORTANCE_NONE         A notification with no importance: does not show in the shade.
               IMPORTANCE_UNSPECIFIED  Value signifying that the user has not expressed an importance. This value is for persisting preferences, and should never be associated with 
                                       an actual notification.
             */

            // NotificationManager#IMPORTANCE_DEFAULT should have a sound. Only modifiable before the channel is submitted to 
            if (notificationchannel == NOTIFICATION_INFO) {

               notificationChannel.SetSound(null, null);
               notificationChannel.EnableLights(false);         // Sets whether notifications posted to this channel should display notification lights, on devices that support that feature. 
               notificationChannel.LockscreenVisibility = NotificationVisibility.Secret;  // Sets whether notifications posted to this channel appear on the lockscreen or not, and if so, whether they appear in a redacted form. 
               notificationChannel.EnableVibration(false);     // Sets whether notification posted to this channel should vibrate.

            } else if (notificationchannel == NOTIFICATION_MINALARM) {

               //notificationChannel.SetSound(Android.Net.Uri.Parse("android.resource://" + context.PackageName + "/" + BatteryCheck.Droid.Resource.Raw.MinAlarm),
               //                             new AudioAttributes.Builder()
               //                                      .SetUsage(AudioUsageKind.Alarm)
               //                                      .SetContentType(AudioContentType.Music)
               //                                      .Build());
               notificationChannel.SetSound(null, null);

               notificationChannel.EnableVibration(true);
               notificationChannel.ShouldVibrate();

               notificationChannel.SetVibrationPattern(new long[] { 100, 200, 300, 400, 500, 400, 300, 200, 400 });

               notificationChannel.EnableLights(true);         // Sets whether notifications posted to this channel should display notification lights, on devices that support that feature. 
               notificationChannel.ShouldShowLights();

               notificationChannel.LockscreenVisibility = NotificationVisibility.Public;  // Sets whether notifications posted to this channel appear on the lockscreen or not, and if so, whether they appear in a redacted form. 

            } else if (notificationchannel == NOTIFICATION_MAXALARM) {

               //RingtoneManager.GetDefaultUri(RingtoneType.Alarm) oder RingtoneManager.GetDefaultUri(RingtoneType.Notification)
               //notificationChannel.SetSound(Android.Net.Uri.Parse("android.resource://" + context.PackageName + "/" + BatteryCheck.Droid.Resource.Raw.MaxAlarm),
               //                             new AudioAttributes.Builder()
               //                                      .SetUsage(AudioUsageKind.Alarm)
               //                                      .SetContentType(AudioContentType.Music)
               //                                      .Build());
               notificationChannel.SetSound(null, null);

               notificationChannel.EnableVibration(true);
               notificationChannel.ShouldVibrate();

               // Sets the vibration pattern for notifications posted to this channel. 
               // If the provided pattern is valid (non-null, non-empty), will enableVibration(boolean) enable vibration as well. Otherwise, vibration will be disabled. 
               // Only modifiable before the channel is submitted to NotificationManager#createNotificationChannel(NotificationChannel).
               notificationChannel.SetVibrationPattern(new long[] { 100, 200, 300, 400, 500, 400, 300, 200, 400 });

               notificationChannel.EnableLights(true);         // Sets whether notifications posted to this channel should display notification lights, on devices that support that feature. 
               notificationChannel.ShouldShowLights();

               notificationChannel.LockscreenVisibility = NotificationVisibility.Public;  // Sets whether notifications posted to this channel appear on the lockscreen or not, and if so, whether they appear in a redacted form. 

            }

            notificationChannel.SetShowBadge(true);         // Sets whether notifications posted to this channel can appear as application icon badges in a Launcher. 

            return notificationChannel;
         }

         NotificationCompat.Builder createNotificationBuilder(string notificationchannel, string text, string title) {
#if WITHOUT_INTENT
         // Mit dieser einfachen Variante kann beim Tippen auf die Notification die App NICHT wieder gestartet werden!

         builder = new NotificationCompat.Builder(context, channelid)
                            //.SetSubText("ein längerer Subtext")         // This provides some additional information that is displayed in the notification. (hinter dem Progname)
                            .SetContentTitle(title)                     // Set the first line of text in the platform notification template. 
                            .SetContentText(text)                       // Set the second line of text in the platform notification template. 
                            .SetSmallIcon(Resource.Drawable.ic_mtrl_chip_close_circle)
                            .SetOngoing(true)                           // Set whether this is an "ongoing" notification. ???
                            .SetProgress(100, 35, false)                // Set the progress this notification represents.
                                                                        //.SetUsesChronometer(true)                   // Show the Notification When field as a stopwatch. 
                                                                        //.SetShowWhen(true)                          // Control whether the timestamp set with setWhen is shown in the content view. 
                                                                        //.SetWhen(?)                                 // Add a timestamp pertaining to the notification(usually the time the event occurred). 
                            .SetContentText(text)
                            .SetContentTitle(title);
#else
            var intent = new Intent(appcontext, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.SingleTop);
            intent.PutExtra("Title", "Message");

            var pendingIntent = PendingIntent.GetActivity(appcontext, 0, intent, PendingIntentFlags.UpdateCurrent);

            NotificationCompat.Builder builder = null;
            if (notificationchannel == NOTIFICATION_INFO) {

               builder = new NotificationCompat.Builder(appcontext, notificationchannel)
                                  .SetOngoing(true)                           // true -> kann vom Anwender nicht beseitigt werden
                                  .SetProgress(100, 0, false);                 // Set the progress this notification represents.
               builder.SetSmallIcon(Icon[0]);

            } else if (notificationchannel == NOTIFICATION_MINALARM) {

               builder = new NotificationCompat.Builder(appcontext, notificationchannel)
                                  .SetOnlyAlertOnce(true)
                                  .SetOngoing(false);
               builder.SetSmallIcon(Icon[0]);

            } else if (notificationchannel == NOTIFICATION_MAXALARM) {

               builder = new NotificationCompat.Builder(appcontext, notificationchannel)
                                  //.SetSound(alarmSound)      // This method was deprecated in API level 26 (BuildVersionCodes.O). use NotificationChannel#setSound(Uri, AudioAttributes) instead.
                                  .SetOnlyAlertOnce(true)
                                  .SetOngoing(false);
               builder.SetSmallIcon(Icon[Icon.Count - 1]);

            }
#endif
            // Building channel if API verion is 26 or above
            if (global::Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.O)
               builder.SetChannelId(ChannelID);


            //builder.SetUsesChronometer(true)                    // Show the Notification When field as a stopwatch. 
            //builder.SetShowWhen(true)                           // Control whether the timestamp set with setWhen is shown in the content view. 
            //builder.SetWhen(?)                                  // Add a timestamp pertaining to the notification(usually the time the event occurred). 
            //builder.SetSmallIcon(Resource.Drawable.ic_mtrl_chip_close_circle);
            builder.SetContentTitle(title);                     // Set the first line of text in the platform notification template. 
            builder.SetContentText(text);                       // Set the second line of text in the platform notification template. 
            builder.SetContentIntent(pendingIntent);
            //builder.SetGroup("mygroupkey");                    // fkt. wahrscheinlich nur bei gleichem Kanal

            return builder;
         }

         public Notification CreateNotification(string text, string title) {
            NotificationBuilder = createNotificationBuilder(ChannelName, text, title);
            return NotificationBuilder.Build();
         }


         public override string ToString() {
            return string.Format("Name '{0}', ID '{1}', NotificationID '{2}'", ChannelName, ChannelID, NotificationID);
         }
      }

      private static Dictionary<string, AppNotificationChannel> channels = new Dictionary<string, AppNotificationChannel>() {
         {  NOTIFICATION_INFO, new AppNotificationChannel(NOTIFICATION_INFO, "INFO", 1000) },
         {  NOTIFICATION_MINALARM, new AppNotificationChannel(NOTIFICATION_MINALARM, ServiceCtrl.NotificationChannelBaseID + "MIN", 1001) },
         {  NOTIFICATION_MAXALARM, new AppNotificationChannel(NOTIFICATION_MAXALARM, ServiceCtrl.NotificationChannelBaseID + "MAX", 1002) },
      };

      /// <summary>
      /// Notification-ID für die normale Info-Notification
      /// </summary>
      public static int InfoNotificationID {
         get {
            return channels[NOTIFICATION_INFO].NotificationID;
         }
      }

      static void createAndRegisterChannels() {
         if (notificationManager == null) {
            notificationManager = appcontext.GetSystemService(Context.NotificationService) as NotificationManager;

            // Building channel if API verion is 26 or above
            if (global::Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.O)
               foreach (var item in channels)
                  item.Value.CreateAndRegisterChannel(notificationManager);
         }
      }

      /// <summary>
      /// Erzeugung der Info-Notification
      /// </summary>
      /// <param name="text"></param>
      /// <param name="title"></param>
      /// <returns></returns>
      public static Notification CreateInfoNotification(string text, string title) {
         return createNotification(NOTIFICATION_INFO, text, title);
      }

      static Notification createNotification(string channelname, string text, string title) {
         createAndRegisterChannels();
         return channels[channelname].CreateNotification(text, title);
      }

      /// <summary>
      /// Anzahl der Icons
      /// </summary>
      public static int Icons4InfoNotification {
         get {
            return channels[NOTIFICATION_INFO].Icon.Count;
         }
      }

      /// <summary>
      /// Aktualisierung der Info-Notification
      /// </summary>
      /// <param name="text"></param>
      /// <param name="percent"></param>
      /// <param name="title"></param>
      public static void UpdateInfoNotification(string text, int percent, int iconno, string title = null) {
         createAndRegisterChannels();

         NotificationCompat.Builder builder = channels[NOTIFICATION_INFO].NotificationBuilder;
         if (builder != null) {
            builder.SetContentText(text);
            if (title != null)
               builder.SetContentTitle(title);
            builder.SetProgress(100, percent, false);
            builder.SetSmallIcon(channels[NOTIFICATION_INFO].Icon[iconno]);
            builder.SetWhen(Java.Lang.JavaSystem.CurrentTimeMillis()); // Zeit ab dieser Aktualisierung wird angezeigt
            notificationManager.Notify(channels[NOTIFICATION_INFO].NotificationID, builder.Build());
         }
      }

      /// <summary>
      /// Anzeige der Min-Alarm-Notification
      /// </summary>
      /// <param name="text"></param>
      /// <param name="title"></param>
      public static void ShowMinAlarmNotification(string text, string title, string soundurl) {
         showAlarmNotification(NOTIFICATION_MINALARM, text, title, soundurl);
      }

      /// <summary>
      /// Anzeige der Max-Alarm-Notification
      /// </summary>
      /// <param name="text"></param>
      /// <param name="title"></param>
      public static void ShowMaxAlarmNotification(string text, string title, string soundurl) {
         showAlarmNotification(NOTIFICATION_MAXALARM, text, title, soundurl);
      }

      static void showAlarmNotification(string channelname, string text, string title, string soundurl) {
         createAndRegisterChannels();
         notificationManager.Cancel(channels[channelname].NotificationID);
         Notification notification = createNotification(channelname, text, title);
         notificationManager.Notify(channels[channelname].NotificationID, notification);

         if (!string.IsNullOrEmpty(soundurl))
            playNotifyRingtone(Android.Net.Uri.Parse(soundurl), 1F);
      }

      /// <summary>
      /// entfernen einer ev. vorhandenen Min-Alarm-Notification
      /// </summary>
      public static void RemoveMinAlarmNotification() {
         removeAlarmNotification(NOTIFICATION_MINALARM);
      }

      /// <summary>
      /// entfernen einer ev. vorhandenen Max-Alarm-Notification
      /// </summary>
      public static void RemoveMaxAlarmNotification() {
         removeAlarmNotification(NOTIFICATION_MAXALARM);
      }

      static void removeAlarmNotification(string channelname) {
         createAndRegisterChannels();
         notificationManager.Cancel(channels[channelname].NotificationID);
      }

      #region Sound als Ringtone abspielen

      static Ringtone alarmRingtone = null;

      /// <summary>
      /// spielt den Ton mit der URI ab
      /// </summary>
      /// <param name="uri"></param>
      /// <param name="volume">0.0 .. 1.0</param>
      static void playNotifyRingtone(Android.Net.Uri uri, float volume = 1.0F) {
         stopNotifyRingtone();
         Ringtone rt = RingtoneManager.GetRingtone(appcontext, uri);
         if (rt != null) {
            rt.AudioAttributes = new AudioAttributes.Builder()
                                     .SetUsage(AudioUsageKind.Alarm)
                                     .SetContentType(AudioContentType.Music)
                                     .Build();
            rt.Looping = false;
            rt.Volume = volume;
            rt.Play();
            alarmRingtone = rt;
         }
      }

      static void stopNotifyRingtone() {
         if (alarmRingtone != null)  {
            if (alarmRingtone.IsPlaying) 
               alarmRingtone.Stop();
            alarmRingtone.Dispose();
            alarmRingtone = null;
         }
      }

      #endregion

   }

}
