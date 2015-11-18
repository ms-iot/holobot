This sample can either be run with Node.js (Chakra) console app or as a Node.js (Chakra) UWP application.
This guide will take you through the steps to set up both versions to run on the Raspberry Pi 2 with 
Windows 10 IoT Core.

###Set up the serial communication between the Raspberry Pi 2 and Arduino
Connect the Raspberry Pi 2 and Arduino with the USB cable. If your Raspberry Pi 2 is connected to a monitor, 
you should see the device getting recognized as "Arduino <type>" or "USB Serial Device":

Get the string that identifies the Arduino and will be used in sample code. Follow these steps to do this:

* In a PowerShell window connected to the Raspberry Pi 2, run `devcon status usb*`. When you do this, you should see a device similar to the one below:

   USB\VID_2341&PID_0043\85436323631351311141  
   Name: USB Serial Device  
   Driver is running.
   
If (UWP Application)
* Replace the port in app.js with the "USB\\\VID_2341&PID_0043\\\85436323631351311141" (be sure to add extra \ after both \\ characters)

If (Console Application)
* Run `reg add "HKLM\SYSTEM\ControlSet001\Enum\USB\VID_2341&PID_0043\85436323631351311141\Device Parameters" /v "PortName" /t REG_SZ /d "COM5" /f`.
* Run `shutdown /r /t 0` to reboot the device.
* When the device restarts, replace the port value in app.js with 'COM5' (Note: This will work for UWP application as well).

<UL>
{% highlight JavaScript %}
...
Cylon.robot({
    name: "B15",
    connections: {
        arduino: { adaptor: 'firmata', port: 'COM5' }
    },
	...
{% endhighlight %}
</UL>


###Console Application
* Open a command prompt at this root.
* Run XXXX to download npm packages
* Even though serialport is installed when Cylon is installed, you still need to get a version that:  
  * Corresponds with the processor architecture of the device you are targeting (in this case ARM for Raspberry Pi 2).
  * Includes an [update](https://github.com/voodootikigod/node-serialport/pull/550) for serialport to work on Windows 10 IoT Core.  
  Steps to get serialport:  
  * Copy and unzip the file [here](https://github.com/ms-iot/ntvsiot/releases/download/2.0.4/serialport_WinIoT.zip) to your PC.
  * Copy &lt;Unzipped folder&gt;\console\arm\serialport.node to [CylonSample folder path]\node_modules\serialport\build\Release\node-v47-win32-arm\serialport.node  
    **Note:** node-v14-win32-arm is a new folder you will create.
* Copy Node.js (Chakra) from [here](XXXX) to c:\Node.js (Chakra) on the Raspberry Pi 2.
* Copy this folder to c:\Holobot on the Raspberry Pi 2.
* In PowerShell, run `& 'C:\Node.js (Chakra)\Node.exe' C:\Holobot\bin\www`


###UWP Application

##Set up your PC
* Install Windows 10 [with November update](http://windows.microsoft.com/en-us/windows-10/windows-update-faq).
* Install Visual Studio 2015 Update 1.
* Install the latest Node.js Tools for Windows IoT from [here](https://github.com/ms-iot/ntvsiot/releases).
* Install [Python 2.7](https://www.python.org/downloads/){:target="_blank"}.


##Deploy Application
* Open Holobot.sln
* Expand the npm node in the Solution Explorer. Right click on the missing npm packages and select "Update npm Packages(s)"
* Right click on the node_modules folder in the Solution Explorer window. Then click on "Open Command Prompt Here...". 
  When the command window opens, run `npm dedupe`.
* Even though serialport is installed when Cylon is installed, you still need to get a version that:  
  * Corresponds with the processor architecture of the device you are targeting (in this case ARM for Raspberry Pi 2).
  * Is UWP (Universal Windows Platform) compatible (built from [this](https://github.com/ms-iot/node-serialport/tree/uwp) fork of serialport).  
  Steps to get serialport:
  * Copy and unzip the file [here](https://github.com/ms-iot/ntvsiot/releases/download/2.0.4/serialport_WinIoT.zip) to your PC.
  * Copy &lt;Unzipped folder&gt;\uwp\arm\serialport.node to [Holobot project root]\node_modules\serialport\build\Release\node-v47-win32-arm\serialport.node  
    **Note:** node-v14-win32-arm is a new folder you will create.
* Copy &lt;Unzipped folder&gt;\uwp\serialport.js to [Holobot project root]\node_modules\serialport\serialport.js.
* Enter IP address of Raspberry Pi in project properties and press F5 run and debug the application.