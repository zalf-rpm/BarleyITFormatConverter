using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AgMIPToMonicaConverter.Data
{
    /// <summary> read, extract site data for soil and set default values 
    /// </summary>
    public class SiteData
    {
        /// <summary> output filename
        /// </summary>
        public static readonly string SITE_FILENAME = "site-min.json";
        /// <summary> tiny value for comparing 
        /// </summary>
        private static readonly double TINY = 0.001;

        /// <summary> internal class for soil layer
        /// </summary>
        private class SoilLayer
        {
            public double Thickness {get; set;}
            public double SoilOrganicCarbon { get; set; }
            public double SoilBulkDensity { get; set; }
            public double Sand { get; set; }
            public double Clay { get; set; }
            public double PoreVolume { get; set; }
            public double PermanentWiltingPoint { get; set; }
            public double FieldCapacity { get; set; }
        }

        /// <summary> get data from AgMIP an convert it to monica format
        /// </summary>
        /// <param name="soilTopLayerDepth">soil layer depth top in cm</param>
        /// <param name="soilBaseLayerDepth">soil layer depth base/bottom in cm</param>
        /// <param name="depth">soil layer depth in cm</param>
        /// <param name="soilOrganicCarbonLayer">soil carbon layer in g[C]/100g[soil]</param>
        /// <param name="bulkDensity"></param>
        /// <param name="sand">part sand in [0-100] %</param>
        /// <param name="clay">part clay in [0-100] % </param>
        /// <param name="saturation"> saturation (pore volume) in [0-100] %</param>
        /// <param name="wiltingPoint">wilting point [0-100] % (m3/m3) </param>
        /// <param name="fieldCapacity">field capacity [0-100]% (m3/m3)</param>
        /// <returns></returns>
        private static SoilLayer FromAgMIP(double soilTopLayerDepth, double soilBaseLayerDepth, double depth, double soilOrganicCarbonLayer, double bulkDensity, double sand, double clay, double saturation, double wiltingPoint, double fieldCapacity)
        {
            SoilLayer soilLayer = new SoilLayer();
            if (((soilBaseLayerDepth - soilTopLayerDepth) - depth) > TINY)
            {
                throw new FormatException("soil_layer_base_depth - soil_layer_top_depth should equal depth");
            }
            soilLayer.Thickness = depth * 0.01; // from cm in m
            soilLayer.SoilOrganicCarbon = soilOrganicCarbonLayer; //g[C]/100g[soil]
            soilLayer.SoilBulkDensity = bulkDensity * 1000; // g/cm3 -> kg/m3
            soilLayer.Sand = sand * 0.01; // recheck
            soilLayer.Clay = clay * 0.01;
            soilLayer.PoreVolume = saturation * 0.01;
            soilLayer.PermanentWiltingPoint = wiltingPoint * 0.01; //vol% [0-1] (m3/m3)
            soilLayer.FieldCapacity = fieldCapacity * 0.01; // vol% [0-1] (m3/m3)
            return soilLayer;
        }

        /// <summary> extract soil data and write a site file
        /// </summary>
        /// <param name="outpath"></param>
        /// <param name="agMipJson"></param>
        public static void ExtractSoilData(string outpath, JObject agMipJson)
        {
            IList<JToken> results = agMipJson["soils"].First["soilLayer"].Children().ToList();
            List<SoilLayer> soilLayers = new List<SoilLayer>();
            foreach (JToken token in results)
            {
                double depth = (double)token["depth"].ToObject(typeof(double)); // not documented in standard = thickness
                double soilTopLayerDepth = (double)token["sllt"].ToObject(typeof(double));
                double soilBaseLayerDepth = (double)token["sllb"].ToObject(typeof(double));

                double soilOrganicCarbonLayer = (double)token["sloc"].ToObject(typeof(double));
                double inertOrganicCarbonLayer = (double)token["slic"].ToObject(typeof(double));    // Inert organic carbon by layer

                double wiltingPoint = (double)token["slwp"].ToObject(typeof(double));  // Soil water content (wilting point) at 15 atmosphere pressure
                double fieldWaterCapacity = (double)token["slfc1"].ToObject(typeof(double)); // Soil water content at 1/3 atmosphere pressure
                double saturation = (double)token["slsat"].ToObject(typeof(double)); // Soil water, saturated

                double bulkDensity = (double)token["sabdm"].ToObject(typeof(double)); // 	Soil bulk density, moist, determined on field sample g/cm3	

                double sand = (double)token["slsnd"].ToObject(typeof(double));
                double clay = (double)token["slcly"].ToObject(typeof(double));
                double silt = (double)token["slsil"].ToObject(typeof(double)); //schluff
                SoilLayer soilLayer = SiteData.FromAgMIP(soilTopLayerDepth, soilBaseLayerDepth, depth, soilOrganicCarbonLayer, bulkDensity, sand, clay, saturation, wiltingPoint, fieldWaterCapacity);
                soilLayers.Add(soilLayer);
            }
            SaveSoilData(outpath, soilLayers);
        }

        /// <summary> save site data file, fill up missing configurations with defaults
        /// </summary>
        /// <param name="outpath"></param>
        /// <param name="soilLayers"></param>
        private static void SaveSoilData(string outpath, List<SoilLayer> soilLayers)
        {
            JObject rss =
                new JObject(
                            new JProperty("SiteParameters",
                                new JObject(
                                    new JProperty("Latitude", 52.80939865112305),
                                    new JProperty("Slope", 0),
                                    new JProperty("HeightNN", new JArray(0, "m")),
                                    new JProperty("NDeposition", new JArray(30, "kg N ha-1 y-1")),
                                    new JProperty("SoilProfileParameters",
                                        new JArray(
                                            from p in soilLayers
                                            select new JObject(
                                                new JProperty("Thickness", new JArray(p.Thickness, "m")),
                                                new JProperty("SoilOrganicCarbon", new JArray(p.SoilOrganicCarbon, "%")),
                                                new JProperty("SoilBulkDensity", new JArray(p.SoilBulkDensity, "kg m-3")),
                                                new JProperty("Sand", p.Sand),
                                                new JProperty("Clay", p.Clay),
                                                new JProperty("PoreVolume", p.PoreVolume),
                                                new JProperty("PermanentWiltingPoint", p.PermanentWiltingPoint),
                                                new JProperty("FieldCapacity", p.FieldCapacity)))))),

                             new JProperty("SoilTemperatureParameters", new JArray("include-from-file", "general/soil-temperature.json")),
                             new JProperty("EnvironmentParameters",
                                 new JObject(
                                    new JProperty("=", new JArray("include-from-file", "general/environment.json")),
                                    new JProperty("LeachingDepth", 2.0),
                                    new JProperty("WindSpeedHeight", 2.5))),
                             new JProperty("SoilOrganicParameters", new JArray("include-from-file", "general/soil-organic.json")),
                             new JProperty("SoilTransportParameters", new JArray("include-from-file", "general/soil-transport.json")),
                             new JProperty("SoilMoistureParameters", new JArray("include-from-file", "general/soil-moisture.json"))
                     );

            File.WriteAllText(outpath + Path.DirectorySeparatorChar + SITE_FILENAME, rss.ToString());
        }
    }
}
