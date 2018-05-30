using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AgMIPToMonicaConverter.Data
{
    /// <summary> extract climate data / weather data for each day
    /// </summary>
    public class ClimateData
    {
        /// <summary> output filename
        /// </summary>
        public static readonly string CLIMATE_FILENAME = "climate.csv";
        /// <summary> define column names for csv file
        /// </summary>
        public static readonly string[] MinWeatherData = { "iso-date", "tavg", "tmin", "tmax", "wind", "globrad", "precip", "relhumid" };
        /// <summary> define measurement units line for csv file
        /// </summary>
        public static readonly string[] TypeLine = { "", "C_deg", "C_deg", "C_deg", "m/s", "MJ m-2 d-1", "mm", "%" };
        /// <summary> define csv sepeparator char
        /// </summary>
        public static readonly string CsvSeperator = ",";

        /// <summary> internal class daily weather 
        /// </summary>
        private class DailyWeather
        {
            public DateTime Isodate { get; set; } // iso-date -> YYYY-MM-DD
            public double DailyTemperatureAverage { get; set; } //tavg degree celsius
            public double DailyTemperatureMin { get; set; }    // tmin degree celsius
            public double DailyTemperatureMax { get; set; }    // tmax degree celsius
            public double SunRadiation { get; set; } // MJ m-2 d-1
            public double Precip { get; set; } // Precipitation %
            public double Relativehumidity { get; set; }
            public double Wind { get; set; } // wind in m/s
        }

        /// <summary> extract climate data from json object and write it into an output file  
        /// </summary>
        /// <param name="outpath"> out path</param>
        /// <param name="agMipJson"> agMipJson object </param>
        public static void ExtractWeatherData(string outpath, JObject agMipJson)
        {
            IList<JToken> results = agMipJson["weathers"].First["dailyWeather"].Children().ToList();

            List<DailyWeather> dailyWeathers = new List<DailyWeather>();

            foreach (JToken token in results)
            {
                String date = token["w_date"].ToString();
                double rain = (double)token["rain"].ToObject(typeof(double));
                double tavg = (double)token["tavd"].ToObject(typeof(double));
                double tmin = (double)token["tmin"].ToObject(typeof(double));
                double tmax = (double)token["tmax"].ToObject(typeof(double));
                double humidity = (double)token["rhavd"].ToObject(typeof(double));
                double radiation = (double)token["srad"].ToObject(typeof(double));
                double wind = (double)token["wind"].ToObject(typeof(double));
                DailyWeather dailyWeather = ClimateData.FromAgMIP(date, tavg, tmin, tmax, radiation, rain, humidity, wind);
                dailyWeathers.Add(dailyWeather);
            }
            SaveClimateData(outpath, dailyWeathers);
        }

        /// <summary> convert paramenters to monica measurement units
        /// </summary>
        /// <param name="date"> date as string in yyyyMMdd</param>
        /// <param name="tempAvgDegC"> avarage temperature in degree celsius </param>
        /// <param name="tempMinDegC">minimal temperature in degree celsius </param>
        /// <param name="tempMaxDegC">maximal temperature in degree celsius </param>
        /// <param name="radiation">sun radiation in MJ m-2 d-1</param>
        /// <param name="precipitation">percipation in mm</param>
        /// <param name="relativehumidity">relative humidity in %</param>
        /// <param name="windKmD">wind speed in km/day </param>
        /// <returns> DailyWeather with monica values</returns>
        private static DailyWeather FromAgMIP(string date, double tempAvgDegC, double tempMinDegC, double tempMaxDegC, double radiation, double precipitation, double relativehumidity, double windKmD)
        {
            DailyWeather dayWeather = new DailyWeather();

            try
            {
                dayWeather.Isodate = DateTime.ParseExact(date, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                dayWeather.DailyTemperatureAverage = tempAvgDegC;
                dayWeather.DailyTemperatureMin = tempMinDegC;
                dayWeather.DailyTemperatureMax = tempMaxDegC;
                dayWeather.SunRadiation = radiation;
                dayWeather.Precip = precipitation;
                dayWeather.Relativehumidity = relativehumidity;
                dayWeather.Wind = windKmD / 86.4; // km/day to m/s

            }
            catch (FormatException f)
            {
                Console.WriteLine("An error occured:");
                Console.WriteLine(f.Message);
            }

            return dayWeather;
        }

        /// <summary> write DailyWeather object as text line for csv format 
        /// </summary>
        /// <param name="dailyWeather"></param>
        /// <returns></returns>
        private static string AsCSVLine(DailyWeather dailyWeather)
        {
            string result = dailyWeather.Isodate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + ClimateData.CsvSeperator;
            result += dailyWeather.DailyTemperatureAverage.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ClimateData.CsvSeperator;
            result += dailyWeather.DailyTemperatureMin.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ClimateData.CsvSeperator;
            result += dailyWeather.DailyTemperatureMax.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ClimateData.CsvSeperator;
            result += dailyWeather.Wind.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ClimateData.CsvSeperator;
            result += dailyWeather.SunRadiation.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ClimateData.CsvSeperator;
            result += dailyWeather.Precip.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ClimateData.CsvSeperator;
            result += dailyWeather.Relativehumidity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ClimateData.CsvSeperator;

            return result;
        }

        /// <summary> save data in csv file format
        /// </summary>
        /// <param name="outpath">out path</param>
        /// <param name="dailyWeathers"> list of daily weather </param>
        private static void SaveClimateData(string outpath, List<DailyWeather> dailyWeathers)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine(string.Join(ClimateData.CsvSeperator, ClimateData.MinWeatherData) + ClimateData.CsvSeperator);
            strBuilder.AppendLine(string.Join(ClimateData.CsvSeperator, ClimateData.TypeLine) + ClimateData.CsvSeperator);

            foreach (var day in dailyWeathers)
            {
                strBuilder.AppendLine(ClimateData.AsCSVLine(day));
            }
            File.WriteAllText(outpath + Path.DirectorySeparatorChar + CLIMATE_FILENAME, strBuilder.ToString());
        }
    }
}
