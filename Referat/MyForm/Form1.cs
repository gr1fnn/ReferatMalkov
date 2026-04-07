using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using ClassLib;

namespace MyForm
{
    public partial class Form1 : Form
    {
        // Поля для хранения загруженных данных
        private string CurrentFilePath;
        private string[] ColumnNames;
        private List<string[]> RawData;
        private List<ClassLib.DataPoint> ProcessedData;
        private ColumnTypeDetector.ColumnTypeResult ColumnTypeInfo;
        private ColumnTypeDetector TypeDetector;

        // Хранение последних результатов для графиков
        private List<FullEncodingResult> allFullResults;
        private FullEncodingResult currentDisplayResult;

        public Form1()
        {
            InitializeComponent();
            InitializeDataGridView();
            InitializeTextBox();
            InitializeChart();

            TypeDetector = new ColumnTypeDetector();
        }

        private void InitializeDataGridView()
        {
            if (dataGridView1 != null)
            {
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dataGridView1.AllowUserToAddRows = false;
                dataGridView1.ReadOnly = true;

                dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
                dataGridView1.RowsDefaultCellStyle.BackColor = System.Drawing.Color.White;
                dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.Navy;
                dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
                dataGridView1.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Bold);
            }
        }

        private void InitializeTextBox()
        {
            if (textBox1 != null)
            {
                textBox1.Multiline = true;
                textBox1.ScrollBars = ScrollBars.Vertical;
                textBox1.Font = new System.Drawing.Font("Consolas", 10);
            }
        }

        private void InitializeChart()
        {
            if (chartMetrics != null)
            {
                chartMetrics.ChartAreas.Clear();
                var chartArea = new ChartArea("MainArea");
                chartArea.AxisX.Title = "Категориальные признаки";
                chartArea.AxisX.Interval = 1;
                chartArea.AxisX.MajorGrid.Enabled = false;
                chartArea.AxisY.Title = "Качество кодирования (%)";
                chartArea.AxisY.Minimum = 0;
                chartArea.AxisY.Maximum = 100;
                chartMetrics.ChartAreas.Add(chartArea);

                chartMetrics.Legends.Clear();
                var legend = new Legend("Legend");
                legend.Docking = Docking.Top;
                chartMetrics.Legends.Add(legend);

                chartMetrics.Titles.Clear();
                chartMetrics.Titles.Add("Сравнение категориальных признаков");
            }
        }

        private void ChoseDataSetButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Выберите CSV файл с категориальными данными";
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadFile(openFileDialog.FileName);
                }
            }
        }

        private void LoadFile(string filePath)
        {
            try
            {
                CurrentFilePath = filePath;

                RawData = LoadCSV(filePath);

                if (RawData == null || RawData.Count == 0)
                {
                    throw new Exception("Файл не содержит данных");
                }

                ColumnNames = RawData[0];
                var dataRows = RawData.Skip(1).ToList();

                ColumnTypeInfo = TypeDetector.DetectColumnTypes(ColumnNames, dataRows);

                var table = ConvertToDataTable(dataRows, ColumnNames);
                dataGridView1.DataSource = table;

                ProcessedData = TypeDetector.ConvertToDataPoints(ColumnTypeInfo);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<string[]> LoadCSV(string filePath)
        {
            var data = new List<string[]>();

            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = ParseCSVLine(line);
                    data.Add(values);
                }
            }

            return data;
        }

        private string[] ParseCSVLine(string line)
        {
            var result = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            string currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            result.Add(currentField);
            return result.ToArray();
        }

        private DataTable ConvertToDataTable(List<string[]> rows, string[] columnNames)
        {
            var table = new DataTable();

            foreach (var columnName in columnNames)
            {
                table.Columns.Add(columnName, typeof(string));
            }

            foreach (var row in rows)
            {
                if (row.Length >= columnNames.Length)
                {
                    var dataRow = table.NewRow();
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        dataRow[i] = row[i];
                    }
                    table.Rows.Add(dataRow);
                }
            }

            return table;
        }

        private async void runButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (ProcessedData == null || ProcessedData.Count == 0)
                {
                    MessageBox.Show("Сначала загрузите файл с данными!", "Предупреждение",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                textBox1.Clear();
                if (chartMetrics != null) chartMetrics.Series.Clear();

                var resultBuilder = new StringBuilder();

                resultBuilder.AppendLine($"Датасет: {Path.GetFileName(CurrentFilePath)}");
                resultBuilder.AppendLine($"Количество записей: {ProcessedData.Count}");
                resultBuilder.AppendLine();

                resultBuilder.AppendLine("Категориальные признаки:");
                for (int i = 0; i < ColumnTypeInfo.CategoricalColumnIndices.Count; i++)
                {
                    resultBuilder.AppendLine($"  {i + 1}. {ColumnNames[ColumnTypeInfo.CategoricalColumnIndices[i]]}");
                }
                resultBuilder.AppendLine();
                resultBuilder.AppendLine($"Целевая переменная: {ColumnNames[ColumnTypeInfo.TargetColumnIndex]}");
                resultBuilder.AppendLine();

                // Получаем данные по колонкам
                var categoricalColumns = new List<(string Name, List<string> Values)>();
                for (int i = 0; i < ColumnTypeInfo.CategoricalColumnIndices.Count; i++)
                {
                    int colIndex = ColumnTypeInfo.CategoricalColumnIndices[i];
                    var values = new List<string>();
                    foreach (var row in ColumnTypeInfo.RawData)
                    {
                        if (colIndex < row.Length)
                            values.Add(row[colIndex]?.Trim() ?? "");
                        else
                            values.Add("");
                    }
                    categoricalColumns.Add((ColumnNames[colIndex], values));
                }

                // Получаем целевые значения
                var targetValues = new List<double>();
                foreach (var row in ColumnTypeInfo.RawData)
                {
                    string targetStr = ColumnTypeInfo.TargetColumnIndex < row.Length ? row[ColumnTypeInfo.TargetColumnIndex] : "0";
                    if (double.TryParse(targetStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double val))
                        targetValues.Add(val);
                    else
                        targetValues.Add(0);
                }

                // Инициализация кодировщиков для столбцов
                var columnEncoders = new IColumnEncoder[]
                {
                    new ColumnIntegerEncoder(),
                    new ColumnOneHotEncoder(),
                    new ColumnEntropyEncoder(),
                    new ColumnTargetEncoder(),
                    new ColumnCatBoostEncoder()
                };

                var allResults = new List<FullEncodingResult>();

                resultBuilder.AppendLine($"{"Метод",-20} {"Ср.Качество",8} {"Ср.Корр",8} {"Ср.Спирм",8} {"Ср.MI",8} {"Ср.R²",8}");

                foreach (var encoder in columnEncoders)
                {
                    textBox1.Text = resultBuilder.ToString();
                    Application.DoEvents();

                    var fullResult = new FullEncodingResult
                    {
                        MethodName = encoder.Name
                    };

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    foreach (var column in categoricalColumns)
                    {
                        var colResult = encoder.EncodeColumn(column.Name, column.Values, targetValues);
                        fullResult.ColumnResults.Add(colResult);
                    }

                    stopwatch.Stop();
                    fullResult.TotalTime = stopwatch.ElapsedMilliseconds;
                    allResults.Add(fullResult);

                    resultBuilder.AppendLine($"{encoder.Name,-20} {fullResult.AvgQuality,7:F1}% {fullResult.AvgCorrelation,7:F3} {fullResult.AvgSpearman,7:F3} {fullResult.AvgMutualInfo,7:F3} {fullResult.AvgR2,7:F3}");
                }

                resultBuilder.AppendLine();
                resultBuilder.AppendLine();

                foreach (var fullResult in allResults)
                {
                    resultBuilder.AppendLine($" {fullResult.MethodName}");
                    resultBuilder.AppendLine($"   Общее время: {fullResult.TotalTime} мс");
                    resultBuilder.AppendLine($"   Среднее качество: {fullResult.AvgQuality:F1}%");
                    resultBuilder.AppendLine();
                    resultBuilder.AppendLine($"   {"Признак",-25} {"Кач",6} {"Пирсон",8} {"Спирмен",8} {"MI",8} {"R²",8} {"Разм",6} {"Уник",6}");
                    resultBuilder.AppendLine($"   {new string('-', 85)}");

                    foreach (var colResult in fullResult.ColumnResults.OrderByDescending(r => r.OverallQuality))
                    {
                        resultBuilder.AppendLine($"   {colResult.ColumnName,-25} {colResult.OverallQuality,5:F1}% {colResult.Correlation,7:F3} {colResult.SpearmanCorrelation,7:F3} {colResult.MutualInformation,7:F3} {colResult.R2Score,7:F3} {colResult.Dimensionality,5} {colResult.UniqueValuesCount,5}");
                    }
                    resultBuilder.AppendLine();

                    if (fullResult.BestFeature != null)
                    {
                        resultBuilder.AppendLine($"    Лучший признак: {fullResult.BestFeature.ColumnName} (качество {fullResult.BestFeature.OverallQuality:F1}%)");
                    }
                    if (fullResult.WorstFeature != null)
                    {
                        resultBuilder.AppendLine($"    Худший признак: {fullResult.WorstFeature.ColumnName} (качество {fullResult.WorstFeature.OverallQuality:F1}%)");
                    }
                    resultBuilder.AppendLine();

                }

                textBox1.Text = resultBuilder.ToString();

                // Сохраняем результаты
                allFullResults = allResults;
                currentDisplayResult = allResults.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении анализа: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Создает окно со сравнением всех методов
        /// </summary>
        private void ShowMethodsComparisonWindow()
        {
            if (allFullResults == null || allFullResults.Count == 0) return;

            var chartForm = new Form
            {
                Text = "Сравнение методов кодирования",
                Size = new Size(1000, 600),
                StartPosition = FormStartPosition.CenterParent,
                WindowState = FormWindowState.Maximized
            };

            var chart = CreateMethodsComparisonChart();
            chart.Dock = DockStyle.Fill;
            chartForm.Controls.Add(chart);

            // Добавляем кнопку сохранения
            var saveButton = new Button
            {
                Text = "Сохранить как изображение",
                Dock = DockStyle.Bottom,
                Height = 35
            };
            saveButton.Click += (s, e) =>
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp";
                    saveDialog.Title = "Сохранить график";
                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        chart.SaveImage(saveDialog.FileName, ChartImageFormat.Png);
                        MessageBox.Show("График сохранен!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };
            chartForm.Controls.Add(saveButton);

            chartForm.ShowDialog();
        }

        /// <summary>
        /// Создает окно с детальными метриками для всех методов
        /// </summary>
        private void ShowDetailedMetricsWindow()
        {
            if (allFullResults == null || allFullResults.Count == 0) return;

            var chartForm = new Form
            {
                Text = "Детальные метрики качества",
                Size = new Size(1200, 700),
                StartPosition = FormStartPosition.CenterParent,
                WindowState = FormWindowState.Maximized
            };

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 85F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15F));

            var chart = new Chart
            {
                Dock = DockStyle.Fill
            };
            chart.ChartAreas.Add(new ChartArea());

            string[] metricNames = { "AvgCorrelation", "AvgSpearman", "AvgMutualInfo", "AvgR2" };
            string[] metricTitles = { "Корреляция Пирсона", "Корреляция Спирмена",
                              "Взаимная информация", "R² (коэф. детерминации)" };

            Color[] metricColors = { Color.SteelBlue, Color.ForestGreen, Color.DarkOrange, Color.Purple };

            for (int m = 0; m < metricNames.Length; m++)
            {
                var series = new Series
                {
                    Name = metricTitles[m],
                    ChartType = SeriesChartType.Column,
                    IsValueShownAsLabel = true,
                    LabelFormat = "F3",
                    Color = metricColors[m]
                };

                foreach (var result in allFullResults)
                {
                    double value = metricNames[m] switch
                    {
                        "AvgCorrelation" => result.AvgCorrelation,
                        "AvgSpearman" => result.AvgSpearman,
                        "AvgMutualInfo" => result.AvgMutualInfo,
                        "AvgR2" => result.AvgR2,
                        _ => 0
                    };
                    series.Points.AddXY(result.MethodName, value);
                }
                chart.Series.Add(series);
            }

            chart.ChartAreas[0].AxisX.Title = "Методы кодирования";
            chart.ChartAreas[0].AxisX.Interval = 1;
            chart.ChartAreas[0].AxisY.Title = "Значение метрики (0-1)";
            chart.ChartAreas[0].AxisY.Minimum = -0.1;
            chart.ChartAreas[0].AxisY.Maximum = 1.1;

            chart.Titles.Clear();
            chart.Titles.Add("Сравнение метрик качества по методам кодирования");

            var legendPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10)
            };

            var flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            for (int i = 0; i < metricTitles.Length; i++)
            {
                var colorPanel = new Panel
                {
                    Width = 200,
                    Height = 30,
                    Margin = new Padding(5)
                };

                var colorBox = new Panel
                {
                    BackColor = metricColors[i],
                    Size = new Size(20, 20),
                    Location = new Point(0, 5)
                };

                var nameLabel = new Label
                {
                    Text = metricTitles[i],
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = metricColors[i],
                    AutoSize = true,
                    Location = new Point(25, 7)
                };

                colorPanel.Controls.Add(colorBox);
                colorPanel.Controls.Add(nameLabel);
                flowPanel.Controls.Add(colorPanel);
            }

            legendPanel.Controls.Add(flowPanel);
            mainPanel.Controls.Add(chart, 0, 0);
            mainPanel.Controls.Add(legendPanel, 0, 1);

            chartForm.Controls.Add(mainPanel);
            chartForm.ShowDialog();
        }

        private Chart CreateMethodsComparisonChart()
        {
            var chart = new Chart();
            chart.ChartAreas.Add(new ChartArea());

            var qualitySeries = new Series
            {
                Name = "Среднее качество кодирования (%)",
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                LabelFormat = "F1"
            };

            foreach (var result in allFullResults)
            {
                qualitySeries.Points.AddXY(result.MethodName, result.AvgQuality);
            }
            chart.Series.Add(qualitySeries);

            var timeSeries = new Series
            {
                Name = "Время выполнения (мс)",
                ChartType = SeriesChartType.Line,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 8,
                BorderWidth = 2
            };

            foreach (var result in allFullResults)
            {
                timeSeries.Points.AddXY(result.MethodName, result.TotalTime);
            }
            chart.Series.Add(timeSeries);

            chart.ChartAreas[0].AxisY2.Enabled = AxisEnabled.True;
            chart.ChartAreas[0].AxisY2.Title = "Время (мс)";
            timeSeries.YAxisType = AxisType.Secondary;

            chart.ChartAreas[0].AxisX.Title = "Методы кодирования";
            chart.ChartAreas[0].AxisX.Interval = 1;
            chart.ChartAreas[0].AxisY.Title = "Качество (%)";
            chart.ChartAreas[0].AxisY.Minimum = 0;
            chart.ChartAreas[0].AxisY.Maximum = 100;

            chart.Titles.Clear();
            chart.Titles.Add("Сравнение методов категориального кодирования");

            return chart;
        }


        private void btnShowMethodsChart_Click(object sender, EventArgs e)
        {
            if (allFullResults == null || allFullResults.Count == 0)
            {
                MessageBox.Show("Сначала выполните анализ (кнопка Запустить анализ)",
                    "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowMethodsComparisonWindow();
        }

        private void btnShowDetailedMetrics_Click(object sender, EventArgs e)
        {
            if (allFullResults == null || allFullResults.Count == 0)
            {
                MessageBox.Show("Сначала выполните анализ (кнопка Запустить анализ)",
                    "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowDetailedMetricsWindow();
        }

        private void regresion_Click(object sender, EventArgs e)
        {
            try
            {
                if (ProcessedData == null || ProcessedData.Count == 0)
                {
                    MessageBox.Show("Сначала загрузите файл с данными и выполните анализ!",
                        "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (allFullResults == null || allFullResults.Count == 0)
                {
                    MessageBox.Show("Сначала выполните анализ кодирования (кнопка 'Запустить анализ')!",
                        "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Получаем категориальные колонки и целевые значения
                var categoricalColumns = new List<(string Name, List<string> Values)>();
                for (int i = 0; i < ColumnTypeInfo.CategoricalColumnIndices.Count; i++)
                {
                    int colIndex = ColumnTypeInfo.CategoricalColumnIndices[i];
                    var values = new List<string>();
                    foreach (var row in ColumnTypeInfo.RawData)
                    {
                        if (colIndex < row.Length)
                            values.Add(row[colIndex]?.Trim() ?? "");
                        else
                            values.Add("");
                    }
                    categoricalColumns.Add((ColumnNames[colIndex], values));
                }

                var targetValues = new List<double>();
                foreach (var row in ColumnTypeInfo.RawData)
                {
                    string targetStr = ColumnTypeInfo.TargetColumnIndex < row.Length
                        ? row[ColumnTypeInfo.TargetColumnIndex] : "0";
                    if (double.TryParse(targetStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double val))
                        targetValues.Add(val);
                    else
                        targetValues.Add(0);
                }

                // Спрашиваем пользователя, сколько лучших признаков использовать
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "Сколько лучших признаков использовать для регрессии?\n\n" +
                    "Рекомендуемые значения:\n" +
                    "• 1 - только лучший признак\n" +
                    "• 3 - топ-3 признака \n" +
                    "• 0 или пусто - использовать все признаки",
                    "Параметры регрессии",
                    "3");

                int? topFeatures = null;
                if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out int top))
                {
                    if (top > 0 && top <= categoricalColumns.Count)
                        topFeatures = top;
                    else if (top > categoricalColumns.Count)
                        MessageBox.Show($"Максимум доступно {categoricalColumns.Count} признаков. Будут использованы все.",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Создаем оценщик
                var evaluator = new RegressionEvaluator(k: 5, testSize: 0.3);

                // Все кодировщики
                var encoders = new IColumnEncoder[]
                {
            new ColumnIntegerEncoder(),
            new ColumnOneHotEncoder(),
            new ColumnEntropyEncoder(),
            new ColumnTargetEncoder(),
            new ColumnCatBoostEncoder()
                };

                // Выполняем оценку
                textBox1.Clear();
                textBox1.Text = "Выполняется регрессионный анализ KNN (k=5)...\n\n";
                Application.DoEvents();

                var results = evaluator.EvaluateAll(encoders, categoricalColumns, targetValues, topFeatures);

                // Выводим результаты
                var sb = new StringBuilder();
                sb.AppendLine("РЕЗУЛЬТАТЫ РЕГРЕССИИ KNN ДЛЯ РАЗНЫХ МЕТОДОВ КОДИРОВАНИЯ");
                sb.AppendLine($"Датасет: {Path.GetFileName(CurrentFilePath)}");
                sb.AppendLine($"Количество записей: {ProcessedData.Count}");
                sb.AppendLine($"KNN параметры: k=5, test_size=30%");
                sb.AppendLine($"Использовано признаков: {(topFeatures.HasValue ? topFeatures.Value : categoricalColumns.Count)} лучших");
                sb.AppendLine();

                sb.AppendLine($"{"Метод кодирования",-22} {"R² (качество)",12} {"MAE (Ср ошибка пред.)",12} {"RMSE (средняя величина ошибки пред.)",12} {"Время,мс",10}");

                foreach (var result in results)
                {
                    sb.AppendLine($"{result.MethodName,-22} {result.R2Score,11:F3}  {result.MAE,10:F2}  {result.RMSE,10:F2}  {result.ExecutionTimeMs,8:F0}");
                }

                sb.AppendLine();
                sb.AppendLine("ИСПОЛЬЗОВАННЫЕ ПРИЗНАКИ:");
                sb.AppendLine();

                var bestResult = results.First();
                sb.AppendLine($"Лучший метод: {bestResult.MethodName}");
                sb.AppendLine($"R² = {bestResult.R2Score:F3} ({(bestResult.R2Score * 100):F1}% дисперсии объяснено)");
                sb.AppendLine($"Использованные признаки: {string.Join(", ", bestResult.UsedFeatures)}");
                sb.AppendLine();
                sb.AppendLine("ДЛЯ ВСЕХ МЕТОДОВ:");

                foreach (var result in results)
                {
                    sb.AppendLine($"\n{result.MethodName}:");
                    sb.AppendLine($"  Признаки: {string.Join(", ", result.UsedFeatures)}");
                }


                textBox1.Text = sb.ToString();

                // Показываем график сравнения
                ShowRegressionChart(results);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении регрессии: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Показывает график сравнения результатов регрессии
        /// </summary>
        private void ShowRegressionChart(List<RegressionResult> results)
        {
            var chartForm = new Form
            {
                Text = "Сравнение методов кодирования (KNN Regression)",
                Size = new Size(1000, 600),
                StartPosition = FormStartPosition.CenterParent
            };

            var chart = new Chart
            {
                Dock = DockStyle.Fill
            };
            chart.ChartAreas.Add(new ChartArea());

            // График R²
            var r2Series = new Series
            {
                Name = "R² (коэффициент детерминации)",
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                LabelFormat = "F3",
                Color = Color.SteelBlue
            };

            foreach (var result in results)
            {
                r2Series.Points.AddXY(result.MethodName, result.R2Score);
            }

            chart.Series.Add(r2Series);

            chart.ChartAreas[0].AxisX.Title = "Методы кодирования";
            chart.ChartAreas[0].AxisX.Interval = 1;
            chart.ChartAreas[0].AxisY.Title = "Коэффициент детерминации R²";
            chart.ChartAreas[0].AxisY.Minimum = 0;
            chart.ChartAreas[0].AxisY.Maximum = 1;

            chart.Titles.Clear();
            chart.Titles.Add("Сравнение качества кодирования через KNN регрессию");

            // Легенда
            chart.Legends.Clear();
            var legend = new Legend("Legend");
            legend.Docking = Docking.Top;
            chart.Legends.Add(legend);

            chartForm.Controls.Add(chart);
            chartForm.ShowDialog();
        }
    }

}