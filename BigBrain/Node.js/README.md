This directory contains the Node.js code for the Holobot project. For hardware setup instructions, go to the repository [home page](https://github.com/ms-iot/holobot). 
Note that the [command list](https://github.com/ms-iot/holobot#command-list) for the Node.js code only includes move, rotate, and stop.
You have the option of using either Johnny-Five or Cylon to control the robot by setting 'J5' to true or false in [bot.js](https://github.com/ms-iot/holobot/blob/master/BigBrain/Node.js/Holobot/routes/bot.js). 

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

* If you are using Johnny-Five, nothing needs to be done. If you are using Cylon, replace the port value in .\Holobot\routes\bot.js (see code example below) with `USB\\VID_2341&PID_0043\\85436323631351311141` i.e. the string
  you get from the devcon command above. Be sure to use double \ in the string.
  
```JavaScript
//...
Cylon.robot({
    name: "B15",
    connections: {
        arduino: { adaptor: 'firmata', port: 'USB\\VID_2341&PID_0043\\85436323631351311141' }
    },
	//...
}).start();
//...
```

**If (Console Application)**

* In PowerShell, run `reg add "HKLM\SYSTEM\ControlSet001\Enum\`USB\VID_2341&PID_0043\85436323631351311141`\Device Parameters" /v "PortName" /t REG_SZ /d "COM5" /f`
  (Use the string you get from the devcon command above)
* Then run `shutdown /r /t 0` to reboot the device.
* When the device restarts, COM5 will be available for the app to use.



##Get Bootstrap code
* Download Bootstrap source zip file from http://getbootstrap.com/getting-started/ and unzip to your PC.
* Copy dist\js\bootstrap.min.js to &lt;Repo root&gt;\public\javascripts
* Copy docs\assets\js\vendor\jquery.min.js to &lt;Repo root&gt;\public\javascripts
* Copy dist\css\bootstrap.min.css to &lt;Repo root&gt;\public\stylesheets



#Node.js (ChakraCore) Console Application

###Set up your PC
* Install Node.js (ChakraCore) on your PC from [here](http://aka.ms/nodecc_msi).

###Install npm packages
* Clone this repository and open a command prompt in &lt;Repo root&gt;\BigBrain\Node.js\Holobot.
* Run `npm install` to download the npm packages that are needed.

###Get serialport
Even though serialport is installed when Johnny-Five or Cylon is installed, you still need to get a version that:  
  * Corresponds with the processor architecture of the device you are targeting (in this case ARM for Raspberry Pi 2).
  * Includes an [update](https://github.com/voodootikigod/node-serialport/pull/550) for serialport to work on Windows 10 IoT Core.  

* Copy and unzip the file [here](http://aka.ms/spcc_zip) to your PC.
* Copy &lt;Unzipped folder&gt;\console\arm\serialport.node to &lt;Repo root&gt;\BigBrain\Node.js\Holobot\node_modules\serialport\build\Release\node-&lt;Node version&gt;-win32-arm\serialport.node
* Copy Node.js (ChakraCore) executable for ARM from [here](http://aka.ms/nodecc_arm) to `c:\Node.js (ChakraCore)` on the Raspberry Pi 2.

###Copy the app to the Raspberry Pi 2
* Copy &lt;Repo root&gt;\BigBrain\Node.js\Holobot to `c:\Holobot` on the Raspberry Pi 2. You can use [Windows file sharing](http://ms-iot.github.io/content/en-US/win10/samples/SMB.htm), 
  [PowerShell](http://ms-iot.github.io/content/en-US/win10/samples/PowerShell.htm), or [SSH](http://ms-iot.github.io/content/en-US/win10/samples/SSH.htm) to do this.
  
###Open up the firewall for Node.js
* In PowerShell window connected to the Raspberry Pi 2, allow Node.js to communicate through the firewall with the following command:  
  `netsh advfirewall firewall add rule name="Node.js" dir=in action=allow program="C:\Node.js (ChakraCore)\Node.exe" enable=yes`
  
###Run the app!
* Finally, run `& 'C:\Node.js (ChakraCore)\Node.exe' C:\Holobot\bin\www`
* You can then use the web page `<IP Address of robot>:3000\drive` to move the robot with buttons or build your own app to send custom requests.



#Node.js (Chakra) UWP Application

###Set up your PC
* Install Windows 10 [with November update](http://windows.microsoft.com/en-us/windows-10/windows-update-faq).
* Install Visual Studio 2015 Update 1.
* Install the latest Node.js Tools for Windows IoT from [here](https://github.com/ms-iot/ntvsiot/releases).
* Install [Python 2.7](https://www.python.org/downloads/)

###Install npm packages
* Open .\Holobot.sln.
* Right click on the npm node in the Solution Explorer and then select "Install Missing npm Packages(s)".
* Right click on the node_modules folder in the Solution Explorer window. Then click on "Open Command Prompt Here...". 
  When the command window opens, run `npm dedupe`. *Don't skip this step*.
  
###Get serialport
Even though serialport is installed when Johnny-Five or Cylon is installed, you still need to get a version that:  
  * Corresponds with the processor architecture of the device you are targeting (in this case ARM for Raspberry Pi 2).
  * Is UWP (Universal Windows Platform) compatible (built from [this](https://github.com/ms-iot/node-serialport/tree/uwp) fork of serialport).  

* Copy and unzip the file [here](http://aka.ms/spc_zip) to your PC.
* Copy &lt;Unzipped folder&gt;\uwp\arm\serialport.node to &lt;Repo root&gt;\BigBrain\Node.js\Holobot\node_modules\serialport\build\Release\node-&lt;Node version&gt;-win32-arm\serialport.node
* Copy &lt;Unzipped folder&gt;\uwp\serialport.js to &lt;Repo root&gt;\BigBrain\Node.js\Holobot\node_modules\serialport\serialport.js.

###Deploy the app!
* Enter the IP address of the Raspberry Pi 2 in the project properties of Holobot.njsproj.
* Press F5 to run and debug the application.
* You can then use the web page `<IP Address of robot>:3000\drive` to move the robot with buttons or build your own app to send custom requests.



#Uploading light sensor data
You can attach a photoresistor (light sensor) to the Arduino on the Holobot and upload the data to the sample server in this repository.

###Hardware 
The hardware required and setup can be found on [this page](https://www.arduino.cc/en/Tutorial/AnalogInput).

###To upload the data:
* In a command prompt, run `node <Repo root>\BigBrain\Node.js\Holobot\server.js`. This will start the server that listens and stores the data being uploaded by the robot.
* In [bot.js](https://github.com/ms-iot/holobot/blob/master/BigBrain/Node.js/Holobot/routes/bot.js):
  * Set `doUpload` to true.
  * In the upload function, enter the IP address of the host which matches the server you started.
* In [drive.jade](https://github.com/ms-iot/holobot/blob/master/BigBrain/Node.js/Holobot/views/drive.jade):
  * Enter the IP address of the server (used in the step above) in the JavaScript code.
Now everytime the robot moves, it will upload the light sensor value to the server. You can use the "Average Brightness" and "Exploration Time" buttons in `<IP Address of robot>:3000\drive` to view the data.

