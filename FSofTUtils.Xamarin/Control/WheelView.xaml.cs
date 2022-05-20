//#define WITH_DEBUG_INFO

using System;
using System.Diagnostics;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FSofTUtils.Xamarin.Control {

   [XamlCompilation(XamlCompilationOptions.Compile)]

   /*
    * Im Prinzip ist das CarouselView gut geeignet, weil es mit der Loop-Eigenschaft den "Rundumlauf" gut realisiert.
    * Leider benötigt man zusätzlich zu ItemSpacing die PeekAreaInsets-Eigenschaft um die Items "zusammenzurücken".
    * Diese Eigenschaft bzw. deren Veränderung läßt sich nur relativ trickreich berechnen.
    * 
    */

   public partial class WheelView : ContentView {

      const int DEFAULT_FONTSIZE = 25;
      const double DEFAULT_PEAKAREAINSET = 126.3; // * 0.8203125;
      const double DEFAULT_GAPSIZE = 4;


      #region Events

      public class ValueChangedEventArgs {
         public readonly int Value;

         public ValueChangedEventArgs(int v) {
            Value = v;
         }
      }

      public delegate void ValueChangedEventDelegate(object sender, ValueChangedEventArgs args);

      /// <summary>
      /// der angezeigte Wert hat sich verändert
      /// </summary>
      public event ValueChangedEventDelegate ValueChangedEvent;

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

      #region Binding-Var ItemFontSize

      public static readonly BindableProperty ItemFontSizeProperty = BindableProperty.Create(
         "ItemFontSize",
         typeof(int),
         typeof(WheelView),
         DEFAULT_FONTSIZE);

      /// <summary>
      /// Font-Größe für die Items
      /// </summary>
      public int ItemFontSize {
         get => (int)GetValue(ItemFontSizeProperty);
         set => SetValue(ItemFontSizeProperty, value);
      }

      #endregion

      #region  Binding-Var GapSize

      public static readonly BindableProperty GapSizeProperty = BindableProperty.Create(
                                                                         "GapSize",             // the name of the bindable property
                                                                         typeof(double),        // the bindable property type
                                                                         typeof(WheelView),     // the parent object type
                                                                         DEFAULT_GAPSIZE,       // the default value for the property
                                                                         propertyChanged: OnGapSizeChanged); // Delegat, der ausgeführt wird, wenn der Wert geändert wurde
      /// <summary>
      /// Abstand zwischen den Items
      /// </summary>
      public double GapSize {
         get => (double)GetValue(GapSizeProperty);
         set => SetValue(GapSizeProperty, value);
      }

      static void OnMaxValueChanged(BindableObject bindable, object oldValue, object newValue) {
         if (bindable is WheelView) {
            WheelView twv = bindable as WheelView;
            twv.changePeekAreaInsets(-((int)newValue - (int)oldValue));
         }
      }

      #endregion

      #region  Binding-Var MaxValue

      public static readonly BindableProperty MaxValueProperty = BindableProperty.Create(
                                                                         "MaxValue",            // the name of the bindable property
                                                                         typeof(int),           // the bindable property type
                                                                         typeof(WheelView),     // the parent object type
                                                                         60,                    // the default value for the property
                                                                         propertyChanged: OnGapSizeChanged); // Delegat, der ausgeführt wird, wenn der Wert geändert wurde
      /// <summary>
      /// Abstand zwischen den Items
      /// </summary>
      public int MaxValue {
         get => (int)GetValue(MaxValueProperty);
         set => SetValue(MaxValueProperty, value);
      }

      static void OnGapSizeChanged(BindableObject bindable, object oldValue, object newValue) {
         if (bindable is WheelView) {
            WheelView wv = bindable as WheelView;
            wv.initItems(0, (int)newValue);
         }
      }

      #endregion

      #region Binding-Var PeekAreaInsets

      public static readonly BindableProperty PeekAreaInsetsProperty = BindableProperty.Create(
         "PeekAreaInsets",
         typeof(Thickness),
         typeof(TimeWheelView),
         new Thickness(0, DEFAULT_PEAKAREAINSET, 0, DEFAULT_PEAKAREAINSET));

      /// <summary>
      /// PeekAreaInsets der 3 CarouselViews
      /// </summary>
      public Thickness PeekAreaInsets {
         get => (Thickness)GetValue(PeekAreaInsetsProperty);
         set {
            if (value != null &&
                value != (Thickness)GetValue(PeekAreaInsetsProperty)) {
               SetValue(PeekAreaInsetsProperty, value);
            }
         }
      }

      #endregion

      #endregion


      /// <summary>
      /// das erste Scrolled() nach der Init. bzw. nach eine Änderung von PeekAreaInsets setzt das Event
      /// </summary>
      ManualResetEvent isreadyManualResetEvents = new ManualResetEvent(false);

      long _lastset = -1;

      /// <summary>
      /// letzter gewünschter Wert (direkt vor ScrollTo() wieder -1)
      /// </summary>
      int lastSet {
         get {
            return (int)Interlocked.Read(ref _lastset);
         }
         set {
            Interlocked.Exchange(ref _lastset, value);
         }
      }


      public int Value {
         get {
            return Wheel.CurrentItem != null ? (int)Wheel.CurrentItem : -1;
         }
         set {
            if (value >= 0) {
#if WITH_DEBUG_INFO
               Debug.WriteLine(string.Format(">>> WheelView A set value=" + value + ", old=" + Value + ", lastSet=" + lastSet));
#endif

               lastSet = value;  // merkt sich den letzten gewünschten Wert

#if WITH_DEBUG_INFO
               Debug.WriteLine(string.Format(">>> WheelView B set value=" + value + ", old=" + Value + ", lastSet=" + lastSet));
#endif

               System.Threading.Tasks.Task.Run(() => {
#if WITH_DEBUG_INFO
                  Debug.WriteLine(string.Format(">>> WheelView C set value=" + value + ", old=" + Value + ", lastSet=" + lastSet));
#endif

                  isreadyManualResetEvents.WaitOne(); // warten auf Freigabe

#if WITH_DEBUG_INFO
                  Debug.WriteLine(string.Format(">>> WheelView C1 isreadyManualResetEvents.WaitOne(), set value=" + value + ", old=" + Value + ", lastSet=" + lastSet));
#endif

                  int newvalue = lastSet;

#if WITH_DEBUG_INFO
                  Debug.WriteLine(string.Format(">>> WheelView D set value=" + value + ", old=" + Value + ", lastSet=" + lastSet));
#endif

                  if (newvalue >= 0) {
                     lastSet = -1;
#if WITH_DEBUG_INFO
                     Debug.WriteLine(string.Format(">>> WheelView E set value=" + value + ", old=" + Value + ", lastSet=" + lastSet));
#endif
                     global::Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() => {  // synchronisiert mit dem UI-Thread ausführen
                        Wheel.ScrollTo(newvalue, -1, ScrollToPosition.Center, false);  // ohne Animation setzen
                     });
                  }
               });
            }
         }
      }

      /// <summary>
      /// Höhe eines Item (kleiner 0, wenn noch nicht bestimmt)
      /// </summary>
      double itemHeight = -1;


      public WheelView() {
         InitializeComponent();

         initItems(0, MaxValue);
         //Wheel.BindingContextChanged += Wheel_BindingContextChanged;
         //Wheel.ScrollToRequested += Wheel_ScrollToRequested;
         Wheel.Scrolled += Wheel_Scrolled;
         Wheel.CurrentItemChanged += Wheel_CurrentItemChanged;
      }

      private void Wheel_Scrolled(object sender, ItemsViewScrolledEventArgs e) {
         isreadyManualResetEvents.Set();

#if WITH_DEBUG_INFO
         Debug.WriteLine(string.Format(">>> Wheel_Scrolled isreadyManualResetEvents.Set()"));
#endif
      }

      //private void Wheel_ScrollToRequested(object sender, ScrollToRequestEventArgs e) {
      //   Debug.WriteLine(string.Format(">>> Wheel_ScrollToRequested"));
      //}

      //private void Wheel_BindingContextChanged(object sender, EventArgs e) {
      //   Debug.WriteLine(string.Format(">>> Wheel_BindingContextChanged"));
      //}

      int[] dat;

      void initItems(int min, int max) {
         int old = Value;

         dat = new int[max - min];
         for (int i = 0; i < dat.Length; i++)
            dat[i] = min++;
         Wheel.ItemsSource = dat;
         Wheel.BindingContext = this;

         Value = Math.Min(max - min - 1, old);
      }

      // Das ScrollToRequested-event ist nur die Antwort auf ein ScrollTo().

      // Das Scrolled-event wird vermutlich nach jedem Neuzeichnen geliefert. Der Offset scheint "unendlich" je nach Richtung weiterzuzählen. Delta gibt die Veränderung zum letzten Offset an.
      // Es ist aber nicht abzulesen, ob noch ein weiteres scrollen erfolgt.
      // Der Item-Wechsel ist besser im CurrentItemChanged-event ablesbar.
      // IsDragging zeigt nur, ob der Nutzer noch "am Rad dreht", NICHT, ob noch ein weiteres Drehen "von alleine" erfolgt.

      //private void Wheel_Scrolled(object sender, ItemsViewScrolledEventArgs e) {
      //   Debug.WriteLine(string.Format(">>> Scrolled e.VerticalOffset=" + e.VerticalOffset + ", e.VerticalDelta=" + e.VerticalDelta + ", " + Wheel.IsDragging));
      //}

      public virtual void OnValueChanged() {
         ValueChangedEvent?.Invoke(this, new ValueChangedEventArgs(Value));
      }

      /// <summary>
      /// eines der CarouselViews hat einen neuen Wert
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Wheel_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e) {
#if WITH_DEBUG_INFO
         Debug.WriteLine(string.Format(">>> Wheel_CurrentItemChanged, A CurrentItem=" + e.CurrentItem + ", _changePeekAreaInsets=" + _changePeekAreaInsets));
#endif

         if (!_changePeekAreaInsets)
            OnValueChanged();

#if WITH_DEBUG_INFO
         Debug.WriteLine(string.Format(">>> Wheel_CurrentItemChanged, E CurrentItem=" + e.CurrentItem));
#endif
      }

      /// <summary>
      /// die Größe für die Items hat sich geändert (wird nur aus CarouselViewHour abgeleitet)
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void ItemFrame_SizeChanged(object sender, EventArgs e) {
         Frame frame = sender as Frame;
         if (frame != null &&
             itemHeight != frame.Height) {
            itemHeight = frame.Height;

#if WITH_DEBUG_INFO
            showDebug("ItemFrame_SizeChanged() A: frame.Height=" + frame.Height + ", Height=" + Height + ", Wheel.Height=" + Wheel.Height +
               ", (frame.Parent as StackLayout).Height=" + (frame.Parent as StackLayout).Height);
#endif

            changePeekAreaInsets();

#if WITH_DEBUG_INFO
            showDebug("ItemFrame_SizeChanged() E");
#endif
         }
      }


      bool _changePeekAreaInsets = false;

      /// <summary>
      /// PeekAreaInsets aller CarouselViews wird geändert
      /// </summary>
      /// <param name="explicitdelta"></param>
      void changePeekAreaInsets(double explicitdelta = 0) {
         double delta = 0;

         if (explicitdelta == 0 &&
             itemHeight > 0) {
            double cvheight = Wheel.Height;
            double oldpeakareainset = PeekAreaInsets.VerticalThickness;
            double newpeakareainset = cvheight - itemHeight - GapSize;
            delta = newpeakareainset - oldpeakareainset;
         } else {
            if (explicitdelta != 0)
               delta = explicitdelta;
         }

         if (delta != 0) {

#if WITH_DEBUG_INFO
            Debug.WriteLine(string.Format(">>> changePeekAreaInsets, A delta=" + delta));
#endif
            Wheel.IsVisible = false;
            int orgvalue = Value;

#if WITH_DEBUG_INFO
            Debug.WriteLine(string.Format(">>> changePeekAreaInsets, B isreadyManualResetEvents.Reset(), delta=" + delta + ", orgvalue=" + orgvalue));
#endif

            isreadyManualResetEvents.Reset();

#if WITH_DEBUG_INFO
            Debug.WriteLine(string.Format(">>> changePeekAreaInsets, C isreadyManualResetEvents.Reset(), delta=" + delta));
#endif

            _changePeekAreaInsets = true;
            PeekAreaInsets = new Thickness(0,
                                           PeekAreaInsets.Top + delta / 2,
                                           0,
                                           PeekAreaInsets.Bottom + delta / 2);
            _changePeekAreaInsets = false;

            int lastsetvalue = lastSet;

#if WITH_DEBUG_INFO
            Debug.WriteLine(string.Format(">>> changePeekAreaInsets, D isreadyManualResetEvents.Reset(), delta=" + delta + ", orgvalue=" + orgvalue + ", lastsetvalue=" + lastsetvalue));
#endif

            Value = lastsetvalue < 0 ?
                        orgvalue :
                        lastsetvalue;

#if WITH_DEBUG_INFO
            Debug.WriteLine(string.Format(">>> changePeekAreaInsets, E delta=" + delta));
#endif

            Wheel.IsVisible = true;
         }
      }

#if WITH_DEBUG_INFO
      void showDebug(string txt = "") {
         Debug.WriteLine(string.Format(">>> WheelView: Value={0}, IsScrollAnimated={1}, ItemFontSize={2}, PeekAreaInsets={3} {4}: {5}",
                                       Value,
                                       Wheel.IsScrollAnimated,
                                       ItemFontSize,
                                       PeekAreaInsets.HorizontalThickness,
                                       PeekAreaInsets.VerticalThickness,
                                       txt
                                       ));
      }
#endif

   }
}
