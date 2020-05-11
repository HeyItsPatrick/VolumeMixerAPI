using System;
using System.Diagnostics;

namespace CoreAudioMac
{
    public class SystemScripts
    {
        public void SetVolumeLevel(int newVolume)
        {
            Process.Start("/Applications/Utilities/Terminal.app/Contents/MacOS/Terminal","osascript -e 'set volume "+newVolume.ToString()+"'");
        }
    }
}
