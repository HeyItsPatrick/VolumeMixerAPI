
namespace VolumeMixerAPISelfHost.Models
{
    public class Volume
    {
        public string ProgramName { get; set; }
        public int CurrentVolume { get; set; }
        public int ProcessID { get; set; }
        public byte[] ProgramIcon { get; set; }

        public Volume()
        {
            ProgramName = "";
            CurrentVolume = 0;
            ProcessID = 0;
        }
    }
}
