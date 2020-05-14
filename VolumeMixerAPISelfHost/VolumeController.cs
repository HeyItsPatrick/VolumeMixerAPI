using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using VolumeMixerAPISelfHost.Models;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Imaging;
using CoreAudio;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace VolumeAPISelfHost.Controllers
{
    [ApiController]
    [Route("volume")]
    public class VolumeController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> GetSystemInfo()
        {
                var deviceEnumerator = new MMDeviceEnumerator();
                var device = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole);
                var deviceCollection = deviceEnumerator.EnumerateAudioEndPoints(EDataFlow.eRender, DEVICE_STATE.DEVICE_STATE_ACTIVE);
                var result = new List<string> {
                "Machine Name: " + Environment.MachineName,
                "Default Device: " + device.FriendlyName,
                "","All Devices",
            };
                var tempDeviceList = new List<MMDevice>();
                for (int i = 0; i < deviceCollection.Count; i++)
                {
                    MMDevice item = deviceCollection[i];
                    tempDeviceList.Add(item);
                }
                result.AddRange(tempDeviceList.OrderBy(d => d.FriendlyName).Select(d => d.FriendlyName).ToList());

                Console.WriteLine("GET: SystemInfo...Success");
                return result;
        }

        // GET: /volume/all
        [HttpGet("all")]
        public IEnumerable<Volume> GetAllProgramVolumes()
        {
            var deviceEnum = new MMDeviceEnumerator();
            MMDevice device = deviceEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole);

            var deviceVolume = (int)Math.Round(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);

            var result = new List<Volume>
                {
                    new Volume
                    {
                        CurrentVolume = deviceVolume,
                        ProcessID = -1,
                        ProgramIcon = new byte[0],
                        ProgramName = device.FriendlyName
                    }
                };
            for (int i = 0; i < device.AudioSessionManager2.Sessions.Count; i++)
            {
                AudioSessionControl2 session = device.AudioSessionManager2.Sessions[i];
                //Prevents returning duplicates
                if (result.Where(v => v.ProcessID == session.GetProcessID).Any())
                    continue;

                //SimpleAudioVolume.MasterVolume is not quite accurate on it's face. It is actually the Control volume as a percentage of the Device volume
                string title = "";
                Icon icon = SystemIcons.Error;

                if (session.IsSystemSoundsSession)
                {
                    title = "System Sounds";
                    icon = SystemIcons.Application;
                }
                else
                {
                    Process sessionProcess = Process.GetProcessById((int)session.GetProcessID);
                    title = Path.GetFileNameWithoutExtension(sessionProcess.MainModule.FileName);
                    if (System.IO.File.Exists(sessionProcess.MainModule.FileName))
                        icon = Icon.ExtractAssociatedIcon(sessionProcess.MainModule.FileName);
                }

                using (var stream = new MemoryStream())
                {
                    var img = icon.ToBitmap();
                    img.Save(stream, ImageFormat.Png); //png so transparency info is maintained
                    var byteArray = stream.ToArray();

                    result.Add(new Volume
                    {
                        CurrentVolume = (int)Math.Round(session.SimpleAudioVolume.MasterVolume * deviceVolume),
                        ProcessID = (int)session.GetProcessID,
                        ProgramIcon = byteArray,
                        ProgramName = (title ?? "NoTitle")
                    });
                }
            }

            Console.WriteLine("GET: VolumeControls...Success");
            return result.OrderBy(v => v.ProcessID).ToList();
        }

        // PUT: /volume/{ProcessID}/{NewVolume}
        [HttpPut("{processID}/{newVolume}")]
        public bool Put(int processID, int newVolume)
        {
                Console.Write("PUT: UpdateVolume...");
                if (newVolume > 100 || newVolume < 0)
                {
                    Console.WriteLine("Failed");
                    return false;
                }
                var deviceEnum = new MMDeviceEnumerator();
                MMDevice device = deviceEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole);

                var deviceVolume = (int)Math.Round(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);

                if (processID < 0)//Device Volume
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)newVolume / 100;
                else
                {
                    if (newVolume > deviceVolume) //No audio control is allowed above system volume
                    {
                        Console.WriteLine("Failed");
                        return false;
                    }
                    for (int i = 0; i < device.AudioSessionManager2.Sessions.Count; i++)
                    {
                        AudioSessionControl2 session = device.AudioSessionManager2.Sessions[i];
                        if (session.GetProcessID == processID)
                        {
                            session.SimpleAudioVolume.MasterVolume = (float)newVolume / deviceVolume;
                        }
                    }
                }

                Console.WriteLine("Success");
                return true;
        }


        // GET: /volume/{processID}
        [HttpGet("{processID}")]
        public Volume GetVolumeByProcessID(int processID)
        {
            var deviceEnum = new MMDeviceEnumerator();
            MMDevice device = deviceEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole);

            var deviceVolume = (int)Math.Round(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            if (processID < 0)
            {
                return new Volume
                {
                    CurrentVolume = deviceVolume,
                    ProcessID = -1,
                    ProgramIcon = new byte[0],
                    ProgramName = device.FriendlyName
                };
            }
            else
            {
                for (int i = 0; i < device.AudioSessionManager2.Sessions.Count; i++)
                {
                    AudioSessionControl2 session = device.AudioSessionManager2.Sessions[i];
                    if (session.GetProcessID == processID)
                    {
                        string title = "";
                        Icon icon = SystemIcons.Error;

                        if (session.IsSystemSoundsSession)
                        {
                            title = "System Sounds";
                            icon = SystemIcons.Application;
                        }
                        else
                        {
                            Process sessionProcess = Process.GetProcessById((int)session.GetProcessID);
                            title = Path.GetFileNameWithoutExtension(sessionProcess.MainModule.FileName);
                            if (System.IO.File.Exists(sessionProcess.MainModule.FileName))
                                icon = Icon.ExtractAssociatedIcon(sessionProcess.MainModule.FileName);
                        }

                        using (var stream = new MemoryStream())
                        {
                            var img = icon.ToBitmap();
                            img.Save(stream, ImageFormat.Png); //png so transparency info is maintained
                            var byteArray = stream.ToArray();
                            Console.WriteLine("GET: VolumeControlByID...Success");
                            return new Volume
                            {
                                CurrentVolume = (int)Math.Round(session.SimpleAudioVolume.MasterVolume * deviceVolume),
                                ProcessID = (int)session.GetProcessID,
                                ProgramIcon = byteArray,
                                ProgramName = (title ?? "NoTitle")
                            };
                        }
                    }
                }
            }
            return new Volume();
        }
    }
}
