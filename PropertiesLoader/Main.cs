using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Diagnostics;
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
            dataGridView1.Columns["Key"].Width = 300;
            dataGridView1.Columns["Key"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns["Translation"].Width = 300;
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
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            textBox1.Text = dataGridView1.SelectedRows[0].Cells["Translation"].Value.ToString();
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
                        break;
                    }
                }
            }
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string strLangPair = string.Format("en|{0} ", textBox2);

                // generate google translate request
                // http://translate.google.com/translate_a/t?client=j&sl=en&tl=ru&text=some%20text
                string url = String.Format("http://translate.google.com/translate_a/t?client=j&sl=en&tl={1}&text={0}",
                  textBox1.Text, textBox2);

                WebClient webClient = new WebClient();
                byte[] resultBin = webClient.DownloadData(url);
                string charset = webClient.ResponseHeaders["Content-Type"];
                charset = charset.Substring(charset.LastIndexOf('=') + 1);

                Encoding transmute = Encoding.GetEncoding(charset);
                string result = transmute.GetString(resultBin);

                // find result box and get a text
                JObject item = JObject.Parse(result);
                JToken token = item["sentences"];
                String translation = string.Empty;

                foreach (JToken childToken in token)
                {
                    string phrase = childToken["trans"].ToString();
                    translation += phrase.TrimStart('"').TrimEnd('"');
                }

                textBox1.Text = translation;
            }
            catch (Exception ex)
            {
                label3.ForeColor = System.Drawing.Color.Red;
                label3.Text = ex.Message;
            }
        }

    }
}
