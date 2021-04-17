namespace BatteryCheck {

   /// <summary>
   /// Interface zum Steuern des Service
   /// </summary>
   public interface IServiceCtrl {

      bool StartService();

      bool StopService();


      bool GetServiceIsActive();


      int GetMinPercent();

      void SetMinPercent(int value);


      int GetMaxPercent();

      void SetMaxPercent(int value);


      int GetMinAlarmPeriod();

      void SetMinAlarmPeriod(int value);


      int GetMaxAlarmPeriod();

      void SetMaxAlarmPeriod(int value);


      bool GetAlarmFor100Percent();

      void SetAlarmFor100Percent(bool value);


      string GetMinAlarm();
      void SetMinAlarm(string url);

      string GetMaxAlarm();
      void SetMaxAlarm(string url);

   }
}
