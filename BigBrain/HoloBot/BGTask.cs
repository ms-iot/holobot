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

using System;
using Windows.ApplicationModel.Background;
using Windows.System.Threading;

namespace HoloBot
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral taskDeferral;
        private ThreadPoolTimer RobotStateTimer;
        private RobotHttpServer server;
        private readonly int port = 3000;
        private readonly int TaskIntervalSeconds = 15;
        internal HoloLensRobot Bot;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Ensure our background task remains running
            taskDeferral = taskInstance.GetDeferral();

            // Example: Create a timer-initiated ThreadPool task
            RobotStateTimer = ThreadPoolTimer.CreatePeriodicTimer(PopulateRobotStateData, TimeSpan.FromSeconds(TaskIntervalSeconds));

            Bot = new HoloLensRobot();

            // Start the server
            server = new RobotHttpServer(port, Bot);
            var asyncAction = ThreadPool.RunAsync((w) => { server.StartServer(); });

            // Task cancellation handler, release our deferral there 
            taskInstance.Canceled += OnCanceled;
        }

        private void PopulateRobotStateData(ThreadPoolTimer timer)
        {
            // DO STUFF ON A TIMED INTERVAL IF NEEDED
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            // Relinquish our task deferral
            taskDeferral.Complete();
        }
    }


}
