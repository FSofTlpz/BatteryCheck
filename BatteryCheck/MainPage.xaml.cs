using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BatteryCheck {
   public partial class MainPage : ContentPage {

      const string TITLE = "Batterieüberwachung, © by FSofT 4.5.2022";

      const string FIRSTSTARTKEY = "FirstStart";

      const int STDMINPERCENT = 30;
      const int STDMAXPERCENT = 85;
      const int STDMINALARMPERIOD = 60;
      const int STDMAXALARMPERIOD = 30;
      const bool STDALARMON100Percent = true;
      const double STDVOLUME = 1;

      readonly List<string> internalAudiofiles = new List<string>();

      readonly ManualResetEvent mreWait4LoadInternalAudiofiles = new ManualResetEvent(false);

      string soundMaxAlarm = "";
      string soundMinAlarm = "";

      bool isOnInit = true;

      public MainPage() {
         InitializeComponent();
         BackgroundImageSource = ImageSource.FromResource("BatteryCheck.mybackground.jpg");
         UnpackAndRegisterMusic().Start();
      }

      protected override void OnAppearing() {
         base.OnAppearing();

         Title = TITLE + " (v" + Xamarin.Essentials.AppInfo.VersionString + ")";

         startbutton.IsEnabled = !DepHelper.ServiceIsActive;
         stopbutton.IsEnabled = !startbutton.IsEnabled;

         Battery.BatteryInfoChanged += battery_BatteryInfoChanged;
         Battery.EnergySaverStatusChanged += battery_EnergySaverStatusChanged;

         showActualBatteryState();

         if (!Application.Current.Properties.ContainsKey(FIRSTSTARTKEY)) {
            DepHelper.MinPercent = STDMINPERCENT;
            DepHelper.MaxPercent = STDMAXPERCENT;
            DepHelper.MinAlarmPeriod = STDMINALARMPERIOD;
            DepHelper.MaxAlarmPeriod = STDMAXALARMPERIOD;
            DepHelper.AlarmOn100Percent = STDALARMON100Percent;
            DepHelper.MinAlarmVolume = STDVOLUME;
            DepHelper.MaxAlarmVolume = STDVOLUME;
            Application.Current.Properties[FIRSTSTARTKEY] = 1;
         }

         sliderMinPercent.Value = DepHelper.MinPercent;
         sliderMaxPercent.Value = DepHelper.MaxPercent;

         labelMinPercent.Text = string.Format("{0}%", sliderMinPercent.Value);
         labelMaxPercent.Text = string.Format("{0}%", sliderMaxPercent.Value);

         pickerMinAlarmPeriod.SelectedIndex = getBestIdx4Period(pickerMinAlarmPeriod, DepHelper.MinAlarmPeriod);
         pickerMaxAlarmPeriod.SelectedIndex = getBestIdx4Period(pickerMaxAlarmPeriod, DepHelper.MaxAlarmPeriod);

         checkbox100PercentAlarm.IsChecked = DepHelper.AlarmOn100Percent;

         soundMinAlarm = DepHelper.MinAlarm;
         soundMaxAlarm = DepHelper.MaxAlarm;

         isOnInit = false;
      }

      protected override void OnDisappearing() {
         Battery.BatteryInfoChanged -= battery_BatteryInfoChanged;
         Battery.EnergySaverStatusChanged -= battery_EnergySaverStatusChanged;

         base.OnDisappearing();
      }

      /// <summary>
      /// speichert die als Resourcen gelieferten mp3-Dateien im internen (App-privaten) Speicher
      /// </summary>
      Task UnpackAndRegisterMusic() {
         Task t = new Task(() => {
            string basepath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic); // intern, z.B.: "/data/user/0/com.fsoft.simpletimer/files/Music"

            if (!Directory.Exists(basepath))
               Directory.CreateDirectory(basepath);

            Assembly ass = GetType().Assembly;

            SortedDictionary<string, string> audiofilenames = new SortedDictionary<string, string>();
            foreach (string resname in ass.GetManifestResourceNames()) {
               string ext = resname.Substring(resname.LastIndexOf('.') + 1);
               if (ext.ToLower() == "mp3") {
                  int start = -1;
                  for (int i = resname.Length - 5; i >= 0; i--) {
                     if (resname[i] == '.') {
                        start = i + 1;
                        break;
                     }
                  }
                  if (start >= 0)
                     audiofilenames.Add(resname, resname.Substring(start));
               }
            }

            foreach (var item in audiofilenames) {
               string filename = item.Value;
               string title = filename.Substring(0, filename.LastIndexOf('.')); // fkt. unter Android 10 wahrscheinlich nicht -> Dateiname verwenden
               string srcfilename = Path.Combine(basepath, filename);

               if (!File.Exists(srcfilename)) {
                  using (Stream stream = ass.GetManifestResourceStream(item.Key)) {
                     using (FileStream fs = new FileStream(srcfilename, FileMode.Create)) {
                        stream.CopyTo(fs);
                     }
                  }
               }
               internalAudiofiles.Add(srcfilename);
            }

            if (DepHelper.MinAlarm == "" && internalAudiofiles.Count >= 2)
               soundMinAlarm = DepHelper.MinAlarm = internalAudiofiles[1];

            if (DepHelper.MaxAlarm == "" && internalAudiofiles.Count >= 1)
               soundMaxAlarm = DepHelper.MaxAlarm = internalAudiofiles[0];

            mreWait4LoadInternalAudiofiles.Set();

         });

         return t;
      }

      void showActualBatteryState(double chargeLevel,
                                  BatteryPowerSource batteryPowerSource,
                                  BatteryState batteryState,
                                  EnergySaverStatus energySaverStatus) {

         labelCharge.Text = string.Format("{0}%", chargeLevel * 100);

         switch (batteryPowerSource) {
            case BatteryPowerSource.Battery:
               labelPowerSource.Text = "Batterie";
               break;
            case BatteryPowerSource.AC:
               labelPowerSource.Text = "Stromnetz";
               break;
            case BatteryPowerSource.Usb:
               labelPowerSource.Text = "USB-Anschluss";
               break;
            case BatteryPowerSource.Wireless:
               labelPowerSource.Text = "drahtlos";
               break;
            case BatteryPowerSource.Unknown:
               labelPowerSource.Text = "unbekannt";
               break;
         }

         switch (batteryState) {
            case BatteryState.Full:             // 5
               labelBatteryState.Text = "voll geladen";
               break;
            case BatteryState.Discharging:      // 3
               labelBatteryState.Text = "wird entladen";
               break;
            case BatteryState.Charging:         // 2
               labelBatteryState.Text = "wird geladen";
               break;
            case BatteryState.NotCharging:      // 4
               labelBatteryState.Text = "wird NICHT geladen";
               break;
            case BatteryState.NotPresent:
               labelBatteryState.Text = "keine Batterie vorhanden";
               break;
            case BatteryState.Unknown:
               labelBatteryState.Text = "unbekannt";
               break;
         }

         switch (energySaverStatus) {
            case EnergySaverStatus.Off:
               labelEnergySaverStatus.Text = "aus";
               break;
            case EnergySaverStatus.On:
               labelEnergySaverStatus.Text = "ein";
               break;
            case EnergySaverStatus.Unknown:
               labelEnergySaverStatus.Text = "unbekannt";
               break;
         }

         DependencyService.Get<IBatteryInfo>().GetAndroidBatteryExtendedData(out int health, out float temperature, out string technology, out long chargetimeremaining);
         string sHealth;
         switch (health) {
            case 1: // BATTERY_HEALTH_UNKNOWN
            default:
               sHealth = "unbekannt";
               break;

            case 2: // BATTERY_HEALTH_GOOD
               sHealth = "gut";
               break;

            case 3: // BATTERY_HEALTH_OVERHEAT
               sHealth = "schlecht (überhitzt)";
               break;

            case 4: // BATTERY_HEALTH_DEAD
               sHealth = "schlecht";
               break;

            case 5: // BATTERY_HEALTH_OVER_VOLTAGE
               sHealth = "schlecht (überladen)";
               break;

            case 6: // BATTERY_HEALTH_UNSPECIFIED_FAILURE
               sHealth = "Fehler";
               break;

            case 7: // BATTERY_HEALTH_COLD
               sHealth = "gut (kalt)";
               break;

         }

         string sChargetimeremaining = "";
         if (chargetimeremaining > 0)
            sChargetimeremaining = ", verbleidende Ladezeit " + (chargetimeremaining / 60000).ToString() + " min";

         labelExtendedData.Text = string.Format("„Batteriegesundheit“ {0}, Temperatur {1}°C{2}, {3}",
                                                sHealth,
                                                temperature / 10,
                                                sChargetimeremaining,
                                                technology);
      }

      void showActualBatteryState() {
         showActualBatteryState(Battery.ChargeLevel,
                                Battery.PowerSource,
                                Battery.State,
                                Battery.EnergySaverStatus);
      }

      private void battery_EnergySaverStatusChanged(object sender, EnergySaverStatusChangedEventArgs e) {
         showActualBatteryState(Battery.ChargeLevel,
                                Battery.PowerSource,
                                Battery.State,
                                e.EnergySaverStatus);
      }

      private void battery_BatteryInfoChanged(object sender, BatteryInfoChangedEventArgs e) {
         showActualBatteryState(e.ChargeLevel,
                                e.PowerSource,
                                e.State,
                                Battery.EnergySaverStatus);
      }

      private void startbutton_Clicked(object sender, EventArgs e) {
         if (!DepHelper.ServiceIsActive) {
            DepHelper.ServiceIsActive = true;
            startbutton.IsEnabled = false;
            stopbutton.IsEnabled = true;
         }
      }

      private void stopbutton_Clicked(object sender, EventArgs e) {
         if (DepHelper.ServiceIsActive) {
            DepHelper.ServiceIsActive = false;
            startbutton.IsEnabled = true;
            stopbutton.IsEnabled = false;
         }
      }

      private int sliderValue(Slider slider) {
         return (int)Math.Round(slider.Value);
      }

      private void sliderMinPercent_ValueChanged(object sender, ValueChangedEventArgs e) {
         int value = isOnInit ?
                           sliderValue(sliderMinPercent) :
                           Math.Max(5, Math.Min(sliderValue(sliderMinPercent), sliderValue(sliderMaxPercent) - 1));     // 5% ...
         sliderMinPercent.Value = value;
         labelMinPercent.Text = string.Format("{0}%", value);
         DepHelper.MinPercent = value;
      }

      private void sliderMaxPercent_ValueChanged(object sender, ValueChangedEventArgs e) {
         int value = isOnInit ?
                           sliderValue(sliderMaxPercent) :
                           Math.Min(Math.Max(sliderValue(sliderMinPercent) + 1, sliderValue(sliderMaxPercent)), 100);   // ... 100%
         sliderMaxPercent.Value = value;
         labelMaxPercent.Text = string.Format("{0}%", value);
         DepHelper.MaxPercent = value;
      }

      private void pickerMinAlarmPeriod_PropertyChanged(object sender, PropertyChangedEventArgs e) {
         if (pickerMinAlarmPeriod.SelectedItem != null) {
            int v = getSeconds4Text(pickerMinAlarmPeriod.SelectedItem.ToString());
            if (v > 0 && v != DepHelper.MinAlarmPeriod)
               DepHelper.MinAlarmPeriod = v;
         }
      }

      private void pickerMaxAlarmPeriod_PropertyChanged(object sender, PropertyChangedEventArgs e) {
         if (pickerMaxAlarmPeriod.SelectedItem != null) {
            int v = getSeconds4Text(pickerMaxAlarmPeriod.SelectedItem.ToString());
            if (v > 0 && v != DepHelper.MaxAlarmPeriod)
               DepHelper.MaxAlarmPeriod = v;
         }
      }

      private void checkbox100PercentAlarm_CheckedChanged(object sender, CheckedChangedEventArgs e) {
         DepHelper.AlarmOn100Percent = checkbox100PercentAlarm.IsChecked;
      }

      private void buttonSoundMinAlarm_Clicked(object sender, EventArgs e) {
         soundChoose = SoundChoose.MinAlarm;
         chooseSound(soundMinAlarm, volumemin);
      }

      private void buttonSoundMaxAlarm_Clicked(object sender, EventArgs e) {
         soundChoose = SoundChoose.MaxAlarm;
         chooseSound(soundMaxAlarm, volumemax);
      }


      enum SoundChoose {
         unknown,
         MinAlarm,
         MaxAlarm,
      }


      SoundChoose soundChoose = SoundChoose.unknown;
      double volumemin = 1.0;
      double volumemax = 1.0;


      async void chooseSound(string orgsound, double orgvolume) {
         mreWait4LoadInternalAudiofiles.WaitOne();

         FSofTUtils.Xamarin.Page.SoundPickerPage soundPickPage = new FSofTUtils.Xamarin.Page.SoundPickerPage();
         soundPickPage.AddAdditionalAudiofiles(internalAudiofiles);
         soundPickPage.InitSound(orgsound, orgvolume);
         soundPickPage.CloseEvent += SoundPickPage_CloseEvent;

         await Navigation.PushAsync(soundPickPage);
      }

      private void SoundPickPage_CloseEvent(object sender, FSofTUtils.Xamarin.Page.SoundPickerPage.CloseEventArgs e) {
         if (e.NativeSoundData != null) {
            switch (soundChoose) {
               case SoundChoose.MinAlarm:
                  DepHelper.MinAlarm = soundMinAlarm = e.NativeSoundData.Data;
                  DepHelper.MinAlarmVolume = volumemin = e.Volume;
                  break;

               case SoundChoose.MaxAlarm:
                  DepHelper.MaxAlarm = soundMaxAlarm = e.NativeSoundData.Data;
                  DepHelper.MaxAlarmVolume = volumemax = e.Volume;
                  break;
            }
         }
      }

      private void resetbutton_Clicked(object sender, EventArgs e) {
         sliderMinPercent.Value = STDMINPERCENT;
         sliderMaxPercent.Value = STDMAXPERCENT;
         pickerMinAlarmPeriod.SelectedIndex = getBestIdx4Period(pickerMinAlarmPeriod, STDMINALARMPERIOD);
         pickerMaxAlarmPeriod.SelectedIndex = getBestIdx4Period(pickerMaxAlarmPeriod, STDMAXALARMPERIOD);

         soundMinAlarm = DepHelper.MinAlarm = internalAudiofiles[1];
         soundMaxAlarm = DepHelper.MaxAlarm = internalAudiofiles[0];

         volumemin = DepHelper.MinAlarmVolume = 1.0;
         volumemax = DepHelper.MaxAlarmVolume = 1.0;

      }

      /// <summary>
      /// interpretiert einen Text wie z.B. "123 s" oder "123min" als Wert in Sekunden
      /// </summary>
      /// <param name="text"></param>
      /// <returns></returns>
      int getSeconds4Text(string text) {
         int v = -1;
         text = text.Trim().ToLower();
         for (int i = 0; i < text.Length; i++) {
            if (!char.IsDigit(text[i]) ||
                i == text.Length - 1) {
               v = Convert.ToInt32(text.Substring(0, i));
               string t = text.Substring(i).Trim();
               if (t == "min")
                  v *= 60;
               else if (t == "min")
                  v *= 3600;
               break;
            }
         }
         return v;
      }

      private int getBestIdx4Period(Picker picker, int period) {
         int lastv = int.MinValue;
         for (int i = 0; i < picker.Items.Count; i++) {
            int v = getSeconds4Text(picker.Items[i].ToString());

            if (v == period)     // Treffer
               return i;

            if (lastv < period &&
                period < v) {
               if (Math.Abs(period - lastv) < (v - period))
                  return i - 1;
               else
                  return i;
            }

            if (i == pickerMaxAlarmPeriod.Items.Count - 1)
               return i;
         }
         return -1;
      }
   }
}
