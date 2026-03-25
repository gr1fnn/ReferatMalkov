using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLib
{
    public static class DataLoader
    {
        public static List<string[]> LoadCSV(string filePath, bool hasHeader)
        {
            var data = new List<string[]>();

            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = ParseCSVLine(line);
                    data.Add(values);
                }
            }

            return data;
        }

        public static string[] GetColumnNames(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string firstLine = reader.ReadLine();
                if (firstLine != null)
                {
                    return ParseCSVLine(firstLine);
                }
            }
            return new string[0];
        }

        private static string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            result.Add(currentField);
            return result.ToArray();
        }
    }
}
