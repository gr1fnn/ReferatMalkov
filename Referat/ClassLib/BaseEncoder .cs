using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLib
{
    public abstract class BaseEncoder : ICategoricalEncoder
    {
        protected readonly EncodingQualityEvaluator _evaluator = new EncodingQualityEvaluator();

        public abstract string Name { get; }

        public abstract EncodingResult EncodeAndEvaluate(List<DataPoint> dataset);

        /// <summary>
        /// Получить кодировку для каждой категории
        /// </summary>
        protected abstract Dictionary<string, double> GetEncoding(List<DataPoint> dataset);

        /// <summary>
        /// Получить размерность результата
        /// </summary>
        protected abstract int GetDimensionality(Dictionary<string, double> encoding, List<DataPoint> dataset);

        public EncodingResult EncodeAndEvaluateInternal(List<DataPoint> dataset)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Получаем кодировку
            var encoding = GetEncoding(dataset);

            // Оцениваем качество
            var qualityMetrics = _evaluator.EvaluateEncoding(encoding, dataset, Name);

            // Получаем размерность
            int dimensionality = GetDimensionality(encoding, dataset);

            stopwatch.Stop();

            // Формируем результат ТОЛЬКО с общими метриками
            var metrics = new Dictionary<string, double>
            {
                ["Correlation"] = qualityMetrics.Correlation,
                ["SpearmanCorr"] = qualityMetrics.SpearmanCorrelation,
                ["MutualInfo"] = qualityMetrics.MutualInformation,
                ["R2_Score"] = qualityMetrics.R2Score
            };

            return new EncodingResult
            {
                MethodName = Name,
                Dimensionality = dimensionality,
                QualityScore = qualityMetrics.OverallQuality,
                EncodingTime = stopwatch.ElapsedMilliseconds,
                InformationLoss = qualityMetrics.InformationLoss,
                Metrics = metrics
            };
        }
    }
}