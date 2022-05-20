using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.App;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BatteryCheck.Droid {

   /*
   - Namespace anpassen
   - MainActivity.cs anpassen:

      protected override void OnCreate(Bundle savedInstanceState) {
         //TabLayoutResource = Resource.Layout.Tabbar;
         //ToolbarResource = Resource.Layout.Toolbar;
         base.OnCreate(savedInstanceState);

         Xamarin.Essentials.Platform.Init(this, savedInstanceState);
         global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
         LoadApplication(new App(this));
         //LoadApplication(new App());

         // zusätzlich
         onCreateExtend(savedInstanceState,
                        new string[] {
                              Manifest.Permission.WriteExternalStorage,    // u.a. für Cache
                              Manifest.Permission.ReadExternalStorage,     // u.a. für Karten und Konfig.
                              Manifest.Permission.AccessFineLocation,      // GPS-Standort            ACHTUNG: Dieses Recht muss ZUSÄTZLICH im Manifest festgelegt sein, sonst wird es NICHT angefordert!
                              Manifest.Permission.AccessNetworkState,
                        });
      }

      public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
         Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

         // zusätzlich
         onRequestPermissionsResult(requestCode, permissions, grantResults);

         base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
      }

    */

   partial class MainActivity {

      /// <summary>
      /// Erweiterung von OnCreate()
      /// </summary>
      /// <param name="savedInstanceState"></param>
      /// <param name="permissions"></param>
      async void onCreateExtend(Bundle savedInstanceState, string[] permissions) {
         Android.Content.PM.Permission[] result = await checkAndRequestAppPermissions(permissions);
         bool permissionsOK = true;
         foreach (var item in result)
            if (item == Permission.Denied) {
               permissionsOK = false;
               break;
            }

         if (permissionsOK)
            LoadApplication(new App(this));
         else {
            AlertDialog dlg = new AlertDialog.Builder(this).Create();
            dlg.SetTitle("Sorry");
            dlg.SetMessage("Der App fehlen die für ihre Ausführung notwendigen Rechte.");
            dlg.SetButton("OK", (c, ev) => {
               Finish();
            });
            dlg.SetCancelable(false);
            dlg.Show();
         }
      }

      /// <summary>
      /// reagiert auf den Software-Backbutton (fkt. NUR mit SetSupportActionBar() in OnCreate())
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      public override bool OnOptionsItemSelected(IMenuItem item) {
         // check if the current item id is equals to the back button id
         if (item.ItemId == Android.Resource.Id.Home) {
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

      #region Permissions anfordern (mit await checkAndRequestAppPermissions())

      const int REQUEST_APPPERMISSIONS = 100;

      Android.Content.PM.Permission[] _appPermissionsRequestResult = null;

      ManualResetEvent _manualResetEvent4RequestAppPermissions = new ManualResetEvent(false);

      /// <summary>
      /// liefert ein Array der Rechte, die noch nicht vorhanden war, mit dem jeweiligen Ergebnis
      /// </summary>
      /// <param name="permissions"></param>
      /// <returns></returns>
      async Task<Android.Content.PM.Permission[]> checkAndRequestAppPermissions(IList<string> permissions) {
         List<string> neededPermissions = new List<string>();
         foreach (var item in permissions) {
            if (CheckSelfPermission(item) == Permission.Denied)   // wenn noch nicht vorhanden, dann anforden
               neededPermissions.Add(item);
         }
         if (neededPermissions.Count > 0)
            await Task.Run(() => {
               ActivityCompat.RequestPermissions(this,
                                                 neededPermissions.ToArray(),
                                                 REQUEST_APPPERMISSIONS);
               _manualResetEvent4RequestAppPermissions.WaitOne();
            });
         else
            _appPermissionsRequestResult = new Permission[0];
         return _appPermissionsRequestResult;
      }

      /// <summary>
      /// jetzt erfolgt die Auswertung der interaktiv vom User angeforderten Rechte und es wird versucht die App zu starten
      /// </summary>
      /// <param name="requestCode"></param>
      /// <param name="permissions"></param>
      /// <param name="grantResults"></param>
      public void onRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
         switch (requestCode) {
            case REQUEST_APPPERMISSIONS: {
                  _appPermissionsRequestResult = new Permission[grantResults.Length];
                  grantResults.CopyTo(_appPermissionsRequestResult, 0);
                  _manualResetEvent4RequestAppPermissions.Set();
               }
               break;
         }
      }

      #endregion

      protected override void OnActivityResult(int requestCode, Result resultCode, Intent data) {
         base.OnActivityResult(requestCode, resultCode, data);
         //System.Diagnostics.Debug.WriteLine(string.Format("OnActivityResult({0}, {1}, {2}", requestCode));
         MyOnActivityResult?.Invoke(requestCode, resultCode, data);
         myActivityResult4AsyncExt(requestCode, resultCode, data);
      }

      /// <summary>
      /// öffnet <see cref="OnActivityResult"/> nach außen
      /// </summary>
      public event Action<int, Result, Intent> MyOnActivityResult;

      #region StartActivityForResultAsync

      private int myActivityResultRegistrationCounter = 10000;

      private readonly Dictionary<int, TaskCompletionSource<Tuple<Result, Intent>>> myActivityResultRegistrations = new Dictionary<int, TaskCompletionSource<Tuple<Result, Intent>>>();

      /// <summary>
      /// Starts another activity and allows awaiting on its return.
      /// </summary>
      /// <param name="intent">The intent that should be used to launch the new activity.</param>
      /// <returns>A task that completes with the finishing of the activity, providing the result of the activity.</returns>
      /// <example>
      /// var result = await this.StartActivityForResultAsync(Intent.CreateChooser(intent, "Select picture"));
      /// if (result.Item1 == Result.Ok) {
      ///     // User chose a picture. Get the Uri
      ///     Android.Net.Uri imageSource = result.Item2.Data;
      /// }
      /// </example>
      public Task<Tuple<Result, Intent>> MyStartActivityForResultAsync(Intent intent, int requestcode = -1) {
         int requestCode = requestcode > 0 ?
                              requestcode :
                              myActivityResultRegistrationCounter++;
         var completionSource = new TaskCompletionSource<Tuple<Result, Intent>>();
         myActivityResultRegistrations[requestCode] = completionSource;
         StartActivityForResult(intent, requestCode);
         return completionSource.Task;
      }

      void myActivityResult4AsyncExt(int requestCode, Result resultCode, Intent data) {
         if (myActivityResultRegistrations.TryGetValue(requestCode, out TaskCompletionSource<Tuple<Result, Intent>> completionSource)) {
            myActivityResultRegistrations.Remove(requestCode);
            completionSource.SetResult(Tuple.Create(resultCode, data));
         }
      }

      #endregion


   }
}