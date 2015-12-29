using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherReporter
{
    interface IWeatherDataProvider
    {
        double GetTemperature();
        double GetHumidity();
    }
}
