using FSofTUtils.Xamarin.DependencyTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FSofTUtils.Xamarin.Control {
   [XamlCompilation(XamlCompilationOptions.Compile)]
   public partial class SoundPicker : ContentView {

      public event EventHandler<EventArgs> CloseEvent;

      #region Binding-Var ItemColor

      public static readonly BindableProperty ItemColorProperty = BindableProperty.Create(
         nameof(ItemColor),
         typeof(Color),
         typeof(SoundPicker),
         Color.LightBlue);

      /// <summary>
      /// Farbe der Items
      /// </summary>
      public Color ItemColor {
         get => (Color)GetValue(ItemColorProperty);
         set => SetValue(ItemColorProperty, value);
      }

      #endregion

      #region Binding-Var SelectedItemColor

      public static readonly BindableProperty SelectedItemColorProperty = BindableProperty.Create(
         nameof(SelectedItemColor),
         typeof(Color),
         typeof(SoundPicker),
         Color.Blue);

      /// <summary>
      /// Farbe des ausgewählten Items
      /// </summary>
      public Color SelectedItemColor {
         get => (Color)GetValue(SelectedItemColorProperty);
         set => SetValue(SelectedItemColorProperty, value);
      }

      #endregion

      #region Binding-Var GadgetsColor

      public static readonly BindableProperty GadgetsColorProperty = BindableProperty.Create(
         nameof(GadgetsColor),
         typeof(Color),
         typeof(SoundPicker),
         Color.Blue);

      /// <summary>
      /// Farbe der Buttons/Regler für Dateiauswahl und Lautstärke
      /// </summary>
      public Color GadgetsColor {
         get => (Color)GetValue(GadgetsColorProperty);
         set => SetValue(GadgetsColorProperty, value);
      }

      #endregion

      #region Binding-Var VolumeSliderIsVisible

      public static readonly BindableProperty VolumeSliderIsVisibleProperty = BindableProperty.Create(
         nameof(VolumeSliderIsVisible),
         typeof(bool),
         typeof(SoundPicker),
         true);

      /// <summary>
      /// Ist eine Lautstärkeauswahl möglich?
      /// </summary>
      public bool VolumeSliderIsVisible {
         get => (bool)GetValue(VolumeSliderIsVisibleProperty);
         set {
            if (VolumeSliderIsVisible != value) {
               SetValue(VolumeSliderIsVisibleProperty, value);
               frameAppendFile.IsVisible = value;
            }
         }
      }

      #endregion

      #region Binding-Var FilePickerIsVisible

      public static readonly BindableProperty FilePickerIsVisibleProperty = BindableProperty.Create(
         nameof(FilePickerIsVisible),
         typeof(bool),
         typeof(SoundPicker),
         true);

      /// <summary>
      /// Ist eine Dateiauswahl möglich?
      /// </summary>
      public bool FilePickerIsVisible {
         get => (bool)GetValue(FilePickerIsVisibleProperty);
         set {
            if (FilePickerIsVisible != value) {
               SetValue(FilePickerIsVisibleProperty, value);
               frameAppendFile.IsVisible = value;
            }
         }
      }

      #endregion


      public class SoundData : NativeSoundData, INotifyPropertyChanged {

         public event PropertyChangedEventHandler PropertyChanged;


         static float _volume = 1F;

         static public float Volume {
            get {
               return _volume;
            }
            set {
               if (_volume != value)
                  _volume = value;
            }
         }

         bool _active;

         public bool Active {
            get {
               return _active;
            }
            set {
               if (_active != value) {
                  _active = value;
                  PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Active)));
                  playSound(_active);
               }
            }
         }


         public SoundData(NativeSoundData sd) : base(sd) {
            Active = false;
         }

         protected void playSound(bool start) {
            if (start) {
               DependencyService.Get<INativeSoundPicker>().PlayExclusiveNativeSound(this, Volume, true);
            } else {
               DependencyService.Get<INativeSoundPicker>().StopExclusiveNativeSound();
            }
         }

      }


      public ObservableCollection<SoundData> SoundDataList {
         get;
         private set;
      }


      /// <summary>
      /// liefert den akt. markierten Sound (oder null)
      /// </summary>
      public SoundData SelectedSound {
         get {
            if (myCollectionView.SelectedItem != null)
               return myCollectionView.SelectedItem as SoundData;
            return null;
         }
      }

      public int SelectedIndex {
         get {
            if (myCollectionView.SelectedItem != null)
               return SoundDataList.IndexOf(myCollectionView.SelectedItem as SoundData);
            return -1;
         }
         set {
            if (0 <= value && value < SoundDataList.Count)
               myCollectionView.SelectedItem = SoundDataList[value];
         }
      }

      /// <summary>
      /// Auswahl erfolgt?
      /// </summary>
      public NativeSoundData Result {
         get;
         protected set;
      }

      public double Volume {
         get => sliderVolume.Value;
         set {
            sliderVolume.Value = Math.Max(sliderVolume.Minimum, Math.Min(value, sliderVolume.Maximum));
         }
      }

      List<NativeSoundData> nativesounddata = null;


      public SoundPicker() {
         InitializeComponent();

         SoundDataList = new ObservableCollection<SoundData>(new List<SoundData>());
         BindingContext = this;

         myCollectionView.RemainingItemsThreshold = 5;         // "Nachfüllportionen" für Daten
         myCollectionView.RemainingItemsThresholdReached += MyCollectionView_RemainingItemsThresholdReached;

         ReloadData();
      }

      public void ReloadData() {
         nativesounddata = DependencyService.Get<INativeSoundPicker>().GetNativeSoundData(true, false, true, false, false);
         try {
            nativesounddata.AddRange(DependencyService.Get<INativeSoundPicker>().GetNativeSoundData(false, false, true, false, false));
         } catch (Exception ex) {

         }
         nativesounddata.Sort();

         for (int i = nativesounddata.Count - 1; i >= 0; i--)
            if (!nativesounddata[i].FileExists)
               nativesounddata.RemoveAt(i);

         SoundDataList.Clear();

         fillDataToCollectionView(); // Erst-Befüllung

         if (SoundDataList.Count > 0) {
            myCollectionView.SelectedItem = SoundDataList[0];
            SoundDataList[0].Active = false;
         }

         sliderVolume.Value = SoundData.Volume;
      }

      void fillDataToCollectionView() {
         if (nativesounddata.Count > 0)
            for (int i = 0; 0 < nativesounddata.Count && i < myCollectionView.RemainingItemsThreshold; i++) {
               addNativeSoundData(nativesounddata[0]);
               nativesounddata.RemoveAt(0);
            }
      }

      /// <summary>
      /// Retrieve more data here and add it to the CollectionView's ItemsSource collection.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void MyCollectionView_RemainingItemsThresholdReached(object sender, EventArgs e) {
         fillDataToCollectionView(); // Nach-Befüllung
      }

      private void sd_PropertyChanged(object sender, PropertyChangedEventArgs e) {
         if (e.PropertyName == nameof(SoundData.Active)) {
            SoundData sd = sender as SoundData;
            if (sd.Active) {  // dann diesen auch als SelectedSound
               if (!SelectedSound.Equals(sd)) {
                  SelectedIndex = SoundDataList.IndexOf(sd);
               }
            }
         }
      }

      /// <summary>
      /// liefert den (ersten) Index für diese Datei (oder -1)
      /// </summary>
      /// <param name="file"></param>
      /// <returns></returns>
      public int GetIndex4File(string file) {
         for (int i = 0; i < SoundDataList.Count; i++)
            if (SoundDataList[i].Data == file)
               return i;
         return -1;
      }

      /// <summary>
      /// liefert den (ersten) Index für diesen Titel (oder -1)
      /// </summary>
      /// <param name="title"></param>
      /// <returns></returns>
      public int GetIndex4Title(string title) {
         for (int i = 0; i < SoundDataList.Count; i++)
            if (SoundDataList[i].Title == title)
               return i;
         return -1;
      }

      /// <summary>
      /// scrollt die Liste so, dass das Item mit dem Index sichtbar ist
      /// </summary>
      /// <param name="idx"></param>
      public void ScrollTo(int idx, bool select) {
         if (0 <= idx && idx < SoundDataList.Count) {
            // Das ScrollTo() hat sich als "sehr sensibel" erwiesen. Es fkt. nur mit diesen Paras und wenn die Parent-Page min. im OnAppearing() ist!
            myCollectionView.ScrollTo(idx,
                                      -1,
                                      ScrollToPosition.MakeVisible,
                                      false);
            if (select)
               SelectedIndex = idx;
         }
      }

      /// <summary>
      /// hängt eine (Audio-)Datei an die Liste an
      /// </summary>
      /// <param name="fullfilename"></param>
      public void AppendFile(string fullfilename) {
         addNativeSoundData(new NativeSoundData(false,
                                                "",
                                                Path.GetFileNameWithoutExtension(fullfilename),
                                                fullfilename));
      }

      void addNativeSoundData(NativeSoundData nsd) {
         SoundData sd = new SoundData(nsd);
         sd.PropertyChanged += sd_PropertyChanged; // damit auf geänderte Props auch reagiert werden kann
         SoundDataList.Add(sd);
      }

      private void cancel_Tapped(object sender, EventArgs e) {
         SelectedSound.Active = false;
         Result = null;
         CloseEvent?.Invoke(this, new EventArgs());
      }

      private void ok_Tapped(object sender, EventArgs e) {
         SelectedSound.Active = false;
         Result = SelectedSound;
         CloseEvent?.Invoke(this, new EventArgs());
      }

      private void myCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
         if (e.PreviousSelection != null && e.PreviousSelection.Count == 1) {
            SoundData sd = e.PreviousSelection[0] as SoundData;
            sd.Active = false;
         }

         if (e.CurrentSelection != null && e.CurrentSelection.Count == 1) {
            SoundData sd = e.CurrentSelection[0] as SoundData;
            //sd.Active = true;     // NICHT automatisch einschalten (Nervfaktor)
         }

      }

      private void sliderVolume_ValueChanged(object sender, ValueChangedEventArgs e) {
         double volume = (sender as Slider).Value;
         SoundData.Volume = (float)volume;

         SoundData sd = SelectedSound;
         if (sd != null && sd.Active) {
            sd.Active = false;
            sd.Active = true;  // Neustart mit akt. Volume
         }
      }

      private async void appendFile_Tapped(object sender, EventArgs e) {
         string filename = await pickAudioFile();
         if (!string.IsNullOrEmpty(filename)) {
            AppendFile(filename);
            ScrollTo(SoundDataList.Count - 1, true);
         }
      }

      /// <summary>
      /// Audio-Dateiauswahl mit dem Xamarin.Essentials.FilePicker
      /// </summary>
      /// <returns></returns>
      async Task<string> pickAudioFile() {
         string filename = null;
         try {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> {
                 { DevicePlatform.Android, new[] { "audio/*" } },
                 /* 
                  On Android and iOS the files not matching this list is only displayed grayed out.
                  When the array is null or empty, all file types can be selected while picking.
                  The contents of this array is platform specific; every platform has its own way to specify the file types.
                  On Android you can specify one or more MIME types, e.g. "image/png"; also wild card characters can be used, e.g. "image/*".
                  On iOS you can specify UTType constants, e.g. UTType.Image.
                  On UWP, specify a list of extensions, like this: ".jpg", ".png".
                  */
                 //{ DevicePlatform.iOS, new[] { "public.my.comic.extension" } }, // or general UTType values


               /*
                * /frameworks/base/media/java/android/media/MediaFile.java
                * 
                      static {
                          addFileType("MP3", FILE_TYPE_MP3, "audio/mpeg", MtpConstants.FORMAT_MP3, true);
                          addFileType("MPGA", FILE_TYPE_MP3, "audio/mpeg", MtpConstants.FORMAT_MP3, false);
                          addFileType("M4A", FILE_TYPE_M4A, "audio/mp4", MtpConstants.FORMAT_MPEG, false);
                          addFileType("WAV", FILE_TYPE_WAV, "audio/x-wav", MtpConstants.FORMAT_WAV, true);
                          addFileType("AMR", FILE_TYPE_AMR, "audio/amr");
                          addFileType("AWB", FILE_TYPE_AWB, "audio/amr-wb");
                          if (isWMAEnabled()) {
                              addFileType("WMA", FILE_TYPE_WMA, "audio/x-ms-wma", MtpConstants.FORMAT_WMA, true);
                          }
                          addFileType("OGG", FILE_TYPE_OGG, "audio/ogg", MtpConstants.FORMAT_OGG, false);
                          addFileType("OGG", FILE_TYPE_OGG, "application/ogg", MtpConstants.FORMAT_OGG, true);
                          addFileType("OGA", FILE_TYPE_OGG, "application/ogg", MtpConstants.FORMAT_OGG, false);
                          addFileType("AAC", FILE_TYPE_AAC, "audio/aac", MtpConstants.FORMAT_AAC, true);
                          addFileType("AAC", FILE_TYPE_AAC, "audio/aac-adts", MtpConstants.FORMAT_AAC, false);
                          addFileType("MKA", FILE_TYPE_MKA, "audio/x-matroska");

                          addFileType("MID", FILE_TYPE_MID, "audio/midi");
                          addFileType("MIDI", FILE_TYPE_MID, "audio/midi");
                          addFileType("XMF", FILE_TYPE_MID, "audio/midi");
                          addFileType("RTTTL", FILE_TYPE_MID, "audio/midi");
                          addFileType("SMF", FILE_TYPE_SMF, "audio/sp-midi");
                          addFileType("IMY", FILE_TYPE_IMY, "audio/imelody");
                          addFileType("RTX", FILE_TYPE_MID, "audio/midi");
                          addFileType("OTA", FILE_TYPE_MID, "audio/midi");
                          addFileType("MXMF", FILE_TYPE_MID, "audio/midi");

                          addFileType("MPEG", FILE_TYPE_MP4, "video/mpeg", MtpConstants.FORMAT_MPEG, true);
                          addFileType("MPG", FILE_TYPE_MP4, "video/mpeg", MtpConstants.FORMAT_MPEG, false);
                          addFileType("MP4", FILE_TYPE_MP4, "video/mp4", MtpConstants.FORMAT_MPEG, false);
                          addFileType("M4V", FILE_TYPE_M4V, "video/mp4", MtpConstants.FORMAT_MPEG, false);
                          addFileType("MOV", FILE_TYPE_QT, "video/quicktime", MtpConstants.FORMAT_MPEG, false);

                          addFileType("3GP", FILE_TYPE_3GPP, "video/3gpp",  MtpConstants.FORMAT_3GP_CONTAINER, true);
                          addFileType("3GPP", FILE_TYPE_3GPP, "video/3gpp", MtpConstants.FORMAT_3GP_CONTAINER, false);
                          addFileType("3G2", FILE_TYPE_3GPP2, "video/3gpp2", MtpConstants.FORMAT_3GP_CONTAINER, false);
                          addFileType("3GPP2", FILE_TYPE_3GPP2, "video/3gpp2", MtpConstants.FORMAT_3GP_CONTAINER, false);
                          addFileType("MKV", FILE_TYPE_MKV, "video/x-matroska");
                          addFileType("WEBM", FILE_TYPE_WEBM, "video/webm");
                          addFileType("TS", FILE_TYPE_MP2TS, "video/mp2ts");
                          addFileType("AVI", FILE_TYPE_AVI, "video/avi");

                          if (isWMVEnabled()) {
                              addFileType("WMV", FILE_TYPE_WMV, "video/x-ms-wmv", MtpConstants.FORMAT_WMV, true);
                              addFileType("ASF", FILE_TYPE_ASF, "video/x-ms-asf");
                          }

                          addFileType("JPG", FILE_TYPE_JPEG, "image/jpeg", MtpConstants.FORMAT_EXIF_JPEG, true);
                          addFileType("JPEG", FILE_TYPE_JPEG, "image/jpeg", MtpConstants.FORMAT_EXIF_JPEG, false);
                          addFileType("GIF", FILE_TYPE_GIF, "image/gif", MtpConstants.FORMAT_GIF, true);
                          addFileType("PNG", FILE_TYPE_PNG, "image/png", MtpConstants.FORMAT_PNG, true);
                          addFileType("BMP", FILE_TYPE_BMP, "image/x-ms-bmp", MtpConstants.FORMAT_BMP, true);
                          addFileType("WBMP", FILE_TYPE_WBMP, "image/vnd.wap.wbmp", MtpConstants.FORMAT_DEFINED, false);
                          addFileType("WEBP", FILE_TYPE_WEBP, "image/webp", MtpConstants.FORMAT_DEFINED, false);
                          addFileType("HEIC", FILE_TYPE_HEIF, "image/heif", MtpConstants.FORMAT_HEIF, true);
                          addFileType("HEIF", FILE_TYPE_HEIF, "image/heif", MtpConstants.FORMAT_HEIF, false);

                          addFileType("DNG", FILE_TYPE_DNG, "image/x-adobe-dng", MtpConstants.FORMAT_DNG, true);
                          addFileType("CR2", FILE_TYPE_CR2, "image/x-canon-cr2", MtpConstants.FORMAT_TIFF, false);
                          addFileType("NEF", FILE_TYPE_NEF, "image/x-nikon-nef", MtpConstants.FORMAT_TIFF_EP, false);
                          addFileType("NRW", FILE_TYPE_NRW, "image/x-nikon-nrw", MtpConstants.FORMAT_TIFF, false);
                          addFileType("ARW", FILE_TYPE_ARW, "image/x-sony-arw", MtpConstants.FORMAT_TIFF, false);
                          addFileType("RW2", FILE_TYPE_RW2, "image/x-panasonic-rw2", MtpConstants.FORMAT_TIFF, false);
                          addFileType("ORF", FILE_TYPE_ORF, "image/x-olympus-orf", MtpConstants.FORMAT_TIFF, false);
                          addFileType("RAF", FILE_TYPE_RAF, "image/x-fuji-raf", MtpConstants.FORMAT_DEFINED, false);
                          addFileType("PEF", FILE_TYPE_PEF, "image/x-pentax-pef", MtpConstants.FORMAT_TIFF, false);
                          addFileType("SRW", FILE_TYPE_SRW, "image/x-samsung-srw", MtpConstants.FORMAT_TIFF, false);

                          addFileType("M3U", FILE_TYPE_M3U, "audio/x-mpegurl", MtpConstants.FORMAT_M3U_PLAYLIST, true);
                          addFileType("M3U", FILE_TYPE_M3U, "application/x-mpegurl", MtpConstants.FORMAT_M3U_PLAYLIST, false);
                          addFileType("PLS", FILE_TYPE_PLS, "audio/x-scpls", MtpConstants.FORMAT_PLS_PLAYLIST, true);
                          addFileType("WPL", FILE_TYPE_WPL, "application/vnd.ms-wpl", MtpConstants.FORMAT_WPL_PLAYLIST, true);
                          addFileType("M3U8", FILE_TYPE_HTTPLIVE, "application/vnd.apple.mpegurl");
                          addFileType("M3U8", FILE_TYPE_HTTPLIVE, "audio/mpegurl");
                          addFileType("M3U8", FILE_TYPE_HTTPLIVE, "audio/x-mpegurl");

                          addFileType("FL", FILE_TYPE_FL, "application/x-android-drm-fl");

                          addFileType("TXT", FILE_TYPE_TEXT, "text/plain", MtpConstants.FORMAT_TEXT, true);
                          addFileType("HTM", FILE_TYPE_HTML, "text/html", MtpConstants.FORMAT_HTML, true);
                          addFileType("HTML", FILE_TYPE_HTML, "text/html", MtpConstants.FORMAT_HTML, false);
                          addFileType("PDF", FILE_TYPE_PDF, "application/pdf");
                          addFileType("DOC", FILE_TYPE_MS_WORD, "application/msword", MtpConstants.FORMAT_MS_WORD_DOCUMENT, true);
                          addFileType("XLS", FILE_TYPE_MS_EXCEL, "application/vnd.ms-excel", MtpConstants.FORMAT_MS_EXCEL_SPREADSHEET, true);
                          addFileType("PPT", FILE_TYPE_MS_POWERPOINT, "application/mspowerpoint", MtpConstants.FORMAT_MS_POWERPOINT_PRESENTATION, true);
                          addFileType("FLAC", FILE_TYPE_FLAC, "audio/flac", MtpConstants.FORMAT_FLAC, true);
                          addFileType("ZIP", FILE_TYPE_ZIP, "application/zip");
                          addFileType("MPG", FILE_TYPE_MP2PS, "video/mp2p");
                          addFileType("MPEG", FILE_TYPE_MP2PS, "video/mp2p");
                      }
                */
             });

            var opt = new PickOptions {
               PickerTitle = "Audiodatei auswählen",
               FileTypes = customFileType,
            };

            var result = await FilePicker.PickAsync(opt);
            if (result != null)
               filename = result.FullPath;
         } catch (Exception ex) {

         }
         return filename;
      }

   }
}