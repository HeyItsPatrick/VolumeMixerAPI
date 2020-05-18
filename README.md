# VolumeMixerAPI
Windows System Volume Mixer API Interface build with C#.<br>
Built to be used with my companion smartphone application,<br>
which can be found here:<br>
https://github.com/pschlapp/VolumeMixerCompanion

## API Endpoints
<b>Root URL:</b> `{localIPv4Address}:{localPort}/volume`

<b>GET: `/`</b><br>
This returns basic information about the host machine:<br>
IPv4 Address, Port Number, All Available Sound Devices, and the Machine Name

<b>GET: `/all`</b><br>
Returns a List of Volume objects, representing all sound-producing programs open on the machine

<b>GET: `/{int processID}`</b><br>
Returns a single Volume object for the program running on the host machine with the passed Process ID

<b>PUT: `/{int processID}/{int newVolume}`</b><br>
Updates the volume level of the program running on the host machine with the passed Process ID<br>
This is limited to a range of [0-100] and by the current Default Audio Device volume level<br>
Returns boolean reflecting the success of the volume change request
