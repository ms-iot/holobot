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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.Media.Capture;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Control
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private HoloLensRobot robot = new HoloLensRobot();
        private Drive drive;
        private MediaCapture _mediaCaptureMgrLeft;
        private MediaCapture _mediaCaptureMgrRight;

        private DispatcherTimer refresh = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1.0 / 30.0) };


        public MainPage()
        {
            this.InitializeComponent();

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await robot.ConnectToArduino();
            drive = new Drive(robot);

            drive.initialize();

            _mediaCaptureMgrRight = await ConnectToCamera(1, CapturePreviewRight);
            _mediaCaptureMgrLeft = await ConnectToCamera(0, CapturePreviewLeft);

            refresh.Tick += Refresh_Tick;
            refresh.Start();
        }

        private void Refresh_Tick(object sender, object e)
        {
            refreshElement.Visibility = refreshElement.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task<MediaCapture> ConnectToCamera(int i, CaptureElement preview)
        {
            var manager = new Windows.Media.Capture.MediaCapture();

            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            if (allVideoDevices.Count > i)
            {
                var cameraDevice = allVideoDevices[i];

                await manager.InitializeAsync(new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id, StreamingCaptureMode = StreamingCaptureMode.Video });

                var cameraProperties = manager.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Select(x => x as VideoEncodingProperties).ToList();
                foreach (var mediaEncodingProperty in cameraProperties)
                {
                    Debug.WriteLine(mediaEncodingProperty.Width + "x" + mediaEncodingProperty.Height + " FPS: " + mediaEncodingProperty.FrameRate.Numerator + "Type:" + mediaEncodingProperty.Type + "   SubType:" + mediaEncodingProperty.Subtype);
                }


                foreach (var mediaEncodingProperty in cameraProperties)
                {
                    if (//mediaEncodingProperty.Width == 960 &&
                        //mediaEncodingProperty.Height == 544 &&
                        mediaEncodingProperty.Width == 320 &&
                        mediaEncodingProperty.Height == 240 &&
                        mediaEncodingProperty.FrameRate.Numerator == 15 &&
                        string.Compare(mediaEncodingProperty.Subtype, "YUY2") == 0)
                    {
                        Debug.WriteLine("Chosen: " + mediaEncodingProperty.Width + "x" + mediaEncodingProperty.Height + " FPS: " + mediaEncodingProperty.FrameRate.Numerator + "Type:" + mediaEncodingProperty.Type + "   SubType:" + mediaEncodingProperty.Subtype);
                        await manager.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, mediaEncodingProperty);
                        break;
                    }
                }

                preview.Source = manager;
                await manager.StartPreviewAsync();
            }

            return manager;
        }
    }
}
