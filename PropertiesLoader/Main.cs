using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace PropertiesLoader
{
    public partial class Main : Form
    {
        string[] lines;
        DataTable table;

        public Main()
        {
            InitializeComponent();
            
            //progressbar
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Lang files (*.lang)|*.lang";
            dialog.RestoreDirectory = true;
            dialog.Title = "Choose DEFAULT lang file";
            dialog.FileName = "en_US.lang";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                //store original lines
                lines = File.ReadAllLines(dialog.FileName);

                InitTable();

                buttonSave.Enabled = false;

                int line = 0;
                foreach (string row in lines)
                {
                    line++;
                    if (row.Split('=').Length == 2)
                    {
                        string key = row.Split('=')[0];
                        if (key.Contains("*")) continue;
                        table.Rows.Add(line, key, string.Join("=", row.Split('=').Skip(1).ToArray()));
                    }                        
                }
            }            
        }

        private void InitTable()
        {
            table = new DataTable();
            dataGridView1.DataSource = table;

            table.Columns.Add("Line", typeof(int));
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Translation", typeof(string));

            dataGridView1.Columns["Line"].Width = 50;
            dataGridView1.Columns["Key"].Width = 200;
            dataGridView1.Columns["Translation"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Lang files (*.lang)|*.lang";
            dialog.RestoreDirectory = true;
            dialog.Title = "Choose YOUR CUSTOM lang file";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                fileLocCustom.Text = dialog.FileName;
                
                if (table == null || table.Rows.Count == 0)
                {
                    //load file
                    lines = File.ReadAllLines(dialog.FileName);

                    InitTable();

                    int line = 0;
                    foreach (string row in lines)
                    {
                        line++;
                        if (row.Split('=').Length == 2)
                        {
                            string key = row.Split('=')[0];
                            if (key.Contains("*")) continue;
                            table.Rows.Add(line, key, string.Join("=", row.Split('=').Skip(1).ToArray()));
                        }
                    }
                    EndLoading();
                }
                else
                {
                    button1.Enabled = false;
                    buttonOpen.Enabled = false;
                    buttonSave.Enabled = false;
                    panel1.Visible = true;
                    label7.Visible = false;

                    backgroundWorker1.RunWorkerAsync();
                }                
            }
        }

        void EndLoading()
        {
            panel1.Visible = false;
            dataGridView1.Enabled = true;
            button1.Enabled = true;
            buttonOpen.Enabled = true;
            buttonSave.Enabled = true;
            buttonSave.Enabled = true;
            if (dataGridView1.Rows.Count > 0)
            {
                dataGridView1.Rows[0].Selected = true;
                textBox4.Text = dataGridView1.Rows[0].Cells["Line"].Value?.ToString();
            }                
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            textBox1.Text = dataGridView1.SelectedRows[0].Cells["Translation"].Value?.ToString();
        }

        void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int count = 0;
            string[] custom = File.ReadAllLines(fileLocCustom.Text);

            foreach (string row in custom)
            {
                count++;
                if (row.Split('=').Length == 2)
                {
                    string key = row.Split('=')[0];
                    string value = string.Join("=", row.Split('=').Skip(1).ToArray());

                    if (key.Contains("*")) continue;

                    DataRow dr = table.Select("Key = '" + key.Replace("'", "''") + "'").FirstOrDefault();
                    if (dr != null && !dr["Translation"].Equals(value))
                    {
                        dr["Translation"] = value;
                    }
                }
                double med = (double)count / custom.Length;
                backgroundWorker1.ReportProgress((int)(med * 100));
            }
        }

        void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            if (e.ProgressPercentage == 100)
                EndLoading();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            foreach (DataRow row in table.Rows)
            {
                int line = int.Parse(row["Line"].ToString());
                string key = row["Key"].ToString();
                string value = row["Translation"].ToString();

                lines[line-1] = key + "=" + value;
            }
            File.WriteAllLines(fileLocCustom.Text, lines);
            label7.Visible = true;
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    object value = row.Cells["Translation"].Value;
                    if (value != null && value.ToString().Contains(textBox3.Text))
                    {
                        row.Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                        textBox4.Text = row.Cells["Line"].Value?.ToString();
                        break;
                    }
                }
            }
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                try
                {
                    WebClient wc = new WebClient();
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0");
                    wc.Headers.Add(HttpRequestHeader.AcceptCharset, "UTF-8");
                    wc.Encoding = Encoding.UTF8;

                    string url = string.Format(@"http://translate.google.com.tr/m?hl=en&sl={0}&tl={1}&ie=UTF-8&prev=_m&q={2}",
                                                "us", textBox2.Text, Uri.EscapeUriString(textBox1.Text));

                    string page = wc.DownloadString(url);
                    page = page.Remove(0, page.IndexOf("<div dir=\"ltr\" class=\"t0\">")).Replace("<div dir=\"ltr\" class=\"t0\">", "");
                    int last = page.IndexOf("</div>");
                    page = page.Remove(last, page.Length - last);

                    textBox1.Text = page;

                    label3.ForeColor = System.Drawing.Color.Black;
                    label3.Text = "No errors";
                }
                catch (Exception ex)
                {
                    label3.ForeColor = System.Drawing.Color.Red;
                    label3.Text = ex.Message;
                }
            }            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0 && dataGridView1.SelectedRows.Count > 0)
            {
                dataGridView1.SelectedRows[0].Cells["Translation"].Value = textBox1.Text;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0 && dataGridView1.SelectedRows.Count > 0)
            {
                int index = dataGridView1.SelectedRows[0].Index + 1;
                if (index < dataGridView1.Rows.Count)
                {
                    dataGridView1.SelectedRows[0].Selected = false;
                    dataGridView1.FirstDisplayedScrollingRowIndex = index;
                    dataGridView1.Rows[index].Selected = true;
                    textBox4.Text = dataGridView1.Rows[index].Cells["Line"].Value?.ToString();
                }
            }                           
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.Rows[e.RowIndex].Selected = true;
            textBox4.Text = dataGridView1.Rows[e.RowIndex].Cells["Line"].Value?.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count > 0 && dataGridView1.SelectedRows.Count > 0)
            {
                dataGridView1.SelectedRows[0].Cells["Translation"].Value = textBox1.Text;

                int index = dataGridView1.SelectedRows[0].Index + 1;
                if (index < dataGridView1.Rows.Count)
                {
                    dataGridView1.SelectedRows[0].Selected = false;
                    dataGridView1.FirstDisplayedScrollingRowIndex = index;
                    dataGridView1.Rows[index].Selected = true;
                    textBox4.Text = dataGridView1.Rows[index].Cells["Line"].Value?.ToString();

                    if (checkBox1.Checked && textBox1.Text.Split(' ').Length > 2)
                    {                        
                        button2.PerformClick();
                    }
                }
            }
        }

        private void textBox4_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && dataGridView1.Rows.Count > 0 && textBox4.Text.Length > 0)
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    object value = row.Cells["Line"].Value;
                    if (value != null && value.ToString().Equals(textBox4.Text))
                    {
                        row.Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                        textBox4.Text = row.Cells["Line"].Value?.ToString();
                        break;
                    }                    
                }                              
            }
        }
    }
}
