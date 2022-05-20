using Android.App;
using Android.Content;
using Android.OS;
using Android.OS.Storage;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static FSofTUtils.Xamarin.DependencyTools.StorageHelper;

namespace FSofTUtils.Android.Storage {
#if DEBUG

   class Tests {

      /// <summary>
      /// public in ExternalStorageProvider.java definiert
      /// </summary>
      const string AUTHORITY_EXTERNALSTORAGE_DOCUMENTS = "com.android.externalstorage.documents";

      const string ANDROID_OS_STORAGE_ACTION_OPEN_EXTERNAL_DIRECTORY = "android.os.storage.action.OPEN_EXTERNAL_DIRECTORY";

      const string ANDROID_OS_STORAGE_EXTRA_DIRECTORY_NAME = "android.os.storage.extra.DIRECTORY_NAME";

      const string CONTENT_EXTERNALSTORAGE_TREE = "content://com.android.externalstorage.documents/tree/";

      // Konstanen u.a. in
      //    Settings
      //    Context
      //    Intent

      Activity Activity;
      SafStorageHelper saf;
      Volume vol;



      /// <summary>
      /// Hilfsfunktionen für den Secondary External Storage
      /// </summary>
      public Tests() {



      }



      public async Task<object> Start(Activity activity,
                                      SafStorageHelper saf,
                                      Volume vol) {
         Activity = activity;
         this.saf = saf;
         this.vol = vol;

         if (Build.VERSION.SdkInt > BuildVersionCodes.Q) {
            if (!global::Android.OS.Environment.IsExternalStorageManager)  // Ex. die Rechte (erst ab R)? (Intent ist nicht nötig)
               await saf.Ask4PersistentPermissonAndWait4Result(null,
                                                               -1,
                                                               12345,
                                                               saf.getAccessIntent_R());
         }

         for (int idx = 0; idx < vol.Volumes; idx++) {
            if (idx > 0 &&
                Build.VERSION.SdkInt <= BuildVersionCodes.Q &&
                !saf.SetPersistentPermissions(vol.StorageVolumeNames[idx], idx))
               await saf.Ask4PersistentPermissonAndWait4Result(vol.StorageVolumeNames[idx], idx);

            testStd(vol.StorageVolumePaths[idx]);

            if (idx > 0)
               testDf(vol.StorageVolumeNames[idx], "");
         }

         return null;
      }



      void testStd(string volpath) {
         testStd4path(volpath);
         try {
            string[] dirs = Directory.GetDirectories(volpath);
            foreach (string dir in dirs) {
               testStd4path(dir);
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(testStd) + "(): " + ex.Message);
         }
      }

      void testDf(string storagename, string volpath) {
         testDf4path(storagename, volpath);
         try {
            foreach (string dir in saf.ObjectList(storagename, volpath, true))
               testDf4path(storagename, Path.Combine(volpath, dir));
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(testDf) + "(): " + ex.Message);
         }
      }





      class MediastoreItem {

         public string Volumename { get; protected set; }
         public string Displayname { get; protected set; }
         public string Title { get; protected set; }
         public string Fullpath { get; protected set; }
         public bool IsFile { get; protected set; }
         public int Size { get; protected set; }
         public DateTime DateCreate { get; protected set; }
         public DateTime DateModified { get; protected set; }

         public int ID { get; protected set; }
         public int Parent { get; protected set; }


         public MediastoreItem(string volumename,
                               string displayname,
                               string title,
                               string fullpath,
                               bool isfile,
                               int size,
                               DateTime datecreate,
                               DateTime datemodified) {
            Volumename = volumename;
            Displayname = displayname;
            Title = title;
            Fullpath = fullpath;
            IsFile = isfile;
            Size = size;
            DateCreate = datecreate;
            DateModified = datemodified;
         }

         public MediastoreItem(global::Android.Database.ICursor cursor) {
            for (int i = 0; i < cursor.ColumnCount; i++) {
               string colname = cursor.GetColumnName(i);

               if (colname == MediaStore.IMediaColumns.VolumeName)
                  Volumename = cursor.GetString(i);
               else if (colname == MediaStore.IMediaColumns.DisplayName)      // The display name of the media item. (i.A. Dateiname)
                  Displayname = cursor.GetString(i);
               else if (colname == MediaStore.Files.IFileColumns.Title)       // ???
                  Title = cursor.GetString(i);
               else if (colname == MediaStore.IMediaColumns.Data)
                  Fullpath = cursor.GetString(i);
               else if (colname == MediaStore.IMediaColumns.Size)             // Value is a non-negative number of bytes. 
                  Size = cursor.GetInt(i);
               else if (colname == MediaStore.Files.IFileColumns.MediaType)   // https://developer.android.com/reference/android/provider/MediaStore.Files.FileColumns#MEDIA_TYPE_DOCUMENT)
                  IsFile = cursor.GetInt(i) != 0;
               else if (colname == MediaStore.IMediaColumns.DateAdded)
                  DateCreate = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime().AddSeconds(cursor.GetInt(i)); // Bezug auf den 1.1.1970
               else if (colname == MediaStore.IMediaColumns.DateModified)
                  DateModified = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime().AddSeconds(cursor.GetInt(i)); // Bezug auf den 1.1.1970

               else if (colname == MediaStore.MediaColumns.Id)
                  ID = cursor.GetInt(i);
               else if (colname == MediaStore.Files.IFileColumns.Parent)      // The index of the parent directory of the file
                  Parent = cursor.GetInt(i);
            }
         }

         /*
[0:] volume_name -> external_primary
[0:] _display_name -> media
[0:] title -> media
[0:] _data -> /storage/emulated/0/Android/media
[0:] media_type -> 0
[0:] _size -> 4096
[0:] date_added -> 1627753018
[0:] date_modified -> 1627752968
ev.:
[0:] _id -> 28
[0:] parent -> 3
          */

         public override string ToString() {
            return string.Format("IsFile={0}, Fullpath={1}", IsFile, Fullpath);
         }

      }

      /// <summary>
      /// Abfrage von MediaStore-Items
      /// </summary>
      /// <param name="uri"></param>
      /// <param name="projection"></param>
      /// <param name="selection"></param>
      /// <param name="selectionArgs"></param>
      /// <param name="sortOrder"></param>
      void query(global::Android.Net.Uri uri,
                 string[] projection = null,
                 string selection = null,
                 string[] selectionArgs = null,
                 string sortOrder = null) {

         try {
            System.Diagnostics.Debug.WriteLine("query(): " + uri.SchemeSpecificPart);

            ContentResolver contentResolver = global::Android.App.Application.Context.ContentResolver;

            /*


   uri                  Uri 
                        The URI, using the content:// scheme, for the content to retrieve.
   projection           String[] 
                        A list of which columns to return. Passing null will return all columns, which is inefficient.
   selection            String 
                        A filter declaring which rows to return, formatted as an SQL WHERE clause (excluding the WHERE itself). Passing null will return all rows for the given URI.
   selectionArgs        String[] 
                        You may include ?s in selection, which will be replaced by the values from selectionArgs, in the order that they appear in the selection. 
                        The values will be bound as Strings.
   sortOrder            String 
                        How to order the rows, formatted as an SQL ORDER BY clause (excluding the ORDER BY itself). Passing null will use the default sort order, which may be unordered.
   cancellationSignal   CancellationSignal 
                        A signal to cancel the operation in progress, or null if none. If the operation is canceled, then OperationCanceledException will be thrown when 
                        the query is executed.
             */

            global::Android.Database.ICursor cursor = contentResolver.Query(uri,
                                                                            projection,
                                                                            selection,
                                                                            selectionArgs,
                                                                            sortOrder,
                                                                            null);
            if (cursor != null && cursor.Count > 0) {
               cursor.MoveToFirst();
               do {
                  for (int i = 0; i < cursor.ColumnCount; i++) {
                     string colname = cursor.GetColumnName(i);
                     if (projection != null) {
                        foreach (var item in projection) {
                           if (item == colname)
                              System.Diagnostics.Debug.WriteLine(string.Format("{0} -> {1}", colname, cursor.GetString(i)));
                        }
                     } else {
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} -> {1}", colname, cursor.GetString(i)));
                     }
                  }
                  //System.Diagnostics.Debug.WriteLine("---");

                  MediastoreItem msi = new MediastoreItem(cursor);

               }
               while (!cursor.IsAfterLast && cursor.MoveToNext());
               cursor.Close();
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception query(): " + ex.Message);
         }
      }

      /*
val projectionDownloads = arrayOf(
 MediaStore.Downloads._ID,
 MediaStore.Downloads.DISPLAY_NAME,
)
val selectionDownloads = ""
val selectionArgsDownloads = emptyArray<String>()
val sortOrderDownloads = "${MediaStore.Downloads.DISPLAY_NAME} ASC"

context.applicationContext.contentResolver.query(
 MediaStore.Downloads.EXTERNAL_CONTENT_URI,
 projectionDownloads,      arrayOf(MediaStore.Downloads._ID, MediaStore.Downloads.DISPLAY_NAME, )
 selectionDownloads,       ""
 selectionArgsDownloads,   emptyArray<String>()
 sortOrderDownloads        "${MediaStore.Downloads.DISPLAY_NAME} ASC"
)?.use { cursor ->
     val idColumn = cursor.getColumnIndex(MediaStore.Images.ImageColumns._ID)
     val nameColumn = cursor.getColumnIndex(MediaStore.Files.FileColumns.DISPLAY_NAME)
     while (cursor.moveToNext()) { //Here return false
         Log.i("SeeMedia", "Move to next!")
         val id = cursor.getLong(idColumn)
         val name = cursor.getString(nameColumn)
         Log.i("SeeMedia", "ID = $id AND NAME= $name")
     }
 }


val projection = arrayOf(
 MediaStore.Files.FileColumns.DISPLAY_NAME
)
context.applicationContext.contentResolver.query(
 MediaStore.Files.getContentUri("external"),
 projection,
 null,
 null,
 null
)?.use { cursor ->
     Log.i("SeeMedia", "we got cursor! $cursor")
     val nameColumn = 
     cursor.getColumnIndex(MediaStore.Files.FileColumns.DISPLAY_NAME)
     while(cursor.moveToNext()){
         Log.i("SeeMedia", "Move to next!")
         val name = cursor.getString(nameColumn)
         Log.i("SeeMedia", "Name = $name")
     }
 }



      List<NativeSoundData> lst = new List<NativeSoundData>();
      ContentResolver contentResolver = global::Android.App.Application.Context.ContentResolver;
      string where = "";
      if (isalarm)
         where = (where.Length == 0 ? "" : " or ") + MediaStore.Audio.Media.InterfaceConsts.IsAlarm + ">0";
      if (isnotification)
         where = (where.Length == 0 ? "" : " or ") + MediaStore.Audio.Media.InterfaceConsts.IsNotification + ">0";
      if (isringtone)
         where = (where.Length == 0 ? "" : " or ") + MediaStore.Audio.Media.InterfaceConsts.IsRingtone + ">0";
      if (ismusic)
         where = (where.Length == 0 ? "" : " or ") + MediaStore.Audio.Media.InterfaceConsts.IsMusic + ">0";
      global::Android.Database.ICursor cursor = contentResolver.Query(intern ?
                                                                  MediaStore.Audio.Media.InternalContentUri :
                                                                  MediaStore.Audio.Media.ExternalContentUri,
                                                              new string[] {
                                                                 MediaStore.Audio.Media.InterfaceConsts.Id,
                                                                 MediaStore.Audio.Media.InterfaceConsts.Title,
                                                                 MediaStore.Audio.Media.InterfaceConsts.Data,
                                                              },
                                                              where,
                                                              null,
                                                              null);
      if (cursor != null && cursor.Count > 0) {
         cursor.MoveToFirst();
         do {
            string id = "";
            string name = "";
            string data = "";
            for (int i = 0; i < cursor.ColumnCount; i++) {
               string colname = cursor.GetColumnName(i);
               if (colname == MediaStore.Audio.Media.InterfaceConsts.Id)
                  id = cursor.GetString(i);
               else if (colname == MediaStore.Audio.Media.InterfaceConsts.Title)
                  name = cursor.GetString(i);
               else if (colname == MediaStore.Audio.Media.InterfaceConsts.Data)
                  data = cursor.GetString(i);
            }
            lst.Add(new NativeSoundData(intern, id, name, data));
         }
         while (!cursor.IsAfterLast && cursor.MoveToNext());
         cursor.Close();
      }
      return lst;



       */

      /// <summary>
      /// Tests zum MediaStore
      /// </summary>
      /// <param name="storagename"></param>
      /// <param name="storagepath"></param>
      /// <param name="volidx"></param>
      async void testMS(string storagename, string storagepath, int volidx) {
         try {

            // https://developer.android.com/training/data-storage/shared/media

            // This method returns the version for MediaStore#VOLUME_EXTERNAL_PRIMARY
            System.Diagnostics.Debug.WriteLine("MediaStore-Version: " + MediaStore.GetVersion(global::Android.App.Application.Context));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q) { // API level 29
               ICollection<string> vols = MediaStore.GetExternalVolumeNames(global::Android.App.Application.Context);
               /*
               -		vols	{Android.Runtime.JavaSet<string>}	Android.Runtime.JavaSet<string>
               -		IEnumerator		
                     [0]	"external_primary"	string
                     [1]	"18ff-161f"	string
                */
               foreach (var item in vols) {
                  System.Diagnostics.Debug.WriteLine("ExternalVolumeName: " + item);
                  saf.SetPersistentPermissions(item, 0);

                  if (item == "external_primary") {
                     showStdDirContent("/storage");
                     showStdDirContent("/storage/emulated");

                     showStdDirContent("/storage/emulated/0");
                     //showDirContent("/storage/emulated/0/MyTest");
                     //showDirContent("/storage/emulated/0/MyTest/GpxToolExt");
                  } else {
                     // Groß(!)schreibung trotz MediaStore.GetExternalVolumeNames() "18ff-161f" !!!
                     showStdDirContent("/storage/" + item.ToUpper());    // Could not find a part of the path 
                  }
               }
            }



            /*
               MediaStore.GetMediaUri(global::Android.App.Application.Context, documentUri)      Added in API level 29

            Return a MediaStore Uri that is an equivalent to the given DocumentsProvider Uri. This only supports ExternalStorageProvider and MediaDocumentsProvider Uris. 
            This allows apps with Storage Access Framework permissions to convert between MediaStore and DocumentsProvider Uris that refer to the same underlying item.

               MediaStore.GetDocumentUri(global::Android.App.Application.Context, mediaUri)      Added in API level 26
            Return a DocumentsProvider Uri that is an equivalent to the given MediaStore Uri.
            This allows apps with Storage Access Framework permissions to convert between MediaStore and DocumentsProvider Uris that refer to the same underlying item. 
            */

            global::Android.Net.Uri uri;
            uri = MediaStore.Files.GetContentUri("internal");   // MediaStore.VolumeInternal
                                                                //uri = MediaStore.Files.GetContentUri("external");  // MediaStore.VolumeExternal
                                                                //uri = MediaStore.Files.GetContentUri(MediaStore.VolumeExternalPrimary);

            //query(uri);

            //query(global::Android.Net.Uri.WithAppendedPath(uri, "27"));

            //query(uri, null, MediaStore.MediaColumns.Data + " like ? ", new string[] { "%Android%" });

            //query(uri, null, MediaStore.MediaColumns.Data + " like ? ", new string[] { "%/Pictures%" });

            //MediaStore.MediaColumns.RelativePath


            //string[] minproj = new string[] { MediaStore.MediaColumns.Data };

            //uri = MediaStore.Files.GetContentUri(MediaStore.VolumeInternal); 
            //query(uri, minproj, MediaStore.MediaColumns.Data + " like ? ", new string[] { "%/Android%" });
            //System.Diagnostics.Debug.WriteLine("~~~");

            //uri = MediaStore.Files.GetContentUri(MediaStore.VolumeExternalPrimary);
            //query(uri, minproj, MediaStore.MediaColumns.Data + " like ? ", new string[] { "%/Android%" });
            //System.Diagnostics.Debug.WriteLine("~~~");

            //uri = MediaStore.Files.GetContentUri(MediaStore.VolumeExternal);
            //query(uri, minproj, MediaStore.MediaColumns.Data + " like ? ", new string[] { "%/Android%" });
            //System.Diagnostics.Debug.WriteLine("~~~");


            uri = MediaStore.Files.GetContentUri(MediaStore.VolumeExternal);
            query(uri, null, MediaStore.MediaColumns.Data + " like ? ", new string[] { 
               //"%/Android/Obb%" ,
               //"%/MyTest%" ,
               "%/%" ,
               //"%/aaa%",
            });
            System.Diagnostics.Debug.WriteLine("~~~");
            /* Es scheint keinen Unterschied bei den Daten zwischen "Verzeichnis" und "Datei" zu geben (?)
   
VIELLEICHT DOCH: (https://developer.android.com/reference/android/provider/MediaStore.Files.FileColumns#MEDIA_TYPE_DOCUMENT)
MEDIA_TYPE_DOCUMENT     Added in API level 30/R
Constant for the MEDIA_TYPE column indicating that file is a document file.
Constant Value: 6 (0x00000006) 



fun createNewImageFile(): Uri? {
    val resolver = context.contentResolver    // On API <= 28, use VOLUME_EXTERNAL instead.
    val imageCollection =    MediaStore.Images.Media.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY)    
    val newImageDetails = ContentValues().apply {
      put(MediaStore.Images.Media.DISPLAY_NAME, "New image.jpg")
    }    //return Uri of newly created file
    return resolver.insert(imageCollection, newImageDetails)
  }

fun deleteMediaFile(fileName: String) {
    val fileInfo = getFileInfoFromName(fileName) //this function returns Kotlin Pair<Long, Uri>
    val id = fileInfo.first
    val uri = fileInfo.second
    val selection = "${MediaStore.Images.Media._ID} = ?"
    val selectionArgs = arrayOf(id.toString())
    val resolver = context.contentResolver
    resolver.delete(uri, selection, selectionArgs)
  }

Updating a file is done the same way, except you need to call contentResolver.update() method instead.
             */

         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(testMS) + "(): " + ex.Message);
         }
      }

      void test1(string storagename, string volpath) {
         DocumentFile srcdoc = GetExistingDocumentFile(storagename, volpath);

         showUriInfos(srcdoc.Uri, Activity.ContentResolver);

         ContentResolver resolver = Activity.ContentResolver;

         ContentValues values = new ContentValues();
         values.Put("document_id", "14F1-0B07:tmp1/tmp2/test2.txt");
         values.Put("mime_type", "text/plain");
         values.Put("_display_name", "test2.txt");
         values.Put("last_modified", 1553853408000);
         values.Put("flags", 326);
         values.Put("_size", 20);
         //int res = resolver.Update(srcdoc.Uri, values, null, null);  // Exception: Update not supported

         values = new ContentValues();
         values.Put("abc", "xyz");
         //resolver.Insert(srcdoc.Uri, values); // Exception: Insert not supported

      }

      private DocumentFile GetExistingDocumentFile(string storagename, string volpath) {
         throw new NotImplementedException();
      }

      async void testDF(string storagename, string storagepath, int volidx) {
         try {
            try {
               showStdDirs("/storage");
            } catch (Exception ex) {
               System.Diagnostics.Debug.WriteLine("Exception " + nameof(testDF) + "(): " + ex.Message);
            }

            try {
               showStdDirs("/storage/emulated/0");
            } catch (Exception ex) {
               System.Diagnostics.Debug.WriteLine("Exception " + nameof(testDF) + "(): " + ex.Message);
            }

            try {
               showStdDirs(storagepath);
            } catch (Exception ex) {
               System.Diagnostics.Debug.WriteLine("Exception " + nameof(testDF) + "(): " + ex.Message);
            }

            bool persistentPermissions = saf.SetPersistentPermissions(storagename, volidx);

            try {
               showStdDirs(storagepath);
            } catch (Exception ex) {
               System.Diagnostics.Debug.WriteLine("Exception " + nameof(testDF) + "(): " + ex.Message);
            }


            DocumentFile documentFile;
            global::Android.Net.Uri voluri;
            DocumentFile[] documentFiles;

            voluri = saf.getTreeDocumentUri(storagename);
            documentFile = DocumentFile.FromTreeUri(Activity, voluri);
            showDocumentFileData(documentFile);
            documentFiles = documentFile.ListFiles();
            foreach (DocumentFile doc in documentFiles) {
               showDocumentFileData(doc);
            }

            voluri = saf.getTreeDocumentUri(storagename + "/Android");
            documentFile = DocumentFile.FromTreeUri(Activity, voluri);
            showDocumentFileData(documentFile);
            documentFiles = documentFile.ListFiles();
            foreach (DocumentFile doc in documentFiles) {
               showDocumentFileData(doc);
            }

            documentFile = saf.getDocumentFile(storagename, "/", true);
            if (documentFile != null)
               foreach (DocumentFile doc in documentFile.ListFiles()) {
                  showDocumentFileData(doc);
               }

            documentFile = saf.getDocumentFile(storagename, "/Android", true);
            if (documentFile != null)
               foreach (DocumentFile doc in documentFile.ListFiles()) {
                  showDocumentFileData(doc);
               }

         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(testDF) + "(): " + ex.Message);
         }
      }


      // DocumentsContract.getDocumentId(uri);
      //  DocumentsContract.BuildTreeDocumentUri(AUTHORITY_EXTERNALSTORAGE_DOCUMENTS, storagename + ":" + volpath);


      //private void requestPermission() {
      //   if (SDK_INT >= Build.VERSION_CODES.R) {
      //      try {
      //         Intent intent = new Intent(Settings.ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION);
      //         intent.addCategory("android.intent.category.DEFAULT");
      //         intent.setData(Uri.parse(String.format("package:%s", getApplicationContext().getPackageName())));
      //         startActivityForResult(intent, 2296);
      //      } catch (Exception e) {
      //         Intent intent = new Intent();
      //         intent.setAction(Settings.ACTION_MANAGE_ALL_FILES_ACCESS_PERMISSION);
      //         startActivityForResult(intent, 2296);
      //      }
      //   } else {
      //      //below android 11
      //      ActivityCompat.requestPermissions(PermissionActivity.this, new String[] { WRITE_EXTERNAL_STORAGE }, PERMISSION_REQUEST_CODE);
      //   }
      //}



      async void ask4PersistentPermisson_R(string storagename = null, int volidx = -1) {
         // global::Android.OS.Environment.IsExternalStorageManager; // Ex. die Rechte? (Intent ist nicht nötig)

         await saf.Ask4PersistentPermissonAndWait4Result(storagename,
                                                         volidx,
                                                         12345,
                                                         saf.getAccessIntent_R());
      }



      bool testStd4path(string path) {
         bool result = showStdDirContent(path);
         if (result)
            result = testStdWrite(path, "TestDir~~", "TestFile~~");
         return result;
      }

      bool testStdWrite(string path, string dirname, string filename) {
         bool result = true;
         try {
            dirname = Path.Combine(path, dirname);
            System.Diagnostics.Debug.WriteLine("Test " + nameof(testStdWrite) + "()");
            Directory.CreateDirectory(dirname);
            System.Diagnostics.Debug.WriteLine("   Directory.CreateDirectory " + dirname + " OK");
            Directory.Delete(dirname);
            System.Diagnostics.Debug.WriteLine("   Directory.Delete " + dirname + " OK");

            filename = Path.Combine(path, filename);
            FileStream fs = File.Create(filename);
            System.Diagnostics.Debug.WriteLine("   File.Create " + filename + " OK");
            fs.Write(new byte[] { 33, 34, 35, 36, 37 }, 0, 5);
            fs.Close();
            System.Diagnostics.Debug.WriteLine("   File.Write " + filename + " OK");
            fs.Dispose();
            File.Delete(filename);
            System.Diagnostics.Debug.WriteLine("   File.Delete " + filename + " OK");
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(testStdWrite) + "(): " + ex.Message);
            result = false;
         }
         return result;
      }

      bool showStdDirContent(string path) {
         bool result = true;
         try {
            System.Diagnostics.Debug.WriteLine("Content in " + path);
            string[] dirs = Directory.GetDirectories(path);
            foreach (string fulldir in dirs) {
               DirectoryInfo di = new DirectoryInfo(fulldir);
               int subdirs = 0;
               int subfiles = 0;
               try {
                  subdirs = Directory.GetDirectories(fulldir).Length;
                  subfiles = Directory.GetFiles(fulldir).Length;
               } catch (Exception ex) {
               }
               System.Diagnostics.Debug.WriteLine("   Directory " + fulldir
                                                         + ", Dirs " + subdirs
                                                         + ", Files " + subfiles
                                                         + ", CreationTime " + di.CreationTime.ToUniversalTime()                  // CreationTime == LastWriteTime ???
                                                         + ", LastWriteTime" + di.LastWriteTime.ToUniversalTime()
                                                         + ", Attributes " + di.Attributes);
            }
            string[] files = Directory.GetFiles(path);
            foreach (string fullfile in files) {
               FileInfo fi = new FileInfo(fullfile);
               System.Diagnostics.Debug.WriteLine("   File " + fullfile
                                                         + ", CreationTime " + fi.CreationTime.ToUniversalTime()
                                                         + ", LastWriteTime " + fi.LastWriteTime.ToUniversalTime()
                                                         + ", Length " + fi.Length
                                                         + ", Attributes " + fi.Attributes
                                                         + ", IsReadOnly " + fi.IsReadOnly);
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(showStdDirContent) + "(): " + ex.Message);
            result = false;
         }
         return result;
      }

      bool showStdDirs(string path) {
         bool result = true;
         try {
            System.Diagnostics.Debug.WriteLine("Directories in " + path);
            string[] dirs = Directory.GetDirectories(path);
            foreach (string fulldir in dirs) {
               DirectoryInfo di = new DirectoryInfo(fulldir);
               int subdirs = 0;
               int files = 0;
               try {
                  subdirs = Directory.GetDirectories(fulldir).Length;
                  files = Directory.GetFiles(fulldir).Length;
               } catch (Exception ex) {
               }
               System.Diagnostics.Debug.WriteLine("   Directory '" + fulldir + "'"
                                                         + ", Dirs " + subdirs
                                                         + ", Files " + files
                                                         + ", CreationTime " + di.CreationTime.ToUniversalTime()                  // CreationTime == LastWriteTime ???
                                                         + ", LastWriteTime" + di.LastWriteTime.ToUniversalTime()
                                                         + ", Attributes " + di.Attributes);
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(showStdDirs) + "(): " + ex.Message);
            result = false;
         }
         return result;
      }

      bool showStdFiles(string path) {
         bool result = true;
         try {
            System.Diagnostics.Debug.WriteLine("Files in " + path);
            string[] files = Directory.GetFiles(path);
            foreach (string fullfile in files) {
               FileInfo fi = new FileInfo(fullfile);
               System.Diagnostics.Debug.WriteLine("   File '" + fullfile + "'"
                                                         + ", CreationTime " + fi.CreationTime.ToUniversalTime()
                                                         + ", LastWriteTime " + fi.LastWriteTime.ToUniversalTime()
                                                         + ", Length " + fi.Length
                                                         + ", Attributes " + fi.Attributes
                                                         + ", IsReadOnly " + fi.IsReadOnly);
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(showStdFiles) + "(): " + ex.Message);
            result = false;
         }
         return result;
      }


      bool testDf4path(string storagename, string path) {
         bool result = showDfContent(storagename, path);
         if (result)
            result = testDfWrite(storagename, path, "TestDir~~", "TestFile~~");
         return result;
      }

      bool testDfWrite(string storagename, string path, string dirname, string filename) {
         bool result = true;
         try {
            dirname = Path.Combine(path, dirname);
            System.Diagnostics.Debug.WriteLine("Test " + nameof(testStdWrite) + "()");
            result = saf.CreateDirectory(storagename, dirname);
            System.Diagnostics.Debug.WriteLine("   SafStorageHelper.CreateDirectory " + dirname + (result ? " OK" : " NOK"));
            if (result) {
               result = saf.Delete(storagename, dirname);
               System.Diagnostics.Debug.WriteLine("   SafStorageHelper.Delete " + dirname + (result ? " OK" : " NOK"));
            }

            filename = Path.Combine(path, filename);
            Stream fs = saf.CreateOpenFile(storagename, filename, "rw");
            result = fs != null;
            System.Diagnostics.Debug.WriteLine("   SafStorageHelper.CreateOpenFile " + filename + (result ? " OK" : " NOK"));
            if (result) {
               fs.Write(new byte[] { 33, 34, 35, 36, 37 }, 0, 5);
               fs.Close();
               System.Diagnostics.Debug.WriteLine("   SafStorageHelper.Write " + filename + (result ? " OK" : " NOK"));
               fs.Dispose();
               result = saf.Delete(storagename, filename);
               System.Diagnostics.Debug.WriteLine("   SafStorageHelper.Delete " + filename + (result ? " OK" : " NOK"));
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(testDfWrite) + "(): " + ex.Message);
            result = false;
         }
         return result;
      }

      bool showDfContent(string storagename, string path) {
         bool result = true;
         try {
            System.Diagnostics.Debug.WriteLine("Content in " + path);
            //foreach (string fulldir in saf.ObjectList(storagename, path, true)) {
            //   System.Diagnostics.Debug.WriteLine("   Directory " + fulldir);
            //}
            //foreach (string fullfile in saf.ObjectList(storagename, path, false)) {
            //   System.Diagnostics.Debug.WriteLine("   File " + fullfile);
            //}

            showDfDirs(storagename, path);
            showDfFiles(storagename, path);
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(showStdDirContent) + "(): " + ex.Message);
            result = false;
         }
         return result;
      }

      bool showDfFiles(string storagename, string path) {
         bool result = true;
         System.Diagnostics.Debug.WriteLine("Files in " + storagename + "/ " + path);
         try {
            //foreach (string file in saf.ObjectList(storagename, path, false)) {
            //   System.Diagnostics.Debug.WriteLine("   File " + file);
            //}

            foreach (StorageItem si in saf.StorageItemList(storagename, path)) {
               if (si.IsFile) {
                  System.Diagnostics.Debug.WriteLine("   File '" + Path.Combine(path, si.Name) + "'"
                                                            + ", Length " + si.Length
                                                            + ", MimeType " + si.MimeType
                                                            + ", LastModified " + si.LastModified.ToUniversalTime()
                                                            + ", CanRead " + si.CanRead
                                                            + ", CanWrite " + si.CanWrite);
               }
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(showDfFiles) + "(): " + ex.Message);
            result = false;
         }
         return result;
      }

      bool showDfDirs(string storagename, string path) {
         bool result = true;
         System.Diagnostics.Debug.WriteLine("Directories in " + storagename + "/ " + path);
         try {
            foreach (StorageItem si in saf.StorageItemList(storagename, path)) {
               if (si.IsDirectory) {
                  int subdirs = 0;
                  int subfiles = 0;
                  try {
                     foreach (StorageItem si2 in saf.StorageItemList(storagename, Path.Combine(path, si.Name))) {
                        if (si2.IsDirectory)
                           subdirs++;
                        else if (si2.IsFile)
                           subfiles++;
                     }
                  } catch (Exception ex) {
                  }
                  System.Diagnostics.Debug.WriteLine("   Directory '" + Path.Combine(path, si.Name) + "'"
                                                            + ", Dirs " + subdirs
                                                            + ", Files " + subfiles
                                                            + ", LastModified " + si.LastModified.ToUniversalTime()
                                                            + ", CanRead " + si.CanRead
                                                            + ", CanWrite " + si.CanWrite);
               }
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception " + nameof(showDfDirs) + "(): " + ex.Message);
            result = false;
         }
         return result;
      }


      void showDocumentFileData(DocumentFile doc) {
         System.Diagnostics.Debug.WriteLine("DocumentFile: IsDirectory=" + doc.IsDirectory + " IsFile=" + doc.IsFile + " Name=" + doc.Name);
      }

      async Task<bool> ask(Intent intent, int requestid) {
         Type activitytype = Activity.GetType();
         System.Reflection.MethodInfo method = activitytype.GetMethod("MyStartActivityForResultAsync");
         if (method != null) {

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
               System.Diagnostics.Debug.WriteLine("IsExternalStorageManager=" + global::Android.OS.Environment.IsExternalStorageManager); // Ex. die Rechte in R?

            Tuple<Result, Intent> result = await (Task<Tuple<Result, Intent>>)method.Invoke(Activity, new object[] { intent, requestid });

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
               System.Diagnostics.Debug.WriteLine("IsExternalStorageManager=" + global::Android.OS.Environment.IsExternalStorageManager); // Ex. die Rechte in R?

            return result.Item1 == Result.Ok;
         }
         throw new Exception("Method 'MyStartActivityForResultAsync' in Activity is not available.");
      }

      void showUriInfos(global::Android.Net.Uri uri, ContentResolver resolver) {
         System.Diagnostics.Debug.WriteLine("URI-Scheme/SchemeSpecificPart: " + uri.Scheme + "; " + uri.SchemeSpecificPart);
         try {
            System.Diagnostics.Debug.WriteLine("GetDocumentId: " + DocumentsContract.GetDocumentId(uri));
         } catch (Exception ex) {
         }
         try {
            System.Diagnostics.Debug.WriteLine("GetTreeDocumentId: " + DocumentsContract.GetTreeDocumentId(uri));
         } catch (Exception ex) {
         }
         try {
            System.Diagnostics.Debug.WriteLine("GetRootId: " + DocumentsContract.GetRootId(uri));
         } catch (Exception ex) {
         }
         global::Android.Database.ICursor cursor = resolver.Query(uri, null, null, null, null);
         if (cursor != null) {
            while (cursor.MoveToNext()) {
               for (int i = 0; i < cursor.ColumnCount; i++) {
                  string val = "";
                  switch (cursor.GetType(i)) {
                     case global::Android.Database.FieldType.String: val = cursor.GetString(i); break;
                     case global::Android.Database.FieldType.Integer: val = cursor.GetLong(i).ToString(); break;    // GetInt() ist hier falsch
                     case global::Android.Database.FieldType.Float: val = cursor.GetFloat(i).ToString(); break;
                     case global::Android.Database.FieldType.Blob: val = "(blob)"; break;
                     case global::Android.Database.FieldType.Null: val = "(null)"; break;
                  }
                  System.Diagnostics.Debug.WriteLine(string.Format("Column={0}; ColumnName={1}; ColumnType={2}: {3}", i, cursor.GetColumnName(i), cursor.GetType(i).ToString(), val));
               }
            }
         }
      }

      ///// <summary>
      ///// liefert die schemaspezifische Pfadangabe(für DocumentFile)
      ///// <para>
      ///// z.B. //com.android.externalstorage.documents/tree/19F4-0903:/document/19F4-0903:abc/def.txt
      ///// </para>
      ///// </summary>
      ///// <param name = "abspath" ></ param >
      ///// < returns ></ returns >
      //public string GetAsSchemeSpecificPath(string abspath, string storagepath = null) {
      //   string volpath = GetAsVolumePath(abspath, storagepath);
      //   if (volpath.StartsWith('/'))
      //      volpath = volpath.Substring(1);

      //   string storagename = StorageName;
      //   if (!string.IsNullOrEmpty(storagepath)) {
      //      int idx = storagepath.LastIndexOf('/');
      //      if (idx > 0)
      //         storagename = storagepath.Substring(idx + 1);

      //      if (storagepath == "0")       // !!! Das ist keine sehr gute Variante !!!
      //         storagepath = "primary";
      //   }

      //   return "//" + AUTHORITY_EXTERNALSTORAGE_DOCUMENTS + "/tree/" + storagename + ":/document/" + storagename + ":" + volpath;
      //}

      bool isSchemeSpecificPath(string path, out string volpath) {
         volpath = "";
         if (path.StartsWith("//" + AUTHORITY_EXTERNALSTORAGE_DOCUMENTS)) { // SchemeSpecific
            int start = path.IndexOf(':', AUTHORITY_EXTERNALSTORAGE_DOCUMENTS.Length + 2, 2);
            if (start >= 0) {
               path = path.Substring(start);
               volpath = path.StartsWith('/') ? path : "/" + path;
               return true;
            }
         }
         return false;
      }

   }
#endif
}