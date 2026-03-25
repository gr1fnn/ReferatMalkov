using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class CatBoostEncoder : BaseEncoder
    {
        public override string Name => "CatBoost Encoding";

        protected override Dictionary<string, double> GetEncoding(List<DataPoint> dataset)
        {
            var encoding = new Dictionary<string, double>();
            var stats = new Dictionary<string, (double sum, int count)>();
            double globalMean = dataset.Average(p => p.TargetValue);
            double priorStrength = 1.0;

            // Упорядоченное кодирование
            for (int i = 0; i < dataset.Count; i++)
            {
                var point = dataset[i];
                foreach (var category in point.Categories)
                {
                    if (!stats.ContainsKey(category))
                        stats[category] = (0, 0);

                    var s = stats[category];
                    double encoded = s.count > 0 ? s.sum / s.count : globalMean;
                    encoded = (encoded * s.count + globalMean * priorStrength) / (s.count + priorStrength);
                    encoding[category] = encoded;

                    s.sum += point.TargetValue;
                    s.count++;
                    stats[category] = s;
                }
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