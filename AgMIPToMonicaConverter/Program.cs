using AgMIPToMonicaConverter.Data;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace AgMIPToMonicaConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            // default filename in same folder as executable
            string filename = "Barley_IT_AgMIP.json";
            string filenameErrorOut = "filenameErrorOut.txt";
            // default output path to user Documents
            string outpath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "AgMIPToMonicaOut";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-filename" && i + 1 < args.Length)
                {
                    filename = args[i + 1];
                }
                if (args[i] == "-out" && i + 1 < args.Length)
                {
                    outpath = args[i + 1];
                }
            }
            // check if input file exists
            if (!File.Exists(filename))
            {
                Console.WriteLine("Input file {0} does not exist!", filename);
                Environment.Exit(10);
            }
            // check if output path exists or can be created
            if (!Directory.Exists(outpath))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(outpath))
                    {
                        Directory.CreateDirectory(outpath);
                    }
                    else
                    {
                        Console.WriteLine("Invaild output directory!");
                        Environment.Exit(10);
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine("An error occured:");
                    Console.WriteLine(ex.Message);
                    Environment.Exit(10);
                }
            }

            // read json file, extract relevant data, write to file
            try
            {
                string jsonText = File.ReadAllText(filename);
                JObject agMipJson = JObject.Parse(jsonText);
                string errorOut = "";
                ClimateData.ExtractWeatherData(outpath, agMipJson, ref errorOut);
                SiteData.ExtractSoilData(outpath, agMipJson, ref errorOut);
                Cultivation.ExtractCropData(outpath, agMipJson);

                if (!string.IsNullOrWhiteSpace(errorOut))
                {
                    File.WriteAllText(outpath + Path.DirectorySeparatorChar + filenameErrorOut, errorOut.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured:");
                Console.WriteLine(ex.Message);
                Console.ReadKey();
                Environment.Exit(10);
            }
        }
    }
}
