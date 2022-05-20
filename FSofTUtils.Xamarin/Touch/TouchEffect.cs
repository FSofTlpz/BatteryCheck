using System;
using Xamarin.Forms;

namespace FSofTUtils.Xamarin.Touch {

   public class TouchEffect : RoutingEffect {

      public delegate void TouchActionEventHandler(object sender, TouchActionEventArgs args);

      public class TouchActionEventArgs : EventArgs {

         public enum TouchActionType {
            Entered,
            Pressed,
            Moved,
            Released,
            Exited,
            Cancelled
         }

         public TouchActionEventArgs(long id, TouchActionType type, Point location, bool isInContact) {
            Id = id;
            Type = type;
            Location = location;
            IsInContact = isInContact;
         }

         public long Id { private set; get; }

         public TouchActionType Type { private set; get; }

         public Point Location { private set; get; }

         public bool IsInContact { private set; get; }
      }


      public event TouchActionEventHandler TouchAction;


      public TouchEffect() :
         base("XamarinDocs.TouchEffect") { }

      public bool Capture { set; get; }

      public void OnTouchAction(Element element, TouchActionEventArgs args) {
         TouchAction?.Invoke(element, args);
      }
   }
}
