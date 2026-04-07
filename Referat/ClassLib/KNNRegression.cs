using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    /// <summary>
    /// Реализация KNN для регрессии
    /// </summary>
    public class KNNRegression
    {
        private List<double[]> _trainFeatures;
        private List<double> _trainTargets;
        private int _k;

        public KNNRegression(int k = 5)
        {
            _k = k;
        }

        /// <summary>
        /// Обучение модели
        /// </summary>
        public void Fit(List<double[]> features, List<double> targets)
        {
            _trainFeatures = features;
            _trainTargets = targets;
        }

        /// <summary>
        /// Предсказание для одного объекта
        /// </summary>
        public double Predict(double[] features)
        {
            if (_trainFeatures == null || _trainFeatures.Count == 0)
                throw new InvalidOperationException("Модель не обучена");

            // Вычисляем расстояния до всех обучающих объектов
            var distances = new List<(double distance, double target)>();

            for (int i = 0; i < _trainFeatures.Count; i++)
            {
                double distance = EuclideanDistance(features, _trainFeatures[i]);
                distances.Add((distance, _trainTargets[i]));
            }

            // Сортируем по расстоянию и берем K ближайших
            var nearest = distances.OrderBy(d => d.distance).Take(_k).ToList();

            // Возвращаем среднее арифметическое целевых значений
            return nearest.Average(n => n.target);
        }

        /// <summary>
        /// Предсказание для набора объектов
        /// </summary>
        public List<double> Predict(List<double[]> features)
        {
            return features.Select(f => Predict(f)).ToList();
        }

        /// <summary>
        /// Евклидово расстояние между двумя векторами
        /// </summary>
        private double EuclideanDistance(double[] a, double[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double diff = a[i] - b[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Оценка качества модели (R²)
        /// </summary>
        public double CalculateR2(List<double> actual, List<double> predicted)
        {
            double meanActual = actual.Average();
            double ssTotal = actual.Sum(a => Math.Pow(a - meanActual, 2));
            double ssResidual = actual.Zip(predicted, (a, p) => Math.Pow(a - p, 2)).Sum();

            if (ssTotal == 0) return 0;
            return 1 - (ssResidual / ssTotal);
        }

        /// <summary>
        /// Средняя абсолютная ошибка (MAE)
        /// </summary>
        public double CalculateMAE(List<double> actual, List<double> predicted)
        {
            return actual.Zip(predicted, (a, p) => Math.Abs(a - p)).Average();
        }

        /// <summary>
        /// Среднеквадратичная ошибка (RMSE)
        /// </summary>
        public double CalculateRMSE(List<double> actual, List<double> predicted)
        {
            return Math.Sqrt(actual.Zip(predicted, (a, p) => Math.Pow(a - p, 2)).Average());
        }
    }
}