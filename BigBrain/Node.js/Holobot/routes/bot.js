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

var express = require('express');
var router = express.Router();
var Cylon = require('cylon');
var constants = require('./constants');
var spawn = require('child_process').spawn;
var http = require('http');

var upload = false;

function uploadData(dist, bright) {
    console.log('Uploading data: Distance Moved [%d], Brightness [%d]', dist, bright);

    var options = {
        //host: '',
        //port: '',
        //method: 'POST'
    };
    
    var req = http.request(options);
    
    req.on('error', function (e) {
        console.log('Problem uploading data: ' + e.message);
    });
    
    var postData = JSON.stringify({ robotName: "B15", brightness : bright, distance: dist, sampleTime: new Date() })
    req.write(postData);
    req.end();
}

function arcLength(deg, radius) {
    return (Math.PI * radius * deg) / 180;
}

var Bot = function() {
    this.lastCommand = "";
    this.moveCount = 0;
    this.armMove = null;

}

Bot.prototype.moveFinish = function() {
    this.moveCount --;
    if (this.moveCount == 0) {
        this.lastCommand = "";
    }
}

Bot.prototype.stop = function(res) {
    Cylon.MCP.robots.B15.stepperRight.stop();
    Cylon.MCP.robots.B15.stepperLeft.stop();
    this.moveCount = 0;
    res.send("{success=\"ok\"}");
}

Bot.prototype.move = function(distance, res) {
    console.log('Moving ' + distance);

    var moveCountR = function() {
        this.moveFinish();
        if (upload) {
            var brightness = Cylon.MCP.robots.B15.lightSensor.analogRead();
            uploadData(distance, brightness);
        }
    }.bind(this);

    var moveCountL = function () {
        this.moveFinish();
    }.bind(this);
    
    if (Cylon.MCP.robots.B15.stepperRight.move(constants.stepsPerCM * distance, moveCountR) &&
        Cylon.MCP.robots.B15.stepperLeft.move(constants.stepsPerCM * distance, moveCountL)) {
        this.moveCount = 2;
        this.lastCommand = "move";
        res.send("{success=\"ok\"}");
    } else {
        this.stop();
        res.send("{success=\"ERROR: already moving\"}");
    }
}

Bot.prototype.rotate = function(deg, res) {
    console.log('Rotating ' + deg);

    var moveCount = function() {
        this.moveFinish();
    }.bind(this);

    var lengthInCM = arcLength(deg, constants.wheelBaseRadius);
    if (Cylon.MCP.robots.B15.stepperRight.move(constants.stepsPerCM * lengthInCM, moveCount) &&
        Cylon.MCP.robots.B15.stepperLeft.move(-constants.stepsPerCM * lengthInCM, moveCount)) {
        this.moveCount = 2;
        this.lastCommand = "rotate";
        res.send("{success=\"ok\"}");
    } else {
        this.stop();
        res.send("{success=\"ERROR: already moving\"}");
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
        default:
            res.send("Unknown command " + req.query.cmd);
            break;
    }
});

module.exports = router;
