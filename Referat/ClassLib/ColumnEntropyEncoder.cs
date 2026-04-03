using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class ColumnEntropyEncoder : BaseColumnEncoder
    {
        public override string Name => "Entropy Encoding";

        protected override Dictionary<string, double> GetEncoding(List<string> categories, List<double> targetValues)
        {
            var encoding = new Dictionary<string, double>();
            var frequency = new Dictionary<string, int>();

            // Считаем частоту каждой категории
            foreach (var cat in categories)
            {
                if (!frequency.ContainsKey(cat))
                    frequency[cat] = 0;
                frequency[cat]++;
            }

            // Вычисляем энтропию Шеннона для каждой категории
            double total = categories.Count;
            foreach (var kvp in frequency)
            {
                double probability = kvp.Value / total;
                // H = -p * log2(p)
                double entropy = -probability * Math.Log2(probability + 0.0001); // +0.0001 для защиты от log(0)
                encoding[kvp.Key] = entropy;
            }

            return encoding;
        }

        protected override int GetDimensionality(Dictionary<string, double> encoding)
        {
            return encoding.Count;
        }
    }
}