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
        private CancellationToken buttonCancel;

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
