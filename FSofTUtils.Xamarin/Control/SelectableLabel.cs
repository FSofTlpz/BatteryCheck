using Xamarin.Forms;

// Thanks to Anna Domashych:
// https://medium.com/@anna.domashych/selectable-read-only-multiline-text-field-in-xamarin-forms-69d09276d580

namespace FSofTUtils.Xamarin.Control {

   /* Verbindung zum Renderer im Android-Teil über:
     
         [assembly: ExportRenderer(typeof(SelectableLabel), typeof(SelectableLabelRenderer))]
         namespace GpxToolExt.Droid.Controls {
            public class SelectableLabelRenderer : EditorRenderer {

      In der XAML-Datei noch:

         <ContentPage ... xmlns:fux="clr-namespace:FSofTUtils.Xamarin.Control;assembly=FSofTUtils.Xamarin"

      Damit kann ein Objekt vom Typ SelectableLabel (auf Basis Editor) verwendet werden.
   */
   public class SelectableLabel : Editor {

   }

}
