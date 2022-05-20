namespace FSofTUtils.Xamarin.DirectoryHelper {

   /// <summary>
   /// Daten eines Rootverzeichnisses
   /// </summary>
   class DirRoot {

      public enum RootState {
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

      public string Path { get; }
      public bool Primary { get; }
      public bool Removable { get; }
      public bool Emulated { get; }
      public RootState State { get; }
      public long TotalBytes { get; }
      public long AvailableBytes { get; }
      public string Description { get; }
      public string UUID { get; }

      public DirRoot(string path, 
                     bool primary, 
                     bool removable, 
                     bool emulated, 
                     RootState state, 
                     long totalbytes, 
                     long availablebytes,
                     string description,
                     string uuid) {
         Path = path;
         Primary = primary;
         Removable = removable;
         Emulated = emulated;
         State = state;
         TotalBytes = totalbytes;
         AvailableBytes = availablebytes;
         Description = description;
         UUID = uuid;
      }

      public override string ToString() {
         return string.Format("Primary {0}, Removable {1}, Emulated {2}, State {3}, Path {4}, TotalBytes {5}, AvailableBytes {6}", Primary, Removable, Emulated, State.ToString(), Path, TotalBytes, AvailableBytes);
      }

   }
}
