namespace MyForm
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dataGridView1 = new DataGridView();
            ChoseDataSetButton = new Button();
            runButton = new Button();
            textBox1 = new TextBox();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(333, 12);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(772, 294);
            dataGridView1.TabIndex = 0;
            // 
            // ChoseDataSetButton
            // 
            ChoseDataSetButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 204);
            ChoseDataSetButton.Location = new Point(38, 12);
            ChoseDataSetButton.Name = "ChoseDataSetButton";
            ChoseDataSetButton.Size = new Size(260, 64);
            ChoseDataSetButton.TabIndex = 1;
            ChoseDataSetButton.Text = "Выберите датасет";
            ChoseDataSetButton.UseVisualStyleBackColor = true;
            ChoseDataSetButton.Click += ChoseDataSetButton_Click;
            // 
            // runButton
            // 
            runButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 204);
            runButton.Location = new Point(38, 109);
            runButton.Name = "runButton";
            runButton.Size = new Size(260, 64);
            runButton.TabIndex = 2;
            runButton.Text = "Запустить преобразование";
            runButton.UseVisualStyleBackColor = true;
            runButton.Click += runButton_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(333, 331);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(772, 260);
            textBox1.TabIndex = 3;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(192, 255, 192);
            ClientSize = new Size(1117, 618);
            Controls.Add(textBox1);
            Controls.Add(runButton);
            Controls.Add(ChoseDataSetButton);
            Controls.Add(dataGridView1);
            Name = "Form1";
            Text = "MyForm";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dataGridView1;
        private Button ChoseDataSetButton;
        private Button runButton;
        private TextBox textBox1;
    }
}
