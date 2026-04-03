using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public class ColumnCatBoostEncoder : BaseColumnEncoder
    {
        public override string Name => "CatBoost Encoding";

        protected override Dictionary<string, double> GetEncoding(List<string> categories, List<double> targetValues)
        {
            var encoding = new Dictionary<string, double>();
            var stats = new Dictionary<string, (double sum, int count)>();
            double globalMean = targetValues.Average();
            double priorStrength = 1.0;

            // Для каждой позиции вычисляем кодировку НА ОСНОВЕ ПРЕДЫДУЩИХ данных
            // и сохраняем СРЕДНЕЕ значение для каждой категории
            var categoryValues = new Dictionary<string, List<double>>();

            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];

                // Инициализируем список для категории
                if (!categoryValues.ContainsKey(cat))
                    categoryValues[cat] = new List<double>();

                // Вычисляем кодировку на основе предыдущих данных (без текущего)
                if (stats.ContainsKey(cat))
                {
                    var s = stats[cat];
                    double encoded = s.count > 0 ? s.sum / s.count : globalMean;
                    encoded = (encoded * s.count + globalMean * priorStrength) / (s.count + priorStrength);
                    categoryValues[cat].Add(encoded);
                }
                else
                {
                    // Первое вхождение - используем глобальное среднее
                    double encoded = globalMean;
                    encoded = (encoded * 0 + globalMean * priorStrength) / (0 + priorStrength);
                    categoryValues[cat].Add(encoded);
                }

                // Обновляем статистику для будущих вычислений
                if (!stats.ContainsKey(cat))
                    stats[cat] = (0, 0);

                var current = stats[cat];
                current.sum += targetValues[i];
                current.count++;
                stats[cat] = current;
            }

            // Вычисляем ФИНАЛЬНОЕ значение для каждой категории как среднее всех полученных кодировок
            foreach (var kvp in categoryValues)
            {
                encoding[kvp.Key] = kvp.Value.Average();
            }

            return encoding;
        }

        protected override int GetDimensionality(Dictionary<string, double> encoding)
        {
            return encoding.Count;
        }
    }
}