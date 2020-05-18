using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VolumeMixerAPISelfHost.Models;
using Microsoft.AspNetCore.Mvc;
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

            //Get Device icon; value is "iconPath,-resourceID"
            string iconPathFull = device.Properties[PKEY.PKEY_DeviceClass_IconPath].Value.ToString();
            string iconResourceIdentifier = iconPathFull.Substring(iconPathFull.IndexOf('-'));

            var result = new List<Volume>
            {
                new Volume
                {
                    CurrentVolume = deviceVolume,
                    ProcessID = {-1 },
                    ProgramIcon = Volume.ExtractIconFromFile(iconPathFull.Substring(0,iconPathFull.IndexOf(',')), int.Parse(iconResourceIdentifier), true),
                    ProgramName = device.FriendlyName
                }
            };
            for (int i = 0; i < device.AudioSessionManager2.Sessions.Count; i++)
            {
                AudioSessionControl2 session = device.AudioSessionManager2.Sessions[i];
                //Prevents returning duplicate processes
                //if (result.Where(v => v.ProcessID == session.GetProcessID).Any())
                if (result.Where(v=> v.ProcessID.Contains((int)session.GetProcessID)).Any())
                    continue;

                //SimpleAudioVolume.MasterVolume is not quite accurate on it's face. It is actually the Control volume as a percentage of the Device volume
                string title = "";
                //Generic application box icon
                byte[] icon = Volume.ExtractIconFromFile("imageres.dll", 11, true);

                if (session.IsSystemSoundsSession)
                {
                    title = "System Sounds";
                    //PC with music note icon
                    icon = Volume.ExtractIconFromFile("audiosrv.dll", 0, true);
                }
                else
                {
                    try
                    {
                        Process sessionProcess = Process.GetProcessById((int)session.GetProcessID);
                        title = Path.GetFileNameWithoutExtension(sessionProcess.MainModule.FileName);

                        //Concat duplicate controls for the same program
                        if (result.Where(v => v.ProgramName == title).Any())
                        {
                            Volume item = result.Find(v => v.ProgramName == title);
                            item.ProcessID.Add((int)session.GetProcessID);
                            item.ProcessID.OrderBy(p => p);
                            continue;
                        }

                        if (System.IO.File.Exists(sessionProcess.MainModule.FileName))
                            icon = Volume.ExtractIconFromFile(sessionProcess.MainModule.FileName, 0, true);
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }

                result.Add(new Volume
                {
                    CurrentVolume = (int)Math.Round(session.SimpleAudioVolume.MasterVolume * deviceVolume),
                    ProcessID = { (int)session.GetProcessID },
                    ProgramIcon = icon,
                    ProgramName = (title ?? "NoTitle")
                });
            }

            Console.WriteLine("GET: VolumeControls...Success");
            return result.OrderBy(v => v.ProcessID.Min()).ToList();
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
            Console.Write("GET: Single Volume...");
            var deviceEnum = new MMDeviceEnumerator();
            MMDevice device = deviceEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole);

            var deviceVolume = (int)Math.Round(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            if (processID < 0)
            {
                string iconPathFull = device.Properties[PKEY.PKEY_DeviceClass_IconPath].Value.ToString();
                string iconResourceIdentifier = iconPathFull.Substring(iconPathFull.IndexOf('-'));

                Console.WriteLine("Success");
                return new Volume
                {
                    CurrentVolume = deviceVolume,
                    ProcessID = { -1 },
                    ProgramIcon = Volume.ExtractIconFromFile(iconPathFull.Substring(0, iconPathFull.IndexOf(',')), int.Parse(iconResourceIdentifier), true),
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
                        byte[] icon = Volume.ExtractIconFromFile("imageres.dll", 11, true);

                        if (session.IsSystemSoundsSession)
                        {
                            title = "System Sounds";
                            icon = Volume.ExtractIconFromFile("audiosrv.dll", 0, true);
                        }
                        else
                        {
                            Process sessionProcess = Process.GetProcessById((int)session.GetProcessID);
                            title = Path.GetFileNameWithoutExtension(sessionProcess.MainModule.FileName);
                            if (System.IO.File.Exists(sessionProcess.MainModule.FileName))
                                icon = Volume.ExtractIconFromFile(sessionProcess.MainModule.FileName, 0, true);
                        }

                        Console.WriteLine("Success");
                        return new Volume
                        {
                            CurrentVolume = (int)Math.Round(session.SimpleAudioVolume.MasterVolume * deviceVolume),
                            ProcessID = { (int)session.GetProcessID },
                            ProgramIcon = icon,
                            ProgramName = (title ?? "NoTitle")
                        };
                    }
                }
            }

            Console.WriteLine("Failure");
            return new Volume();
        }
    }
}
