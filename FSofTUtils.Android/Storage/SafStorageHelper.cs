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

   /// <summary>
   /// Hilfsfunktionen für Storage Access Framework
   /// </summary>
   class SafStorageHelper {

      /// <summary>
      /// public in ExternalStorageProvider.java definiert
      /// </summary>
      const string AUTHORITY_EXTERNALSTORAGE_DOCUMENTS = "com.android.externalstorage.documents";

      const string ANDROID_OS_STORAGE_ACTION_OPEN_EXTERNAL_DIRECTORY = "android.os.storage.action.OPEN_EXTERNAL_DIRECTORY";

      const string ANDROID_OS_STORAGE_EXTRA_DIRECTORY_NAME = "android.os.storage.extra.DIRECTORY_NAME";

      const string CONTENT_EXTERNALSTORAGE_TREE = "content://com.android.externalstorage.documents/tree/";


      readonly Activity Activity;


      /// <summary>
      /// Hilfsfunktionen für den Secondary External Storage
      /// </summary>
      /// <param name="activity"></param>
      public SafStorageHelper(Activity activity) {
         if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)  // "Lollipop", 5.0
            throw new Exception("Need Version 5.0 ('Lollipop', API 21) or higher.");
         Activity = activity as Activity;
      }

      /// <summary>
      /// liefert das DocumentFile für eine Datei oder ein Verzeichnis, wenn es existiert (sonst null) (API level 14)
      /// <para>Die Funktion ist realtiv zeitaufwendig.</para>
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <param name="volpath">abs. Pfad im Volume</param>
      /// <returns></returns>
      public DocumentFile GetExistingDocumentFile(string storagename, string volpath) {
         global::Android.Net.Uri rooturi = getTreeDocumentUri(storagename);
         DocumentFile document = DocumentFile.FromTreeUri(Activity.ApplicationContext, rooturi);

         string[] pathelems = volpath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

         for (int i = 0; document != null && i < pathelems.Length; i++) {
            DocumentFile nextDocument = document.FindFile(pathelems[i]);
            document = nextDocument;
         }

         //System.Diagnostics.Debug.WriteLine("GetExistingDocumentFile(" + storagename + ", " + volpath + ") = " + (document != null).ToString());

         return document;
      }

      /// <summary>
      /// Ex. das Objekt?
      /// </summary>
      /// <param name="storagename"></param>
      /// <param name="volpath"></param>
      /// <param name="dir">true, wenn Verzeichnis</param>
      /// <returns></returns>
      public bool ObjectExists(string storagename, string volpath, bool dir) {
         DocumentFile doc = GetExistingDocumentFile(storagename, volpath);
         if (doc != null) {
            if (dir && doc.IsDirectory)
               return true;
            if (!dir && doc.IsFile)
               return true;
         }
         return false;
      }

      /// <summary>
      /// liefert eine Liste der Verzeichnis- oder Dateinamen
      /// </summary>
      /// <param name="storagename"></param>
      /// <param name="volpath"></param>
      /// <param name="dir"></param>
      /// <returns></returns>
      public List<string> ObjectList(string storagename, string volpath, bool dir) {
         List<string> lst = new List<string>();
         DocumentFile doc = GetExistingDocumentFile(storagename, volpath);
         if (doc != null) {
            // Returns an array of files contained in the directory represented by this file.
            foreach (DocumentFile file in doc.ListFiles()) {
               if (dir && file.IsDirectory)
                  lst.Add(file.Name);
               if (!dir && file.IsFile)
                  lst.Add(file.Name);
            }
         }
         return lst;
      }

      /// <summary>
      /// liefert eine Liste aus einem <see cref="StorageItem"/> für den Pfad (Index 0) und den <see cref="StorageItem"/> für den Inhalt
      /// <para>Es ist nur 1 zeitaufwändiges <see cref="GetExistingDocumentFile(string, string)"/> nötig mit dem gleich eine ganze Reihe von zusätzlichen Infos geliefert werden.
      /// Deshalb könnte die Funktion u.U. besser geeignet sein als <see cref="ObjectList(string, string, bool)"/>.</para>
      /// </summary>
      /// <param name="storagename"></param>
      /// <param name="volpath"></param>
      /// <returns></returns>
      public List<StorageItem> StorageItemList(string storagename, string volpath) {
         List<StorageItem> lst = new List<StorageItem>();
         DocumentFile doc = GetExistingDocumentFile(storagename, volpath);
         if (doc != null) {
            lst.Add(new SafStorageItem(doc));
            // Returns an array of files contained in the directory represented by this file.
            foreach (DocumentFile file in doc.ListFiles())
               lst.Add(new SafStorageItem(file));
         }
         return lst;
      }

      #region Veränderungen eines Objektes

      /// <summary>
      /// löscht das Objekt
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <param name="volpath">abs. Pfad im Volume</param>
      /// <returns></returns>
      public bool Delete(string storagename, string volpath) {
         DocumentFile doc = GetExistingDocumentFile(storagename, volpath);
         bool ok = doc != null && doc.Delete();

         System.Diagnostics.Debug.WriteLine("AndroidDelete(" + storagename + ", " + volpath + ") = " + ok.ToString());

         return ok;
      }

      /// <summary>
      /// erzeugt ein Verzeichnis
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <param name="volpath">abs. Pfad im Volume</param>
      /// <returns></returns>
      public bool CreateDirectory(string storagename, string volpath) {
         return getDocumentFile(storagename, volpath, true) != null;
      }

      /// <summary>
      /// liefert einen Dateistream (erzeugt die Datei bei Bedarf)
      /// <para>ACHTUNG: Der Stream erfüllt nur primitivste Bedingungen. Er ist nicht "seekable", es wird keine Position oder Länge geliefert.
      /// Auch ein "rwt"-Stream scheint nur beschreibbar zu sein.</para>
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <param name="volpath">abs. Pfad im Volume</param>
      /// <param name="mode">May be "w", "wa", "rw", or "rwt". This value must never be null. Zusatz: "r" verwendet OpenInputStream()</param>
      /// <returns></returns>
      public Stream CreateOpenFile(string storagename, string volpath, string mode) {
         DocumentFile doc = getDocumentFile(storagename, volpath, false);
         if (doc != null) {
            /*
            InputStream openInputStream(Uri uri)                        Open a stream on to the content associated with a content URI. 
                                                                        If there is no data associated with the URI, FileNotFoundException is thrown. 
            Accepts the following URI schemes:
               content(SCHEME_CONTENT)
               android.resource (SCHEME_ANDROID_RESOURCE)
                  A Uri object can be used to reference a resource in an APK file. 
                     z.B.:
                     Uri uri = Uri.parse("android.resource://com.example.myapp/" + R.raw.my_resource");
                     Uri uri = Uri.parse("android.resource://com.example.myapp/raw/my_resource");
               file(SCHEME_FILE)

            OutputStream openOutputStream(Uri uri)                      Synonym for openOutputStream(uri, "w")
            OutputStream openOutputStream(Uri uri, String mode)         Open a stream on to the content associated with a content URI. 
                                                                        If there is no data associated with the URI, FileNotFoundException is thrown. 
                                                                        mode: May be "w", "wa", "rw", or "rwt". This value must never be null.
            Accepts the following URI schemes:
               content(SCHEME_CONTENT)
               file(SCHEME_FILE)
            */
            return mode == "r" ?
                        Activity.ContentResolver.OpenInputStream(doc.Uri) :
                        Activity.ContentResolver.OpenOutputStream(doc.Uri, mode);
         }
         return null;
      }

      /// <summary>
      /// Ändert den Objektnamen (API level 14)
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <param name="volpath">abs. Pfad im Volume</param>
      /// <param name="newfilename"></param>
      /// <returns></returns>
      public bool Rename(string storagename, string volpath, string newfilename) {
         if (newfilename.Contains('/'))
            throw new Exception("Path for newname is not allowed.");
         /*
            boolean renameTo (String displayName)

            Renames this file to displayName.
            Note that this method does not throw IOException on failure. Callers must check the return value.
            Some providers may need to create a new document to reflect the rename, potentially with a different MIME type, so getUri() and getType() may change to reflect the rename.
            When renaming a directory, children previously enumerated through listFiles() may no longer be valid.

            Parameters
            displayName 	String: the new display name.

            Returns
            boolean 	true on success.
         */
         DocumentFile doc = GetExistingDocumentFile(storagename, volpath);
         bool ok = doc.RenameTo(newfilename);

         System.Diagnostics.Debug.WriteLine("AndroidRename(" + storagename + ", " + volpath + ", " + newfilename + ") = " + ok.ToString());

         return ok;
      }

      /// <summary>
      /// verschiebt das Objekt (API level 14)
      /// </summary>
      /// <param name="storagenamesrc">z.B. "primary" oder "19F4-0903"</param>
      /// <param name="volpathsrc">abs. Pfad im Volume</param>
      /// <param name="storagenamenewparent">Zielvolume</param>
      /// <param name="volpathnewparent">Zielverzeichnis</param>
      /// <returns></returns>
      public bool Move(string storagenamesrc, string volpathsrc, string storagenamenewparent, string volpathnewparent) {
         /*
         DocumentFile srcdoc = GetExistingDocumentFile(volpathsrc);
         DocumentFile dstdoc = GetDocumentFile(volpathdst, true); // Zielverzeichnis (wird notfalls erzeugt)
         Android.Net.Uri srcuri = srcdoc.Uri;
         Android.Net.Uri srcparenturi = srcdoc.ParentFile.Uri;
         Android.Net.Uri dstparenturi = dstdoc.Uri;
         */
         DocumentFile dstdoc = getDocumentFile(storagenamesrc, volpathnewparent, true); // Zielverzeichnis (wird notfalls erzeugt)

         /*
         public static Uri moveDocument (ContentResolver content, 
                                         Uri sourceDocumentUri,            document with FLAG_SUPPORTS_MOVE
                                         Uri sourceParentDocumentUri,      parent document of the document to move
                                         Uri targetParentDocumentUri)      document which will become a new parent of the source document
         return the moved document, or null if failed.

         Moves the given document under a new parent.
         Added in API level 24; Deprecated in API level Q

         neu:
         ContentProviderClient provclient = Activity.ContentResolver.AcquireUnstableContentProviderClient(srcdoc.Uri.Authority);
         DocumentsContract.MoveDocument(provclient, srcdoc.Uri, srcparenturi.Uri, dstparentdoc.Uri);

               intern:
               Bundle inbundle = new Bundle();
               inbundle.PutParcelable(DocumentsContract.EXTRA_URI, srcdoc.Uri);                    "uri"
               inbundle.PutParcelable(DocumentsContract.EXTRA_PARENT_URI, srcparenturi.Uri);       "parentUri"
               inbundle.PutParcelable(DocumentsContract.EXTRA_TARGET_URI, dstparentdoc.Uri);       "android.content.extra.TARGET_URI"
               Bundle outbundle = provclient.Call(METHOD_MOVE_DOCUMENT, null, inbundle);           "android:moveDocument"
               return outbundle.GetParcelable(DocumentsContract.EXTRA_URI);                        "uri"
         */
         global::Android.Net.Uri newuri = DocumentsContract.MoveDocument(Activity.ContentResolver,
                                                                         getDocumentUriUsingTree(storagenamesrc, volpathsrc),
                                                                         getDocumentUriUsingTree(storagenamenewparent, Path.GetDirectoryName(volpathsrc)),
                                                                         dstdoc.Uri);

         System.Diagnostics.Debug.WriteLine("AndroidMove(" + storagenamesrc + ", " + volpathsrc + ", " + storagenamenewparent + ", " + volpathnewparent + ") = " + (newuri != null).ToString());

         return newuri != null;
      }

      /// <summary>
      /// liefert selbst innerhalb des secondary ext. storage bei Oreo eine Exception: "Copy not supported"
      /// <para>Das scheint eine Exception der abstrakten Klasse DocumentProvider zu sein, d.h. die Copy-Funktion wurd in der konkreten Klasse nicht implemetiert.
      /// Das Flag FLAG_SUPPORTS_COPY für Dokumente fehlt (deswegen).</para>
      /// </summary>
      /// <param name="storagenamesrc"></param>
      /// <param name="volpathsrc"></param>
      /// <param name="storagenamenewparent"></param>
      /// <param name="volpathnewparent"></param>
      /// <returns></returns>
      public bool Copy(string storagenamesrc, string volpathsrc, string storagenamenewparent, string volpathnewparent) {
         DocumentFile srcdoc = GetExistingDocumentFile(storagenamesrc, volpathsrc);
         DocumentFile dstparentdoc = getDocumentFile(storagenamenewparent, volpathnewparent, true); // Zielverzeichnis (wird notfalls erzeugt)
         /*    Added in API level 24; Nougat, 7.0, Deprecated in API level Q

               public static Uri copyDocument (ContentResolver content, 
                                               Uri sourceDocumentUri,            Document mit FLAG_SUPPORTS_COPY
                                               Uri targetParentDocumentUri)

         neu:
         ContentProviderClient provclient = Activity.ContentResolver.AcquireUnstableContentProviderClient(srcdoc.Uri.Authority);
         DocumentsContract.CopyDocument(provclient, srcdoc.Uri, dstparentdoc.Uri);

               intern:
               Bundle inbundle = new Bundle();
               inbundle.PutParcelable(DocumentsContract.EXTRA_URI, srcdoc.Uri);                    "uri"
               inbundle.PutParcelable(DocumentsContract.EXTRA_TARGET_URI, dstparentdoc.Uri);       "android.content.extra.TARGET_URI"
               Bundle outbundle = provclient.Call(METHOD_COPY_DOCUMENT, null, inbundle);           "android:copyDocument"
               return outbundle.GetParcelable(DocumentsContract.EXTRA_URI);                        "uri"
         */
         global::Android.Net.Uri newuri = DocumentsContract.CopyDocument(Activity.ContentResolver,
                                                                         srcdoc.Uri,
                                                                         dstparentdoc.Uri);

         System.Diagnostics.Debug.WriteLine("AndroidCopy(" + storagenamesrc + ", " + volpathsrc + ", " + storagenamenewparent + ", " + volpathnewparent + ") = " + (newuri != null).ToString());

         return newuri != null;
      }

      #endregion

      #region private Methoden

#if DEBUG
      public
#endif
      /// <summary>
      /// liefert das DocumentFile für eine Datei oder ein Verzeichnis (API level 14)
      /// <para>Wenn das Objekt noch nicht ex., wird es erzeugt. Auch ein notwendiger Pfad wird vollständig erzeugt.</para>
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <param name="volpath">abs. Pfad im Volume</param>
      /// <param name="isDirectory">Pfad bezieht sich auf eine Datei oder ein Verzeichnis</param>
      /// <returns></returns>
      DocumentFile getDocumentFile(string storagename, string volpath, bool isDirectory) {
         global::Android.Net.Uri rooturi = getTreeDocumentUri(storagename);
         DocumentFile document = DocumentFile.FromTreeUri(Activity.ApplicationContext, rooturi);

         string[] pathelems = volpath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
         for (int i = 0; document != null && i < pathelems.Length; i++) {
            DocumentFile nextDocument = document.FindFile(pathelems[i]);
            if (nextDocument == null) {
               if ((i < pathelems.Length - 1) || isDirectory) {
                  nextDocument = document.CreateDirectory(pathelems[i]);
               } else {
                  nextDocument = document.CreateFile("", pathelems[i]);
               }
            }
            document = nextDocument;
         }

         System.Diagnostics.Debug.WriteLine("AndroidGetDocumentFile(" + storagename + ", " + volpath + ", " + isDirectory.ToString() + ") = " + (document != null && (isDirectory == document.IsDirectory)).ToString());

         return document != null ?
                  (isDirectory == document.IsDirectory ? document : null) :
                  null;
      }

#if DEBUG
      public
#endif
      /// <summary>
      /// z.B.: //com.android.externalstorage.documents/tree/19F4-0903:abc/def/ghi.txt aus /abc/def/ghi.txt (API level 21 / Lollipop / 5.0)
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <param name="volpath">abs. Pfad im Volume</param>
      /// <returns></returns>
      global::Android.Net.Uri getTreeDocumentUri(string storagename, string volpath = "") {
         if (volpath.Length > 0 &&
             volpath[0] == '/')
            volpath = volpath.Substring(1);

         // Build URI representing access to descendant documents of the given Document#COLUMN_DOCUMENT_ID. (API level 21 / Lollipop / 5.0)
         return DocumentsContract.BuildTreeDocumentUri(AUTHORITY_EXTERNALSTORAGE_DOCUMENTS, storagename + ":" + volpath);
      }

      /// <summary>
      /// z.B.: //com.android.externalstorage.documents/tree/19F4-0903:/document/19F4-0903:abc/def/ghi.txt aus /abc/def/ghi.txt (API level 21 / Lollipop / 5.0)
      /// </summary>
      /// <param name="storagename">z.B. "primary" oder "19F4-0903"</param>
      /// <param name="volpath">abs. Pfad im Volume</param>
      /// <returns></returns>
      global::Android.Net.Uri getDocumentUriUsingTree(string storagename, string volpath) {
         if (volpath.Length > 0 &&
             volpath[0] == '/')
            volpath = volpath.Substring(1);

         // Build URI representing the target Document#COLUMN_DOCUMENT_ID in a document provider. (API level 21 / Lollipop / 5.0)
         return DocumentsContract.BuildDocumentUriUsingTree(getTreeDocumentUri(storagename), storagename + ":" + volpath);
      }

      #endregion

      #region public-Methoden für Permissions

      /* 
         * takePersistableUriPermission
         * Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION
         * 
         * "Persistente" Permissons können NICHT im Manifest angefordert werden, sondern nur zur Laufzeit.
         * 
         * Persist permissions
         * When your app opens a file for reading or writing, the system gives your app a URI permission grant for that file, which lasts until the user's device restarts. 
         * But suppose your app is an image-editing app, and you want users to be able to access the last 5 images they edited, directly from your app. If the user's device 
         * has restarted, you'd have to send the user back to the system picker to find the files, which is obviously not ideal.
         * To prevent this from happening, you can persist the permissions that the system gives your app. Effectively, your app "takes" the persistable URI permission grant 
         * that the system is offering. This gives the user continued access to the files through your app, even if the device has been restarted.
         * 

         ActivityCompat.requestPermissions(Activity activity, String[] permissions, int requestCode)              Requests permissions to be granted to this application. 
         ActivityCompat.shouldShowRequestPermissionRationale(Activity activity, String permission)                Gets whether you should show UI with rationale for requesting a permission.
         ActivityCompat.checkSelfPermission(Context context, String permission)                                   Determine whether you have been granted a particular permission.
       
       */

      /// <summary>
      /// versucht die Schreib- und Leserechte für dieses Volume zu setzen (API level 19 / Kitkat / 4.4) (nur für secondary external storage nötig)
      /// </summary>
      /// <param name="storagename"></param>
      /// <param name="volidx">Index zum Volumenamen</param>
      /// <returns>true, wenn die Rechte ex.</returns>
      public bool SetPersistentPermissions(string storagename, int volidx) {
         /*
            https://developer.xamarin.com/api/type/Android.Content.ActivityFlags/

            GrantReadUriPermission	
            If set, the recipient of this Intent will be granted permission to perform read operations on the Uri in the Intent's data and any URIs specified in its ClipData. 
            When applying to an Intent's ClipData, all URIs as well as recursive traversals through data or other ClipData in Intent items will be granted; only the grant flags 
            of the top-level Intent are used. 

            GrantWriteUriPermission	
            If set, the recipient of this Intent will be granted permission to perform write operations on the Uri in the Intent's data and any URIs specified in its ClipData. 
            When applying to an Intent's ClipData, all URIs as well as recursive traversals through data or other ClipData in Intent items will be granted; only the grant flags 
            of the top-level Intent are used. 

            GrantPersistableUriPermission
            When combined with FLAG_GRANT_READ_URI_PERMISSION and/or FLAG_GRANT_WRITE_URI_PERMISSION, the URI permission grant can be persisted across device reboots until 
            explicitly revoked with Context.revokeUriPermission(Uri, int). 
            This flag only offers the grant for possible persisting; the receiving application must call ContentResolver.takePersistableUriPermission(Uri, int) to actually persist.

            GrantPrefixUriPermission
            When combined with FLAG_GRANT_READ_URI_PERMISSION and/or FLAG_GRANT_WRITE_URI_PERMISSION, the URI permission grant applies to any URI that is a prefix match against 
            the original granted URI. (Without this flag, the URI must match exactly for access to be granted.) 
            Another URI is considered a prefix match only when scheme, authority, and all path segments defined by the prefix are an exact match. 
          */
         ActivityFlags flags = ActivityFlags.GrantReadUriPermission |
                               ActivityFlags.GrantWriteUriPermission;

         bool ok = false;
         try {

            if (Build.VERSION.SdkInt < BuildVersionCodes.R) {
               global::Android.Net.Uri voluri = getTreeDocumentUri(storagename);

               /* API level 19 / Kitkat / 4.4
                *
                * Take a persistable URI permission grant that has been offered. 
                * Once taken, the permission grant will be remembered across device reboots. Only URI permissions granted with Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION can be 
                * persisted. If the grant has already been persisted, taking it again will touch UriPermission.PersistedTime.
                * 
                * Value is either 0 or combination of FLAG_GRANT_READ_URI_PERMISSION or FLAG_GRANT_WRITE_URI_PERMISSION.
                */

               //Activity.GrantUriPermission(Activity.PackageName, voluri, flags);
               // Exception: SetPersistentPermissions(3563 - 3766) Exception: UID 10134 does not have permission to content://com.android.externalstorage.documents/tree/3563-3766%3A [user 0]; you could obtain access using ACTION_OPEN_DOCUMENT or related APIs

               Activity.ContentResolver.TakePersistableUriPermission(voluri, flags);
               // Exception z.B.: No persistable permission grants found for UID 10084 and Uri 0 @ content://com.android.externalstorage.documents/tree/19F4-0903:

               /* Return list of all URI permission grants that have been persisted by the calling app. That is, the returned permissions have been granted to the calling app. 
                * Only persistable grants taken with ContentResolver.TakePersistableUriPermission(Uri,ActivityFlags) are returned.
                * Note: Some of the returned URIs may not be usable until after the user is unlocked.
                */
               foreach (UriPermission perm in Activity.ContentResolver.PersistedUriPermissions) {
                  if (voluri.Equals(perm.Uri))
                     ok = true;
               }
            } else {
               ok = global::Android.OS.Environment.IsExternalStorageManager; // Ex. die Rechte? (Intent ist nicht nötig)
            }
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception in SafStorageHelper.SetPersistentPermissions(" + storagename + "): " + ex.Message);
         }
         return ok;
      }

      /// <summary>
      /// entfernt die persistenten Schreib- und Leserechte (API level 19 / Kitkat / 4.4) (nur für secondary external storage sinnvoll)
      /// </summary>
      /// <param name="storagename"></param>
      /// <param name="volidx">Index zum Volumenamen</param>
      public void ReleasePersistentPermissions(string storagename, int volidx) {
         ActivityFlags flags = ActivityFlags.GrantReadUriPermission |
                               ActivityFlags.GrantWriteUriPermission;
         global::Android.Net.Uri voluri = getTreeDocumentUri(storagename);
         try {
            /* Relinquish a persisted URI permission grant. The URI must have been previously made persistent with ContentResolver.TakePersistableUriPermission(Uri,ActivityFlags). 
             * Any non-persistent grants to the calling package will remain intact.
             */
            Activity.ContentResolver.ReleasePersistableUriPermission(voluri, flags);
         } catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine("Exception in SafStorageHelper.ReleasePersistentPermissions(" + storagename + "): " + ex.Message);
         }
      }

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen (API level 24 / Nougat / 7.0; deprecated in API level Q) (nur für secondary external storage sinnvoll)
      /// </summary>
      /// <param name="storagevolumeno">Nummer des StorageVolumes; min. 2</param>
      /// <param name="volidx">Index zum Volumenamen</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      public void Ask4PersistentPermisson(string storagename, int volidx, int requestid = 12345) {
         Intent intent = getAccessIntent(storagename, volidx);
         if (intent != null)
            Activity.StartActivityForResult(intent, requestid);
      }

      /// <summary>
      /// führt zur Anfrage an den Nutzer, ob die Permissions erteilt werden sollen und wartet auf das Ergebnnis 
      /// (API level 24 / Nougat / 7.0; deprecated in API level Q) (nur für secondary external storage sinnvoll)
      /// <para>fkt. nur, wenn in der Activity die Funktion MyStartActivityForResultAsync() ex.</para>
      /// </summary>
      /// <param name="storagevolumeno">Nummer des StorageVolumes; min. 2</param>
      /// <param name="volidx">Index zum Volumenamen</param>
      /// <param name="requestid">ID für OnActivityResult() der activity</param>
      /// <param name="intent4tests">NUR FÜR TESTs</param>
      async public Task<bool> Ask4PersistentPermissonAndWait4Result(string storagename, int volidx, int requestid = 12345, Intent intent4tests = null) {
         Intent intent = intent4tests ?? getAccessIntent(storagename, volidx);
         return intent != null ?
            await ask4PersistentPermissonAndWait4Result(intent, requestid) :
            false;
      }

      #endregion

      #region private-Methoden für Permissions

      async Task<bool> ask4PersistentPermissonAndWait4Result(Intent intent, int requestid) {
         if (intent != null) {
            if (intent != null) {

               Type activitytype = Activity.GetType();
               System.Reflection.MethodInfo method = activitytype.GetMethod("MyStartActivityForResultAsync");
               if (method != null) {
                  Tuple<Result, Intent> result = await (Task<Tuple<Result, Intent>>)method.Invoke(Activity, new object[] { intent, requestid });
                  return result.Item1 == Result.Ok;
               }
               throw new Exception("Method 'MyStartActivityForResultAsync' in Activity is not available.");
            }
         }
         return false;
      }

      /// <summary>
      /// Intent, um die persistenten Permissions anzufordern (fkt. nur bis Android P)
      /// </summary>
      /// <param name="storagename"></param>
      /// <param name="volidx">Index zum Volumenamen</param>
      /// <returns></returns>
      Intent getAccessIntent(string storagename, int volidx) {
         if (Build.VERSION.SdkInt < BuildVersionCodes.Q)             // vor Android Q

            return getAccessIntent_PreQ(volidx);

         else if (Build.VERSION.SdkInt == BuildVersionCodes.Q)

            return getAccessIntent_Q(storagename);

         else

            return getAccessIntent_R();
      }

#if DEBUG
      public
#endif
      Intent getAccessIntent_PreQ(int volidx) {
         Java.Lang.Object ss = Application.Context.GetSystemService(Context.StorageService);   // STORAGE_SERVICE  "storage" 
         StorageManager sm = ss as StorageManager;
         Intent intent = null;

         //// -- NO WAY
         //Android.Net.Uri uri;
         //uri = null;
         //uri = GetTreeDocumentUri("primary");
         //uri = GetDocumentUriUsingTree("primary", global::Android.OS.Environment.DirectoryMusic);
         //intent = new Intent(Intent.ActionOpenDocumentTree, uri);   // Constant Value: "android.intent.action.OPEN_DOCUMENT_TREE" 
         //if (intent == null)
         //   return false;
         //intent.AddCategory(Intent.CategoryOpenable);
         //intent.SetType("*/*");

         //// -- OK, öffnet Android-Auswahl
         //intent = new Intent(Intent.ActionOpenDocument);       // Constant Value: "android.intent.action.OPEN_DOCUMENT" 
         //intent.AddCategory(Intent.CategoryOpenable);
         //intent.SetType("*/*");

         //// -- OK
         //string pextdir = global::Android.OS.Environment.DirectoryMusic;
         //pextdir = global::Android.OS.Environment.DirectoryDocuments;
         //intent = sm.StorageVolumes[0].CreateAccessIntent(pextdir);

         //// -- NO WAY
         //intent = new Intent();
         //intent.AddFlags(ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantPrefixUriPermission | ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
         //Android.Net.Uri voluri = GetTreeDocumentUri("primary");
         //intent.SetData(voluri);


         /*

        StorageVolume
        -------------

        public Intent createAccessIntent (String directoryName)                 Added in API level 24 (N), Deprecated in API level 29 (Q)

        This method was deprecated in API level 29.
        Callers should migrate to using Intent#ACTION_OPEN_DOCUMENT_TREE instead. 
        Launching this Intent on devices running Build.VERSION_CODES.Q or higher, will immediately finish with a result code of Activity.RESULT_CANCELED.           !!!! MIST !!!!

        Builds an intent to give access to a standard storage directory or entire volume after obtaining the user's approval.

        When invoked, the system will ask the user to grant access to the requested directory (and its descendants). The result of the request will be returned 
        to the activity through the onActivityResult method. 

        To gain access to descendants (child, grandchild, etc) documents, use DocumentsContract#buildDocumentUriUsingTree(Uri, String), or 
        DocumentsContract#buildChildDocumentsUriUsingTree(Uri, String) with the returned URI.

        If your application only needs to store internal data, consider using Context.getExternalFilesDirs, Context#getExternalCacheDirs(), 
        or Context#getExternalMediaDirs(), which require no permissions to read or write.

        Access to the entire volume is only available for non-primary volumes (for the primary volume, apps can use the 
        Manifest.permission.READ_EXTERNAL_STORAGE and Manifest.permission.WRITE_EXTERNAL_STORAGE permissions) and should be used with caution, 
        since users are more likely to deny access when asked for entire volume access rather than specific directories.
         */

         // CreateAccessIntent(null) ist veraltet. Intern erfolgt in StorageVolume:
         intent = new Intent(ANDROID_OS_STORAGE_ACTION_OPEN_EXTERNAL_DIRECTORY);
         intent.PutExtra(StorageVolume.ExtraStorageVolume, sm.StorageVolumes[volidx]);
         intent.PutExtra(ANDROID_OS_STORAGE_EXTRA_DIRECTORY_NAME, null as string);

         return intent;
      }

#if DEBUG
      public
#endif
      Intent getAccessIntent_Q(string storagename) {
         // Dieser Intent führt leider auch zum Öffnen des Android-Explorers:
         Intent intent = new Intent(Intent.ActionOpenDocumentTree);
         intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

         // startet den Android-Explorer mit der SDCARD:
         /* EXTRA_INITIAL_URI
            Sets the desired initial location visible to user when file chooser is shown.

            Applicable to Intent with actions:

                Intent#ACTION_OPEN_DOCUMENT
                Intent#ACTION_CREATE_DOCUMENT
                Intent#ACTION_OPEN_DOCUMENT_TREE

            Location should specify a document URI or a tree URI with document ID. If this URI identifies a non-directory, document navigator will attempt to 
            use the parent of the document as the initial location.

            The initial location is system specific if this extra is missing or document navigator failed to locate the desired initial location.
          */
         intent.PutExtra(DocumentsContract.ExtraInitialUri, CONTENT_EXTERNALSTORAGE_TREE + storagename + "%3A");

         return intent;
      }

#if DEBUG
      public
#endif
      Intent getAccessIntent_R() {
         Intent intent = null;

         /* Access to the entire volume is only available for non-primary volumes (for the primary volume, apps can use the 
          * Manifest.permission.READ_EXTERNAL_STORAGE and Manifest.permission.WRITE_EXTERNAL_STORAGE permissions) and should be used with caution, 
          * since users are more likely to deny access when asked for entire volume access rather than specific directories.
          * 
          * directoryName        ... or null to request access to the entire volume
          * 
          * ... deprecated in API level Q. Callers should migrate to using Intent#ACTION_OPEN_DOCUMENT_TREE instead ...
          * 
          * An StartActivityForResult() wird mit CreateAccessIntent() die Action "android.os.storage.action.OPEN_EXTERNAL_DIRECTORY" geliefert.
          * Bei einer älteren Version muss vermutlich der Anroid-interne Auswahldialog für das Rootverzeichnis verwendet werden.
          */


         //string directory = storagevolumeno == 0 ?
         //                        global::Android.OS.Environment.DirectoryDocuments :
         //                        null;
         //return sm.StorageVolumes[storagevolumeno].CreateAccessIntent(directory);

         //return sm.StorageVolumes[storagevolumeno].CreateOpenDocumentTreeIntent();      // <- öffnet nur den Android-Auswahldialog

         /*
         Request All files access

         An app can request All files access from the user by doing the following:

            Declare the MANAGE_EXTERNAL_STORAGE permission in the manifest.
            Use the ACTION_MANAGE_ALL_FILES_ACCESS_PERMISSION intent action to direct users to a system settings page where they can enable the 
            following option for your app: Allow access to manage all files.

         To determine whether your app has been granted the MANAGE_EXTERNAL_STORAGE permission, call Environment.isExternalStorageManager().
          */

         /*
            Intent intent = new Intent(ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION, 
                                       Uri.parse("package:" + BuildConfig.APPLICATION_ID));

            Uri uri = Uri.parse("package:" + BuildConfig.APPLICATION_ID);
            Intent intent = new Intent(Settings.ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION, uri);

            Intent intent = new Intent();
            intent.setAction(Settings.ACTION_MANAGE_ALL_FILES_ACCESS_PERMISSION);

          */


         /*
            ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION
            Activity Action: Show screen for controlling if the app specified in the data URI of the intent can manage external storage.

            Launching the corresponding activity requires the permission Manifest.permission#MANAGE_EXTERNAL_STORAGE.

            In some cases, a matching Activity may not exist, so ensure you safeguard against this.

            Input: The Intent's data URI MUST specify the application package name whose ability of managing external storage you want to control. 
            For example "package:com.my.app". 
          */
         //global::Android.Net.Uri uri = global::Android.Net.Uri.Parse("package:" + Activity.ApplicationContext.PackageName);   // EncodedSchemeSpecificPart	"com.companyname.test4fsoftutils"	string
         //intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission, uri);
         // Exception

         /*
            ACTION_MANAGE_ALL_FILES_ACCESS_PERMISSION
            Added in API level 30

            public static final String ACTION_MANAGE_ALL_FILES_ACCESS_PERMISSION

            Activity Action: Show screen for controlling which apps have access to manage external storage.

            In some cases, a matching Activity may not exist, so ensure you safeguard against this.

            If you want to control a specific app's access to manage external storage, use ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION instead. 
          */

         if (!global::Android.OS.Environment.IsExternalStorageManager) // sonst ex. die Rechte schon
            intent = new Intent(Settings.ActionManageAllFilesAccessPermission);
         // 		resultCode	Android.App.Result.Canceled	Android.App.Result
         // ABER: Recht wurde gesetzt
         // Nur hierüber kann das Recht vom Nutzer auch wieder zurückgesetzt werden.
         // z.B. Einstellunge -> Datenschutz -> Berechtigungsmanager -> Dateien und Medien -> dann bestimmte App auswählen usw.
         return intent;
      }

      #endregion

   }
}