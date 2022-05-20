using AndroidX.DocumentFile.Provider;
using System;
using static FSofTUtils.Xamarin.DependencyTools.StorageHelper;

namespace FSofTUtils.Android.Storage {

   /// <summary>
   /// <see cref="StorageItem"/> auf Basis des Storage Access Framework
   /// </summary>
   class SafStorageItem : StorageItem {

      public SafStorageItem(DocumentFile file) {
         Name = file.Name;
         IsDirectory = file.IsDirectory;
         IsFile = file.IsFile;
         MimeType = file.Type;
         CanRead = file.CanRead();
         CanWrite = file.CanWrite();
         LastModified = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime().AddMilliseconds(file.LastModified()); // Bezug auf den 1.1.1970
         Length = file.Length();
      }

   }
}