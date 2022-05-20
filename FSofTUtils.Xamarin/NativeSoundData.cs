using System;
using System.IO;

namespace FSofTUtils.Xamarin {
   public class NativeSoundData : IComparable {

      public bool Intern { get; protected set; }

      public string ID { get; protected set; }

      public string Title { get; protected set; }

      public string Data { get; protected set; }

      /// <summary>
      /// Ex. die Datei (bzw. kann darauf zugegriffen werden)?
      /// </summary>
      public bool FileExists {
         get {
            try {
               if (File.Exists(Data))
                  return true;
            } catch { }
            return false;
         }
      }


      public NativeSoundData(bool intern, string id, string title, string data) {
         Intern = intern;
         ID = id;
         Title = title;
         Data = data;
      }

      public NativeSoundData(NativeSoundData sd)
         : this(sd.Intern, sd.ID, sd.Title, sd.Data) { }


      public int CompareTo(object obj) {
         if (obj == null)
            return 1;

         NativeSoundData nativeSoundData = obj as NativeSoundData;

         int result = string.Compare(Title, nativeSoundData.Title);
         if (result == 0) {
            if (Intern != nativeSoundData.Intern)
               result = Intern ? 1 : -1;
            else
               result = string.Compare(ID, nativeSoundData.ID);
         }
         return result;
      }

      public override string ToString() {
         return ID + ": intern=" + Intern + ", " + Title + " (" + Data + ")";
      }

   }
}
