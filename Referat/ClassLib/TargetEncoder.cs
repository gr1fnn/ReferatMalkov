using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class TargetEncoder : BaseEncoder
    {
        public override string Name => "Target Encoding";

        protected override Dictionary<string, double> GetEncoding(List<DataPoint> dataset)
        {
            var encoding = new Dictionary<string, double>();
            var stats = new Dictionary<string, (double sum, int count)>();

            foreach (var point in dataset)
            {
                foreach (var category in point.Categories)
                {
                    if (!stats.ContainsKey(category))
                        stats[category] = (0, 0);

                    var s = stats[category];
                    s.sum += point.TargetValue;
                    s.count++;
                    stats[category] = s;
                }
            }

            double globalMean = dataset.Average(p => p.TargetValue);
            double smoothingFactor = 10;

            foreach (var kvp in stats)
            {
                double categoryMean = kvp.Value.sum / kvp.Value.count;
                double smoothed = (categoryMean * kvp.Value.count + globalMean * smoothingFactor)
                                    / (kvp.Value.count + smoothingFactor);
                encoding[kvp.Key] = smoothed;
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