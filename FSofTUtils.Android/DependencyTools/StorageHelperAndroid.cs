using Android.App;
using AndroidX.DocumentFile.Provider;
using FSofTUtils.Android.Storage;
using FSofTUtils.Xamarin.DependencyTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(FSofTUtils.Android.DependencyTools.DepTools))]
namespace FSofTUtils.Android.DependencyTools {

   /// <summary>
   /// StorageVolumes und StorageVolume erst ab API 24 (Nougat, 7.0)
   /// </summary>
   public class StorageHelperAndroid : StorageHelper {

      /* Für das Volume 0 werden immer die Standarddateifunktionen verwendet.
       * 
       * Für die weiteren Volumes (z.Z. max. 1, "external Storage") werden
       *    vor Android Q auch die Standarddateifunktionen
       *    bei Android Q je nach USESTD4Q entweder die Standarddateifunktionen oder das Storage Access Framework
       *    ab Android R die Funktionen des Storage Access Framework 
       * verwendet.
       * 
       * Es muss in AndroidManifest.xml
       * 	<application ... android:requestLegacyExternalStorage="true"></application>
       * aktiviert sein!
       * 
       */

      readonly Activity Activity;
      readonly Volume vol;
      readonly SafStorageHelper saf;
      /// <summary>
      /// Version VOR Android-R (11)
      /// </summary>
      readonly bool ispre_r = false;
      /// <summary>
      /// Version VOR Android-Q (10)
      /// </summary>
      readonly bool ispre_q = false;

      /*    Achtung
       *    
       *    Auf dem dem primary external storage (emuliert; interne SD-Karte) werden die Funktionen mit URI, DocumentsContract und DocumentFile NICHT benötigt 
       *    (und funktionieren auch nicht). Hier sollten alle normalen .Net-Funktionen funktionieren.
       *    
       *    Erst auf dem secondary external storage (echte SD-Karte und/oder USB-Stick) sind diese Funktionen nötig (Storage Access Framework). Dafür sind auch nochmal zusätzliche Rechte erforderlich.
       *    "PersistentPermissions".
       *    Das zentrale Element ist der DocumentsProvider über den der Zugriff auf die "Dokumente" erfolgt.
       */


      public StorageHelperAndroid(object activity) {
         ispre_q = global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.Q;  // 10
         ispre_r = global::Android.OS.Build.VERSION.SdkInt <= global::Android.OS.BuildVersionCodes.Q;

         if (activity is Activity) {
            Activity = activity as Activity;
            vol = new Volume();
            saf = new SafStorageHelper(Activity);

            RefreshVolumesAndSpecPaths();
         } else
            throw new Exception("'activity' must be a valid Activity.");
      }

      #region Android-Permissions

      /// <summary>
      /// versucht die Schreib- und Leserechte für das Volume zu setzen (API level 19 / Kitkat / 4.4) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="storagename">z.B. "19F4-0903"</param>
      /// <returns></returns>
      override public bool SetAndroidPersistentPermissions(string storagename) {
         return SetAndroidPersistentPermissions(idx4VolumeName(storagename));
      }

      /// <summary>
      /// versucht die Schreib- und Leserechte für das Volume zu setzen (API level 19 / Kitkat / 4.4) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="volidx">Volume-Index (min. 1)</param>
      /// <returns></returns>
      override public bool SetAndroidPersistentPermissions(int volidx) {
         return 0 <= volidx ?
                     saf.SetPersistentPermissions(VolumeNames[volidx], volidx) :
                     false;
      }

      /// <summary>
      /// gibt die persistenten Schreib- und Leserechte frei (API level 19 / Kitkat / 4.4) (für primäres externes Volume nicht geeignet!)
      /// <param name="storagename">z.B. "19F4-0903"</param>
      /// </summary>
      override public void ReleaseAndroidPersistentPermissions(string storagename) {
         ReleaseAndroidPersistentPermissions(idx4VolumeName(storagename));
      }

      /// <summary>
      /// gibt die persistenten Schreib- und Leserechte frei (API level 19 / Kitkat / 4.4) (für primäres externes Volume nicht geeignet!)
      /// <param name="volidx">Volume-Index (min. 1)</param>
      /// </summary>
      override public void ReleaseAndroidPersistentPermissions(int volidx) {
         if (0 <= volidx)
            saf.ReleasePersistentPermissions(VolumeNames[volidx], volidx);
      }

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen (API level 24 / Nougat / 7.0; deprecated in API level Q) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="storagename">z.B. "19F4-0903"</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      override public void Ask4AndroidPersistentPermisson(string storagename, int requestid = 12345) {
         Ask4AndroidPersistentPermisson(idx4VolumeName(storagename), requestid);
      }

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen (API level 24 / Nougat / 7.0; deprecated in API level Q) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="volidx">Volume-Index (min. 1)</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      override public void Ask4AndroidPersistentPermisson(int volidx, int requestid = 12345) {
         if (0 <= volidx)
            saf.Ask4PersistentPermisson(VolumeNames[volidx], volidx, requestid);
      }

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen (API level 24 / Nougat / 7.0; deprecated in API level Q) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="storagename">z.B. "19F4-0903"</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      override async public Task<bool> Ask4AndroidPersistentPermissonAndWait(string storagename, int requestid = 12345) {
         bool ok = await Ask4AndroidPersistentPermissonAndWait(vol.GetVolumeNo4Name(storagename), requestid);
         return ok;
      }

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen (API level 24 / Nougat / 7.0; deprecated in API level Q) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="volidx">Volume-Index (min. 1)</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      override async public Task<bool> Ask4AndroidPersistentPermissonAndWait(int volidx, int requestid = 12345) {
         //if (useStd(volidx))
         //   return true;
         bool ok = false;
         if (0 <= volidx && volidx < Volumes)
            ok = await saf.Ask4PersistentPermissonAndWait4Result(vol.StorageVolumeNames[volidx], volidx, requestid);
         return ok;
      }

      #endregion


      /// <summary>
      /// Volumenamen und spez. Pfadangaben aktualisieren
      /// </summary>
      override public void RefreshVolumesAndSpecPaths() {
         getSpecPaths(out List<string> datapaths, out List<string> cachepaths);

         for (int i = 0; i < datapaths.Count; i++)
            if (datapaths[i].EndsWith('/'))
               datapaths[i] = datapaths[i].Substring(0, datapaths[i].Length - 1);

         for (int i = 0; i < cachepaths.Count; i++)
            if (cachepaths[i].EndsWith('/'))
               cachepaths[i] = cachepaths[i].Substring(0, cachepaths[i].Length - 1);

         AppDataPaths = new List<string>(datapaths);
         AppTmpPaths = new List<string>(cachepaths);

         vol.RefreshVolumes();
         VolumePaths = new List<string>(vol.StorageVolumePaths);
         VolumeNames = new List<string>(vol.StorageVolumeNames);
      }

      /// <summary>
      /// liefert die akt. Daten für ein Volume (oder null)
      /// </summary>
      /// <param name="volidx">Volume-Index</param>
      /// <returns></returns>
      override public VolumeData GetVolumeData(int volidx) {
         return volidx < Volumes ?
                     vol.GetVolumeData(volidx) :
                     null;
      }

      /// <summary>
      /// liefert die akt. Daten für ein Volume (oder null)
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <returns></returns>
      override public VolumeData GetVolumeData(string storagename) {
         return vol.GetVolumeData(vol.GetVolumeNo4Name(storagename));
      }

      override public bool DirectoryExists(string fullpath) {
         return exists(fullpath, true, vol.GetVolumeNo4Path(fullpath));
      }

      override public bool FileExists(string fullpath) {
         return exists(fullpath, false, vol.GetVolumeNo4Path(fullpath));
      }

      /// <summary>
      /// liefert eine Liste <see cref="StorageItem"/>; das 1. Item gehört zu fullpath
      /// </summary>
      /// <param name="fullpath"></param>
      /// <returns></returns>
      override public List<StorageItem> StorageItemList(string fullpath) {
         List<StorageItem> lst = new List<StorageItem>();
         try {
            int v = vol.GetVolumeNo4Path(fullpath);
            //if (useStd(v)) {
            if (true) { // IMMER für die Listenabfrage (ist auch deutlich schneller!!!)

               if (exists(fullpath, true, 0)) { // ex. und ist Verzeichnis
                  if (Directory.Exists(fullpath))
                     lst.Add(new StdStorageItem(new DirectoryInfo(fullpath)));
                  else if (File.Exists(fullpath))
                     lst.Add(new StdStorageItem(new FileInfo(fullpath)));

                  foreach (string item in Directory.GetDirectories(fullpath)) {
                     lst.Add(new StdStorageItem(new DirectoryInfo(item)));
                  }
                  foreach (string item in Directory.GetFiles(fullpath)) {
                     lst.Add(new StdStorageItem(new FileInfo(item)));
                  }
               }

            } else {

               lst = saf.StorageItemList(vol.StorageVolumeNames[v],
                                         fullpath.Substring(vol.StorageVolumePaths[v].Length)); // Objektnamen ohne Pfad!

            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("StorageItemList(" + fullpath + ") Exception: " + ex.Message);
         }
         return lst;
      }

      override public List<string> Files(string fullpath) {
         return objectList(fullpath, false);
      }

      override public List<string> Directories(string fullpath) {
         return objectList(fullpath, true);
      }

      /// <summary>
      /// löscht das Verzeichnis
      /// </summary>
      /// <param name="fullpath">Pfad einschließlich Volumepfad</param>
      /// <returns>true falls erfolgreich</returns>
      /// <returns></returns>
      override public bool DeleteDirectory(string fullpath) {
         int v = vol.GetVolumeNo4Path(fullpath);
         if (exists(fullpath, true, v))
            try {
               if (useStd(v)) {

                  Directory.Delete(fullpath, true);
                  return true;

               } else {

                  return saf.Delete(vol.StorageVolumeNames[v],
                                    fullpath.Substring(vol.StorageVolumePaths[v].Length));

               }
            } catch (Exception ex) {
               System.Diagnostics.Debug.WriteLine("DeleteDirectory(" + fullpath + ") Exception: " + ex.Message);
            }
         return false;
      }

      /// <summary>
      /// erzeugt ein Verzeichnis
      /// </summary>
      /// <param name="fullpath">Pfad einschließlich Volumepfad</param>
      /// <returns>true falls erfolgreich oder schon ex.</returns>
      override public bool CreateDirectory(string fullpath) {
         int v = vol.GetVolumeNo4Path(fullpath);
         if (!exists(fullpath, true, v))
            try {
               if (useStd(v)) {

                  Directory.CreateDirectory(fullpath);
                  return true;

               } else {

                  return saf.CreateDirectory(vol.StorageVolumeNames[v], fullpath.Substring(vol.StorageVolumePaths[v].Length));

               }
            } catch (Exception ex) {
               System.Diagnostics.Debug.WriteLine("CreateDirectory(" + fullpath + ") Exception: " + ex.Message);
            }
         return false;
      }

      /// <summary>
      /// löscht die Datei
      /// </summary>
      /// <param name="fullpath">Pfad einschließlich Volumepfad</param>
      /// <returns>true falls erfolgreich</returns>
      override public bool DeleteFile(string fullpath) {
         int v = vol.GetVolumeNo4Path(fullpath);
         if (exists(fullpath, false, v))
            try {
               if (useStd(v)) {

                  File.Delete(fullpath);
                  return true;

               } else {

                  return saf.Delete(vol.StorageVolumeNames[v],
                                    fullpath.Substring(vol.StorageVolumePaths[v].Length));

               }
            } catch (Exception ex) {
               System.Diagnostics.Debug.WriteLine("DeleteFile(" + fullpath + ") Exception: " + ex.Message);
            }
         return false;
      }

      /// <summary>
      /// liefert einen Dateistream
      /// <para>Falls die Datei nicht ex. wird sie erzeugt. Auch das Verzeichnis wird bei Bedarf erzeugt.</para>
      /// <para>I.A. wird damit ein StreamWriter bzw. ein StreamReader erzeugt.</para>
      /// <para>ACHTUNG: Der Stream erfüllt nur primitivste Bedingungen. Er ist nicht "seekable", es wird keine Position oder Länge geliefert.
      /// Auch ein "rwt"-Stream scheint nur beschreibbar zu sein.</para>
      /// </summary>
      /// <param name="fullpath">Pfad einschließlich Volumepfad</param>
      /// <param name="mode"><para>"w" (write-only access and erasing whatever data is currently in the file),</para>
      ///                    <para>"wa" (write-only access to append to existing data),</para>
      ///                    <para>"rw" (read and write access), or</para>
      ///                    <para>"rwt" (read and write access that truncates any existing file)</para>
      ///                    <para>["r" (read-only)] ???,</para></param>
      /// <returns></returns>
      override public Stream OpenFile(string fullpath, string mode) {
         if (mode == "r" ||
             mode == "w" ||
             mode == "wa" ||
             mode == "rw" ||
             mode == "rwt") {

            int v = vol.GetVolumeNo4Path(fullpath);

            if (!exists(fullpath, true, v)) // es gibt kein gleichnamiges Verzeichnis
               try {
                  FileMode fmode = FileMode.OpenOrCreate;
                  FileAccess access = FileAccess.ReadWrite; // analog rw
                  if (mode == "rwt") {
                     fmode = FileMode.Create;
                  } else if (mode == "wa") {
                     fmode = FileMode.Append;
                     access = FileAccess.Write;
                  } else if (mode == "w") {
                     fmode = FileMode.Create;
                     access = FileAccess.Write;
                  } else if (mode == "r") {
                     fmode = FileMode.Open;
                     access = FileAccess.Read;
                  }

                  if (useStd(v)) {

                     string parentpath = Path.GetDirectoryName(fullpath);
                     if (!Directory.Exists(parentpath))
                        if (!CreateDirectory(parentpath))
                           return null;
                     return new FileStream(fullpath, fmode, access);

                  } else {

                     return saf.CreateOpenFile(vol.StorageVolumeNames[v],
                                               fullpath.Substring(vol.StorageVolumePaths[v].Length),
                                               mode);

                  }
               } catch (Exception ex) {
                  System.Diagnostics.Debug.WriteLine("OpenFile(" + fullpath + ", " + mode + ") Exception: " + ex.Message);
               }
         }
         return null;
      }

      /// <summary>
      /// verschiebt eine Datei (ev. als Kombination aus Kopie und Löschen)
      /// </summary>
      /// <param name="fullpathsrc">Quelldatei: Pfad einschließlich Volumepfad</param>
      /// <param name="fullpathdst">Zieldatei: Pfad einschließlich Volumepfad</param>
      /// <returns></returns>
      override public bool Move(string fullpathsrc, string fullpathdst) {
         int v1 = vol.GetVolumeNo4Path(fullpathsrc);
         int v2 = vol.GetVolumeNo4Path(fullpathdst);
         if (!exists(fullpathdst, true, v2) &&
             !exists(fullpathdst, false, v2))
            try {
               if (useStd(v1) &&
                   useStd(v2)) {
                  File.Move(fullpathsrc, fullpathdst);
                  return true;
               } else {
                  if (Path.GetDirectoryName(fullpathsrc) == Path.GetDirectoryName(fullpathdst))  // nur ein Rename
                     if (saf.Rename(vol.StorageVolumeNames[v1],
                                    fullpathsrc.Substring(vol.StorageVolumePaths[v1].Length),
                                    Path.GetFileName(fullpathdst)))
                        return true;

                  if (!useStd(v1) && !useStd(v2)) {
                     string filesrc = fullpathsrc.Substring(vol.StorageVolumePaths[v1].Length);
                     string pathdst = Path.GetDirectoryName(fullpathdst.Substring(vol.StorageVolumePaths[v2].Length));
                     string filetmp = Path.Combine(pathdst, Path.GetFileName(fullpathsrc));
                     if (saf.Move(vol.StorageVolumeNames[v1],
                                  filesrc,
                                  vol.StorageVolumeNames[v2],
                                  pathdst)) {
                        if (saf.Rename(vol.StorageVolumeNames[v2],
                                       filetmp,
                                       Path.GetFileName(fullpathdst)))
                           return true;
                     }
                  }

                  // Sollte Move auch über Volume-Grenzen hinweg fkt.???


                  if (copy(fullpathsrc, fullpathdst))
                     if (DeleteFile(fullpathsrc))
                        return true;
               }
            } catch (Exception ex) {
               System.Diagnostics.Debug.WriteLine("Move(" + fullpathsrc + ", " + fullpathdst + ") Exception: " + ex.Message);
            }
         return false;
      }

      /// <summary>
      /// erzeugt eine Dateikopie
      /// </summary>
      /// <param name="fullpathsrc">Quelldatei: Pfad einschließlich Volumepfad</param>
      /// <param name="fullpathdst">Zieldatei: Pfad einschließlich Volumepfad</param>
      /// <returns></returns>
      override public bool Copy(string fullpathsrc, string fullpathdst) {
         return copy(fullpathsrc, fullpathdst);
      }

      /// <summary>
      /// liefert die Länge einer Datei, Lese- und Schreibberechtigung und den Zeitpunkt der letzten Änderung
      /// </summary>
      /// <param name="fullpath"></param>
      /// <param name="pathisdir"></param>
      /// <param name="canread">wenn nicht secondary external storage, dann immer true (?)</param>
      /// <param name="canwrite"></param>
      /// <param name="lastmodified">letzte Änderung einer Datei bzw. Zeitpunkt der Erzeugung eines Verzeichnisses</param>
      /// <returns>Dateilänge</returns>
      override public long GetFileAttributes(string fullpath, bool pathisdir, out bool canread, out bool canwrite, out DateTime lastmodified) {
         int v = vol.GetVolumeNo4Path(fullpath);
         long len = -1;

         if (useStd(v)) {

            if (pathisdir) {
               DirectoryInfo di = new DirectoryInfo(fullpath);
               lastmodified = di.CreationTime;
               canread = canwrite = true;
               try {
                  di.GetFiles();
               } catch {
                  canread = canwrite = false;
               }
            } else {
               FileInfo fi = new FileInfo(fullpath);
               len = fi.Length;
               canwrite = !fi.IsReadOnly;
               lastmodified = fi.LastWriteTime;
               canread = false;
               using StreamReader sr = new StreamReader(fullpath); canread = true;
            }

         } else {

            DocumentFile doc = saf.GetExistingDocumentFile(vol.StorageVolumeNames[v],
                                                           fullpath.Substring(vol.StorageVolumePaths[v].Length));
            canread = doc.CanRead();
            canwrite = doc.CanWrite();
            lastmodified = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime().AddMilliseconds(doc.LastModified()); // Bezug auf den 1.1.1970
            len = doc.Length();

         }
         return len;
      }

      /// <summary>
      /// fkt. NICHT im secondary external storage (und bei Android 7 auch nicht im primary storage)
      /// </summary>
      /// <param name="fullpath"></param>
      /// <param name="canwrite"></param>
      /// <param name="lastmodified"></param>
      override public void SetFileAttributes(string fullpath, bool canwrite, DateTime lastmodified) {
         if (useStd(fullpath)) {
            FileInfo fi = new FileInfo(fullpath) {
               IsReadOnly = !canwrite
            };
            fi.LastWriteTime = fi.LastAccessTime = lastmodified;
         }
      }

      #region private Methoden

      /// <summary>
      /// Standardmethoden oder SAF-Methoden verwenden
      /// </summary>
      /// <param name="vol"></param>
      /// <returns></returns>
      bool useStd(int vol) {
         return vol < 1;
      }

      bool useStd(string fullpath) {
         return useStd(vol.GetVolumeNo4Path(fullpath));
      }

      /// <summary>
      /// liefert die Data- und Cache-Pfade (Index 0 immer mit den internen Pfaden)
      /// <para>Nicht für jedes Volume existieren diese Pfade!</para>
      /// </summary>
      /// <param name="DataPaths"></param>
      /// <param name="CachePaths"></param>
      void getSpecPaths(out List<string> DataPaths, out List<string> CachePaths) {
         DataPaths = new List<string>();
         CachePaths = new List<string>();

         DataPaths.Add(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
         Java.IO.File[] paths = Activity.ApplicationContext.GetExternalFilesDirs("");
         for (int i = 0; i < paths.Length; i++)
            DataPaths.Add(paths[i].CanonicalPath);

         CachePaths.Add(System.IO.Path.GetTempPath());
         paths = Activity.ApplicationContext.GetExternalCacheDirs();
         for (int i = 0; i < paths.Length; i++)
            CachePaths.Add(paths[i].CanonicalPath);
      }

      bool exists(string fullpath, bool isdir, int vol) {
         if (useStd(vol)) {

            if (isdir)
               return Directory.Exists(fullpath);
            else
               return File.Exists(fullpath);

         } else

            return saf.ObjectExists(this.vol.StorageVolumeNames[vol],
                                    fullpath.Substring(this.vol.StorageVolumePaths[vol].Length),
                                    isdir);
      }

      List<string> objectList(string fullpath, bool isdir) {
         List<string> lst = new List<string>();
         try {
            int v = vol.GetVolumeNo4Path(fullpath);
            if (exists(fullpath, true, v)) { // Verzeichnis ex.
               if (ispre_r || useStd(v)) {      // ev. fkt. das auch für >= R ???

                  if (isdir)
                     lst.AddRange(Directory.GetDirectories(fullpath));
                  else
                     lst.AddRange(Directory.GetFiles(fullpath));

               } else {

                  List<string> obj = saf.ObjectList(vol.StorageVolumeNames[v],
                                                    fullpath.Substring(vol.StorageVolumePaths[v].Length),
                                                    isdir); // Objektnamen ohne Pfad!
                  foreach (string name in obj) {
                     lst.Add(Path.Combine(fullpath, name));
                  }

               }
            }
            return lst;
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("ObjectList(" + fullpath + "," + isdir + ") Exception: " + ex.Message);
         }
         return lst;
      }

      bool copy(string fullpathsrc, string fullpathdst, int blksize = 512) {
         try {
            int v1 = vol.GetVolumeNo4Path(fullpathsrc);
            int v2 = vol.GetVolumeNo4Path(fullpathdst);
            if (v1 < 1 &&
                v2 < 1) {
               File.Copy(fullpathsrc, fullpathdst);
               return true;
            } else {

               //string filesrc = fullpathsrc.Substring(svh.StorageVolumePaths[v1].Length);
               //string pathdst = Path.GetDirectoryName(fullpathdst.Substring(svh.StorageVolumePaths[v2].Length));
               //string filetmp = Path.Combine(pathdst, Path.GetFileName(fullpathsrc));

               //if (seh.Copy(svh.StorageVolumeNames[v1],
               //             filesrc,
               //             svh.StorageVolumeNames[v2],
               //             pathdst))
               //   if (seh.Rename(svh.StorageVolumeNames[v2],
               //                  filetmp,
               //                  Path.GetFileName(fullpathdst)))
               //      return true;

               // letzter Versuch:
               using (Stream outp = OpenFile(fullpathdst, "w")) {
                  using (Stream inp = OpenFile(fullpathsrc, "r")) {
                     byte[] buffer = new byte[blksize * 1024];
                     int len;
                     while ((len = inp.Read(buffer, 0, buffer.Length)) > 0) {
                        outp.Write(buffer, 0, len);
                     }
                     //try {
                     //   inp.Seek(0, SeekOrigin.Begin);
                     //} catch (Exception ex) {
                     //   System.Diagnostics.Debug.WriteLine(ex.Message);     // "Specified method is not supported."
                     //}
                  }
               }
               return true;
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Copy(" + fullpathsrc + ", " + fullpathdst + ") Exception: " + ex.Message);
         }
         return false;
      }

      int idx4VolumeName(string storagename) {
         for (int i = 0; i < Volumes; i++)
            if (VolumeNames[i] == storagename)
               return i;
         return -1;
      }


      #endregion

      public override string ToString() {
         return string.Format("Volumes={0}: {1}", Volumes, string.Join(", ", VolumeNames));
      }

#if DEBUG

      public override object test1(object o1) {
         object result = null;

         Task.Run(async () => {
            Tests t = new Tests();
            result = await t.Start(Activity, saf, vol);
         });

         return result;
      }

#endif

   }

}