using FSofTUtils.Xamarin.DependencyTools;
using Xamarin.Forms;

[assembly: Dependency(typeof(FSofTUtils.Android.DependencyTools.DepTools))]
namespace FSofTUtils.Android.DependencyTools {

   /// <summary>
   /// Sammlung von Funktionen, die über <see cref="FSofTUtils.Xamarin.DependencyTools.IDepTools"/> aufgerufen werden können
   /// </summary>
   public class DepTools : FSofTUtils.Xamarin.DependencyTools.IDepTools {

      public StorageHelper GetStorageHelper(object androidactivity) {
         return new StorageHelperAndroid(androidactivity);
      }


   }

}