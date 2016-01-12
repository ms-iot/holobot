/*
    Copyright(c) Microsoft Corp. All rights reserved.
    
    The MIT License(MIT)
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

var J5 = true; // Set to false if Cylon will be used

var express = require('express');
var router = express.Router();
if (J5) {
    var five = require("johnny-five");
    var board = new five.Board();
} else {
    var Cylon = require('cylon');
}
var constants = require('./constants');
var http = require('http');

// Initialize the robot with Johnny-Five
if (J5) {
    board.on("ready", function () {
        var acceleration = 800;
        var deceleration = 800;
        var maxSpeed = 1500;
        
        stepperRight = new five.Stepper({
            type: five.Stepper.TYPE.DRIVER,
            stepsPerRev: 200,
            pins: {
                step: 4,
                dir: 13
            },
            speed: maxSpeed,
            accel: acceleration,
            decel: deceleration
        });
        
        stepperLeft = new five.Stepper({
            type: five.Stepper.TYPE.DRIVER,
            stepsPerRev: 200,
            pins: {
                step: 7,
                dir: 6
            },
            speed: maxSpeed,
            accel: acceleration,
            decel: deceleration
        });

        lightSensor = new five.Sensor("A0");
    });
// Initialize the robot with Johnny-Five
} else {
    Cylon.config({
        logging: {
            level: 'debug'
        }
    });
    
    Cylon.robot({
        name: "B15",
        connections: {
            arduino: { adaptor: 'firmata', port: 'COM5' }
        },
        
        devices: {
            stepperRight: { driver: 'stepper', driveType: 1, stepsPerRevolution: constants.stepsPerRotation, deviceNum: 0, stepPin: 4, dirPin: 13, enablePin: 5 },
            stepperLeft: { driver: 'stepper', driveType: 1, stepsPerRevolution: constants.stepsPerRotation, deviceNum: 1, stepPin: 7, dirPin: 6, enablePin: 8 },
            lightSensor: { driver: 'analog-sensor', pin: 0, lowerLimit: 100, upperLimit: 900 }
        },
        
        work: function (my) {
            var acceleration = 800;
            var maxSpeed = 1500;
            my.stepperRight.setAcceleration(acceleration);
            my.stepperRight.setMaxSpeed(maxSpeed);
            my.stepperLeft.setAcceleration(acceleration);
            my.stepperLeft.setMaxSpeed(maxSpeed);
        }
    }).start();
}

function arcLength(deg, radius) {
    return (Math.PI * radius * deg) / 180;
}

var Bot = function() {
    this.lastCommand = "";
    this.moveCount = 0;
}

Bot.prototype.stop = function(res) {
    if (J5) {
        stepperRight.step({ state : Stepper.RUNSTATE.STOP }, function () {});
        stepperLeft.step({ state : Stepper.RUNSTATE.STOP }, function () {});
    } else {
        Cylon.MCP.robots.B15.stepperRight.stop();
        Cylon.MCP.robots.B15.stepperLeft.stop();
    }

    res.header('Cache-Control', 'private, no-cache, no-store, must-revalidate');
    res.header('Expires', '-1');
    res.header('Pragma', 'no-cache');
    res.send("{success=\"ok\"}");
}

var WHEELCOUNT = 2;

Bot.prototype.move = function(distance, res) {
    console.log('Moving ' + distance);

    var moveCount = function () {
        this.moveCount++;
        if (this.moveCount == WHEELCOUNT) {
            if (doUpload) {
                this.upload(distance, '', false);
            }
            this.moveCount = 0;
        }
    }.bind(this);
    
    if (J5) {
        // CW=1,CCW=0
        var dir = 1;

        if (distance < 0) {
            dir = 0;
            distance = Math.abs(distance);
        }
        var stepRightResult = stepperRight.step({
            steps : constants.stepsPerCM * distance, 
            direction: dir
        }, moveCount);
        
        var stepLeftResult = stepperLeft.step({
            steps : constants.stepsPerCM * distance, 
            direction: dir
        }, moveCount);
    } else {
        Cylon.MCP.robots.B15.stepperRight.move(constants.stepsPerCM * distance, moveCount);
        Cylon.MCP.robots.B15.stepperLeft.move(constants.stepsPerCM * distance, moveCount);
    }

    this.lastCommand = "move";
    res.header('Cache-Control', 'private, no-cache, no-store, must-revalidate');
    res.header('Expires', '-1');
    res.header('Pragma', 'no-cache');
    res.send("{success=\"ok\"}");
}

Bot.prototype.rotate = function(deg, res) {
    console.log('Rotating ' + deg);
 
    var moveCount = function () {
        this.moveCount++;
        if (this.moveCount == WHEELCOUNT) { 
            this.moveCount = 0;
        }
    }.bind(this);

    var lengthInCM = arcLength(deg, constants.wheelBaseRadius);
    
    if (J5) {
        // CW=1,CCW=0
        var dirRight = 1;
        var dirLeft = 0;

        if (deg < 0) {
            dirRight = 0;
            dirLeft = 1
        }

        lengthInCM = Math.abs(lengthInCM);

        stepperRight.step({
            steps : constants.stepsPerCM * lengthInCM, 
            direction: dirRight
        }, moveCount);
        
        stepperLeft.step({
            steps : constants.stepsPerCM * lengthInCM, 
            direction: dirLeft
        }, moveCount);
    } else {
        Cylon.MCP.robots.B15.stepperRight.move(constants.stepsPerCM * lengthInCM, moveCount);
        Cylon.MCP.robots.B15.stepperLeft.move(-constants.stepsPerCM * lengthInCM, moveCount);
    }

    this.lastCommand = "rotate";
    res.header('Cache-Control', 'private, no-cache, no-store, must-revalidate');
    res.header('Expires', '-1');
    res.header('Pragma', 'no-cache');
    res.send("{success=\"ok\"}");
}

var doUpload = false; // Set to true to call upload() after move

Bot.prototype.upload = function (dist, res, sendRes) {
    var brightnessValue = 0;
    if (J5) {
        brightnessValue = lightSensor.value;
    } else {
        brightnessValue = Cylon.MCP.robots.B15.lightSensor.analogRead();
    }
    console.log('Uploading: Distance Moved [%d], Brightness [%d]', dist, brightnessValue);
    var options = {
        // Uncomment and add host/port values before using this function
        //host: '',
        //port: '',
        method: 'POST'
    };
    
    var req = http.request(options);
    
    req.on('error', function (e) {
        console.log('Problem uploading data: ' + e.message);
    });
    
    var postData = JSON.stringify({ robotName: "B15", brightness : brightnessValue, distance: dist, sampleTime: new Date() })
    req.write(postData);
    req.end();
    
    if(sendRes)
    {
        res.header('Cache-Control', 'private, no-cache, no-store, must-revalidate');
        res.header('Expires', '-1');
        res.header('Pragma', 'no-cache');
        res.send("{success=\"ok\"}");
    }
}

var bot = new Bot();

/* GET home page. */
router.get('/', function(req, res, next) {
    console.log('Processing bot command - ' + req.query.cmd);
    switch (req.query.cmd) {
        case "move":
            bot.move(req.query.dst, res);
            break;
        case "rotate":
            bot.rotate(req.query.deg, res);
           break
        case "upload":
            bot.upload(req.query.dst, res, true);
           break
        default:
            res.send("Unknown command " + req.query.cmd);
            break;
    }
});

module.exports = router;
