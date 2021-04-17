using Xamarin.Forms;

namespace BatteryCheck {
   /// <summary>
   ///  Hilfsfunktionen zum einfacheren Zugriff auf Dependecy-Funktionen
   /// </summary>
   class DepHelper {

      /// <summary>
      /// Liefert und setzt den Status des Überwachungsservice?
      /// </summary>
      public static bool ServiceIsActive {
         get {
            return DependencyService.Get<IServiceCtrl>().GetServiceIsActive();
         }
         set {
            if (value)
               DependencyService.Get<IServiceCtrl>().StartService();
            else
               DependencyService.Get<IServiceCtrl>().StopService();
         }
      }

      /// <summary>
      /// unterer proz. Grenzwert der Akkuladung für die Alarm-Auslösungen
      /// </summary>
      public static int MinPercent {
         get {
            return DependencyService.Get<IServiceCtrl>().GetMinPercent();
         }
         set {
            DependencyService.Get<IServiceCtrl>().SetMinPercent(value);
         }
      }

      /// <summary>
      /// oberer proz. Grenzwert der Akkuladung für die Alarm-Auslösungen
      /// </summary>
      public static int MaxPercent {
         get {
            return DependencyService.Get<IServiceCtrl>().GetMaxPercent();
         }
         set {
            DependencyService.Get<IServiceCtrl>().SetMaxPercent(value);
         }
      }

      /// <summary>
      /// Zeit zwischen 2 Alarm-Auslösungen in Sekunden
      /// </summary>
      public static int MinAlarmPeriod {
         get {
            return DependencyService.Get<IServiceCtrl>().GetMinAlarmPeriod();
         }
         set {
            DependencyService.Get<IServiceCtrl>().SetMinAlarmPeriod(value);
         }
      }

      /// <summary>
      /// Zeit zwischen 2 Alarm-Auslösungen in Sekunden
      /// </summary>
      public static int MaxAlarmPeriod {
         get {
            return DependencyService.Get<IServiceCtrl>().GetMaxAlarmPeriod();
         }
         set {
            DependencyService.Get<IServiceCtrl>().SetMaxAlarmPeriod(value);
         }
      }

      /// <summary>
      /// Alarm auch bei 100% und Verbindung zum Stromnetz
      /// </summary>
      public static bool AlarmOn100Percent {
         get {
            return DependencyService.Get<IServiceCtrl>().GetAlarmFor100Percent();
         }
         set {
            DependencyService.Get<IServiceCtrl>().SetAlarmFor100Percent(value);
         }
      }

      /// <summary>
      /// URL des MinAlarm
      /// </summary>
      public static string MinAlarm {
         get {
            return DependencyService.Get<IServiceCtrl>().GetMinAlarm();
         }
         set {
            DependencyService.Get<IServiceCtrl>().SetMinAlarm(value);
         }
      }

      /// <summary>
      /// URL des MaxAlarm
      /// </summary>
      public static string MaxAlarm {
         get {
            return DependencyService.Get<IServiceCtrl>().GetMaxAlarm();
         }
         set {
            DependencyService.Get<IServiceCtrl>().SetMaxAlarm(value);
         }
      }


      /// <summary>
      /// liefert (unter Android) zusätzliche Daten für die Batterie
      /// </summary>
      /// <param name="health"></param>
      /// <param name="temperature"></param>
      /// <param name="technology"></param>
      public static void GetAndroidBatteryExtendedData(out int health, out float temperature, out string technology, out long chargetimeremaining) {
         DependencyService.Get<IBatteryInfo>().GetAndroidBatteryExtendedData(out health, out temperature, out technology, out chargetimeremaining);
      }

   }
}
