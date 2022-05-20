using Android.Content;
using Android.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

// Thanks to Anna Domashych:
// https://medium.com/@anna.domashych/selectable-read-only-multiline-text-field-in-xamarin-forms-69d09276d580

[assembly: ExportRenderer(typeof(FSofTUtils.Xamarin.Control.SelectableLabel), typeof(FSofTUtils.Android.Controls.SelectableLabelRenderer))]
namespace FSofTUtils.Android.Controls {
   public class SelectableLabelRenderer : EditorRenderer {

      public SelectableLabelRenderer(Context context) :
         base(context) {
      }

      protected override void OnElementChanged(ElementChangedEventArgs<Editor> e) {
         base.OnElementChanged(e);

         if (Control == null)
            return;

         Control.Background = null;
         Control.SetPadding(0, 0, 0, 0);
         Control.ShowSoftInputOnFocus = false;
         Control.SetTextIsSelectable(true);
         Control.CustomSelectionActionModeCallback = new CustomSelectionActionModeCallback();
         Control.CustomInsertionActionModeCallback = new CustomInsertionActionModeCallback();
      }

      private class CustomInsertionActionModeCallback : Java.Lang.Object, ActionMode.ICallback {
         public bool OnCreateActionMode(ActionMode mode, IMenu menu) => false;

         public bool OnActionItemClicked(ActionMode m, IMenuItem i) => false;

         public bool OnPrepareActionMode(ActionMode mode, IMenu menu) => true;

         public void OnDestroyActionMode(ActionMode mode) { }
      }

      private class CustomSelectionActionModeCallback : Java.Lang.Object, ActionMode.ICallback {

         private const int CopyId = global::Android.Resource.Id.Copy;

         public bool OnActionItemClicked(ActionMode m, IMenuItem i) => false;

         public bool OnCreateActionMode(ActionMode mode, IMenu menu) => true;

         public void OnDestroyActionMode(ActionMode mode) { }

         public bool OnPrepareActionMode(ActionMode mode, IMenu menu) {
            try {
               var copyItem = menu.FindItem(CopyId);
               var title = copyItem.TitleFormatted;
               menu.Clear();
               menu.Add(0, CopyId, 0, title);
            } catch {
               // ignored
            }

            return true;
         }
      }

      protected override FormsEditText CreateNativeControl() =>
         new CustomEditText(Context);

   }
}