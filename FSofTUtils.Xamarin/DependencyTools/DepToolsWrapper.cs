using Xamarin.Forms;

namespace FSofTUtils.Xamarin.DependencyTools {

   /// <summary>
   /// nur zum einfacheren Aufrufen der DependencyService-Funktionen aus <see cref="IDepTools"/>
   /// </summary>
   public class DepToolsWrapper {

      public static StorageHelper GetStorageHelper(object androidactivity) {
         return DependencyService.Get<IDepTools>().GetStorageHelper(androidactivity);
      }

   }
}
