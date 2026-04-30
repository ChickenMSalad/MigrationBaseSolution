using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Migration.Connectors.Sources.Aem.Files
{
    public class StringMethods
    {
        public static int GetTakeValue(string filename)
        {
            // Regex to match "take" followed by one or more digits
            var match = Regex.Match(filename, @"take(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int result))
            {
                return result;
            }
            // If "take" not found, or not followed by a number, return 1
            return 1;
        }

        public static string ToAzureTableName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "DefaultTable";

            // 1. Remove all non-alphanumeric characters
            string alphanumeric = Regex.Replace(input, "[^A-Za-z0-9]", "");

            // 2. Ensure it starts with a letter
            if (string.IsNullOrEmpty(alphanumeric) || !char.IsLetter(alphanumeric[0]))
                alphanumeric = "T" + alphanumeric;

            // 3. Enforce min/max length
            if (alphanumeric.Length < 3)
                alphanumeric = alphanumeric.PadRight(3, '0');
            if (alphanumeric.Length > 63)
                alphanumeric = alphanumeric.Substring(0, 63);

            return alphanumeric;
        }
    }
}
