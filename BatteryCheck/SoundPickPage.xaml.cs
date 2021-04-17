using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BatteryCheck {
   [XamlCompilation(XamlCompilationOptions.Compile)]
   public partial class SoundPickPage : ContentPage {

      public class CloseEventArgs {

         public FSofTUtils.Xamarin.NativeSoundData NativeSoundData {
            get;
            protected set;
         }

         public CloseEventArgs(FSofTUtils.Xamarin.NativeSoundData data) {
            NativeSoundData = data;
         }
      }

      public event EventHandler<CloseEventArgs> CloseEvent;



      public SoundPickPage() {
         InitializeComponent();
      }

      protected override void OnAppearing() {
         base.OnAppearing();

         if (soundPicker.SelectedIndex >= 0)
            soundPicker.ScrollTo(soundPicker.SelectedIndex, false);  // ScrollTo() fkt. vorher noch NICHT
      }


      protected override void OnDisappearing() {
         base.OnDisappearing();
         FSofTUtils.Xamarin.SoundPicker.SoundData soundData = soundPicker.SelectedSound;
         if (soundData != null)
            soundData.Active = false;
      }

      public void AddAdditionalAudiofiles(IList<string> audiofiles) {
         foreach (var file in audiofiles)
            soundPicker.AppendFile(file);
      }

      public void InitSound(string sound) {
         if (!string.IsNullOrEmpty(sound)) {
            sound = sound.Trim();
            if (sound != "") {
               int idx = soundPicker.GetIndex4File(sound);
               if (idx < 0)
                  idx = soundPicker.GetIndex4Title(sound);
               if (idx >= 0)
                  soundPicker.ScrollTo(idx, true);    // fkt. hier NICHT -> OnAppearing()
            }
         }
      }

      private void soundPicker_CloseEvent(object sender, EventArgs e) {
         CloseEvent?.Invoke(this, new CloseEventArgs((sender as FSofTUtils.Xamarin.SoundPicker).Result));
         Navigation.PopAsync();
      }
   }
}