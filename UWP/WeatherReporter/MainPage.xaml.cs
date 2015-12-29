using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace WeatherReporter
{
    public sealed partial class MainPage : Page
    {
        IPBIClient pbiClient;
        IWeatherDataProvider dataProvider;

        private const int N = 10*1000;
        private const int delay = 5*1000;
        private const string datasetName = "WeatherReport";
        private const string tableName = "WeatherData";

        async Task GenerateData()
        {
#if true
            pbiClient = new PBIClient();
#else
            pbiClient = new MockupPBIClient();
#endif

            this.statusValue.Text = "Waiting for authentication token...";

            AuthData authData = await pbiClient.AcquireDeviceCodeAsync();

            this.passcode.Text = string.Format("Navigate to {0} and enter {1}", authData.VerificationUrl, authData.UserCode);

            await pbiClient.CompleteAuthentication();

            this.statusValue.Text = "Token received, connecting to PBI...";

            this.resetButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
            this.passcode.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            await pbiClient.ConnectAsync();

            this.statusValue.Text = "Connection to PBI established. Creating dataset...";

            await pbiClient.CreateDatasetAsync(datasetName);

            this.statusValue.Text = "Dataset created";

            await ClearData();

#if true
            dataProvider = await SensorWeatherDataProvider.Create();
#else
            dataProvider = new SimulatedWeatherDataProvider();
#endif

            await GenerateWeatherData();

            this.statusValue.Text = "All data pushed to Power BI";
        }

        private async Task GenerateWeatherData()
        {
            var dataset = (await pbiClient.GetDatasetsWithNameAsync(datasetName)).First();

            this.statusValue.Text = String.Format("Pushing data into dataset {0}...", datasetName);

            for (int i = 0; i < N; i++)
            {
                List<WeatherData> elements = new List<WeatherData>() {
                    new WeatherData
                    {
                        Humidity = dataProvider.GetHumidity(),
                        Temperature = dataProvider.GetTemperature(),
                        Time = DateTime.Now
                    }
                };

                var humidityFormatted = elements[0].Humidity.ToString("F2");
                var temperatureFormatted = elements[0].Temperature.ToString("F2");

                humValue.Text = String.Format("{0}%", humidityFormatted);
                tempValue.Text = String.Format("{0}°", temperatureFormatted);

                await pbiClient.PushDataToTableAsync(dataset, tableName, elements);

                this.statusValue.Text = String.Format("[{0:t}] Pushed {1}: Temperature: {2}, Humidity: {3}", DateTime.Now, i, temperatureFormatted, humidityFormatted);

                await Task.Delay(delay);
            }
        }

        public async Task ClearData()
        {
            await pbiClient.ClearRowsAsync(datasetName, tableName);

            this.statusValue.Text = "Cleared all rows";
        }

        public MainPage()
        {
            this.InitializeComponent();

            this.resetButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            this.ipAddress.Text = String.Format("My IP address: {0}", Util.LocalIPAddress());

#pragma warning disable 4014
            GenerateData();
#pragma warning restore 4014
        }

        private async void resetButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            await ClearData();
        }
    }
}
