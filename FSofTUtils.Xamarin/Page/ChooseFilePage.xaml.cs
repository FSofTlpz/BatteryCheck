using FSofTUtils.Xamarin.Control;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static FSofTUtils.Xamarin.Control.ChooseFile;

namespace FSofTUtils.Xamarin.Page {
   [XamlCompilation(XamlCompilationOptions.Compile)]
   public partial class ChooseFilePage : ContentPage {

      public event EventHandler<ChoosePathAndFileEventArgs> ChooseFileReadyEvent;


      public object AndroidActivity { get; set; }

      /// <summary>
      /// letzter ausgewählter Pfad (aus dem App-Dictionary)
      /// <para>Wird i.A. nur von den aufrufenden Pages gesetzt und gelesen.</para>
      /// </summary>
      public static string LastChoosedPath {
         get => ChooseFile.LastChoosedPath;
         set => ChooseFile.LastChoosedPath = value;
      }

      /// <summary>
      /// Startpfad für die Auswahl (und Endpfad falls <see cref="Result"/>==true)
      /// </summary>
      public string Path {
         get;
         set;
      } = "";

      /// <summary>
      /// Startdateiname für die Auswahl (und Enddateiname falls <see cref="Result"/>==true)
      /// </summary>
      public string Filename {
         get;
         set;
      } = "";

      /// <summary>
      /// Können nur ex. Dateien ausgewählt werden?
      /// </summary>
      public bool OnlyExistingFile {
         get;
         set;
      } = true;

      /// <summary>
      /// Ergebnis der Auswahl
      /// </summary>
      public bool Result {
         get;
         protected set;
      }

      DependencyTools.StorageHelper storageHelper = null;

      List<string> VolumenPaths;

      bool hasChooseFileReadyEvent = false;


      public ChooseFilePage() {
         InitializeComponent();
      }

      protected override void OnAppearing() {
         base.OnAppearing();

         init(AndroidActivity);
      }

      protected override void OnDisappearing() {
         base.OnDisappearing();

         if (!hasChooseFileReadyEvent)
            ChooseFileReadyEvent?.Invoke(this, new ChoosePathAndFileEventArgs(false, Path, Filename));
      }

      async void init(object androidactivity) {
         try {

            Result = false;

            if (androidactivity == null)
               throw new Exception("AndroidActivity darf nicht null sein.");

            storageHelper = DependencyTools.DepToolsWrapper.GetStorageHelper(androidactivity);
            chooseFile.Activate(Path, Filename, OnlyExistingFile, storageHelper);

            VolumenPaths = storageHelper.VolumePaths;

            if (VolumenPaths.Count < 1)
               throw new Exception("Kein external Storage vorhanden.");

         } catch (Exception ex) {
            await Helper.MessageBox(this, "Fehler", ex.Message);
         }
      }

      private void ctrl_ChooseFileReadyEvent(object sender, ChoosePathAndFileEventArgs e) {
         if (e.OK) {
            Path = e.Path;
            Filename = e.Filename;
            Result = e.OK;
         }
         hasChooseFileReadyEvent = true;
         Navigation.PopAsync();
         ChooseFileReadyEvent?.Invoke(this, new ChoosePathAndFileEventArgs(e.OK, e.Path, e.Filename));
      }
   }
}