using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using static FSofTUtils.Xamarin.Touch.TouchEffect.TouchActionEventArgs;

[assembly: ResolutionGroupName("XamarinDocs")]
[assembly: ExportEffect(typeof(FSofTUtils.Android.TouchEffect), "TouchEffect")]

namespace FSofTUtils.Android {

   public class TouchEffect : PlatformEffect {
      global::Android.Views.View view;
      Element formsElement;

      Xamarin.Touch.TouchEffect libTouchEffect;
      bool capture;
      Func<double, double> fromPixels;
      //readonly int[] twoIntArray = vs;
      int[] twoIntArray = new int[2];

      static readonly Dictionary<global::Android.Views.View, TouchEffect> viewDictionary = new Dictionary<global::Android.Views.View, TouchEffect>();

      static readonly Dictionary<int, TouchEffect> idToEffectDictionary = new Dictionary<int, TouchEffect>();


      protected override void OnAttached() {
         // Get the Android View corresponding to the Element that the effect is attached to
         view = Control ?? Container;

         // Get access to the TouchEffect class in the .NET Standard library
         Xamarin.Touch.TouchEffect touchEffect = (Xamarin.Touch.TouchEffect)Element.Effects.FirstOrDefault(e => e is Xamarin.Touch.TouchEffect);

         if (touchEffect != null && view != null) {
            viewDictionary.Add(view, this);

            formsElement = Element;

            libTouchEffect = touchEffect;

            // Save fromPixels function
            fromPixels = view.Context.FromPixels;

            // Set event handler on View
            view.Touch += OnTouch;
         }
      }

      protected override void OnDetached() {
         if (viewDictionary.ContainsKey(view)) {
            viewDictionary.Remove(view);
            view.Touch -= OnTouch;
         }
      }

      void OnTouch(object sender, global::Android.Views.View.TouchEventArgs args) {
         // Two object common to all the events
         global::Android.Views.View senderView = sender as global::Android.Views.View;
         MotionEvent motionEvent = args.Event;

         // Get the pointer index
         int pointerIndex = motionEvent.ActionIndex;

         // Get the id that identifies a finger over the course of its progress
         int id = motionEvent.GetPointerId(pointerIndex);
         // For Android.Views.MotionEvent.ACTION_POINTER_DOWN or Android.Views.MotionEvent.ACTION_POINTER_UP as returned by ActionMasked, this returns the associated pointer index.

         senderView.GetLocationOnScreen(twoIntArray);
         Point screenPointerCoords = new Point(twoIntArray[0] + motionEvent.GetX(pointerIndex),
                                               twoIntArray[1] + motionEvent.GetY(pointerIndex));

         // Use ActionMasked here rather than Action to reduce the number of possibilities
         switch (args.Event.ActionMasked) {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
               FireEvent(this, id, TouchActionType.Pressed, screenPointerCoords, true);

               idToEffectDictionary.Add(id, this);
               //if (!idToEffectDictionary.ContainsKey(id))
               //   idToEffectDictionary.Add(id, this);
               //else
               //   idToEffectDictionary[id] = this;

               capture = libTouchEffect.Capture;
               break;

            case MotionEventActions.Move:
               // Multiple Move events are bundled, so handle them in a loop
               for (pointerIndex = 0; pointerIndex < motionEvent.PointerCount; pointerIndex++) {
                  id = motionEvent.GetPointerId(pointerIndex);
                  screenPointerCoords = new Point(twoIntArray[0] + motionEvent.GetX(pointerIndex),
                                                  twoIntArray[1] + motionEvent.GetY(pointerIndex));

                  if (capture) {
                     senderView.GetLocationOnScreen(twoIntArray);
                     FireEvent(this, id, TouchActionType.Moved, screenPointerCoords, true);
                  } else {
                     CheckForBoundaryHop(id, screenPointerCoords);
                     if (idToEffectDictionary[id] != null) {
                        FireEvent(idToEffectDictionary[id], id, TouchActionType.Moved, screenPointerCoords, true);
                     }
                  }
               }
               break;

            case MotionEventActions.Up:
            case MotionEventActions.Pointer1Up:
               if (capture) {
                  FireEvent(this, id, TouchActionType.Released, screenPointerCoords, false);
               } else {
                  CheckForBoundaryHop(id, screenPointerCoords);

                  if (idToEffectDictionary[id] != null) {
                     FireEvent(idToEffectDictionary[id], id, TouchActionType.Released, screenPointerCoords, false);
                  }
               }
               idToEffectDictionary.Remove(id);
               break;

            case MotionEventActions.Cancel:
               if (capture) {
                  FireEvent(this, id, TouchActionType.Cancelled, screenPointerCoords, false);
               } else {
                  if (idToEffectDictionary[id] != null) {
                     FireEvent(idToEffectDictionary[id], id, TouchActionType.Cancelled, screenPointerCoords, false);
                  }
               }
               idToEffectDictionary.Remove(id);
               break;
         }
      }

      void CheckForBoundaryHop(int id, Point pointerLocation) {
         TouchEffect touchEffectHit = null;

         foreach (global::Android.Views.View view in viewDictionary.Keys) {
            // Get the view rectangle
            try {
               view.GetLocationOnScreen(twoIntArray);
            } catch // System.ObjectDisposedException: Cannot access a disposed object.
              {
               continue;
            }
            Rectangle viewRect = new Rectangle(twoIntArray[0], twoIntArray[1], view.Width, view.Height);

            if (viewRect.Contains(pointerLocation)) {
               touchEffectHit = viewDictionary[view];
            }
         }

         if (touchEffectHit != idToEffectDictionary[id]) {
            if (idToEffectDictionary[id] != null) {
               FireEvent(idToEffectDictionary[id], id, TouchActionType.Exited, pointerLocation, true);
            }
            if (touchEffectHit != null) {
               FireEvent(touchEffectHit, id, TouchActionType.Entered, pointerLocation, true);
            }
            idToEffectDictionary[id] = touchEffectHit;
         }
      }

      void FireEvent(TouchEffect touchEffect, int id, TouchActionType actionType, Point pointerLocation, bool isInContact) {
         // Get the method to call for firing events
         Action<Element, Xamarin.Touch.TouchEffect.TouchActionEventArgs> onTouchAction = touchEffect.libTouchEffect.OnTouchAction;

         // Get the location of the pointer within the view
         touchEffect.view.GetLocationOnScreen(twoIntArray);
         double x = pointerLocation.X - twoIntArray[0];
         double y = pointerLocation.Y - twoIntArray[1];
         Point point = new Point(fromPixels(x), fromPixels(y));

         // Call the method
         onTouchAction(touchEffect.formsElement, new Xamarin.Touch.TouchEffect.TouchActionEventArgs(id, actionType, point, isInContact));
      }
   }

}
