namespace FSofTUtils.Xamarin.DependencyTools {

   /// <summary>
   /// diverse Hilfsfunktionen, die den Dependency Service nutzen
   /// </summary>
   public interface IDepTools {

      /// <summary>
      /// Hilfefkt. für den (Android-)Speicher
      /// </summary>
      /// <returns></returns>
      StorageHelper GetStorageHelper(object androidactivity);


   }

}
