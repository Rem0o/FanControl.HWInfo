# FanControl.HWInfo [![Build status](https://ci.appveyor.com/api/projects/status/ea76b9272trofoa6/branch/master?svg=true)](https://ci.appveyor.com/project/Rem0o/fancontrol-hwinfo/branch/master)

[![Download](https://img.shields.io/badge/Download-Plugin-green.svg?style=flat&logo=download)](https://github.com/Rem0o/FanControl.HWInfo/releases/)
[![Donate](https://img.shields.io/badge/Donate-PayPal-blue.svg?style=flat&logo=paypal)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=N4JPSTUQHRJM8&currency_code=USD&source=url&item_name=Fan+Control)

Plugin for [FanControl](https://github.com/Rem0o/FanControl.Releases) (V113 and up) that provides support for HWInfo sensors using the "Reporting to Gadget" feature. 

Inspiration from https://docs.rainmeter.net/tips/hwinfo/

## To install

Either
* Download the latest [release](https://github.com/Rem0o/FanControl.HWInfo/releases)
* Download the latest binaries from [AppVeyor](https://ci.appveyor.com/project/Rem0o/fancontrol-hwinfo/branch/master/artifacts)
* Compile the solution.

And then

1. Copy the FanControl.HWInfo.dll into FanControl's "Plugins" folder. You might need to unblock it in its properties.
2. Go the HWInfo's "Configure Sensors" option menu, then to the "HWInfo Gadget" tab. Have the "Enable reporting to gadget" checkbox checked and select "Report value in Gadget" for all sensors you want to import.
3. Open FanControl and enjoy!

## HWInfo settings

The following setting should be checked if you want to export any fan sensor.
<br/><br/>
![image](https://user-images.githubusercontent.com/14118956/197402866-53a81c80-83a3-4cd4-a0bc-28f2712088a3.png)

## Notes

* HWInfo needs to be running at least in "Sensors-only" mode.
* You can use the "refresh sensors detection" menu option in FanControl while it is running if you didn't have HWInfo running or you changed the exported sensors.

