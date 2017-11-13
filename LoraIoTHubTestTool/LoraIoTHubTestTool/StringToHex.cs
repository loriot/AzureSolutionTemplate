using System;
using System.Collections.Generic;
using System.Text;

namespace LoraIoTHubTestTool
{
    class StringToHex
    {

        public static string ConvertToHex(string stringToConvert)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char t in stringToConvert)
                sb.Append(Convert.ToInt32(t).ToString("x"));
            return sb.ToString();
        }
    }
}
