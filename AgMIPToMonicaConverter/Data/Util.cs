using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgMIPToMonicaConverter.Data
{
    public class Util
    {
        public static bool HasMissingParameter(List<string> paramentersToCheck, string parameterType, JToken token, ref string errorOut)
        {
            List<string> missingParameters = new List<string>();
            foreach (var parameter in paramentersToCheck)
            {
                if (token[parameter] == null) missingParameters.Add(parameter);
            }
            if (missingParameters.Count > 0)
            {
                Console.WriteLine("Missing " + parameterType + " parameter in:");
                Console.WriteLine(token.ToString());
                Console.Write("Missing: ");
                missingParameters.ForEach(p => Console.WriteLine(p));

                errorOut += "Missing " + parameterType + " parameter in :\r\n";
                errorOut += token.ToString() + "\r\n";
                errorOut += "Missing: \r\n";
                errorOut += String.Join("\r\n", missingParameters);
                errorOut += "\r\n\r\n";

                return true;
            }
            return false;
        }
    }
}
