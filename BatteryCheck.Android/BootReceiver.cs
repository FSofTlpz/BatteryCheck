using Android.App;
using Android.Content;

namespace BatteryCheck.Droid {

   /*
      Rechte der App:
     
      <uses-permission android:name="android.permission.FOREGROUND_SERVICE"></uses-permission>
      <uses-permission android:name="android.permission.INTERNET"></uses-permission>
      <uses-permission android:name="android.permission.WAKE_LOCK" />
      <uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED"/>

      im Manifest nötig:

        <receiver android:enabled="true" android:name=".BootReceiver">
            <intent-filter>
                <action android:name="android.intent.action.BOOT_COMPLETED"/>
            </intent-filter>
        </receiver>

      wird in Xamarin durch die []'s der Klasse erzeugt
   */

   [BroadcastReceiver(Enabled = true)]
   [IntentFilter(new[] { Intent.ActionBootCompleted })]
   class BootReceiver : BroadcastReceiver {
      // Neustart nach Reboot

      /// <summary>
      /// Falls die App die Berechtigung RECEIVE_BOOT_COMPLETED hat, wird hier (auch) Intent.ActionBootCompleted gemeldet
      /// und der Service wird gestartet.
      /// </summary>
      /// <param name="context"></param>
      /// <param name="intent"></param>
      public override void OnReceive(Context context, Intent intent) {
         if (intent.Action == Intent.ActionBootCompleted) {
            if (ServiceCtrl.GetServiceIsActive(context)) {
               Intent it = new Intent(context, Java.Lang.Class.FromType(typeof(MyService)));
               ComponentName componentName;
               if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                  componentName = context.StartForegroundService(it);
               else
                  componentName = context.StartService(it);
            }
         }
      }

   }
}