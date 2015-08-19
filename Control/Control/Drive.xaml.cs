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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Input;
using System.Threading;

namespace Control
{
    public sealed partial class Drive : Page
    {
        private string connectionName = null;
        private CancellationToken 
            buttonCancel;

        public Drive()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            connectionName = e.Parameter as string;
            base.OnNavigatedTo(e);
        }

        private void sendCommandToBot(string command, string paramName, string value)
        {
            if (command != null && paramName != null && value != null)
            {
                string url = "http://" + connectionName + ":3000/bot?cmd=" + command + "&" + paramName + "=" + value;
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                webRequest.BeginGetResponse(BotRequest_Async, null);

            }
        }

        private void BotRequest_Async(IAsyncResult result)
        {
            var req = result.AsyncState as HttpWebRequest;
            try
            {
                var resp = req.EndGetResponse(result);
            }
            catch
            {

            }
        }

        private void RotateButton_OnClick(object sender, RoutedEventArgs e)
        {
            var el = sender as Button;
            sendCommandToBot("rotate", "deg", el.Tag as string);

        }

        private void MoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var el = sender as Button;
            sendCommandToBot("move", "dst", el.Tag as string);
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
