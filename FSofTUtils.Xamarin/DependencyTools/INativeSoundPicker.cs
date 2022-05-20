using System.Collections.Generic;

namespace FSofTUtils.Xamarin.DependencyTools {
   public interface INativeSoundPicker {

      List<NativeSoundData> GetNativeSoundData(bool intern,
                                               bool isalarm,
                                               bool isnotification,
                                               bool isringtone,
                                               bool ismusic);

      void PlayExclusiveNativeSound(NativeSoundData nativeSoundData, float volume, bool looping);

      /// <summary>
      /// Ein Pfad, der mit '//' anfängt, muss mit einer Authority, z.B. 'media' beginnen. Jeder andere Pfad wird als normaler Dateipfad interpretiert.
      /// </summary>
      /// <param name="path"></param>
      /// <param name="volume"></param>
      /// <param name="looping"></param>
      void PlayExclusiveNativeSound(string path, float volume, bool looping);

      void StopExclusiveNativeSound();

   }
}
