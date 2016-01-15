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

var http = require('http');

var responseCount = 0;
var robotData = [];

function handleDataUpload(data)
{
	console.dir(data);
    var objData = JSON.parse(data);
    robotData.push(objData);
}

function handleUploadRequest(req, res)
{
    var body = "";
    req.on('data', function (chunk)
    {
        body += chunk;
    });

    req.on('end', function ()
    {
        handleDataUpload(body);

        res.writeHead(200);
        res.end();
    });
}

function formatTime(d)
{    
    return d.getHours() + ":" + (d.getMinutes() < 10 ? "0" : "") + d.getMinutes() + ":" + d.getSeconds() + "." + d.getMilliseconds();
}

function handleRequest(req, res)
{
	console.log(req.url);
    var responseCode = 204;
    if(robotData.length === 0)
    {
        res.writeHead(responseCode, { 'Content-Type': 'text/plain' });
        res.write("No robot data yet.\n");
		console.log("No robot data yet");
        res.end();
        
        return;
    }
    
    var responseStr = "";
    var timeStamp = formatTime(new Date()) + "-#" + (responseCount++);
    
    switch(req.url)
    {
        case "/time":
            {
                var startTime = robotData[0].sampleTime;
                var endTime = robotData[robotData.length - 1].sampleTime;
                
                responseCode = 200;
                responseStr = "Samples collected between " + formatTime(new Date(startTime)) + " and " + formatTime(new Date(endTime));
                break;
            }
        case "/bright":
            {
                var totalBrightness = 0;
                for(var i = 0; i < robotData.length; ++i)
                {
                    totalBrightness += robotData[i].brightness;
                }
                
                responseCode = 200;
                responseStr = "Average brightness is " + (totalBrightness / robotData.length);
                break;
            }
        default:
            {
                responseStr = "Error unknown data request " + req.url;
                break;
            }
    }
	
    res.writeHead(responseCode, { 'Content-Type': 'text/plain', 'Cache-control': 'no-cache', 'Access-Control-Allow-Origin': '*' });
    res.write(responseStr + " @" + timeStamp + "\n");
    res.end();
}

var server = http.createServer(handleRequest);
server.listen(1337);

var upServer = http.createServer(handleUploadRequest);
upServer.listen(1338);

