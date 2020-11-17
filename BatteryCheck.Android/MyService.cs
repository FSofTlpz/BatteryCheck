using System;
using Android.App;
using Android.Content;
using Android.OS;

namespace BatteryCheck.Droid {

   [Service]
   public class MyService : Service {

      static System.Threading.Timer timer = null;


      /// <summary>
      /// The system invokes this method by calling startService() when another component (such as an activity) requests that the service be started. 
      /// When this method executes, the service is started and can run in the background indefinitely. If you implement this, it is your responsibility 
      /// to stop the service when its work is complete by calling stopSelf() or stopService(). If you only want to provide binding, you don't need to 
      /// implement this method.
      /// </summary>
      /// <param name="intent"></param>
      /// <param name="flags"></param>
      /// <param name="startId"></param>
      /// <returns></returns>
      public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId) {
         Xamarin.Essentials.Battery.BatteryInfoChanged += battery_BatteryInfoChanged;

         // normale Notification erzeugen ...
         double chargelevel = Xamarin.Essentials.Battery.ChargeLevel;
         Notification notif = NotificationHelper.CreateInfoNotification(text4InfoBatteryState(chargelevel * 100, ServiceCtrl.MinPercent, ServiceCtrl.MaxPercent),
                                                                        "Batterieladung");

         // ... und starten
         StartForeground(NotificationHelper.InfoNotificationID, notif);

         // sofort Update der Daten und der Progressbar
         updateInfoNotification(chargelevel);

         /*
START_NOT_STICKY        If the system kills the service after onStartCommand() returns, do not recreate the service unless there are pending intents to deliver. 
                        This is the safest option to avoid running your service when not necessary and when your application can simply restart any unfinished jobs.
START_STICKY            If the system kills the service after onStartCommand() returns, recreate the service and call onStartCommand(), but do not redeliver the last intent. 
                        Instead, the system calls onStartCommand() with a null intent unless there are pending intents to start the service. 
                        In that case, those intents are delivered. This is suitable for media players (or similar services) that are not executing commands 
                        but are running indefinitely and waiting for a job.
START_REDELIVER_INTENT  If the system kills the service after onStartCommand() returns, recreate the service and call onStartCommand() with the last intent that 
                        was delivered to the service. Any pending intents are delivered in turn. This is suitable for services that are actively performing a job 
                        that should be immediately resumed, such as downloading a file.
         */
         return StartCommandResult.Sticky;
      }

      /// <summary>
      /// The system invokes this method to perform one-time setup procedures when the service is initially created (before it calls either onStartCommand() or onBind()). 
      /// If the service is already running, this method is not called.
      /// </summary>
      public override void OnCreate() {
         base.OnCreate();
      }

      /// <summary>
      /// The system invokes this method by calling bindService() when another component wants to bind with the service (such as to perform RPC). 
      /// In your implementation of this method, you must provide an interface that clients use to communicate with the service by returning an IBinder. 
      /// You must always implement this method; however, if you don't want to allow binding, you should return null.
      /// </summary>
      /// <param name="intent"></param>
      /// <returns></returns>
      public override IBinder OnBind(Intent intent) {
         return null;
      }

      /// <summary>
      /// The system invokes this method when the service is no longer used and is being destroyed. Your service should implement this to clean up any resources 
      /// such as threads, registered listeners, or receivers. This is the last call that the service receives.
      /// </summary>
      public override void OnDestroy() {
         Xamarin.Essentials.Battery.BatteryInfoChanged -= battery_BatteryInfoChanged;
         timerStopAndRemoveNotifications();
         base.OnDestroy();
      }


      /// <summary>
      /// vom System gemeldet
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void battery_BatteryInfoChanged(object sender, Xamarin.Essentials.BatteryInfoChangedEventArgs e) {
         OnUpdateData();
      }

      /// <summary>
      /// Die Konfigurationsdaten und/oder die akt. Batteriedaten haben sich geändert.
      /// </summary>
      public static void OnUpdateData() {
         if (ServiceCtrl.GetServiceIsActive(null)) {
            double chargelevel = Xamarin.Essentials.Battery.ChargeLevel;
            if (chargelevel >= 0) {

               updateInfoNotification(chargelevel);

               /*
               Fälle:

               level          Aufladung      
               <= min         nein           -> MinAlarm
               <= min         ja             -> alle Alarme löschen / Timer stop
               min<level<max  nein           -> alle Alarme löschen / Timer stop
               min<level<max  ja             -> alle Alarme löschen / Timer stop
               max <=         nein           -> alle Alarme löschen / Timer stop
               max <=         ja             -> MaxAlarm
                */

               bool ischarging = Xamarin.Essentials.Battery.State == Xamarin.Essentials.BatteryState.Charging ||
                                 (ServiceCtrl.AlarmFor100Percent &&        // Alarm auch bei 100% und Verbindung zum Stromnetz o.ä.
                                  Xamarin.Essentials.Battery.PowerSource != Xamarin.Essentials.BatteryPowerSource.Battery &&
                                  chargelevel == 1);
               bool needminalarm = !ischarging && chargelevel * 100 <= ServiceCtrl.MinPercent;
               bool needmaxalarm = ischarging && ServiceCtrl.MaxPercent <= chargelevel * 100;

               if ((needminalarm && !ischarging) ||
                   (needmaxalarm && ischarging)) {    // Alarm-Timer ist nötig
                  if (!timerIsRunning)
                     timerStopAndRemoveNotifications();

                  if (!timerIsRunning) {

                     if (needminalarm)
                        startTimer(ServiceCtrl.MinAlarmPeriod * 1000);
                     else if (needmaxalarm)
                        startTimer(ServiceCtrl.MaxAlarmPeriod * 1000);

                  } else {

                     if (needminalarm &&
                         actualtimerperiod != ServiceCtrl.MinAlarmPeriod * 1000) {        // Timer neu setzen
                        startTimer(ServiceCtrl.MinAlarmPeriod * 1000);
                     } else if (needmaxalarm &&
                                actualtimerperiod != ServiceCtrl.MaxAlarmPeriod * 1000) { // Timer neu setzen
                        startTimer(ServiceCtrl.MaxAlarmPeriod * 1000);
                     }

                  }

               } else
                  timerStopAndRemoveNotifications();
            }
         } else
            timerStopAndRemoveNotifications();
      }

      /// <summary>
      /// Die Info-Notification erhält ein Update (akt., min, max).
      /// </summary>
      /// <param name="chargelevel"></param>
      static void updateInfoNotification(double chargelevel) {
         if (0 <= chargelevel && chargelevel <= 1) {
            double range = 1.0 / NotificationHelper.Icons4InfoNotification;
            int iconno;
            for (iconno = 0; iconno < NotificationHelper.Icons4InfoNotification; iconno++)
               if (chargelevel <= (iconno + 1) * range)
                  break;
            NotificationHelper.UpdateInfoNotification(text4InfoBatteryState(chargelevel * 100, ServiceCtrl.MinPercent, ServiceCtrl.MaxPercent),
                                                      (int)(chargelevel * 100),
                                                      iconno);
         }
      }

      /// <summary>
      /// erzeugt den Text für die Info-Notification
      /// </summary>
      /// <param name="actual"></param>
      /// <param name="min"></param>
      /// <param name="max"></param>
      /// <returns></returns>
      static string text4InfoBatteryState(double actual, double min, double max) {
         return string.Format("{0}% (gewünschter Bereich {1}% .. {2}%)", actual, min, max);
      }

      /// <summary>
      /// Timer stoppen und Min/Max-Notification entfernen
      /// </summary>
      static void timerStopAndRemoveNotifications() {
         stopTimer();
         NotificationHelper.RemoveMinAlarmNotification();
         NotificationHelper.RemoveMaxAlarmNotification();
      }

      /// <summary>
      /// akt. eingestellte Timer-Periode
      /// </summary>
      static int actualtimerperiod = 0;

      /// <summary>
      /// der Timer wird gestartet
      /// </summary>
      /// <param name="period">Intervall in ms</param>
      static void startTimer(int period) {
         stopTimer();
         timer = new System.Threading.Timer(onTimerStep, null, 0, period);
         actualtimerperiod = period;
      }

      /// <summary>
      /// der Timer wird gestoppt
      /// </summary>
      static void stopTimer() {
         if (timerIsRunning) {
            timer.Change(System.Threading.Timeout.Infinite, 0);
            timer.Dispose();
            timer = null;
            actualtimerperiod = 0;
         }
      }

      static bool timerIsRunning {
         get {
            return timer != null;
         }
      }

      /// <summary>
      /// Timerintervall abgelaufen
      /// </summary>
      /// <param name="state"></param>
      static void onTimerStep(object state) {
         if (Xamarin.Essentials.Battery.ChargeLevel * 100 <= ServiceCtrl.MinPercent) {

            NotificationHelper.ShowMinAlarmNotification(string.Format("Minimum {0}% unterschritten: {1}%; Alarm nach jeweils {2}",
                                                                      ServiceCtrl.MinPercent,
                                                                      Xamarin.Essentials.Battery.ChargeLevel * 100,
                                                                      alarmTime2Text(ServiceCtrl.MinAlarmPeriod)),
                                                        "Batterieentladung");

         } else if (ServiceCtrl.MaxPercent <= Xamarin.Essentials.Battery.ChargeLevel * 100) {

            NotificationHelper.ShowMaxAlarmNotification(string.Format("Maximum {0}% überschritten: {1}%; Alarm nach jeweils {2}",
                                                                      ServiceCtrl.MaxPercent,
                                                                      Xamarin.Essentials.Battery.ChargeLevel * 100,
                                                                      alarmTime2Text(ServiceCtrl.MaxAlarmPeriod)),
                                                        "Batterieaufladung");

         } else {

            timerStopAndRemoveNotifications();

         }
      }

      static string alarmTime2Text(int sec) {
         if (sec < 60)
            return sec.ToString() + "s";

         int min = sec / 60;
         sec %= 60;

         if (sec == 0)
            return min.ToString() + "min";

         return min.ToString() + ":" + sec.ToString("d2") + "min";
      }


   }
}
