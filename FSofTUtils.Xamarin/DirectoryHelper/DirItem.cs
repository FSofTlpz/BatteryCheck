using System;
using System.IO;

namespace FSofTUtils.Xamarin.DirectoryHelper {
   /// <summary>
   /// Daten eines einzelnen Verzeichniseintrages
   /// </summary>
   class DirItem {

      /// <summary>
      /// abs. Pfad
      /// </summary>
      public string FullName { get; private set; }

      /// <summary>
      /// Name ohne Pfad
      /// </summary>
      public string Name {
         get {
            return string.IsNullOrEmpty(FullName) ? "" : IsRoot ? FullName : Path.GetFileName(FullName);
         }
      }

      /// <summary>
      /// Verzeichnis oder Datei
      /// </summary>
      public bool IsDirectory { get; private set; }

      /// <summary>
      /// Das Element ist ein Rootverzeichnis.
      /// </summary>
      public bool IsRoot { get; private set; }

      /// <summary>
      /// Die zugehörige Root ist entfernbar (unmount; i.A. die SD-Karte).
      /// </summary>
      public bool RootIsRemovable { get; private set; }

      /// <summary>
      /// Die zugehörige Root ist emuliert (i.A. das interne "externe" Verzeichnis)
      /// </summary>
      public bool RootIsEmulated { get; private set; }

      /// <summary>
      /// Dateigröße in Byte
      /// </summary>
      public long Filesize { get; private set; }

      /// <summary>
      /// Unterverzeichnisse im Verzeichnis
      /// </summary>
      public int Subdirs { get; private set; }

      /// <summary>
      /// Dateien im Verzeichnis
      /// </summary>
      public int Files { get; private set; }

      /// <summary>
      /// Größe eines Volumes in Byte
      /// </summary>
      public long Totalsize { get; private set; }

      /// <summary>
      /// verfügbarer Platz eines Volumes in Byte
      /// </summary>
      public long Availablesize { get; private set; }

      /// <summary>
      /// intern erzeugter Info-Text
      /// </summary>
      public string InfoText {
         get {
            if (IsRoot) {
               return string.Format("Root: {0} entfernbar, {1} emuliert, {2} GByte gesamt, {3} GByte frei, {4} Verzeichnis{5}, {6} Datei{7}",
                                    RootIsRemovable ? "" : "nicht",
                                    RootIsEmulated ? "" : "nicht",
                                    Math.Round(Totalsize / (1024.0 * 1024.0 * 1024.0), 1),
                                    Math.Round(Availablesize / (1024.0 * 1024.0 * 1024.0), 1),
                                    Subdirs,
                                    Subdirs != 1 ? "se" : "",
                                    Files,
                                    Files != 1 ? "en" : "");
            } else if (IsDirectory) {
               return string.Format("{0} Verzeichnis{1}, {2} Datei{3}, {4}",
                                    Subdirs,
                                    Subdirs != 1 ? "se" : "",
                                    Files,
                                    Files != 1 ? "en" : "",
                                    Timestamp.ToString("G"));
            }
            return string.Format("{0} Byte, {1} kByte, {2} MByte, {3}",
                                 Filesize,
                                 Math.Round(Filesize / 1024.0, 1),
                                 Math.Round(Filesize / (1024.0 * 1024.0), 1),
                                 Timestamp.ToString("G"));
         }
      }

      /// <summary>
      /// Zeitpunkt des Erzeugens (Verzeichnis) oder der letzten Veränderung (Datei)
      /// </summary>
      public DateTime Timestamp { get; private set; }


      /// <summary>
      /// für Verzeichnis
      /// </summary>
      /// <param name="name"></param>
      /// <param name="isremovable"></param>
      /// <param name="isemulated"></param>
      /// <param name="subdirs"></param>
      /// <param name="files"></param>
      /// <param name="dt"></param>
      public DirItem(string name, bool isremovable, bool isemulated, int subdirs, int files, DateTime dt) {
         FullName = name;
         IsDirectory = true;
         IsRoot = false;
         RootIsRemovable = isremovable;
         RootIsEmulated = isemulated;
         Filesize = 0;
         Subdirs = subdirs;
         Files = files;
         Totalsize = 0;
         Availablesize = 0;
         Timestamp = dt;
      }

      /// <summary>
      /// für Datei
      /// </summary>
      /// <param name="name"></param>
      /// <param name="isremovable"></param>
      /// <param name="isemulated"></param>
      /// <param name="size"></param>
      /// <param name="dt"></param>
      public DirItem(string name, bool isremovable, bool isemulated, long size, DateTime dt) {
         FullName = name;
         IsDirectory = false;
         IsRoot = false;
         RootIsRemovable = isremovable;
         RootIsEmulated = isemulated;
         Filesize = size;
         Subdirs = 0;
         Files = 0;
         Totalsize = 0;
         Availablesize = 0;
         Timestamp = dt;
      }

      /// <summary>
      /// für Root
      /// </summary>
      /// <param name="name"></param>
      /// <param name="isremovable"></param>
      /// <param name="isemulated"></param>
      /// <param name="total"></param>
      /// <param name="available"></param>
      /// <param name="subdirs"></param>
      /// <param name="files"></param>
      /// <param name="dt"></param>
      public DirItem(string name, bool isremovable, bool isemulated, long total, long available, int subdirs, int files, DateTime dt) {
         FullName = name;
         IsDirectory = true;
         IsRoot = true;
         RootIsRemovable = isremovable;
         RootIsEmulated = isemulated;
         Filesize = 0;
         Subdirs = subdirs;
         Files = files;
         Totalsize = total;
         Availablesize = available;
         Timestamp = dt;
      }

      public DirItem(DirItem item) {
         FullName = item.FullName;
         IsDirectory = item.IsDirectory;
         IsRoot = item.IsRoot;
         RootIsRemovable = item.RootIsRemovable;
         RootIsEmulated = item.RootIsEmulated;
         Filesize = item.Filesize;
         Subdirs = item.Subdirs;
         Files = item.Files;
         Totalsize = item.Totalsize;
         Availablesize = item.Availablesize;
         Timestamp = item.Timestamp;
      }


      public override string ToString() {
         return string.Format("{0}: {1}", IsDirectory ? "Dir" : "File", FullName);
      }

   }
}
