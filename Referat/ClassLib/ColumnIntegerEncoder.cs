using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class ColumnIntegerEncoder : BaseColumnEncoder
    {
        public override string Name => "Integer Encoding";

        protected override Dictionary<string, double> GetEncoding(List<string> categories, List<double> targetValues)
        {
            var encoding = new Dictionary<string, double>();
            int nextId = 0;

            foreach (var cat in categories)
            {
                if (!encoding.ContainsKey(cat))
                {
                    encoding[cat] = nextId++;
                }
            }

            return encoding;
        }

        protected override int GetDimensionality(Dictionary<string, double> encoding)
        {
            return 1;
        }
    }
}