using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    /// <summary>
    /// Результат оценки регрессии
    /// </summary>
    public class RegressionResult
    {
        public string MethodName { get; set; }
        public List<string> UsedFeatures { get; set; }
        public double R2Score { get; set; }
        public double MAE { get; set; }
        public double RMSE { get; set; }
        public double ExecutionTimeMs { get; set; }

        public override string ToString()
        {
            return $"Метод: {MethodName}, R² = {R2Score:F3}, MAE = {MAE:F2}, RMSE = {RMSE:F2}, Время = {ExecutionTimeMs:F0}мс";
        }
    }

    /// <summary>
    /// Оценщик качества кодирования через регрессию
    /// </summary>
    public class RegressionEvaluator
    {
        private readonly int _k;
        private readonly double _testSize;

        /// <param name="k">Количество соседей для KNN</param>
        /// <param name="testSize">Доля тестовой выборки (0.0-1.0)</param>
        public RegressionEvaluator(int k = 5, double testSize = 0.3)
        {
            _k = k;
            _testSize = testSize;
        }

        /// <summary>
        /// Оценивает качество кодирования для указанного метода и признаков
        /// </summary>
        /// <param name="encoder">Кодировщик</param>
        /// <param name="featureColumns">Список категориальных признаков</param>
        /// <param name="targetValues">Целевая переменная</param>
        /// <param name="useTopFeatures">Сколько лучших признаков использовать (null = все)</param>
        public RegressionResult Evaluate(IColumnEncoder encoder,
            List<(string Name, List<string> Values)> featureColumns,
            List<double> targetValues,
            int? useTopFeatures = 3)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 1. Кодируем все признаки
            var encodedColumns = new List<ColumnEncodingResult>();
            foreach (var column in featureColumns)
            {
                var result = encoder.EncodeColumn(column.Name, column.Values, targetValues);
                encodedColumns.Add(result);
            }

            // 2. Сортируем по качеству и берем лучшие признаки
            var bestColumns = encodedColumns
                .OrderByDescending(c => c.OverallQuality)
                .Take(useTopFeatures ?? encodedColumns.Count)
                .ToList();

            // 3. Строим матрицу признаков
            int nSamples = targetValues.Count;
            var featuresMatrix = new List<double[]>();

            for (int i = 0; i < nSamples; i++)
            {
                var row = new List<double>();
                foreach (var column in bestColumns)
                {
                    // Получаем категорию для текущей строки
                    string category = featureColumns.First(f => f.Name == column.ColumnName).Values[i];
                    if (column.CategoryEncodings.TryGetValue(category, out double code))
                    {
                        row.Add(code);
                    }
                    else
                    {
                        row.Add(0); // fallback
                    }
                }
                featuresMatrix.Add(row.ToArray());
            }

            // 4. Разделяем на train/test
            var indices = Enumerable.Range(0, nSamples).ToList();
            var random = new Random(42); // Фиксируем seed для воспроизводимости
            var shuffledIndices = indices.OrderBy(x => random.Next()).ToList();

            int testCount = (int)(nSamples * _testSize);
            var testIndices = shuffledIndices.Take(testCount).ToList();
            var trainIndices = shuffledIndices.Skip(testCount).ToList();

            var trainFeatures = trainIndices.Select(i => featuresMatrix[i]).ToList();
            var trainTargets = trainIndices.Select(i => targetValues[i]).ToList();
            var testFeatures = testIndices.Select(i => featuresMatrix[i]).ToList();
            var testTargets = testIndices.Select(i => targetValues[i]).ToList();

            // 5. Обучаем KNN и предсказываем
            var knn = new KNNRegression(_k);
            knn.Fit(trainFeatures, trainTargets);
            var predictions = knn.Predict(testFeatures);

            // 6. Вычисляем метрики
            double r2 = knn.CalculateR2(testTargets, predictions);
            double mae = knn.CalculateMAE(testTargets, predictions);
            double rmse = knn.CalculateRMSE(testTargets, predictions);

            stopwatch.Stop();

            return new RegressionResult
            {
                MethodName = encoder.Name,
                UsedFeatures = bestColumns.Select(c => c.ColumnName).ToList(),
                R2Score = r2,
                MAE = mae,
                RMSE = rmse,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }

        /// <summary>
        /// Оценивает все методы кодирования и возвращает сравнение
        /// </summary>
        public List<RegressionResult> EvaluateAll(IColumnEncoder[] encoders,
            List<(string Name, List<string> Values)> featureColumns,
            List<double> targetValues,
            int? useTopFeatures = 3)
        {
            var results = new List<RegressionResult>();

            foreach (var encoder in encoders)
            {
                var result = Evaluate(encoder, featureColumns, targetValues, useTopFeatures);
                results.Add(result);
            }

            return results.OrderByDescending(r => r.R2Score).ToList();
        }
    }
}