using FSofTUtils.Xamarin.DependencyTools;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FSofTUtils.Xamarin {
   public class Helper {

      /// <summary>
      /// Wenn das Element in ein ScrollView eingebettet ist, wird das ScrollView so verschoben, dass das Ende des Elementes sichtbar ist.
      /// </summary>
      /// <param name="el"></param>
      async public static void SrollToEnd(Element el) {
         Element parent = el.Parent;
         while (parent != null && !(parent is ScrollView)) {
            parent = parent.Parent;
         }
         if (parent != null &&
             parent is ScrollView)
            await (parent as ScrollView).ScrollToAsync(el, ScrollToPosition.End, false);
      }


      /// <summary>
      /// wartet eine Anwort ab (nur bei 'accept' wird true geliefert)
      /// </summary>
      /// <param name="title"></param>
      /// <param name="msg"></param>
      /// <param name="accept"></param>
      /// <param name="cancel"></param>
      /// <returns></returns>
      async public static Task<bool> MessageBox(global::Xamarin.Forms.Page page, string title, string msg, string accept, string cancel) {
         var answer = await page.DisplayAlert(title, msg, accept, cancel);
         return answer;
      }

      /// <summary>
      /// wartet eine Bestätigung ab
      /// </summary>
      /// <param name="title"></param>
      /// <param name="msg"></param>
      /// <param name="cancel"></param>
      /// <returns></returns>
      async public static Task MessageBox(global::Xamarin.Forms.Page page, string title, string msg, string cancel = "weiter") {
         await page.DisplayAlert(title, msg, cancel);
      }

      public static void PlaySound(string fullpath, float volume = 1F, bool looping = false) {
         DependencyService.Get<INativeSoundPicker>().PlayExclusiveNativeSound(fullpath, volume, looping);
      }

      // await DisplayPromptAsync
      // bool answer = await DisplayAlert

   }
}
