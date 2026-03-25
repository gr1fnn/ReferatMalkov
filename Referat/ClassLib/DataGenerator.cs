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

        public List<DataPoint> GenerateHighDimensionalData(int numSamples, int numFeatures)
        {
            var data = new List<DataPoint>();

            for (int i = 0; i < numSamples; i++)
            {
                var categories = new string[numFeatures];
                for (int j = 0; j < numFeatures; j++)
                {
                    // Генерируем категории с разной частотой встречаемости
                    int numCategories = random.Next(10, 100);
                    int categoryIndex = random.Next(numCategories);
                    categories[j] = $"cat_{j}_{categoryIndex}";
                }

                // Целевая переменная с семантической связью с категориями
                double target = CalculateTargetWithSemantics(categories);
                data.Add(new DataPoint(categories, target));
            }

            return data;
        }

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
