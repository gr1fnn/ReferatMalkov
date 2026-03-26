using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ClassLib;

namespace MyForm
{
    public partial class Form1 : Form
    {
        // Поля для хранения загруженных данных
        private string CurrentFilePath;
        private string[] ColumnNames;
        private List<string[]> RawData;
        private List<DataPoint> ProcessedData;
        private ColumnTypeDetector.ColumnTypeResult ColumnTypeInfo;
        private ColumnTypeDetector TypeDetector;

        public Form1()
        {
            InitializeComponent();
            InitializeDataGridView();
            InitializeTextBox();

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

                // Загружаем данные
                RawData = LoadCSV(filePath);

                if (RawData == null || RawData.Count == 0)
                {
                    throw new Exception("Файл не содержит данных");
                }

                // Получаем названия колонок
                ColumnNames = RawData[0];

                // Удаляем заголовок из данных
                var dataRows = RawData.Skip(1).ToList();

                // Определяем типы колонок с помощью отдельного класса
                ColumnTypeInfo = TypeDetector.DetectColumnTypes(ColumnNames, dataRows);

                // Конвертируем в DataTable для отображения
                var table = ConvertToDataTable(dataRows, ColumnNames);
                dataGridView1.DataSource = table;

                // Конвертируем данные в формат DataPoint для обработки
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

        private void runButton_Click(object sender, EventArgs e)
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

                var resultBuilder = new StringBuilder();

                resultBuilder.AppendLine($"Датасет: {Path.GetFileName(CurrentFilePath)}");
                resultBuilder.AppendLine($"Количество записей: {ProcessedData.Count}");
                resultBuilder.AppendLine($"Количество категориальных признаков: {ProcessedData[0].Categories.Length}");
                resultBuilder.AppendLine($"Количество уникальных категорий: {GetUniqueCategoriesCount()}");
                resultBuilder.AppendLine();

                // Список категориальных признаков
                resultBuilder.AppendLine("Категориальные признаки:");
                for (int i = 0; i < ColumnTypeInfo.CategoricalColumnIndices.Count; i++)
                {
                    resultBuilder.AppendLine($"  {i + 1}. {ColumnNames[ColumnTypeInfo.CategoricalColumnIndices[i]]}");
                }
                resultBuilder.AppendLine();
                resultBuilder.AppendLine($"Целевая переменная: {ColumnNames[ColumnTypeInfo.TargetColumnIndex]}");
                resultBuilder.AppendLine();


                // Инициализация всех кодировщиков
                var encoders = new ICategoricalEncoder[]
                {
            new OneHotEncoder(),
            new IntegerEncoder(),
            new EntropyEncoder(),
            new TargetEncoder(),
            new CatBoostEncoder()
                };

                var results = new List<EncodingResult>();

                // Заголовки таблицы
                resultBuilder.AppendLine("Метод                Размерность  Качество    Время(мс)  Потеря инф.");

                foreach (var encoder in encoders)
                {
                    textBox1.Text = resultBuilder.ToString();
                    Application.DoEvents();

                    var result = encoder.EncodeAndEvaluate(ProcessedData);
                    results.Add(result);

                    resultBuilder.AppendLine($"{result.MethodName,-20} {result.Dimensionality,10}   {result.QualityScore,7:F2}%   {result.EncodingTime,8:F0}     {result.InformationLoss,8:F2}%");
                }



                foreach (var result in results)
                {
                    resultBuilder.AppendLine(result.MethodName);
                    resultBuilder.AppendLine($"  Размерность: {result.Dimensionality}");
                    resultBuilder.AppendLine($"  Качество кодирования: {result.QualityScore:F2}%");
                    resultBuilder.AppendLine($"  Время кодирования: {result.EncodingTime:F0} мс");
                    resultBuilder.AppendLine($"  Потеря информации: {result.InformationLoss:F2}%");

                    if (result.Metrics != null && result.Metrics.Count > 0)
                    {
                        resultBuilder.AppendLine("  Метрики качества:");

                        foreach (var metric in result.Metrics)
                        {
                            string metricName = metric.Key switch
                            {
                                "Correlation" => "    Корреляция Пирсона",
                                "SpearmanCorr" => "    Корреляция Спирмена",
                                "MutualInfo" => "    Взаимная информация",
                                "R2_Score" => "    R² (коэф. детерминации)",
                                _ => $"    {metric.Key}"
                            };

                            resultBuilder.AppendLine($"{metricName}: {metric.Value:F4}");
                        }
                    }
                    resultBuilder.AppendLine();
                }

                textBox1.Text = resultBuilder.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении анализа: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int GetUniqueCategoriesCount()
        {
            if (ProcessedData == null || ProcessedData.Count == 0)
                return 0;

            var uniqueCategories = new System.Collections.Generic.HashSet<string>();

            foreach (var point in ProcessedData)
            {
                foreach (var category in point.Categories)
                {
                    uniqueCategories.Add(category);
                }
            }

            return uniqueCategories.Count;
        }
    }
}