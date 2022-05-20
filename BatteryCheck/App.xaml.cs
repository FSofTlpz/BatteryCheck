using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BatteryCheck {
   public partial class App : Application {
      public App(object androidactivity = null) {
         InitializeComponent();

         //MainPage = new MainPage();

         MainPage = new NavigationPage(new MainPage()) {
            //BarBackgroundColor = Color.FromRgba(0.5, 0.5, 0.5, 1),
            BarTextColor = Color.White,
         };

      }

      protected override void OnStart() {
      }

      protected override void OnSleep() {
      }

      protected override void OnResume() {
      }
   }
}
