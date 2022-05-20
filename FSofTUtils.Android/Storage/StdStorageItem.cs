using System.IO;
using static FSofTUtils.Xamarin.DependencyTools.StorageHelper;

namespace FSofTUtils.Android.Storage {

   /// <summary>
   /// <see cref="StorageItem"/> auf Basis der normalen Dateifunktionen
   /// </summary>
   class StdStorageItem : StorageItem {

      public StdStorageItem(DirectoryInfo di) {
         Name = di.Name;
         IsDirectory = true;
         IsFile = false;
         LastModified = di.CreationTime;
         CanRead = CanWrite = true;
         try {
            di.GetFiles();
         } catch {
            CanRead = CanWrite = false;
         }
         //MimeType = file.Type;
         //Length = file.Length();
      }

      public StdStorageItem(FileInfo fi) {
         Name = fi.Name;
         IsDirectory = false;
         IsFile = true;
         Length = fi.Length;
         CanWrite = !fi.IsReadOnly;
         LastModified = fi.LastWriteTime;
         CanRead = false;
         using (StreamReader sr = new StreamReader(fi.FullName)) {
            CanRead = true;
         }
         //MimeType = file.Type;
      }

   }
}