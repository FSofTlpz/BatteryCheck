using System;
using System.Collections.Generic;
using System.Text;

namespace FSofTUtils.Xamarin {
   public interface INativeSoundPicker {

      List<NativeSoundData> GetNativeSoundData(bool intern,
                                               bool isalarm,
                                               bool isnotification,
                                               bool isringtone,
                                               bool ismusic);

      void PlayExclusiveNativeSound(NativeSoundData nativeSoundData, float volume);

      void StopExclusiveNativeSound();

   }
}
