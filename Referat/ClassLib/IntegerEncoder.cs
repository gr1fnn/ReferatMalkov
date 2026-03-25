using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class IntegerEncoder : BaseEncoder
    {
        public override string Name => "Integer Encoding";

        protected override Dictionary<string, double> GetEncoding(List<DataPoint> dataset)
        {
            var encoding = new Dictionary<string, double>();
            int nextId = 0;

            foreach (var point in dataset)
            {
                foreach (var category in point.Categories)
                {
                    if (!encoding.ContainsKey(category))
                    {
                        encoding[category] = nextId++;
                    }
                }
            }

            return encoding;
        }

        protected override int GetDimensionality(Dictionary<string, double> encoding, List<DataPoint> dataset)
        {
            return 1; // Integer Encoding всегда создает 1 признак
        }

        public override EncodingResult EncodeAndEvaluate(List<DataPoint> dataset)
        {
            return EncodeAndEvaluateInternal(dataset);
        }
    }
}