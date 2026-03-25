using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class OneHotEncoder : BaseEncoder
    {
        public override string Name => "One-Hot Encoding";

        protected override Dictionary<string, double> GetEncoding(List<DataPoint> dataset)
        {
            // Для One-Hot используем частоту категории как вес
            var encoding = new Dictionary<string, double>();
            var frequency = new Dictionary<string, int>();

            foreach (var point in dataset)
            {
                foreach (var category in point.Categories)
                {
                    if (!frequency.ContainsKey(category))
                        frequency[category] = 0;
                    frequency[category]++;
                }
            }

            foreach (var kvp in frequency)
            {
                encoding[kvp.Key] = kvp.Value / (double)dataset.Count;
            }

            return encoding;
        }

        protected override int GetDimensionality(Dictionary<string, double> encoding, List<DataPoint> dataset)
        {
            return encoding.Count;
        }

        public override EncodingResult EncodeAndEvaluate(List<DataPoint> dataset)
        {
            return EncodeAndEvaluateInternal(dataset);
        }
    }
}