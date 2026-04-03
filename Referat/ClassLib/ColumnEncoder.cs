using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ClassLib
{
    /// <summary>
    /// Результат кодирования одного категориального столбца.
    /// Содержит все метрики качества, время выполнения и полученные коды.
    /// </summary>
    public class ColumnEncodingResult
    {
        /// <summary>Название категориального признака (название колонки в CSV)</summary>
        public string ColumnName { get; set; }

        /// <summary>Название метода кодирования (Integer, One-Hot, Target и т.д.)</summary>
        public string MethodName { get; set; }

        /// <summary>Количество уникальных значений в категориальном признаке</summary>
        public int UniqueValuesCount { get; set; }

        /// <summary>Размерность после кодирования (сколько числовых признаков создано)</summary>
        public int Dimensionality { get; set; }

        /// <summary>Время выполнения кодирования в миллисекундах</summary>
        public double EncodingTime { get; set; }

        /// <summary>Коэффициент корреляции Пирсона (линейная связь между кодом и целью)</summary>
        public double Correlation { get; set; }

        /// <summary>Коэффициент корреляции Спирмена (монотонная связь между кодом и целью)</summary>
        public double SpearmanCorrelation { get; set; }

        /// <summary>Нормированная взаимная информация (нелинейная связь между кодом и целью)</summary>
        public double MutualInformation { get; set; }

        /// <summary>Коэффициент детерминации R² (насколько код предсказывает цель)</summary>
        public double R2Score { get; set; }

        /// <summary>Итоговая комплексная оценка качества (0-100%)</summary>
        public double OverallQuality { get; set; }

        /// <summary>Словарь: категория → её числовой код</summary>
        public Dictionary<string, double> CategoryEncodings { get; set; }

        /// <summary>Краткое строковое представление результата</summary>
        public override string ToString()
        {
            return $"{ColumnName} ({MethodName}): Качество={OverallQuality:F1}%, Корр={Correlation:F3}, Время={EncodingTime:F0}мс";
        }
    }

    /// <summary>
    /// Результат кодирования всего набора данных (всех категориальных столбцов).
    /// Содержит результаты для каждого признака и агрегированные метрики.
    /// </summary>
    public class FullEncodingResult
    {
        /// <summary>Название метода кодирования</summary>
        public string MethodName { get; set; }

        /// <summary>Список результатов кодирования для каждого категориального признака</summary>
        public List<ColumnEncodingResult> ColumnResults { get; set; } = new List<ColumnEncodingResult>();

        /// <summary>Общее время выполнения кодирования всех признаков (мс)</summary>
        public double TotalTime { get; set; }

        /// <summary>Среднее качество по всем признакам</summary>
        public double AvgQuality => ColumnResults.Average(r => r.OverallQuality);

        /// <summary>Средняя корреляция Пирсона по всем признакам</summary>
        public double AvgCorrelation => ColumnResults.Average(r => r.Correlation);

        /// <summary>Средняя корреляция Спирмена по всем признакам</summary>
        public double AvgSpearman => ColumnResults.Average(r => r.SpearmanCorrelation);

        /// <summary>Средняя взаимная информация по всем признакам</summary>
        public double AvgMutualInfo => ColumnResults.Average(r => r.MutualInformation);

        /// <summary>Средний R² по всем признакам</summary>
        public double AvgR2 => ColumnResults.Average(r => r.R2Score);

        /// <summary>Лучший признак по качеству кодирования</summary>
        public ColumnEncodingResult BestFeature => ColumnResults.OrderByDescending(r => r.OverallQuality).FirstOrDefault();

        /// <summary>Худший признак по качеству кодирования</summary>
        public ColumnEncodingResult WorstFeature => ColumnResults.OrderBy(r => r.OverallQuality).FirstOrDefault();
    }

    /// <summary>
    /// Интерфейс для кодировщика отдельного категориального столбца.
    /// Определяет контракт для всех методов категориального кодирования.
    /// </summary>
    public interface IColumnEncoder
    {
        /// <summary>Название метода кодирования</summary>
        string Name { get; }

        /// <summary>
        /// Выполняет кодирование одного категориального столбца и оценивает качество.
        /// </summary>
        /// <param name="columnName">Название столбца</param>
        /// <param name="categories">Список категорий для каждой строки</param>
        /// <param name="targetValues">Список значений целевой переменной для каждой строки</param>
        /// <returns>Результат кодирования с метриками качества</returns>
        ColumnEncodingResult EncodeColumn(string columnName, List<string> categories, List<double> targetValues);
    }

    /// <summary>
    /// Абстрактный базовый класс для всех кодировщиков столбцов.
    /// Реализует общую логику оценки качества и управления временем выполнения.
    /// Конкретные методы кодирования должны переопределить GetEncoding() и GetDimensionality().
    /// </summary>
    public abstract class BaseColumnEncoder : IColumnEncoder
    {
        /// <summary>Название метода кодирования (должно быть переопределено в наследниках)</summary>
        public abstract string Name { get; }

        /// <summary>
        /// Получить словарь кодировок для каждой уникальной категории.
        /// </summary>
        /// <param name="categories">Список категорий для каждой строки</param>
        /// <param name="targetValues">Список значений целевой переменной</param>
        /// <returns>Словарь: категория → числовой код</returns>
        protected abstract Dictionary<string, double> GetEncoding(List<string> categories, List<double> targetValues);

        /// <summary>
        /// Получить размерность после кодирования.
        /// </summary>
        /// <param name="encoding">Словарь кодировок</param>
        /// <returns>Количество числовых признаков после кодирования</returns>
        protected abstract int GetDimensionality(Dictionary<string, double> encoding);

        /// <summary>
        /// Выполняет полный цикл кодирования: получение кодировок, оценка качества, замер времени.
        /// </summary>
        public ColumnEncodingResult EncodeColumn(string columnName, List<string> categories, List<double> targetValues)
        {
            var stopwatch = Stopwatch.StartNew();

            // Получаем кодировку для категорий этого столбца
            var encoding = GetEncoding(categories, targetValues);

            // Оцениваем качество кодировки
            var qualityMetrics = EvaluateEncodingQuality(encoding, categories, targetValues);

            stopwatch.Stop();

            return new ColumnEncodingResult
            {
                ColumnName = columnName,
                MethodName = Name,
                UniqueValuesCount = encoding.Count,
                Dimensionality = GetDimensionality(encoding),
                EncodingTime = stopwatch.ElapsedMilliseconds,
                Correlation = qualityMetrics.Correlation,
                SpearmanCorrelation = qualityMetrics.SpearmanCorrelation,
                MutualInformation = qualityMetrics.MutualInformation,
                R2Score = qualityMetrics.R2Score,
                OverallQuality = qualityMetrics.OverallQuality,
                CategoryEncodings = encoding
            };
        }

        /// <summary>
        /// Оценка качества кодировки для одного столбца.
        /// Вычисляет корреляцию Пирсона, корреляцию Спирмена, взаимную информацию и R².
        /// </summary>
        /// <param name="encoding">Словарь кодировок</param>
        /// <param name="categories">Список категорий</param>
        /// <param name="targetValues">Список целевых значений</param>
        /// <returns>Кортеж из всех метрик качества</returns>
        private (double Correlation, double SpearmanCorrelation, double MutualInformation, double R2Score, double OverallQuality)
            EvaluateEncodingQuality(Dictionary<string, double> encoding, List<string> categories, List<double> targetValues)
        {
            // Получаем закодированные значения для каждой строки
            var encodedValues = new List<double>();
            var targets = new List<double>();

            for (int i = 0; i < categories.Count; i++)
            {
                if (encoding.ContainsKey(categories[i]))
                {
                    encodedValues.Add(encoding[categories[i]]);
                    targets.Add(targetValues[i]);
                }
            }

            if (encodedValues.Count < 2)
                return (0, 0, 0, 0, 0);

            // 1. Корреляция Пирсона (линейная связь)
            double correlation = Math.Abs(CalculatePearsonCorrelation(encodedValues, targets));

            // 2. Корреляция Спирмена (монотонная связь)
            double spearman = Math.Abs(CalculateSpearmanCorrelation(encodedValues, targets));

            // 3. Взаимная информация (нелинейная связь)
            double mutualInfo = CalculateMutualInformation(encodedValues, targets);

            // 4. Коэффициент детерминации R² (предсказательная способность)
            double r2 = Math.Max(0, CalculateR2Score(encodedValues, targets));

            // 5. Итоговая комплексная оценка (взвешенная сумма)
            // Веса: Пирсон 35%, Спирмен 25%, MI 25%, R² 15%
            double overallQuality = (correlation * 0.35 + spearman * 0.25 + mutualInfo * 0.25 + r2 * 0.15) * 100;
            overallQuality = Math.Max(5, Math.Min(98, overallQuality));

            return (correlation, spearman, mutualInfo, r2, overallQuality);
        }

        /// <summary>
        /// Вычисляет коэффициент корреляции Пирсона между двумя наборами данных.
        /// Измеряет силу линейной связи.
        /// </summary>
        private double CalculatePearsonCorrelation(List<double> x, List<double> y)
        {
            double meanX = x.Average();
            double meanY = y.Average();

            double covariance = 0;
            double varX = 0;
            double varY = 0;

            for (int i = 0; i < x.Count; i++)
            {
                covariance += (x[i] - meanX) * (y[i] - meanY);
                varX += Math.Pow(x[i] - meanX, 2);
                varY += Math.Pow(y[i] - meanY, 2);
            }

            if (varX == 0 || varY == 0) return 0;
            return covariance / Math.Sqrt(varX * varY);
        }

        /// <summary>
        /// Вычисляет коэффициент корреляции Спирмена между двумя наборами данных.
        /// Измеряет силу монотонной связи (не обязательно линейной).
        /// </summary>
        private double CalculateSpearmanCorrelation(List<double> x, List<double> y)
        {
            var xRanks = GetRanks(x);
            var yRanks = GetRanks(y);

            double dSquared = 0;
            for (int i = 0; i < xRanks.Count; i++)
            {
                dSquared += Math.Pow(xRanks[i] - yRanks[i], 2);
            }

            double n = xRanks.Count;
            return 1 - (6 * dSquared) / (n * (n * n - 1));
        }

        /// <summary>
        /// Вычисляет нормированную взаимную информацию между двумя наборами данных.
        /// Измеряет любую (в том числе нелинейную) зависимость.
        /// </summary>
        private double CalculateMutualInformation(List<double> x, List<double> y)
        {
            int bins = Math.Min(10, (int)Math.Sqrt(x.Count));
            var xBins = Discretize(x, bins);
            var yBins = Discretize(y, bins);

            double mi = 0;
            int n = xBins.Count;

            for (int i = 0; i < bins; i++)
            {
                for (int j = 0; j < bins; j++)
                {
                    int jointCount = xBins.Zip(yBins, (a, b) => a == i && b == j).Count(v => v);
                    double pxy = jointCount / (double)n;

                    int xCount = xBins.Count(v => v == i);
                    double px = xCount / (double)n;

                    int yCount = yBins.Count(v => v == j);
                    double py = yCount / (double)n;

                    if (pxy > 0 && px > 0 && py > 0)
                    {
                        mi += pxy * Math.Log(pxy / (px * py));
                    }
                }
            }

            double hx = CalculateEntropy(xBins, bins);
            double hy = CalculateEntropy(yBins, bins);

            if (hx == 0 && hy == 0) return 0;
            return mi / Math.Max(hx, hy);
        }

        /// <summary>
        /// Вычисляет коэффициент детерминации R².
        /// Показывает, насколько хорошо закодированные значения предсказывают целевую переменную.
        /// </summary>
        private double CalculateR2Score(List<double> encoded, List<double> target)
        {
            double meanTarget = target.Average();
            double ssTotal = target.Sum(t => Math.Pow(t - meanTarget, 2));

            if (ssTotal == 0) return 0;

            double ssResidual = 0;
            for (int i = 0; i < encoded.Count; i++)
            {
                ssResidual += Math.Pow(target[i] - encoded[i], 2);
            }

            return 1 - (ssResidual / ssTotal);
        }

        /// <summary>
        /// Преобразует список значений в ранги (порядковые номера).
        /// Используется для вычисления корреляции Спирмена.
        /// </summary>
        private List<double> GetRanks(List<double> values)
        {
            var indexed = values.Select((v, i) => (value: v, index: i)).OrderBy(x => x.value).ToList();
            var ranks = new double[values.Count];
            for (int i = 0; i < indexed.Count; i++)
            {
                ranks[indexed[i].index] = i + 1;
            }
            return ranks.ToList();
        }

        /// <summary>
        /// Дискретизирует непрерывные значения в заданное количество корзин (bins).
        /// Используется для вычисления взаимной информации.
        /// </summary>
        private List<int> Discretize(List<double> values, int bins)
        {
            if (values.Count == 0) return new List<int>();
            double min = values.Min();
            double max = values.Max();
            double step = (max - min) / bins;
            if (step == 0) step = 1;
            return values.Select(v => (int)Math.Min(bins - 1, (v - min) / step)).ToList();
        }

        /// <summary>
        /// Вычисляет энтропию Шеннона для дискретизированных данных.
        /// Используется для нормализации взаимной информации.
        /// </summary>
        private double CalculateEntropy(List<int> bins, int numBins)
        {
            double entropy = 0;
            int n = bins.Count;
            for (int i = 0; i < numBins; i++)
            {
                double p = bins.Count(b => b == i) / (double)n;
                if (p > 0) entropy -= p * Math.Log(p);
            }
            return entropy;
        }
    }
}