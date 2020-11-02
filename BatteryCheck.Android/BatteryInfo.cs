using System;

using Android.App;
using Android.Content;
using Android.OS;

// https://github.com/NDimo/BatteryInfo

[assembly: Xamarin.Forms.Dependency(typeof(BatteryCheck.Droid.BatteryInfo))]
namespace BatteryCheck.Droid {
   public class BatteryInfo : IBatteryInfo {

      /// <summary>
      /// integer field containing the current battery level, from 0 to EXTRA_SCALE (in %)
      /// </summary>
      public int Level { get; protected set; }
      /// <summary>
      /// integer containing the maximum battery level.
      /// </summary>
      public int Scale { get; protected set; }
      /// <summary>
      /// integer containing the current status constant.
      /// </summary>
      public BatteryStatus Status { get; protected set; }
      /// <summary>
      /// integer containing the current health constant
      /// </summary>
      public BatteryHealth Health { get; protected set; }
      /// <summary>
      /// integer containing the current battery temperature. (in 1/10 °C)
      /// </summary>
      public float Temperature { get; protected set; }
      /// <summary>
      ///  String describing the technology of the current battery.
      /// </summary>
      public string Technology { get; protected set; }


      public BatteryInfo() {
         Update();
      }

      private void Update() {
         try {
            var ifilter = new IntentFilter(Intent.ActionBatteryChanged);
            Intent batteryStatusIntent = Application.Context.RegisterReceiver(null, ifilter);
            getBatteryValuesFromIntent(batteryStatusIntent);
         } catch {
         }
      }

      private void getBatteryValuesFromIntent(Intent intent) {
         Scale = intent.GetIntExtra(BatteryManager.ExtraScale, -1);

         Level = intent.GetIntExtra(BatteryManager.ExtraLevel, 0);

         int statusExtra = intent.GetIntExtra(BatteryManager.ExtraStatus, -1);
         Status = getBatteryStatus(statusExtra);

         int healthExtra = intent.GetIntExtra(BatteryManager.ExtraHealth, -1);
         Health = getBatteryHealth(healthExtra);

         Temperature = intent.GetIntExtra(BatteryManager.ExtraTemperature, -1);

         Technology = intent.GetStringExtra(BatteryManager.ExtraTechnology);
      }

      private BatteryStatus getBatteryStatus(int status) {
         var result = BatteryStatus.Unknown;
         if (Enum.IsDefined(typeof(BatteryStatus), status)) {
            result = (BatteryStatus)status;
         }
         return result;
      }

      private BatteryHealth getBatteryHealth(int health) {
         var result = BatteryHealth.Unknown;
         if (Enum.IsDefined(typeof(BatteryHealth), health)) {
            result = (BatteryHealth)health;
         }
         return result;
      }

      public void GetAndroidBatteryExtendedData(out int health, out float temperature, out string technology, out long chargetimeremaining) {
         Update();

         health = (int)Health;
         temperature = Temperature;
         technology = Technology;
         chargetimeremaining = -1;

         if (Build.VERSION.SdkInt >= BuildVersionCodes.P) {
            Context context = global::Android.App.Application.Context;
            if (context != null) {
               BatteryManager mBatteryManager = context.GetSystemService(Context.BatteryService) as BatteryManager;
               chargetimeremaining = mBatteryManager.ComputeChargeTimeRemaining();
            }
         }
      }

   }

}