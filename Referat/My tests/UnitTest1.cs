using ClassLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace My_tests
{
    public class EncoderTests
    {
        #region Test Data Helpers

        /// <summary>
        /// Создает простой тестовый датасет
        /// </summary>
        private List<DataPoint> CreateSimpleDataset()
        {
            var data = new List<DataPoint>
            {
                new DataPoint(new[] { "A" }, 100),
                new DataPoint(new[] { "A" }, 150),
                new DataPoint(new[] { "B" }, 50),
                new DataPoint(new[] { "B" }, 70),
                new DataPoint(new[] { "C" }, 200),
                new DataPoint(new[] { "C" }, 180)
            };
            return data;
        }

        /// <summary>
        /// Создает датасет с несколькими категориальными признаками
        /// </summary>
        private List<DataPoint> CreateMultiFeatureDataset()
        {
            var data = new List<DataPoint>
            {
                new DataPoint(new[] { "Red", "Small" }, 100),
                new DataPoint(new[] { "Red", "Large" }, 150),
                new DataPoint(new[] { "Blue", "Small" }, 50),
                new DataPoint(new[] { "Blue", "Large" }, 80),
                new DataPoint(new[] { "Green", "Medium" }, 120)
            };
            return data;
        }

        /// <summary>
        /// Создает датасет с высокой кардинальностью
        /// </summary>
        private List<DataPoint> CreateHighCardinalityDataset(int samples, int uniqueCategories)
        {
            var data = new List<DataPoint>();
            var random = new Random(42);

            for (int i = 0; i < samples; i++)
            {
                int catIndex = random.Next(uniqueCategories);
                string category = $"Cat_{catIndex}";
                double target = 100 + random.NextDouble() * 100;
                data.Add(new DataPoint(new[] { category }, target));
            }
            return data;
        }

        #endregion

        #region OneHotEncoder Tests

        [Fact]
        public void OneHotEncoder_GetEncoding_ReturnsCorrectNumberOfCategories()
        {
            // Arrange
            var encoder = new OneHotEncoder();
            var dataset = CreateSimpleDataset();

            // Act
            var encoding = encoder.GetType()
                .GetMethod("GetEncoding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(encoder, new object[] { dataset }) as Dictionary<string, double>;

            // Assert
            Assert.NotNull(encoding);
            Assert.Equal(3, encoding.Count); // A, B, C - 3 уникальные категории
            Assert.Contains("A", encoding.Keys);
            Assert.Contains("B", encoding.Keys);
            Assert.Contains("C", encoding.Keys);
        }

        [Fact]
        public void OneHotEncoder_Dimensionality_EqualsNumberOfCategories()
        {
            // Arrange
            var encoder = new OneHotEncoder();
            var dataset = CreateSimpleDataset();

            // Act
            var result = encoder.EncodeAndEvaluate(dataset);

            // Assert
            Assert.Equal(3, result.Dimensionality);
        }

        #endregion

        #region IntegerEncoder Tests

        [Fact]
        public void IntegerEncoder_GetEncoding_AssignsUniqueIntegerIds()
        {
            // Arrange
            var encoder = new IntegerEncoder();
            var dataset = CreateSimpleDataset();

            // Act
            var encoding = encoder.GetType()
                .GetMethod("GetEncoding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(encoder, new object[] { dataset }) as Dictionary<string, double>;

            // Assert
            Assert.NotNull(encoding);
            Assert.Equal(3, encoding.Count);

            var values = encoding.Values.ToList();
            Assert.Contains(0, values);
            Assert.Contains(1, values);
            Assert.Contains(2, values);
        }

        [Fact]
        public void IntegerEncoder_Dimensionality_AlwaysOne()
        {
            // Arrange
            var encoder = new IntegerEncoder();
            var dataset = CreateSimpleDataset();

            // Act
            var result = encoder.EncodeAndEvaluate(dataset);

            // Assert
            Assert.Equal(1, result.Dimensionality);
        }

        #endregion


        #region CatBoostEncoder Tests

        [Fact]
        public void CatBoostEncoder_OrderedEncoding_PreventsDataLeakage()
        {
            // Arrange
            var encoder = new CatBoostEncoder();
            var dataset = CreateSimpleDataset();

            // Act
            var encoding = encoder.GetType()
                .GetMethod("GetEncoding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(encoder, new object[] { dataset }) as Dictionary<string, double>;

            // Assert
            Assert.NotNull(encoding);

            // Для CatBoost значения зависят от порядка, но должны быть в разумных пределах
            foreach (var value in encoding.Values)
            {
                Assert.InRange(value, 50, 200);
            }
        }

        #endregion

        #region EncodingQualityEvaluator Tests

        [Fact]
        public void QualityEvaluator_PearsonCorrelation_ReturnsCorrectValue()
        {
            // Arrange
            var evaluator = new EncodingQualityEvaluator();
            var dataset = CreateSimpleDataset();

            // Создаем идеальную корреляцию: encoded = target
            var perfectEncoding = new Dictionary<string, double>
            {
                ["A"] = 125, // среднее для A
                ["B"] = 60,  // среднее для B
                ["C"] = 190  // среднее для C
            };

            // Act
            var quality = evaluator.GetType()
                .GetMethod("EvaluateEncoding", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                ?.Invoke(evaluator, new object[] { perfectEncoding, dataset, "Test" }) as EncodingQualityEvaluator.QualityMetrics;

            // Assert
            Assert.NotNull(quality);
            Assert.InRange(quality.Correlation, 0.9, 1.0);
        }

        [Fact]
        public void QualityEvaluator_R2Score_ReturnsCorrectRange()
        {
            // Arrange
            var evaluator = new EncodingQualityEvaluator();
            var dataset = CreateSimpleDataset();

            // Создаем случайную кодировку (низкое качество)
            var randomEncoding = new Dictionary<string, double>
            {
                ["A"] = 10,
                ["B"] = 20,
                ["C"] = 30
            };

            // Act
            var quality = evaluator.GetType()
                .GetMethod("EvaluateEncoding", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                ?.Invoke(evaluator, new object[] { randomEncoding, dataset, "Test" }) as EncodingQualityEvaluator.QualityMetrics;

            // Assert
            Assert.NotNull(quality);
            Assert.InRange(quality.R2Score, 0, 1);
        }

        #endregion

        #region ColumnTypeDetector Tests

        [Fact]
        public void ColumnTypeDetector_DetectsCategoricalColumns()
        {
            // Arrange
            var detector = new ColumnTypeDetector();
            var columnNames = new[] { "ID", "Color", "Size", "Price" };
            var dataRows = new List<string[]>
            {
                new[] { "1", "Red", "Small", "100" },
                new[] { "2", "Red", "Large", "150" },
                new[] { "3", "Blue", "Small", "50" },
                new[] { "4", "Blue", "Large", "80" }
            };

            // Act
            var result = detector.DetectColumnTypes(columnNames, dataRows);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.CategoricalColumnIndices.Count); // Color и Size
            Assert.Contains(1, result.CategoricalColumnIndices); // Color
            Assert.Contains(2, result.CategoricalColumnIndices); // Size
        }

        [Fact]
        public void ColumnTypeDetector_DetectsTargetColumn_ByKeywords()
        {
            // Arrange
            var detector = new ColumnTypeDetector();
            var columnNames = new[] { "Color", "Size", "Price" };
            var dataRows = new List<string[]>
            {
                new[] { "Red", "Small", "100" },
                new[] { "Red", "Large", "150" },
                new[] { "Blue", "Small", "50" }
            };

            // Act
            var result = detector.DetectColumnTypes(columnNames, dataRows);

            // Assert
            Assert.Equal(2, result.TargetColumnIndex); // Price (индекс 2)
        }

        [Fact]
        public void ColumnTypeDetector_IdentifiesIdColumns()
        {
            // Arrange
            var detector = new ColumnTypeDetector();
            var columnNames = new[] { "Patient_ID", "Age", "Disease" };
            var dataRows = new List<string[]>
            {
                new[] { "P001", "45", "Flu" },
                new[] { "P002", "32", "Cold" },
                new[] { "P003", "67", "Pneumonia" }
            };

            // Act
            var result = detector.DetectColumnTypes(columnNames, dataRows);

            // Assert
            Assert.NotNull(result);
            // ID колонка не должна попасть в категориальные
            Assert.DoesNotContain(0, result.CategoricalColumnIndices);
        }

        #endregion

        #region DataPoint Tests

        [Fact]
        public void DataPoint_Constructor_InitializesCorrectly()
        {
            // Arrange
            var categories = new[] { "Category1", "Category2" };
            double target = 123.45;

            // Act
            var dataPoint = new DataPoint(categories, target);

            // Assert
            Assert.NotNull(dataPoint.Categories);
            Assert.Equal(2, dataPoint.Categories.Length);
            Assert.Equal("Category1", dataPoint.Categories[0]);
            Assert.Equal("Category2", dataPoint.Categories[1]);
            Assert.Equal(target, dataPoint.TargetValue);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Performance_AllEncoders_CompleteWithinReasonableTime()
        {
            // Arrange
            var dataset = CreateHighCardinalityDataset(1000, 50);
            var encoders = new ICategoricalEncoder[]
            {
                new OneHotEncoder(),
                new IntegerEncoder(),
                new EntropyEncoder(),
                new TargetEncoder(),
                new CatBoostEncoder()
            };

            // Act & Assert
            foreach (var encoder in encoders)
            {
                var result = encoder.EncodeAndEvaluate(dataset);
                Assert.True(result.EncodingTime < 5000,
                    $"{encoder.Name} took {result.EncodingTime}ms, expected <5000ms");
            }
        }

        [Fact]
        public void Quality_OneHotEncoder_Vs_TargetEncoder_Comparison()
        {
            // Arrange
            var dataset = CreateSimpleDataset();
            var oneHot = new OneHotEncoder();
            var target = new TargetEncoder();

            // Act
            var oneHotResult = oneHot.EncodeAndEvaluate(dataset);
            var targetResult = target.EncodeAndEvaluate(dataset);

            // Assert
            // В этом датасете Target Encoding должен показать лучшее качество
            Assert.True(targetResult.QualityScore > oneHotResult.QualityScore,
                $"Target ({targetResult.QualityScore:F2}%) should beat One-Hot ({oneHotResult.QualityScore:F2}%)");
        }

        #endregion

        #region Edge Cases Tests

 

        [Fact]
        public void EdgeCases_SingleCategory_HandlesCorrectly()
        {
            // Arrange
            var dataset = new List<DataPoint>
            {
                new DataPoint(new[] { "OnlyOne" }, 100),
                new DataPoint(new[] { "OnlyOne" }, 200),
                new DataPoint(new[] { "OnlyOne" }, 300)
            };
            var encoder = new IntegerEncoder();

            // Act
            var result = encoder.EncodeAndEvaluate(dataset);

            // Assert
            Assert.Equal(1, result.Dimensionality);
            Assert.InRange(result.QualityScore, 0, 100);
        }

        [Fact]
        public void EdgeCases_MissingTargetValues_HandlesGracefully()
        {
            // Arrange
            var detector = new ColumnTypeDetector();
            var columnNames = new[] { "Category", "Target" };
            var dataRows = new List<string[]>
            {
                new[] { "A", "" },  // Пустое значение целевой
                new[] { "B", "50" },
                new[] { "C", "100" }
            };

            // Act
            var result = detector.DetectColumnTypes(columnNames, dataRows);
            var dataPoints = detector.ConvertToDataPoints(result);

            // Assert
            Assert.NotNull(dataPoints);
            Assert.Equal(3, dataPoints.Count);
            // Пустое значение должно быть заменено на хэш-код
            Assert.True(dataPoints[0].TargetValue >= 0);
        }

        #endregion
    }

    #region Integration Tests

    public class IntegrationTests
    {
        [Fact]
        public void FullPipeline_LoadData_Encode_Evaluate_WorksCorrectly()
        {
            // Arrange
            var detector = new ColumnTypeDetector();
            var columnNames = new[] { "Color", "Size", "Price" };
            var dataRows = new List<string[]>
            {
                new[] { "Red", "Small", "100" },
                new[] { "Red", "Large", "150" },
                new[] { "Blue", "Small", "50" },
                new[] { "Blue", "Large", "80" },
                new[] { "Green", "Medium", "120" }
            };

            // Act - Detect columns
            var columnTypes = detector.DetectColumnTypes(columnNames, dataRows);

            // Assert - Columns detection
            Assert.Equal(2, columnTypes.CategoricalColumnIndices.Count);
            Assert.Equal(2, columnTypes.TargetColumnIndex); // Price

            // Act - Convert to DataPoints
            var dataPoints = detector.ConvertToDataPoints(columnTypes);

            // Assert - DataPoints conversion
            Assert.Equal(5, dataPoints.Count);
            Assert.Equal(2, dataPoints[0].Categories.Length);

            // Act - Encode with different methods
            var encoders = new ICategoricalEncoder[]
            {
                new OneHotEncoder(),
                new IntegerEncoder(),
                new TargetEncoder(),
                new CatBoostEncoder()
            };

            // Assert - All encoders work
            foreach (var encoder in encoders)
            {
                var result = encoder.EncodeAndEvaluate(dataPoints);
                Assert.NotNull(result);
                Assert.True(result.QualityScore >= 0);
                Assert.True(result.EncodingTime >= 0);
            }
        }

        [Fact]
        public void DifferentEncoders_ProduceDifferentDimensionality()
        {
            // Arrange
            var dataset = new List<DataPoint>
            {
                new DataPoint(new[] { "A", "X" }, 100),
                new DataPoint(new[] { "B", "Y" }, 150),
                new DataPoint(new[] { "C", "Z" }, 200)
            };

            var encoders = new Dictionary<string, ICategoricalEncoder>
            {
                ["OneHot"] = new OneHotEncoder(),
                ["Integer"] = new IntegerEncoder(),
                ["Target"] = new TargetEncoder(),
                ["CatBoost"] = new CatBoostEncoder()
            };

            // Act & Assert
            foreach (var kvp in encoders)
            {
                var result = kvp.Value.EncodeAndEvaluate(dataset);

                if (kvp.Key == "Integer")
                {
                    Assert.Equal(1, result.Dimensionality);
                }
                else
                {
                    Assert.Equal(6, result.Dimensionality); // 3 + 3 = 6 уникальных категорий
                }
            }
        }
    }

    #endregion
}