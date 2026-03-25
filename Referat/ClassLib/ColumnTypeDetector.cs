using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ClassLib
{
    public class ColumnTypeDetector
    {
        public class ColumnTypeResult
        {
            public List<int> CategoricalColumnIndices { get; set; } = new List<int>();
            public int TargetColumnIndex { get; set; }
            public string[] ColumnNames { get; set; }
            public List<string[]> RawData { get; set; }
            public Dictionary<int, ColumnAnalysis> ColumnAnalysis { get; set; } = new Dictionary<int, ColumnAnalysis>();
        }

        public class ColumnAnalysis
        {
            public string ColumnName { get; set; }
            public int UniqueValuesCount { get; set; }
            public double NumericRatio { get; set; }
            public double UniqueRatio { get; set; }
            public double Entropy { get; set; }
            public bool IsLikelyId { get; set; }
            public bool IsNumeric { get; set; }
            public bool IsOrdinal { get; set; }           // Порядковая переменная
            public bool IsCategorical { get; set; }
            public string SuggestedType { get; set; }
        }

        /// <summary>
        /// Определяет категориальные колонки и целевую переменную автоматически
        /// </summary>
        public ColumnTypeResult DetectColumnTypes(string[] columnNames, List<string[]> dataRows, int? targetColumnIndex = null)
        {
            var result = new ColumnTypeResult
            {
                ColumnNames = columnNames,
                RawData = dataRows,
                TargetColumnIndex = targetColumnIndex ?? AutoDetectTargetColumn(columnNames, dataRows)
            };

            // Анализируем каждую колонку
            for (int i = 0; i < columnNames.Length; i++)
            {
                if (i == result.TargetColumnIndex)
                    continue;

                var analysis = AnalyzeColumn(columnNames[i], dataRows, i, dataRows.Count);
                result.ColumnAnalysis[i] = analysis;

                if (analysis.IsCategorical)
                {
                    result.CategoricalColumnIndices.Add(i);
                }
            }

            return result;
        }

        /// <summary>
        /// Автоматическое определение целевой переменной
        /// </summary>
        private int AutoDetectTargetColumn(string[] columnNames, List<string[]> dataRows)
        {
            // Приоритет: числовые колонки с высоким количеством уникальных значений
            // и названиями, указывающими на целевую переменную

            var targetKeywords = new[] { "price", "target", "class", "label", "value", "cost", "rating", "century", "disease", "deaths", "cases" };

            // Сначала ищем по названию
            for (int i = 0; i < columnNames.Length; i++)
            {
                string nameLower = columnNames[i].ToLower();
                if (targetKeywords.Any(k => nameLower.Contains(k)) && IsNumericColumn(dataRows, i))
                {
                    return i;
                }
            }

            // Затем ищем числовую колонку с наибольшим количеством уникальных значений
            int bestCandidate = columnNames.Length - 1;
            int maxUniqueValues = 0;

            for (int i = 0; i < columnNames.Length; i++)
            {
                var uniqueValues = GetUniqueValues(dataRows, i);
                // Целевая переменная обычно имеет много уникальных значений
                if (uniqueValues.Count > maxUniqueValues && IsNumericColumn(dataRows, i))
                {
                    maxUniqueValues = uniqueValues.Count;
                    bestCandidate = i;
                }
            }

            return bestCandidate;
        }

        /// <summary>
        /// Анализ колонки для определения типа
        /// </summary>
        private ColumnAnalysis AnalyzeColumn(string columnName, List<string[]> dataRows, int columnIndex, int totalRows)
        {
            var analysis = new ColumnAnalysis
            {
                ColumnName = columnName
            };

            // Собираем статистику по колонке
            var values = new List<string>();
            var uniqueValues = new HashSet<string>();
            int numericCount = 0;
            int totalValid = 0;

            for (int i = 0; i < dataRows.Count; i++)
            {
                if (columnIndex < dataRows[i].Length)
                {
                    string value = dataRows[i][columnIndex]?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(value))
                    {
                        totalValid++;
                        values.Add(value);
                        uniqueValues.Add(value);

                        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        {
                            numericCount++;
                        }
                    }
                }
            }

            analysis.UniqueValuesCount = uniqueValues.Count;
            analysis.NumericRatio = totalValid > 0 ? numericCount / (double)totalValid : 0;
            analysis.UniqueRatio = totalValid > 0 ? uniqueValues.Count / (double)totalValid : 0;
            analysis.Entropy = CalculateEntropy(values);

            // Определяем тип колонки
            DetermineColumnType(analysis, totalRows, columnName);

            return analysis;
        }

        /// <summary>
        /// Определение типа колонки на основе статистики
        /// </summary>
        private void DetermineColumnType(ColumnAnalysis analysis, int totalRows, string columnName)
        {
            string nameLower = columnName.ToLower();

            // 1. Проверка на ID-колонку (уникальные идентификаторы)
            bool isIdByName = nameLower.Contains("id") ||
                              nameLower.Contains("name") && !nameLower.Contains("pathogen") ||
                              nameLower == "event_name" ||
                              nameLower == "car_name";

            bool isHighUniqueness = analysis.UniqueRatio > 0.7 && analysis.UniqueValuesCount > totalRows * 0.6;

            analysis.IsLikelyId = isIdByName && isHighUniqueness || analysis.UniqueRatio > 0.85;

            if (analysis.IsLikelyId)
            {
                analysis.IsCategorical = false;
                analysis.IsNumeric = false;
                analysis.SuggestedType = "ID/Уникальный идентификатор";
                return;
            }

            // 2. Проверка на порядковые числовые переменные (Century, Year)
            bool isOrdinalByName = nameLower.Contains("century") ||
                                   nameLower.Contains("year") ||
                                   nameLower.Contains("score") ||
                                   nameLower.Contains("rating");

            if (isOrdinalByName && analysis.NumericRatio > 0.8)
            {
                analysis.IsOrdinal = true;
                analysis.IsCategorical = false;
                analysis.IsNumeric = true;
                analysis.SuggestedType = "Числовая (порядковая)";
                return;
            }

            // 3. Проверка на числовую колонку
            if (analysis.NumericRatio > 0.8)
            {
                analysis.IsNumeric = true;
                analysis.IsCategorical = false;
                analysis.SuggestedType = "Числовая";
                return;
            }

            // 4. Определение категориальности

            // 4.1 Очень мало уникальных значений (<= 10) - точно категория
            if (analysis.UniqueValuesCount <= 10)
            {
                analysis.IsCategorical = true;
                analysis.SuggestedType = "Категориальная (низкая кардинальность)";
                return;
            }

            // 4.2 Мало уникальных значений (<= 20) и много нечисловых
            if (analysis.UniqueValuesCount <= 20 && analysis.NumericRatio < 0.5)
            {
                analysis.IsCategorical = true;
                analysis.SuggestedType = "Категориальная (средняя кардинальность)";
                return;
            }

            // 4.3 Умеренное количество уникальных (<= 35) и почти все нечисловые
            if (analysis.UniqueValuesCount <= 35 && analysis.NumericRatio < 0.3)
            {
                // Дополнительная проверка: если это Pathogen_Name с повторяющимися значениями
                if (nameLower.Contains("pathogen") && analysis.UniqueValuesCount <= 15)
                {
                    analysis.IsCategorical = true;
                    analysis.SuggestedType = "Категориальная (возбудитель)";
                    return;
                }

                analysis.IsCategorical = true;
                analysis.SuggestedType = "Категориальная (высокая кардинальность)";
                return;
            }

            // 4.4 Высокая энтропия и много нечисловых значений
            if (analysis.Entropy > 2.0 && analysis.NumericRatio < 0.2 && analysis.UniqueValuesCount <= 100)
            {
                analysis.IsCategorical = true;
                analysis.SuggestedType = "Категориальная (на основе энтропии)";
                return;
            }

            // 5. Проверка на текстовую колонку (не категориальная)
            if (analysis.NumericRatio < 0.1 && analysis.UniqueValuesCount > 35)
            {
                analysis.IsCategorical = false;
                analysis.SuggestedType = "Текстовая (свободный текст)";
                return;
            }

            // По умолчанию - не категориальная
            analysis.IsCategorical = false;
            analysis.SuggestedType = "Другое";
        }

        /// <summary>
        /// Вычисление энтропии Шеннона для колонки
        /// </summary>
        private double CalculateEntropy(List<string> values)
        {
            if (values.Count == 0)
                return 0;

            var frequency = new Dictionary<string, int>();
            foreach (var value in values)
            {
                if (!frequency.ContainsKey(value))
                    frequency[value] = 0;
                frequency[value]++;
            }

            double entropy = 0;
            double total = values.Count;

            foreach (var count in frequency.Values)
            {
                double probability = count / total;
                entropy -= probability * Math.Log2(probability);
            }

            return entropy;
        }

        /// <summary>
        /// Проверка, является ли колонка числовой
        /// </summary>
        private bool IsNumericColumn(List<string[]> dataRows, int columnIndex)
        {
            int sampleSize = Math.Min(100, dataRows.Count);
            int numericCount = 0;
            int validCount = 0;

            for (int i = 0; i < sampleSize; i++)
            {
                if (columnIndex < dataRows[i].Length)
                {
                    string value = dataRows[i][columnIndex]?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(value))
                    {
                        validCount++;
                        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        {
                            numericCount++;
                        }
                    }
                }
            }

            return validCount > 0 && (numericCount / (double)validCount) > 0.7;
        }

        /// <summary>
        /// Получение уникальных значений из колонки
        /// </summary>
        private HashSet<string> GetUniqueValues(List<string[]> dataRows, int columnIndex)
        {
            var unique = new HashSet<string>();

            foreach (var row in dataRows)
            {
                if (columnIndex < row.Length)
                {
                    string value = row[columnIndex]?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(value))
                    {
                        unique.Add(value);
                    }
                }
            }

            return unique;
        }

        /// <summary>
        /// Преобразует данные в формат DataPoint для обработки кодировщиками
        /// </summary>
        public List<DataPoint> ConvertToDataPoints(ColumnTypeResult result)
        {
            var dataPoints = new List<DataPoint>();

            foreach (var row in result.RawData)
            {
                var categories = new string[result.CategoricalColumnIndices.Count];
                for (int i = 0; i < result.CategoricalColumnIndices.Count; i++)
                {
                    int colIndex = result.CategoricalColumnIndices[i];
                    categories[i] = colIndex < row.Length ? row[colIndex] : "";
                }

                double targetValue = 0;
                string targetStr = result.TargetColumnIndex < row.Length ? row[result.TargetColumnIndex] : "0";

                if (!double.TryParse(targetStr, NumberStyles.Any, CultureInfo.InvariantCulture, out targetValue))
                {
                    targetValue = Math.Abs(targetStr.GetHashCode() % 1000) / 1000.0;
                }

                dataPoints.Add(new DataPoint(categories, targetValue));
            }

            return dataPoints;
        }
    }
}