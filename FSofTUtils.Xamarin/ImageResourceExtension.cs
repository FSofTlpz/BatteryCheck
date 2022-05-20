using System;
using System.Reflection;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FSofTUtils.Xamarin {
   /// <summary>
   /// DO NOT FORGET THAT YOU NEED TO MAKE THE IMAGE FILES "Embedded Resource Files"
   /// </summary>
   [ContentProperty(nameof(Source))]
   public class ImageResourceExtension : IMarkupExtension {
      public string Source { get; set; }

      public object ProvideValue(IServiceProvider serviceProvider) {
         if (Source == null) 
            return null;

         // Do your translation lookup here, using whatever method you require
         var imageSource = ImageSource.FromResource(Source, typeof(ImageResourceExtension).GetTypeInfo().Assembly);

         return imageSource;
      }
   }

}
