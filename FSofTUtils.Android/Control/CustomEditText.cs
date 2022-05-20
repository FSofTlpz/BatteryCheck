using Android.Content;
using Xamarin.Forms.Platform.Android;

// Thanks to Anna Domashych:
// https://medium.com/@anna.domashych/selectable-read-only-multiline-text-field-in-xamarin-forms-69d09276d580

namespace FSofTUtils.Android.Controls {
   public  class CustomEditText : FormsEditText {
      public CustomEditText(Context context) : 
         base(context) {
      }

      protected override void OnAttachedToWindow() {
         base.OnAttachedToWindow();

         Enabled = false;
         Enabled = true;
      }
   }
}