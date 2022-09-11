# FanControl.HWInfo [![Build status](https://ci.appveyor.com/api/projects/status/ea76b9272trofoa6/branch/master?svg=true)](https://ci.appveyor.com/project/Rem0o/fancontrol-hwinfo/branch/master)

Plugin for [FanControl](https://github.com/Rem0o/FanControl.Releases) (>= V113) that provides support for HWInfo sensors using the "Reporting to Gadget" feature. 

Inspiration from https://docs.rainmeter.net/tips/hwinfo/

## To install

Either
* Download the latest binaries from [AppVeyor](https://ci.appveyor.com/project/Rem0o/fancontrol-hwinfo/branch/master/artifacts)
* Download the latest [release](https://github.com/Rem0o/FanControl.HWInfo/releases)
* Compile the solution.

And then

1. Copy the FanControl.HWInfo.dll into FanControl's "Plugins" folder. You might need to unlock it in its properties.
2. Go the HWInfo's "Configure Sensors" option menu, then to the "HWInfo Gadget" tab. Have the "Enable reporting to gadget" checkbox checked and select "Report value in Gadget" for all sensors you want to import.
3. Open FanControl and enjoy!

## Notes

* HWInfo needs to be running at least in "Sensors-only" mode.
* You can use the "refresh sensors detection" menu option in FanControl while it is running if you didn't have HWInfo running or you changed the exported sensors.

