namespace BatteryCheck {
   public interface IBatteryInfo {

      void GetAndroidBatteryExtendedData(out int health, out float temperature, out string technology, out long chargetimeremaining);

   }
}
