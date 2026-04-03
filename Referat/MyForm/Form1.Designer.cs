namespace MyForm
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            dataGridView1 = new DataGridView();
            ChoseDataSetButton = new Button();
            runButton = new Button();
            textBox1 = new TextBox();
            btnShowMethodsChart = new Button();
            btnShowDetailedMetrics = new Button();
            panelButtons = new Panel();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            panelButtons.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(340, 12);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.Size = new Size(982, 250);
            dataGridView1.TabIndex = 0;
            // 
            // ChoseDataSetButton
            // 
            ChoseDataSetButton.Font = new Font("Segoe UI", 12F);
            ChoseDataSetButton.Location = new Point(15, 12);
            ChoseDataSetButton.Name = "ChoseDataSetButton";
            ChoseDataSetButton.Size = new Size(310, 45);
            ChoseDataSetButton.TabIndex = 1;
            ChoseDataSetButton.Text = "Выберите датасет";
            ChoseDataSetButton.UseVisualStyleBackColor = true;
            ChoseDataSetButton.Click += ChoseDataSetButton_Click;
            // 
            // runButton
            // 
            runButton.Font = new Font("Segoe UI", 12F);
            runButton.Location = new Point(15, 65);
            runButton.Name = "runButton";
            runButton.Size = new Size(310, 45);
            runButton.TabIndex = 2;
            runButton.Text = "Запустить анализ";
            runButton.UseVisualStyleBackColor = true;
            runButton.Click += runButton_Click;
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBox1.Location = new Point(340, 280);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox1.Size = new Size(982, 388);
            textBox1.TabIndex = 3;
            // 
            // btnShowMethodsChart
            // 
            btnShowMethodsChart.Font = new Font("Segoe UI", 10F);
            btnShowMethodsChart.Location = new Point(17, 116);
            btnShowMethodsChart.Name = "btnShowMethodsChart";
            btnShowMethodsChart.Size = new Size(310, 108);
            btnShowMethodsChart.TabIndex = 6;
            btnShowMethodsChart.Text = "Сравнение методов (отдельное окно)";
            btnShowMethodsChart.UseVisualStyleBackColor = true;
            btnShowMethodsChart.Click += btnShowMethodsChart_Click;
            // 
            // btnShowDetailedMetrics
            // 
            btnShowDetailedMetrics.Font = new Font("Segoe UI", 10F);
            btnShowDetailedMetrics.Location = new Point(12, 230);
            btnShowDetailedMetrics.Name = "btnShowDetailedMetrics";
            btnShowDetailedMetrics.Size = new Size(310, 100);
            btnShowDetailedMetrics.TabIndex = 7;
            btnShowDetailedMetrics.Text = "Детальные метрики (отдельное окно)";
            btnShowDetailedMetrics.UseVisualStyleBackColor = true;
            btnShowDetailedMetrics.Click += btnShowDetailedMetrics_Click;
            // 
            // panelButtons
            // 
            panelButtons.Controls.Add(ChoseDataSetButton);
            panelButtons.Controls.Add(runButton);
            panelButtons.Controls.Add(btnShowMethodsChart);
            panelButtons.Controls.Add(btnShowDetailedMetrics);
            panelButtons.Dock = DockStyle.Left;
            panelButtons.Location = new Point(0, 0);
            panelButtons.Name = "panelButtons";
            panelButtons.Size = new Size(330, 680);
            panelButtons.TabIndex = 10;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(192, 255, 192);
            ClientSize = new Size(1334, 680);
            Controls.Add(panelButtons);
            Controls.Add(textBox1);
            Controls.Add(dataGridView1);
            Name = "Form1";
            Text = "Анализ методов категориального кодирования";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            panelButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private DataGridView dataGridView1;
        private Button ChoseDataSetButton;
        private Button runButton;
        private TextBox textBox1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartMetrics;
        private Button btnShowMethodsChart;
        private Button btnShowDetailedMetrics;
        private Panel panelButtons;
    }
}