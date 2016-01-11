The Node.js Holobot code can either be run with either:

* [Node.js (ChakraCore) console application](http://aka.ms/nodecc_arm) or
* [Node.js (Chakra) UWP application](http://aka.ms/ntvsiotlatest).

This guide will take you through the steps to set up both versions to run on the Raspberry Pi 2 with 
Windows 10 IoT Core.


##Set up the serial communication between the Raspberry Pi 2 and Arduino
Get the string that identifies the COM port connected to the Arduino:

* In a [PowerShell](http://ms-iot.github.io/content/en-US/win10/samples/PowerShell.htm) window connected to the Raspberry Pi 2, run `devcon status usb*`.
  When you do this, you should see a device similar to the one below:

   `USB\VID_2341&PID_0043\85436323631351311141`  
   Name: USB Serial Device  
   Driver is running.
   
**If (UWP Application)**

* Replace the port in .\Holobot\app.js (see code snippet below) with the `USB\\\VID_2341&PID_0043\\\85436323631351311141` (be sure to add extra \ after both \\ characters)

**If (Console Application)**

* In PowerShell, run `reg add "HKLM\SYSTEM\ControlSet001\Enum\`USB\VID_2341&PID_0043\85436323631351311141`\Device Parameters" /v "PortName" /t REG_SZ /d "COM5" /f`.
* Then run `shutdown /r /t 0` to reboot the device.
* When the device restarts, replace the port value in .\Holobot\app.js with 'COM5' (Note: This will work for UWP application as well).

```JavaScript
//...
Cylon.robot({
    name: "B15",
    connections: {
        arduino: { adaptor: 'firmata', port: 'COM5' }
    },
	//...
}).start();
//...
```


##Get Bootstrap code
* Download Bootstrap source zip file from http://getbootstrap.com/getting-started/ and unzip to your PC.
* Copy dist\js\bootstrap.min.js to &lt;Repo root&gt;\public\javascripts
* Copy docs\assets\js\vendor\jquery.min.js to &lt;Repo root&gt;\public\javascripts
* Copy dist\css\bootstrap.min.css to &lt;Repo root&gt;\public\stylesheets


#Node.js (ChakraCore) Console Application
* Install Node.js (ChakraCore) on your PC from [here](http://aka.ms/nodecc_msi).
* Clone this repository and open a command prompt in &lt;Repo root&gt;\BigBrain\Node.js\Holobot.
* Run `npm install` to download npm packages.
* Even though serialport is installed when Cylon is installed, you still need to get a version that:  
  * Corresponds with the processor architecture of the device you are targeting (in this case ARM for Raspberry Pi 2).
  * Includes an [update](https://github.com/voodootikigod/node-serialport/pull/550) for serialport to work on Windows 10 IoT Core.  

**Steps to get serialport:**

* Copy and unzip the file [here](http://aka.ms/spcc_zip) to your PC.
* Copy &lt;Unzipped folder&gt;\console\arm\serialport.node to &lt;Repo root&gt;\node_modules\serialport\build\Release\node-&lt;Node version&gt;-win32-arm\serialport.node
* Copy Node.js (ChakraCore) executable for ARM from [here](http://aka.ms/nodecc_arm) to `c:\Node.js (ChakraCore)` on the Raspberry Pi 2.
* Copy &lt;Repo root&gt;\BigBrain\Node.js\Holobot to `c:\Holobot` on the Raspberry Pi 2.
* In PowerShell, allow Node.js to communicate through the firewall with the following command:  
  `netsh advfirewall firewall add rule name="Node.js" dir=in action=allow program="C:\Node.js (ChakraCore)\Node.exe" enable=yes`
* Finally, run `& 'C:\Node.js (ChakraCore)\Node.exe' C:\Holobot\bin\www`


#Node.js (Chakra) UWP Application

###Set up your PC
* Install Windows 10 [with November update](http://windows.microsoft.com/en-us/windows-10/windows-update-faq).
* Install Visual Studio 2015 Update 1.
* Install the latest Node.js Tools for Windows IoT from [here](https://github.com/ms-iot/ntvsiot/releases).
* Install [Python 2.7](https://www.python.org/downloads/)


###Deploy Application
* Open .\Holobot.sln.
* Right click on the npm node in the Solution Explorer and then select "Install Missing npm Packages(s)".
* Right click on the node_modules folder in the Solution Explorer window. Then click on "Open Command Prompt Here...". 
  When the command window opens, run `npm dedupe`.
* Even though serialport is installed when Cylon is installed, you still need to get a version that:  
  * Corresponds with the processor architecture of the device you are targeting (in this case ARM for Raspberry Pi 2).
  * Is UWP (Universal Windows Platform) compatible (built from [this](https://github.com/ms-iot/node-serialport/tree/uwp) fork of serialport).  

**Steps to get serialport:**

* Copy and unzip the file [here](http://aka.ms/spc_zip) to your PC.
* Copy &lt;Unzipped folder&gt;\uwp\arm\serialport.node to &lt;Repo root&gt;\node_modules\serialport\build\Release\node-&lt;Node version&gt;-win32-arm\serialport.node
* Copy &lt;Unzipped folder&gt;\uwp\serialport.js to &lt;Repo root&gt;\node_modules\serialport\serialport.js.
* Enter the IP address of the Raspberry Pi 2 in project properties.
* Press F5 to run and debug the application.