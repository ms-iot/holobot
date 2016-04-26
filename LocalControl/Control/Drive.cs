using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.UI.Xaml;

namespace Control
{
    class Drive
    {
        HoloLensRobot _robot;
        Gamepad _controller;
        DispatcherTimer _timer = new DispatcherTimer();

        public Drive (HoloLensRobot robot)
        {
            _robot = robot;
        }

        public void initialize()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(300);
            _timer.Tick += _timer_Tick;
            _timer.Start();
            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
        }

        private async void _timer_Tick(object sender, object e)
        {
            if (_controller == null)
            {
                return;
            }

            GamepadReading reading = _controller.GetCurrentReading();

            double xReading = reading.LeftThumbstickX;
            double yReading = reading.LeftThumbstickY;

            await _robot.MoveAnalog(xReading, yReading);
        }

        private void Gamepad_GamepadRemoved(object sender, Gamepad e)
        {
            if (_controller == e)
            {
                _controller = null;
            }
        }

        private void Gamepad_GamepadAdded(object sender, Gamepad e)
        {
            if (_controller == null)
            {
                _controller = e;
            }
        }
    }
}
