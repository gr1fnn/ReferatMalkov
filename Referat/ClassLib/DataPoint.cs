using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLib
{
    // Класс для хранения данных
    public class DataPoint
    {
        public string[] Categories { get; set; }
        public double TargetValue { get; set; }

        public DataPoint(string[] categories, double target)
        {
            Categories = categories;
            TargetValue = target;
        }
    }
}
