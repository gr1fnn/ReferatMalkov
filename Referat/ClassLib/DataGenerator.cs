using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLib
{
    public class DataGenerator
    {
        private Random random = new Random(42);


        private double CalculateTargetWithSemantics(string[] categories)
        {
            double target = 0;
            for (int i = 0; i < categories.Length; i++)
            {
                // Искусственная семантическая связь: некоторые категории сильнее влияют на целевую переменную
                int categoryValue = int.Parse(categories[i].Split('_')[2]);
                target += Math.Sin(categoryValue * 0.1) * Math.Exp(-i * 0.05);
            }
            target += random.NextDouble() * 0.5; // Добавляем шум
            return target;
        }
    }
}
