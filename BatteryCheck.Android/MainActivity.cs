using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace BatteryCheck.Droid {
   [Activity(Label = "BatteryCheck", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
   public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity {
      protected override void OnCreate(Bundle savedInstanceState) {
         TabLayoutResource = Resource.Layout.Tabbar;
         ToolbarResource = Resource.Layout.Toolbar;

         base.OnCreate(savedInstanceState);

         Xamarin.Essentials.Platform.Init(this, savedInstanceState);
         global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
         LoadApplication(new App());
      }

      public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
         Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

         base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
      }


      /// <summary>
      /// reagiert auf den Software-Backbutton (fkt. NUR mit SetSupportActionBar() in OnCreate())
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      public override bool OnOptionsItemSelected(IMenuItem item) {
         // check if the current item id is equals to the back button id
         if (item.ItemId == 16908332) {
            Xamarin.Forms.Application myapplication = Xamarin.Forms.Application.Current;
            if (myapplication.MainPage.SendBackButtonPressed())
               return false;
         }
         return base.OnOptionsItemSelected(item);
      }

      /// <summary>
      /// Hardware-Backbutton
      /// </summary>
      public override void OnBackPressed() {
         // this is not necessary, but in Android user has both Nav bar back button and physical back button its safe to cover the both events
         Xamarin.Forms.Application myapplication = Xamarin.Forms.Application.Current;
         if (myapplication.MainPage.SendBackButtonPressed())
            return;
         base.OnBackPressed();
      }

   }
}