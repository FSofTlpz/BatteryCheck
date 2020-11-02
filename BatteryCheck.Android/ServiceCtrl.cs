using Android.Content;

[assembly: Xamarin.Forms.Dependency(typeof(BatteryCheck.Droid.ServiceCtrl))]
namespace BatteryCheck.Droid {

   /// <summary>
   /// Interface zur Xamarin-Seite zum Steuern des Service
   /// </summary>
   internal class ServiceCtrl : IServiceCtrl {

      private static Android.Content.Context context = global::Android.App.Application.Context;

      public bool StartService() {
         var myserviceintent = new Android.Content.Intent(context, Java.Lang.Class.FromType(typeof(MyService)));

         Android.Content.ComponentName componentName;
         if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            componentName = context.StartForegroundService(myserviceintent);
         else
            componentName = context.StartService(myserviceintent);

         SetServiceActive(context, componentName != null);

         return componentName != null;
      }

      public bool StopService() {
         var intent = new Android.Content.Intent(context, typeof(MyService));

         bool result = context.StopService(intent); ;

         SetServiceActive(context, false);
         NotificationHelper.RemoveMinAlarmNotification();
         NotificationHelper.RemoveMaxAlarmNotification();

         return result;
      }


      #region Speichern/Lesen spezieller privater (Android-)Vars

      const string SERVICEISACTIVE = "ServiceIsActive";

      /// <summary>
      /// 
      /// </summary>
      /// <param name="context">wenn null, dann wird automatisch der App-Context verwendet</param>
      /// <returns></returns>
      public static bool GetServiceIsActive(Context context) {
         if (context != null)
            return GetPrivateData(context, SERVICEISACTIVE, false);
         else
            return GetPrivateData(SERVICEISACTIVE, false);
      }

      /// <summary>
      /// nur für die Abfrage aus der App
      /// </summary>
      /// <returns></returns>
      public bool GetServiceIsActive() {
         return GetServiceIsActive(context);
      }

      private void SetServiceActive(Context context = null, bool on = true) {
         if (context != null)
            SetPrivateData(context, SERVICEISACTIVE, on);
         else
            SetPrivateData(SERVICEISACTIVE, on);
      }


      const string MINPERCENT = "MinPercent";

      public static int MinPercent {
         get {
            return GetPrivateData(MINPERCENT, 0);
         }
         set {
            if (value < 0)
               value = 0;
            if (value > 100)
               value = 100;
            SetPrivateData(MINPERCENT, value);
         }
      }

      public int GetMinPercent() {
         return MinPercent;
      }

      public void SetMinPercent(int value) {
         if (MinPercent != value) {
            MinPercent = value;
            MyService.OnUpdateData();
         }
      }


      const string MAXPERCENT = "MaxPercent";

      public static int MaxPercent {
         get {
            return GetPrivateData(MAXPERCENT, 100);
         }
         set {
            if (value > 100)
               value = 100;
            if (value < 0)
               value = 0;
            SetPrivateData(MAXPERCENT, value);
         }
      }

      public int GetMaxPercent() {
         return MaxPercent;
      }

      public void SetMaxPercent(int value) {
         if (MaxPercent != value) {
            MaxPercent = value;
            MyService.OnUpdateData();
         }
      }


      const string MINTIMERPERIOD = "MinTimerPeriod";

      /// <summary>
      /// setzt und liefert die Zeit zwischen 2 Alarmen bei Unterschreitung des Minimalwertes (10s .. 1h, Standard 60s)
      /// </summary>
      public static int MinAlarmPeriod {
         get {
            return GetPrivateData(MINTIMERPERIOD, 60);
         }
         set {
            if (value > 3600)
               value = 3600;
            if (value < 10)
               value = 10;
            SetPrivateData(MINTIMERPERIOD, value);
         }
      }

      public int GetMinAlarmPeriod() {
         return MinAlarmPeriod;
      }

      /// <summary>
      /// 10s .. 1h
      /// </summary>
      /// <param name="value"></param>
      public void SetMinAlarmPeriod(int value) {
         if (MinAlarmPeriod != value) {
            MinAlarmPeriod = value;
            MyService.OnUpdateData();
         }
      }


      const string MAXTIMERPERIOD = "MaxTimerPeriod";

      /// <summary>
      /// setzt und liefert die Zeit zwischen 2 Alarmen bei Überschreitung des Maximalwertes (5s .. 5min, Standard 30s)
      /// </summary>
      public static int MaxAlarmPeriod {
         get {
            return GetPrivateData(MAXTIMERPERIOD, 30);
         }
         set {
            if (value > 300)
               value = 300;
            if (value < 5)
               value = 5;
            SetPrivateData(MAXTIMERPERIOD, value);
         }
      }

      public int GetMaxAlarmPeriod() {
         return MaxAlarmPeriod;
      }

      public void SetMaxAlarmPeriod(int value) {
         if (MaxAlarmPeriod != value) {
            MaxAlarmPeriod = value;
            MyService.OnUpdateData();
         }
      }


      const string NOTIFICATIONCHANNELBASEID = "NotificationChannelBaseID";

      public static string NotificationChannelBaseID {
         get {
            return GetPrivateData(NOTIFICATIONCHANNELBASEID, "");
         }
         set {
            SetPrivateData(NOTIFICATIONCHANNELBASEID, value);
         }
      }

      const string ALARM100PERCENT = "Alarm100Percent";

      public static bool AlarmFor100Percent {
         get {
            return GetPrivateData(ALARM100PERCENT, true);
         }
         set {
            SetPrivateData(ALARM100PERCENT, value);
         }
      }

      public bool GetAlarmFor100Percent() {
         return AlarmFor100Percent;
      }

      public void SetAlarmFor100Percent(bool value) {
         AlarmFor100Percent = value;
      }

      #endregion

      #region allg. Funktionen zum Speichern/Lesen privater (Android-)Vars

      const string PREFFILE = "servicevars";

      static void SetPrivateData(string varname, bool value) {
         SetPrivateData(context, varname, value);
      }

      static void SetPrivateData(string varname, int value) {
         SetPrivateData(context, varname, value);
      }

      static void SetPrivateData(string varname, string value) {
         SetPrivateData(context, varname, value);
      }

      static void SetPrivateData(string varname, float value) {
         SetPrivateData(context, varname, value);
      }

      static bool GetPrivateData(string varname, bool defvalue) {
         return GetPrivateData(context, varname, defvalue);
      }

      static int GetPrivateData(string varname, int defvalue) {
         return GetPrivateData(context, varname, defvalue);
      }

      static string GetPrivateData(string varname, string defvalue) {
         return GetPrivateData(context, varname, defvalue);
      }

      static float GetPrivateData(string varname, float defvalue) {
         return GetPrivateData(context, varname, defvalue);
      }


      static void SetPrivateData(Context context, string varname, bool value) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         var editor = pref.Edit();
         editor.PutBoolean(varname, value);
         editor.Apply();
      }

      static void SetPrivateData(Context context, string varname, int value) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         var editor = pref.Edit();
         editor.PutInt(varname, value);
         editor.Apply();
      }

      static void SetPrivateData(Context context, string varname, string value) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         var editor = pref.Edit();
         editor.PutString(varname, value);
         editor.Apply();
      }

      static void SetPrivateData(Context context, string varname, float value) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         var editor = pref.Edit();
         editor.PutFloat(varname, value);
         editor.Apply();
      }

      static bool GetPrivateData(Context context, string varname, bool defvalue) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         return pref.GetBoolean(varname, defvalue);
      }

      static int GetPrivateData(Context context, string varname, int defvalue) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         return pref.GetInt(varname, defvalue);
      }

      static string GetPrivateData(Context context, string varname, string defvalue) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         return pref.GetString(varname, defvalue);
      }

      static float GetPrivateData(Context context, string varname, float defvalue) {
         var pref = context.GetSharedPreferences(PREFFILE, FileCreationMode.Private);
         return pref.GetFloat(varname, defvalue);
      }

      #endregion


      public void ChangeMinAlarm() {
         NotificationHelper.ChangeMinAlarmChannelUI();
      }

      public void ChangeMaxAlarm() {
         NotificationHelper.ChangeMaxAlarmChannelUI();
      }

      public void ResetAlamsound() {
         NotificationHelper.ResetAlarmChannels();
      }

   }

}