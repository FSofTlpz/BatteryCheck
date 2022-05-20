//#define WITH_DEBUG_INFO

using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FSofTUtils.Xamarin.Control {

   [XamlCompilation(XamlCompilationOptions.Compile)]

   public partial class TimeWheelView : ContentView {

      const int DEFAULT_CONTROLHEIGHT = 150;
      const int DEFAULT_CONTROLWIDTH = 300;
      const int DEFAULT_FONTSIZE = 25;

      #region Events

      public class TimeChangedEventArgs {
         public readonly TimeSpan NewTimeSpan;

         public TimeChangedEventArgs(TimeSpan ts) {
            NewTimeSpan = ts;
         }
      }

      public delegate void TimeChangedEventDelegate(object sender, TimeChangedEventArgs args);

      /// <summary>
      /// die angezeigte Zeit hat sich verändert
      /// </summary>
      public event TimeChangedEventDelegate TimeChangedEvent;


      public class TimeSettableStatusChangedEventArgs {
         public readonly bool IsSettable;

         public TimeSettableStatusChangedEventArgs(bool issettable) {
            IsSettable = issettable;
         }
      }

      public delegate void TimeSettableStatusChangedEventDelegate(object sender, TimeSettableStatusChangedEventArgs args);

      /// <summary>
      /// Kann die Zeit gesetzt werden?
      /// </summary>
      public event TimeSettableStatusChangedEventDelegate TimeSettableStatusChangedEvent;

      #endregion

      #region Binding-Vars

      #region  Binding-Var BackColor

      public static readonly BindableProperty BackColorProperty = BindableProperty.Create(
         "BackColor",
         typeof(Color),
         typeof(TimeWheelView),
         Color.LightSalmon);

      /// <summary>
      /// Hintergrundfarbe des Controls
      /// </summary>
      public Color BackColor {
         get => (Color)GetValue(BackColorProperty);
         set => SetValue(BackColorProperty, value);
      }

      #endregion

      #region  Binding-Var ItemColor

      public static readonly BindableProperty ItemColorProperty = BindableProperty.Create(
         "ItemColor",
         typeof(Color),
         typeof(TimeWheelView),
         Color.LightSkyBlue);

      /// <summary>
      /// Hintergrundfarbe der Items
      /// </summary>
      public Color ItemColor {
         get => (Color)GetValue(ItemColorProperty);
         set => SetValue(ItemColorProperty, value);
      }

      #endregion

      #region Binding-Var ControlWidth

      public static readonly BindableProperty ControlWidthProperty = BindableProperty.Create("ControlWidth", typeof(int), typeof(TimeWheelView), DEFAULT_CONTROLWIDTH);

      /// <summary>
      /// Gesamtbreite des Controls
      /// </summary>
      public int ControlWidth {
         get => (int)GetValue(ControlWidthProperty);
         set => SetValue(ControlWidthProperty, value);
      }

      #endregion

      #region Binding-Var ControlHeight

      public static readonly BindableProperty ControlHeightProperty = BindableProperty.Create(
         "ControlHeight",
         typeof(int),
         typeof(TimeWheelView),
         DEFAULT_CONTROLHEIGHT,
         propertyChanged: OnControlHeightChanged);

      /// <summary>
      /// Höhe des Controls
      /// </summary>
      public int ControlHeight {
         get => (int)GetValue(ControlHeightProperty);
         set => SetValue(ControlHeightProperty, value);
      }

      static void OnControlHeightChanged(BindableObject bindable, object oldValue, object newValue) {
         if (bindable is TimeWheelView) {
            //TimeWheelView twv = bindable as TimeWheelView;
            //twv.changePeekAreaInsets((int)newValue - (int)oldValue);
         }
      }

      #endregion

      #region Binding-Var ItemFontSize

      public static readonly BindableProperty ItemFontSizeProperty = BindableProperty.Create(
         "ItemFontSize",
         typeof(int),
         typeof(TimeWheelView),
         DEFAULT_FONTSIZE,
         propertyChanged: OnItemFontSizeChanged);

      /// <summary>
      /// Font-Größe für die Items
      /// </summary>
      public int ItemFontSize {
         get => (int)GetValue(ItemFontSizeProperty);
         set => SetValue(ItemFontSizeProperty, value);
      }

      static void OnItemFontSizeChanged(BindableObject bindable, object oldValue, object newValue) {
         if (bindable is TimeWheelView) {
            TimeWheelView twv = bindable as TimeWheelView;
            twv.WheelViewHour.ItemFontSize = (int)newValue;
            twv.WheelViewMinute.ItemFontSize = (int)newValue;
            twv.WheelViewSecond.ItemFontSize = (int)newValue;
         }
      }

      #endregion

      #endregion


      /// <summary>
      /// akt. Zeit
      /// </summary>
      public TimeSpan TimeSpan {
         get {
            return new TimeSpan(WheelViewHour.Value,
                                WheelViewMinute.Value,
                                WheelViewSecond.Value);
         }
         set {
            WheelViewHour.Value = value.Hours;
            WheelViewMinute.Value = value.Minutes;
            WheelViewSecond.Value = value.Seconds;
         }
      }


      public TimeWheelView() {
         InitializeComponent();

         WheelViewHour.ValueChangedEvent += WheelView_ValueChangedEvent;
         WheelViewMinute.ValueChangedEvent += WheelView_ValueChangedEvent;
         WheelViewSecond.ValueChangedEvent += WheelView_ValueChangedEvent;
      }

      private void WheelView_ValueChangedEvent(object sender, WheelView.ValueChangedEventArgs args) {
         OnTimeChanged();
      }

      public virtual void OnTimeChanged() {
         TimeChangedEvent?.Invoke(this, new TimeChangedEventArgs(TimeSpan));
      }

   }
}
