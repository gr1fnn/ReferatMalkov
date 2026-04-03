using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class ColumnTargetEncoder : BaseColumnEncoder
    {
        public override string Name => "Target Encoding";

        protected override Dictionary<string, double> GetEncoding(List<string> categories, List<double> targetValues)
        {
            var encoding = new Dictionary<string, double>();
            var stats = new Dictionary<string, (double sum, int count)>();

            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                if (!stats.ContainsKey(cat))
                    stats[cat] = (0, 0);

                var s = stats[cat];
                s.sum += targetValues[i];
                s.count++;
                stats[cat] = s;
            }

            double globalMean = targetValues.Average();
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

        protected override int GetDimensionality(Dictionary<string, double> encoding)
        {
            return encoding.Count;
        }
    }
}