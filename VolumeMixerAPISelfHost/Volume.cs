
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace VolumeMixerAPISelfHost.Models
{
    public class Volume
    {
        public string ProgramName { get; set; }
        public int CurrentVolume { get; set; }
        public List<int> ProcessID { get; set; }
        public byte[] ProgramIcon { get; set; }

        public Volume()
        {
            ProgramName = "";
            CurrentVolume = 0;
            ProcessID = new List<int>();
        }

        public static byte[] ExtractIconFromFile(string systemFile, int index, bool largeIcon)
        {
            IntPtr large;
            IntPtr small;
            ExtractIconEx(systemFile, index, out large, out small, 1);
            try
            {
                Icon icon = Icon.FromHandle(largeIcon ? large : small);
                using (var stream = new System.IO.MemoryStream())
                {
                    var img = icon.ToBitmap();
                    img.Save(stream, System.Drawing.Imaging.ImageFormat.Png); //png so transparency info is maintained
                    return stream.ToArray();
                }

            }
            catch
            {
                return null;
            }

        }
        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

    }
}
