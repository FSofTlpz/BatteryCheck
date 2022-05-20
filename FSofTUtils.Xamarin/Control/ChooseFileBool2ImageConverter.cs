using System;
using System.Globalization;
using Xamarin.Forms;

namespace FSofTUtils.Xamarin.Control {
  public class ChooseFileBool2ImageConverter : IValueConverter {

      /// <summary>
      /// liefert ein <see cref="ImageSource"/> aus <see cref="ChooseFile"/> entsprechend des boolschen Wertes
      /// </summary>
      /// <param name="value"></param>
      /// <param name="targetType"></param>
      /// <param name="parameter"></param>
      /// <param name="culture"></param>
      /// <returns></returns>
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
         if (targetType != typeof(ImageSource))
            throw new InvalidOperationException("The target must be a ImageSource");
         if (!(parameter is ChooseFile))
            throw new InvalidOperationException("The parameter must be ChooseFile");

         ChooseFile chooseFile = parameter as ChooseFile;
         return (bool)value ?
                     chooseFile.ImageFolder :
                     chooseFile.ImageFile;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
         return null;   // Optional: throw new NotSupportedException();
      }
   }
}
