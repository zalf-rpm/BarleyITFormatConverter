using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AgMIPToMonicaConverter.Data
{
    /// <summary> read, extract crop data and set default values 
    /// </summary>
    public class Cultivation
    {
        /// <summary> output filename
        /// </summary>
        private static readonly string CROP_FILENAME = "crop.json";

        /// <summary> internal class for workstep 
        /// </summary>
        private class CropRoationWorkstep
        {
            public string WorkstepType { get; set; } // "type": "AutomaticSowing"
            public string Crop { get; set; } // crop as reference "crop": ["ref", "crops", "WW"] or struct
            public DateTime Isodate { get; set; } 
            public double FertilizerAmount { get; set; } // "amount": [125, "kg N"],
            public bool IsWinterCrop { get; set; }
            public double PlantsPerSquareMeter { get; set; }

            /// <summary> constructor for workstep
            /// </summary>
            public CropRoationWorkstep()
            {
                this.FertilizerAmount = 0;
                this.Isodate = DateTime.MinValue;
                this.Crop = "";
                this.WorkstepType = "";
                this.IsWinterCrop = false;
                this.PlantsPerSquareMeter = 0;
            }
        }

        /// <summary> create workstep for given 
        /// </summary>
        /// <param name="date">(required) date for workstep execution </param>
        /// <param name="cropID">(required) crop id</param>
        /// <param name="fertilizerAmount">required for workstep 'fertilizer', else ignored</param>
        /// <param name="type">type of workstep</param>
        /// <param name="isWinterCrop">identify if crop is a winter crop</param>
        /// <param name="plantsPerSqm">required for workstep 'planting' plant per square meter</param>
        /// <returns>workstep</returns>
        private static CropRoationWorkstep ExtractCropRoationWorkstep(string date, string cropID, double fertilizerAmount, string type, bool isWinterCrop, double plantsPerSqm)
        {
            CropRoationWorkstep cropRoationWorkstep = new CropRoationWorkstep();
            cropRoationWorkstep.Isodate = DateTime.ParseExact(date, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            cropRoationWorkstep.Crop = cropID;

            switch (type)
            {
                case "planting":
                    cropRoationWorkstep.WorkstepType = "Sowing";
                    cropRoationWorkstep.IsWinterCrop = isWinterCrop;
                    cropRoationWorkstep.PlantsPerSquareMeter = plantsPerSqm;
                    break;
                case "fertilizer":
                    // use MineralFertilization since there is no distinction between organic an mineral 
                    cropRoationWorkstep.WorkstepType = "MineralFertilization";
                    cropRoationWorkstep.FertilizerAmount = fertilizerAmount;
                    break;
                case "tillage":
                    cropRoationWorkstep.WorkstepType = "Tillage";
                    break;
                default:
                    cropRoationWorkstep.WorkstepType = "unknown type";
                    break;

            }

            return cropRoationWorkstep;
        }

        /// <summary> convert workstep to json object
        /// </summary>
        /// <param name="step">workstep </param>
        /// <returns></returns>
        private static JObject CropRoationWorkstepToJSON(CropRoationWorkstep step)
        {
            JObject jObject = new JObject(
                                    new JProperty("date", step.Isodate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
                                    new JProperty("type", step.WorkstepType));

            if (step.WorkstepType == "Sowing")
            {
                if (step.Crop == "BAR")
                {
                    if (step.IsWinterCrop && step.Crop == "BAR")
                    {
                        jObject.Add(new JProperty("crop", new JArray("ref", "crops", "WG")));
                    }
                    else
                    {
                        jObject.Add(new JProperty("crop", new JArray("ref", "crops", "SG")));
                    }
                }
                jObject.Add(new JProperty("PlantDensity", new JArray(step.PlantsPerSquareMeter, "plants m-2")));
            }
            if (step.WorkstepType == "Tillage")
            {
                jObject.Add(new JProperty("depth", new JArray(0.30, "m") ));
            }
            if (step.WorkstepType == "MineralFertilization")
            {
                jObject.Add(new JProperty("amount", new JArray(step.FertilizerAmount, "kg N")));
                jObject.Add(new JProperty("partition", new JArray("ref", "fert-params", "AN")));
            }

            return jObject;
        }

        /// <summary> extract workstep data and them save to file
        /// </summary>
        /// <param name="outpath"></param>
        /// <param name="agMipJson"></param>
        public static void ExtractCropData(string outpath, JObject agMipJson)
        {
            List<Cultivation.CropRoationWorkstep> cropRoationWorksteps = new List<Cultivation.CropRoationWorkstep>();
            IList<JToken> eventData = agMipJson["experiments"].First["management"]["events"].Children().ToList();
            double yield = (double)agMipJson["experiments"].First["observed"]["hwam"].ToObject(typeof(double)); //  (dry wt) kg/ha

            string plantingDateStr = agMipJson["experiments"].First["management"]["pdate"].ToString();
            string harvestDateStr = agMipJson["experiments"].First["management"]["hadate"].ToString();
            DateTime plantingDate = DateTime.ParseExact(plantingDateStr, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            DateTime harvestDate = DateTime.ParseExact(harvestDateStr, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

            bool isWinterCrop = harvestDate.DayOfYear < plantingDate.DayOfYear;

            foreach (JToken token in eventData)
            {
                string date = token["date"].ToString();
                string eventName = token["event"].ToString();
                string crop = "BAR";
                double plantsPerSqm = 0;
                if (token.Contains("crid")) crop = token["event"].ToString();
                double feAmount = 0;
                if (token["feamn"] != null)
                {
                    feAmount = (double)token["feamn"].ToObject(typeof(double));
                }
                if (token["plpop"] != null)
                {
                    plantsPerSqm = (double)token["plpop"].ToObject(typeof(double));
                }
                cropRoationWorksteps.Add(Cultivation.ExtractCropRoationWorkstep(date, crop, feAmount, eventName, isWinterCrop, plantsPerSqm));
            }

            Cultivation.CropRoationWorkstep harvestEvent = new Cultivation.CropRoationWorkstep();
            harvestEvent.WorkstepType = "Harvest";
            harvestEvent.Isodate = DateTime.ParseExact(harvestDateStr, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            cropRoationWorksteps.Add(harvestEvent);
            var sortedSteps = cropRoationWorksteps.OrderBy(c => c.Isodate); 

            SaveCropData(outpath, sortedSteps);
        }

        /// <summary> save crop data and default values
        /// </summary>
        /// <param name="outpath">out path</param>
        /// <param name="cropRoationWorksteps"> crop rotation worksteps</param>
        private static void SaveCropData(string outpath, IOrderedEnumerable<Cultivation.CropRoationWorkstep> cropRoationWorksteps)
        {
            JObject rss =
                new JObject(
                     new JProperty("fert-params",
                         new JObject(
                            new JProperty("AN", new JArray("include-from-file", "mineral-fertilisers/AN.json")),
                            new JProperty("CADLM", new JArray("include-from-file", "organic-fertilisers/CADLM.json")))),
                     new JProperty("crops",
                        new JObject(
                           new JProperty("WG", new JObject(
                               new JProperty("is-winter-crop", true),
                               new JProperty("cropParams", new JObject(
                                   new JProperty("species", new JArray("include-from-file", "crops/barley.json")),
                                   new JProperty("cultivar", new JArray("include-from-file", "crops/barley/winter-barley.json")))),
                               new JProperty("residueParams", new JArray("include-from-file", "crop-residues/barley.json")))),
                           new JProperty("SG", new JObject(
                               new JProperty("is-winter-crop", false),
                               new JProperty("cropParams", new JObject(
                                   new JProperty("species", new JArray("include-from-file", "crops/barley.json")),
                                   new JProperty("cultivar", new JArray("include-from-file", "crops/barley/spring-barley.json")))),
                               new JProperty("residueParams", new JArray("include-from-file", "crop-residues/barley.json")))))),
                    new JProperty("cropRotation",
                        new JArray(
                            new JObject(
                            new JProperty("worksteps",
                                new JArray(
                                    from p in cropRoationWorksteps
                                    select Cultivation.CropRoationWorkstepToJSON(p)))))),
                    new JProperty("CropParameters", new JArray("include-from-file", "general/crop.json"))
                    );
            File.WriteAllText(outpath + Path.DirectorySeparatorChar + CROP_FILENAME, rss.ToString());
        }
    }

}
