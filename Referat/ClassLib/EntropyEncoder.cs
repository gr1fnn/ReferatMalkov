using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class EntropyEncoder : BaseEncoder
    {
        public override string Name => "Entropy Encoding";

        protected override Dictionary<string, double> GetEncoding(List<DataPoint> dataset)
        {
            var encoding = new Dictionary<string, double>();
            var frequency = new Dictionary<string, int>();

            // Считаем частоту категорий
            foreach (var point in dataset)
            {
                foreach (var category in point.Categories)
                {
                    if (!frequency.ContainsKey(category))
                        frequency[category] = 0;
                    frequency[category]++;
                }
            }

            // Вычисляем энтропию Шеннона
            double totalSamples = dataset.Count;
            foreach (var kvp in frequency)
            {
                double probability = kvp.Value / totalSamples;
                encoding[kvp.Key] = -probability * Math.Log2(probability + 0.0001);
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