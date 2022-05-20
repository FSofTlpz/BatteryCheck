using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FSofTUtils.Xamarin.DependencyTools {

   /// <summary>
   /// die abstrakte Klasse liefert Hilfsfunktionen für Volumes und Dateioperationen
   /// </summary>
   abstract public class StorageHelper {

      /*
      Im internen Speicher und im primary external Storage können die normalen .Net-Funktionen verwendet werden.
      Für den primary external Storage muss lediglich das Recht
         Manifest.Permission.WriteExternalStorage
      gesetzt sein. Das kann über das Manifest oder zur Laufzeit erfolgen.

      Für den "secondary" external Storage, d.h. eine echte SD-Karte und/oder ein USB-Stick genügen diese Rechte nicht. Hier müssen zusätzliche
      Rechte als
         PersistentPermissions
      erteilt werden. Das geht ausschließlich zur Laufzeit mit der Bestätigung durch den Anwender.
      Außerdem steht hier eine zusätzliche Softwareschicht zwischen der App und den Dateisystemobjekten. Viele .Net-Funktionen wie z.B. das Ausflisten von
      Dateien und Verzeichnissen oder der Existenztest einer Datei oder eines Verzeichnisses funktionieren zwar, aber einige der wichtigsten nicht.
      Es gibt z.B. einen speziellen Mechanismus zum Öffnen/Erzeugen einer Datei, zum Löschen von Dateisystemobjekten usw.

      Mit dieser Hilfsklasse werden diese Funktionen so bereit gestellt, dass aus Sicht der App keine Unterscheidung des Speicherortes notwendig ist.

      */

      /// <summary>
      /// Status eines Volumes
      /// </summary>
      public enum VolumeState {
         /// <summary>
         /// Unknown storage state, such as when a path isn't backed by known storage media.
         /// </summary>
         MEDIA_UNKNOWN,
         /// <summary>
         /// Storage state if the media is not present. 
         /// </summary>
         MEDIA_REMOVED,
         /// <summary>
         /// Storage state if the media is present but not mounted. 
         /// </summary>
         MEDIA_UNMOUNTED,
         /// <summary>
         /// Storage state if the media is present and being disk-checked. 
         /// </summary>
         MEDIA_CHECKING,
         /// <summary>
         /// Storage state if the media is in the process of being ejected.
         /// </summary>
         MEDIA_EJECTING,
         /// <summary>
         /// Storage state if the media is present but is blank or is using an unsupported filesystem. 
         /// </summary>
         MEDIA_NOFS,
         /// <summary>
         /// Storage state if the media is present and mounted at its mount point with read/write access. 
         /// </summary>
         MEDIA_MOUNTED,
         /// <summary>
         /// Storage state if the media is present and mounted at its mount point with read-only access. 
         /// </summary>
         MEDIA_MOUNTED_READ_ONLY,
         /// <summary>
         /// Storage state if the media is present not mounted, and shared via USB mass storage.  
         /// </summary>
         MEDIA_SHARED,
         /// <summary>
         /// Storage state if the media was removed before it was unmounted. 
         /// </summary>
         MEDIA_BAD_REMOVAL,
         /// <summary>
         /// Storage state if the media is present but cannot be mounted. Typically this happens if the file system on the media is corrupted. 
         /// </summary>
         MEDIA_UNMOUNTABLE,
      }

      /// <summary>
      /// Daten eines Volumes
      /// </summary>
      public class VolumeData {
         /// <summary>
         /// Anzahl der vorhandenen Volumes
         /// </summary>
         public int Volumes;
         /// <summary>
         /// Index des abgefragten Volumes
         /// </summary>
         public int VolumeNo;
         /// <summary>
         /// Pfad zum Volume, z.B.: "/storage/emulated/0" und "/storage/19F4-0903"
         /// </summary>
         public string Path;
         /// <summary>
         /// Name des Volumes, z.B. "primary" oder "19F4-0903"
         /// </summary>
         public string Name;
         /// <summary>
         /// Beschreibung des Volumes
         /// </summary>
         public string Description;
         /// <summary>
         /// Gesamtspeicherplatz des Volumes
         /// </summary>
         public long TotalBytes;
         /// <summary>
         /// freier Speicherplatz des Volumes
         /// </summary>
         public long AvailableBytes;
         /// <summary>
         /// Ist das abgefragte Volume ein primäres Volume?
         /// </summary>
         public bool IsPrimary;
         /// <summary>
         /// Ist das abgefragte Volume entfernbar?
         /// </summary>
         public bool IsRemovable;
         /// <summary>
         /// Ist das abgefragte Volume nur emuliert?
         /// </summary>
         public bool IsEmulated;
         /// <summary>
         /// Status des Volumes
         /// </summary>
         public VolumeState State;

         public VolumeData() {
            Volumes = 0;
            VolumeNo = -1;
            Path = Name = Description = "";
            TotalBytes = AvailableBytes = 0;
            IsPrimary = IsRemovable = IsEmulated = false;
            State = VolumeState.MEDIA_UNKNOWN;
         }

         public VolumeData(VolumeData vd) {
            Volumes = vd.Volumes;
            VolumeNo = vd.VolumeNo;
            Path = vd.Path;
            Name = vd.Name;
            Description = vd.Description;
            TotalBytes = vd.TotalBytes;
            AvailableBytes = vd.AvailableBytes;
            IsPrimary = vd.IsPrimary;
            IsRemovable = vd.IsRemovable;
            IsEmulated = vd.IsEmulated;
            State = vd.State;
         }

         public override string ToString() {
            return string.Format("VolumeNo={0}, Name={1}, Description={2}, Path={3}", VolumeNo, Name, Description, Path);
         }
      }

      public class StorageItem : IComparable {
         public string Name { get; protected set; }
         public bool IsDirectory { get; protected set; }
         public bool IsFile { get; protected set; }
         public long Length { get; protected set; }
         public string MimeType { get; protected set; }
         public bool CanRead { get; protected set; }
         public bool CanWrite { get; protected set; }
         public DateTime LastModified { get; protected set; }
         public StorageItem() {
            Name = "";
            IsDirectory = false;
            IsFile = false;
            MimeType = "";
            CanRead = false;
            CanWrite = false;
            LastModified = DateTime.MinValue;
            Length = 0;
         }

         public int CompareTo(object obj) {
            if (obj is StorageItem) {
               // erst nach IsDirectory sortieren (IsDirectory zuerst) ...
               StorageItem sti = obj as StorageItem;
               if (IsDirectory != sti.IsDirectory)
                  return IsDirectory ? -1 : 1;
               // ... dann nach Namen sortieren
               return string.Compare(Name, sti.Name);
            }

            throw new ArgumentException();
         }
      }



      /// <summary>
      /// ext. StorageVolume-Pfade beim letzten <see cref="RefreshStorageVolumePathsAndNames"/>(), z.B. "/storage/emulated/0" oder "/storage/19F4-0903"
      /// </summary>
      public List<string> VolumePaths { get; protected set; }

      /// <summary>
      /// ext. StorageVolume-Namen beim letzten <see cref="RefreshStorageVolumePathsAndNames"/>(), z.B. "primary" oder "19F4-0903"
      /// </summary>
      public List<string> VolumeNames { get; protected set; }

      /// <summary>
      /// Anzahl der Volumes
      /// </summary>
      public int Volumes {
         get {
            return VolumePaths.Count;
         }
      }

      /// <summary>
      /// die "privaten" Verzeichnisse der App, z.B.: "/data/user/0/APKNAME/files" oder "/storage/emulated/0/Android/data/APKNAME/files" oder "/storage/19F4-0903/Android/data/APKNAME/files"
      /// <para>Bei Index 0 steht der "interne" Android-Pfad.</para>
      /// </summary>
      public List<string> AppDataPaths { get; protected set; }

      /// <summary>
      /// die "privaten" Verzeichnisse für temp. Daten der App z.B.: "/data/user/0/APKNAME/cache" oder "/storage/emulated/0/Android/data/APKNAME/cache" oder "/storage/19F4-0903/Android/data/APKNAME/cache"
      /// <para>Bei Index 0 steht der "interne" Android-Pfad.</para>
      /// </summary>
      public List<string> AppTmpPaths { get; protected set; }

      #region spez. für Android nötige Funktionen

      /// <summary>
      /// versucht die Schreib- und Leserechte für das Volume zu setzen (API level 19 / Kitkat / 4.4) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <returns></returns>
      abstract public bool SetAndroidPersistentPermissions(string storagename);

      /// <summary>
      /// versucht die Schreib- und Leserechte für das Volume zu setzen (API level 19 / Kitkat / 4.4) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="volidx">Volume-Index (min. 1)</param>
      /// <returns></returns>
      abstract public bool SetAndroidPersistentPermissions(int volidx);

      /// <summary>
      /// gibt die persistenten Schreib- und Leserechte frei (API level 19 / Kitkat / 4.4) (für primäres externes Volume nicht geeignet!)
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// </summary>
      abstract public void ReleaseAndroidPersistentPermissions(string storagename);

      /// <summary>
      /// gibt die persistenten Schreib- und Leserechte frei (API level 19 / Kitkat / 4.4) (für primäres externes Volume nicht geeignet!)
      /// <param name="volidx">Volume-Index (min. 1)</param>
      /// </summary>
      abstract public void ReleaseAndroidPersistentPermissions(int volidx);

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen (API level 24 / Nougat / 7.0; deprecated in API level Q) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="storagename">z.B. "19F4-0903"</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      abstract public void Ask4AndroidPersistentPermisson(string storagename, int requestid = 12345);

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen (API level 24 / Nougat / 7.0; deprecated in API level Q) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="volidx">Volume-Index (min. 1)</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      abstract public void Ask4AndroidPersistentPermisson(int volidx, int requestid = 12345);

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen (API level 24 / Nougat / 7.0; deprecated in API level Q) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="storagename">z.B. "19F4-0903"</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      abstract public Task<bool> Ask4AndroidPersistentPermissonAndWait(string storagename, int requestid = 12345);

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen (API level 24 / Nougat / 7.0; deprecated in API level Q) (für primäres externes Volume nicht geeignet!)
      /// </summary>
      /// <param name="volidx">Volume-Index (min. 1)</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      abstract public Task<bool> Ask4AndroidPersistentPermissonAndWait(int volidx, int requestid = 12345);

      #endregion

      /// <summary>
      /// liefert die akt. Daten für ein Volume (oder null)
      /// </summary>
      /// <param name="volidx">Volume-Index</param>
      /// <returns></returns>
      abstract public VolumeData GetVolumeData(int volidx);

      /// <summary>
      /// liefert die akt. Daten für ein Volume (oder null)
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <returns></returns>
      abstract public VolumeData GetVolumeData(string storagename);

      /// <summary>
      /// Akt. der Infos
      /// </summary>
      abstract public void RefreshVolumesAndSpecPaths();

      /// <summary>
      /// Gibt es das Verzeichnis?
      /// </summary>
      /// <param name="abspath"></param>
      /// <returns></returns>
      abstract public bool DirectoryExists(string abspath);

      /// <summary>
      /// Gibt es die Datei?
      /// </summary>
      /// <param name="abspath"></param>
      /// <returns></returns>
      abstract public bool FileExists(string abspath);

      /// <summary>
      /// Liste der Dateien im Verzeichnis
      /// </summary>
      /// <param name="abspath"></param>
      /// <returns></returns>
      abstract public List<string> Files(string abspath);

      /// <summary>
      /// Liste der Verzeichnisse im Verzeichnis
      /// </summary>
      /// <param name="abspath"></param>
      /// <returns></returns>
      abstract public List<string> Directories(string abspath);

      /// <summary>
      /// liefert eine Liste <see cref="StorageItem"/>; das 1. Item gehört zu fullpath
      /// </summary>
      /// <param name="fullpath"></param>
      /// <returns></returns>
      abstract public List<StorageItem> StorageItemList(string fullpath);

      /// <summary>
      /// liefert einen Dateistream
      /// <para>Falls die Datei nicht ex. wird sie erzeugt. Auch das Verzeichnis wird bei Bedarf erzeugt.</para>
      /// <para>I.A. wird damit ein StreamWriter bzw. ein StreamReader erzeugt.</para>
      /// <para>ACHTUNG: Der Stream erfüllt nur primitivste Bedingungen. Er ist nicht "seekable", es wird keine Position oder Länge geliefert.
      /// Auch ein "rwt"-Stream scheint nur beschreibbar zu sein.</para>
      /// </summary>
      /// <param name="abspath">Android- oder Volume-Pfad</param>
      /// <param name="mode"><para>"w" (write-only access and erasing whatever data is currently in the file),</para>
      ///                    <para>"wa" (write-only access to append to existing data),</para>
      ///                    <para>"rw" (read and write access), or</para>
      ///                    <para>"rwt" (read and write access that truncates any existing file)</para>
      ///                    <para>["r" (read-only)] ???,</para></param>
      /// <param name="isinternal">wenn true, verweist der Pfad auf den internal Storage</param>
      /// <returns></returns>
      abstract public Stream OpenFile(string abspath, string mode);

      /// <summary>
      /// löscht die Datei
      /// </summary>
      /// <param name="abspath">Android- oder Volume-Pfad</param>
      /// <param name="isinternal">wenn true, verweist der Pfad auf den internal Storage</param>
      /// <returns></returns>
      abstract public bool DeleteFile(string abspath);

      /// <summary>
      /// erzeugt ein Verzeichnis
      /// </summary>
      /// <param name="abspath">Android- oder Volume-Pfad</param>
      /// <param name="isinternal">wenn true, verweist der Pfad auf den internal Storage</param>
      /// <returns></returns>
      abstract public bool CreateDirectory(string abspath);

      /// <summary>
      /// löscht das Verzeichnis
      /// </summary>
      /// <param name="abspath">Android- oder Volume-Pfad</param>
      /// <param name="isinternal">wenn true, verweist der Pfad auf den internal Storage</param>
      /// <returns></returns>
      abstract public bool DeleteDirectory(string abspath);

      /// <summary>
      /// verschiebt eine Datei (einschließlich Umbenennung)
      /// </summary>
      /// <param name="abspathsrc"></param>
      /// <param name="abspathdst"></param>
      /// <returns></returns>
      abstract public bool Move(string abspathsrc, string abspathdst);

      /// <summary>
      /// kopiert eine Datei in eine neue Datei
      /// </summary>
      /// <param name="abspathsrc"></param>
      /// <param name="abspathdst"></param>
      /// <returns></returns>
      abstract public bool Copy(string abspathsrc, string abspathdst);

      /// <summary>
      /// liefert die Länge einer Datei, Lese- und Schreibberechtigung und den Zeitpunkt der letzten Änderung
      /// <para>Das Leserecht kann nur beim secondary external storage false ergeben.</para>
      /// </summary>
      /// <param name="abspath"></param>
      /// <param name="pathisdir">wenn true, ist Pfad ein Verzeichnis</param>
      /// <param name="canread"></param>
      /// <param name="canwrite"></param>
      /// <param name="lastmodified"></param>
      /// <returns>Dateilänge</returns>
      abstract public long GetFileAttributes(string abspath, bool pathisdir, out bool canread, out bool canwrite, out DateTime lastmodified);

      /// <summary>
      /// setzt den ReadOnly-Status und den Zeitpunkt der letzten Änderung
      /// <para>fkt. NICHT im secondary external storage</para>
      /// </summary>
      /// <param name="fullpath"></param>
      /// <param name="canwrite"></param>
      /// <param name="lastmodified"></param>
      abstract public void SetFileAttributes(string fullpath, bool canwrite, DateTime lastmodified);

#if DEBUG

      abstract public object test1(object o1);

#endif
   }

}
