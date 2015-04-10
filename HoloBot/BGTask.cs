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
        private readonly int port = 50001;
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
