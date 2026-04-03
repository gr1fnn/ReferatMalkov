using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class ColumnOneHotEncoder : BaseColumnEncoder
    {
        public override string Name => "One-Hot Encoding";

        protected override Dictionary<string, double> GetEncoding(List<string> categories, List<double> targetValues)
        {
            var encoding = new Dictionary<string, double>();
            var frequency = new Dictionary<string, int>();

            foreach (var cat in categories)
            {
                if (!frequency.ContainsKey(cat))
                    frequency[cat] = 0;
                frequency[cat]++;
            }

            foreach (var kvp in frequency)
            {
                encoding[kvp.Key] = kvp.Value / (double)categories.Count;
            }

            return encoding;
        }

        protected override int GetDimensionality(Dictionary<string, double> encoding)
        {
            return encoding.Count;
        }
    }
}