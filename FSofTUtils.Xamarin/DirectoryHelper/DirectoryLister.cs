using System;
using System.Collections.Generic;
using System.IO;
using FSofTUtils.Xamarin.DependencyTools;

namespace FSofTUtils.Xamarin.DirectoryHelper {
   /// <summary>
   /// zum Lesen der Daten von Verzeichnissen und Volumes
   /// </summary>
   class DirectoryLister {

      /// <summary>
      /// Liste aller Roots
      /// </summary>
      public List<StorageHelper.VolumeData> Root {
         get;
         private set;
      }

      /// <summary>
      /// Anzahl der Roots
      /// </summary>
      public int RootCount {
         get => Root.Count;
      }

      /// <summary>
      /// akt. Pfad
      /// </summary>
      public string ActualPath {
         get;
         private set;
      }

      /// <summary>
      /// aktueller (zuletzt) mit <see cref="Content(string)"/> eingelesener Verzeichnisinhalt (ev. auch leere Liste)
      /// </summary>
      public List<DirItem> ActualContent {
         get;
         private set;
      }

      /// <summary>
      /// Hilfsfunktionen für die Arbeit mit dem Speicher
      /// </summary>
      public StorageHelper StorageHelper {
         get;
         private set;
      }


      public DirectoryLister(StorageHelper sh) {
         ActualPath = "";
         Root = new List<StorageHelper.VolumeData>();
         StorageHelper = sh;
         ActualContent = new List<DirItem>();
      }

      #region public Methods

      /// <summary>
      /// fügt eine Root zur internen Liste hinzu
      /// </summary>
      /// <param name="root"></param>
      public void AddRoot(StorageHelper.VolumeData root) {
         if (root != null) {
            bool found = false;
            foreach (StorageHelper.VolumeData r in Root) {
               if (r.Path == root.Path) {
                  found = true;
                  break;
               }
            }
            if (!found)
               Root.Add(root);
         }
      }

      /// <summary>
      /// entfernt eine Root aus der internen Liste
      /// </summary>
      /// <param name="rootpath"></param>
      public void RemoveRoot(string rootpath) {
         for (int i = 0; i < Root.Count; i++) {
            if (Root[i].Path == rootpath) {
               Root.RemoveAt(i);
               break;
            }
         }
      }

      /// <summary>
      /// setzt <see cref="ActualContent"/> neu mit dem Inhalt des aktuellen Verzeichnis bzw. den Inhalt von path und liefert diese Liste
      /// </summary>
      /// <param name="path">wenn "", wird die Liste der Root-Pfade gebildet</param>
      /// <returns></returns>
      public List<DirItem> Content(string path = null) {
         if (path is null)
            path = ActualPath;

         List<DateTime> dttest = new List<DateTime>();

         ActualContent.Clear();

         dttest.Add(DateTime.Now);

         if (!string.IsNullOrEmpty(path) &&
             StorageHelper.DirectoryExists(path)) {   // ein ex. Pfad wird abgefragt

            dttest.Add(DateTime.Now);

            ActualPath = path;
            bool isremovable = rootIsRemovable(path);
            bool isemulated = rootIsEmulated(path);

            dttest.Add(DateTime.Now);

            List<StorageHelper.StorageItem> lst = StorageHelper.StorageItemList(path);
            ActualContent.Add(buildItem4Dir(lst, isremovable, isemulated));     // 285ms

            dttest.Add(DateTime.Now);

            lst.RemoveAt(0);  // Item für ".." entfernen
            lst.Sort();
            foreach (StorageHelper.StorageItem item in lst) {
               if (item.IsDirectory)
                  ActualContent.Add(buildItem4Dir(path, item.Name, isremovable, isemulated));
               else 
                  ActualContent.Add(new DirItem(item.Name,
                                                isremovable,
                                                isemulated,
                                                item.Length,
                                                item.LastModified));
            }

            dttest.Add(DateTime.Now);
            showDateTimeDiffs(dttest, ">>> Timediff (ms): ");

         } else { // ein nicht ex. Pfad wird abgefragt -> Liste der Volumes

            ActualPath = "";
            foreach (var root in Root) {     // ungültiger/unbekannter Pfad -> alle Roots auflisten
               DateTime dt = getDirectoryInfos(root.Path, out int subdirs, out int files);
               ActualContent.Add(new DirItem(root.Path,
                                             root.IsRemovable,
                                             root.IsEmulated,
                                             root.TotalBytes,
                                             root.AvailableBytes,
                                             subdirs,
                                             files,
                                             dt));
            }

         }
         return ActualContent;
      }

      /// <summary>
      /// der <see cref="ActualPath"/> wird bei ".." um eine Ebene verkürzt oder um subdir verlängert
      /// <para>Ein leeres subdir oder null setzt <see cref="ActualPath"/> auf "".</para>
      /// </summary>
      /// <param name="subdir"></param>
      /// <returns></returns>
      public bool Move(string subdir = null) {
         if (string.IsNullOrEmpty(subdir)) {    // dann keine Änderung

            ActualPath = "";
            return true;

         } else if (subdir != ".") { // bleibt nicht gleich

            string newpath;
            if (subdir == "..") {         // nach "oben"

               if (pathIsRoot(ActualPath)) {
                  ActualPath = "";
                  return true;
               } else {
                  ActualPath = Path.GetDirectoryName(ActualPath);
                  return StorageHelper.DirectoryExists(ActualPath);
               }

            } else {                      // nach "unten"

               newpath = Path.Combine(ActualPath, subdir);
               if (StorageHelper.DirectoryExists(newpath)) {
                  ActualPath = newpath;
                  return true;
               }

            }
         }
         return false;
      }

      #endregion

      #region private Methods

      /// <summary>
      /// ACHTUNG: rel. zeitintensiv, da für die Infos ein Auruf von <see cref="StorageHelper.StorageItemList(string)"/> erfolgt
      /// </summary>
      /// <param name="dirpath"></param>
      /// <param name="subdirs"></param>
      /// <param name="files"></param>
      /// <returns></returns>
      DateTime getDirectoryInfos(string dirpath, out int subdirs, out int files) {
         subdirs = 0;
         files = 0;
         DateTime dt = DateTime.MinValue;
         try {
            List<StorageHelper.StorageItem> lst = StorageHelper.StorageItemList(dirpath);
            files = getFileCount(lst);
            subdirs = getDirCount(lst);

            if (lst.Count > 0) {
               if (lst[0].IsFile)
                  files--;
               else if (lst[0].IsDirectory)
                  subdirs--;
               dt = lst[0].LastModified;
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("GetDirectoryInfos(" + dirpath + ", ...) Exception: " + ex.Message);
         }

         return dt;
      }


      int getFileCount(List<StorageHelper.StorageItem> sti) {
         int count = 0;
         foreach (var item in sti) {
            if (item.IsFile)
               count++;
         }
         return count;
      }

      int getDirCount(List<StorageHelper.StorageItem> sti) {
         int count = 0;
         foreach (var item in sti) {
            if (item.IsDirectory)
               count++;
         }
         return count;
      }

      DirItem buildItem4Dir(string path, string subdir, bool isremovable, bool isemulated) {
         DateTime dt = getDirectoryInfos(Path.Combine(path, subdir), out int subdirs, out int files);
         return new DirItem(subdir,
                            isremovable,
                            isemulated,
                            subdirs,
                            files,
                            dt);
      }

      /// <summary>
      /// nur für das erstes Element der Liste ("..")
      /// </summary>
      /// <param name="lst"></param>
      /// <param name="isremovable"></param>
      /// <param name="isemulated"></param>
      /// <returns></returns>
      DirItem buildItem4Dir(List<StorageHelper.StorageItem> lst, bool isremovable, bool isemulated) {
         if (lst.Count > 0) {
            StorageHelper.StorageItem parent = lst[0];

            int files = getFileCount(lst);
            int subdirs = getDirCount(lst);

            if (lst.Count > 0) {
               if (parent.IsFile)
                  files--;
               else if (parent.IsDirectory)
                  subdirs--;
            }

            return new DirItem("..",
                               isremovable,
                               isemulated,
                               subdirs,
                               files,
                               parent.LastModified);
         }
         return null;
      }


      /// <summary>
      /// liefert die Root des Pfades oder null
      /// </summary>
      /// <param name="path"></param>
      /// <returns></returns>
      StorageHelper.VolumeData getRoot(string path) {
         foreach (var root in Root) {
            if (root.Path.Length <= path.Length) {
               if (path.Substring(0, root.Path.Length) == root.Path)
                  return root;
            }
         }
         return null;
      }

      /// <summary>
      /// liefert den Root-Pfad des Pfades oder ""
      /// </summary>
      /// <param name="path"></param>
      /// <returns></returns>
      string getRootPath(string path) {
         StorageHelper.VolumeData root = getRoot(path);
         return root != null ?
                     root.Path :
                     "";
      }

      bool rootIsRemovable(string path) {
         StorageHelper.VolumeData root = getRoot(path);
         return root != null && root.IsRemovable;
      }

      bool rootIsEmulated(string path) {
         StorageHelper.VolumeData root = getRoot(path);
         return root != null && root.IsEmulated;
      }

      /// <summary>
      /// der Pfad ist ein root-Verzeichnis
      /// </summary>
      /// <param name="path"></param>
      /// <returns></returns>
      bool pathIsRoot(string path) {
         return getRootPath(path) == path;
      }

      /// <summary>
      /// Passt der Pfad zu (irgendeiner) Root ?
      /// </summary>
      /// <param name="path"></param>
      /// <returns></returns>
      bool isValidPath4Roots(string path) {
         return getRoot(path) != null;
      }

      #endregion

      void showDateTimeDiffs(IList<DateTime> dt, string starttxt) {
         System.Text.StringBuilder sb = new System.Text.StringBuilder(starttxt);
         for (int d = 1; d < dt.Count; d++) {
            if (d > 1)
               sb.Append("; ");
            sb.Append(dt[d].Subtract(dt[d - 1]).TotalMilliseconds);
         }
         System.Diagnostics.Debug.WriteLine(sb);
      }

      public override string ToString() {
         return string.Format("Count: {0}, ActualPath: {1}", RootCount, ActualPath);
      }

   }
}
