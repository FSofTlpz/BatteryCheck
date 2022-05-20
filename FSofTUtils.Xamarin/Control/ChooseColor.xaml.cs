using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FSofTUtils.Xamarin.Control {

   /// <summary>
   /// einfaches Control zum Festlegen einer Farbe (4 Slider)
   /// </summary>
   [DesignTimeVisible(true)]
   [XamlCompilation(XamlCompilationOptions.Compile)]
   public partial class ChooseColor : ContentView {

      /// <summary>
      /// die Farbe wurde geändert
      /// </summary>
      public event EventHandler<EventArgs> ColorChanged;

 
      #region  Binding-Var BorderSize

      public static readonly BindableProperty BorderSizeProperty = BindableProperty.Create(
          nameof(BorderSize),
          typeof(double),
          typeof(ChooseColor),
          5.0);

      public double BorderSize {
         get => (double)GetValue(BorderSizeProperty);
         set => SetValue(BorderSizeProperty, value);
      }

      #endregion

      #region  Binding-Var SliderMargin

      public static readonly BindableProperty SliderMarginProperty = BindableProperty.Create(
         "SliderMargin",
         typeof(Thickness),
         typeof(ChooseColor),
         new Thickness(0, 0, 0, 0));

      public Thickness SliderMargin {
         get => (Thickness)GetValue(SliderMarginProperty);
         set => SetValue(SliderMarginProperty, value);
      }

      #endregion

      #region  Binding-Vars ColorComponentR, ColorComponentG, ColorComponentB, ColorComponentA

      public static readonly BindableProperty ColorComponentRProperty = BindableProperty.Create(
          nameof(ColorComponentR),
          typeof(double),
          typeof(ChooseColor),
          0.0,
          propertyChanged: changeColorComponent);

      public double ColorComponentR {
         get => (double)GetValue(ColorComponentRProperty);
         set {
            if (ColorComponentR != value)
               SetValue(ColorComponentRProperty, value);
         }
      }

      public static readonly BindableProperty ColorComponentGProperty = BindableProperty.Create(
          nameof(ColorComponentG),
          typeof(double),
          typeof(ChooseColor),
          0.0,
          propertyChanged: changeColorComponent);

      public double ColorComponentG {
         get => (double)GetValue(ColorComponentGProperty);
         set {
            if (ColorComponentG != value)
               SetValue(ColorComponentGProperty, value);
         }
      }

      public static readonly BindableProperty ColorComponentBProperty = BindableProperty.Create(
          nameof(ColorComponentB),
          typeof(double),
          typeof(ChooseColor),
          0.0,
          propertyChanged: changeColorComponent);

      public double ColorComponentB {
         get => (double)GetValue(ColorComponentBProperty);
         set {
            if (ColorComponentB != value)
               SetValue(ColorComponentBProperty, value);
         }
      }

      public static readonly BindableProperty ColorComponentAProperty = BindableProperty.Create(
          nameof(ColorComponentA),
          typeof(double),
          typeof(ChooseColor),
          1.0,
          propertyChanged: changeColorComponent);

      public double ColorComponentA {
         get => (double)GetValue(ColorComponentAProperty);
         set {
            if (ColorComponentA != value)
               SetValue(ColorComponentAProperty, value);
         }
      }

      static void changeColorComponent(BindableObject bindable, object oldValue, object newValue) {
         var control = bindable as ChooseColor;
         if (control != null &&
             (double)oldValue != (double)newValue)
            control.colorComponentChanged();
      }

      #endregion

      #region  Binding-Var Color

      public static readonly BindableProperty ColorProperty = BindableProperty.Create(
           nameof(Color),
           typeof(Color),
           typeof(ChooseColor),
           Color.Red,
           propertyChanged: changeColor);

      bool setColorComponentIntern = false;

      public Color Color {
         get => (Color)GetValue(ColorProperty);
         set {
            setColorComponentIntern = true;
            ColorComponentR = value.R;
            ColorComponentG = value.G;
            ColorComponentB = value.B;
            ColorComponentA = value.A;
            setColorComponentIntern = false;

            BackgroundColor = Color;

            if (Color != value) {
               SetValue(ColorProperty, value);
               OnColorChanged(new EventArgs());
            }
         }
      }

      private static void changeColor(BindableObject bindable, object oldValue, object newValue) {
         var control = bindable as ChooseColor;
         if (control != null &&
             (Color)oldValue != (Color)newValue)
            control.Color = (Color)newValue;
      }

      #endregion


      public ChooseColor() {
         InitializeComponent();
      }

      /// <summary>
      /// eine einzelne Farbkomponente wurde verändert
      /// <para>(wird nur berücksichtigt, wenn es von einem Slider kam)</para>
      /// </summary>
      void colorComponentChanged() {
         if (!setColorComponentIntern)
            BackgroundColor = Color = new Color(ColorComponentR, ColorComponentG, ColorComponentB, ColorComponentA);
      }

      protected virtual void OnColorChanged(EventArgs e) {
         ColorChanged?.Invoke(this, e);
      }

   }
}