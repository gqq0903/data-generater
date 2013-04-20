using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace DataGenerate
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void generate_Click(object sender, EventArgs e)
        {
            if (isErrorPresent())
            {
                MessageBox.Show("Some cells are blank.");
            }
            else
            {
                dataGridView1.Enabled = false;
                numericUpDown1.Enabled = false;
                resultFilename.Enabled = false;
                generate.Enabled = false;
                saveButton.Enabled = false;
                loadButton.Enabled = false;
                progressBar1.Value = 0;
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void menuitem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in this.dataGridView1.SelectedRows) //遍历所选中的dataGridView记录行
            {
                if (row.Index < dataGridView1.RowCount - 1)
                {
                    dataGridView1.Rows.RemoveAt(row.Index);
                }
            }
            if (dataGridView1.RowCount == 1)
            {
                generate.Enabled = false;
            }
        }

        private void dataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (dataGridView1.SelectedRows.Count > 0)
                {
                    menuitem.Enabled = true;
                    menu.Show(dataGridView1, e.Location);
                }
                else
                {
                    menuitem.Enabled = false;
                    menu.Show(dataGridView1, e.Location);
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int period = 10000;
            int minRegexPossible = 250;
            int regexCount = period / minRegexPossible;
            String tempFileName = "temp.txt";
            String regexFileName = "regexTemp.txt";
            int times = int.Parse(numericUpDown1.Value.ToString());
            int runTimes = times / period;

            int attributeCount = dataGridView1.RowCount - 1;

            int[] keyCount = new int[attributeCount];
            String attributeNames = "";
            String[] s_randomTypes = new String[attributeCount];
            String[] s_randomStrings = new String[attributeCount];

            String[] values = new String[period];

            for (int i = 0; i < attributeCount; i++)
            {
                String str = dataGridView1.Rows[i].Cells[0].Value.ToString();
                if (attributeNames == null || attributeNames.Equals(""))
                {
                    attributeNames = str;
                }
                else
                {
                    attributeNames = attributeNames + "," + str;
                }
                s_randomTypes[i] = dataGridView1.Rows[i].Cells[1].Value.ToString();
                s_randomStrings[i] = dataGridView1.Rows[i].Cells[2].Value.ToString();

                keyCount[i] = 1;
            }

            var sw = new StreamWriter(resultFilename.Text, false, Encoding.UTF8);
            sw.WriteLine(attributeNames);

            for (int i = 0; i < runTimes; i++)
            {
                for (int k = 0; k < attributeCount; k++)
                {
                    long tick = DateTime.Now.Ticks;
                    Random ran = new Random((int)(tick & 0xffffffffL) | (int)(tick >> 32));
                    switch (s_randomTypes[k])
                    {
                        case "Regex":
                            String regex = "^" + s_randomStrings[k] + "$";
                            var rsw = new StreamWriter(regexFileName, false, Encoding.UTF8);
                            for (int l = 0; l < regexCount; l++)
                            {
                                rsw.WriteLine(regex);
                            }
                            rsw.Close();
                            rsw.Dispose();
                            Process myprocess = new Process();
                            ProcessStartInfo startInfo = new ProcessStartInfo("Rex.exe", "/regexfile:" + regexFileName + " " + "/k:" + period + " " + "/file:" + tempFileName + " " + "/seed:" + ran.Next(1, 10000) + " /e:ASCII");
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            startInfo.WorkingDirectory = Application.StartupPath;
                            myprocess.StartInfo = startInfo;
                            myprocess.StartInfo.UseShellExecute = true;
                            myprocess.Start();
                            myprocess.WaitForExit();
                            StreamReader reader = new StreamReader(tempFileName);
                            for (int j = 0; j < period; j++)
                            {
                                string sLine = "";
                                sLine = reader.ReadLine();
                                if (sLine == null)
                                {
                                    sLine = "";
                                }
                                else
                                {
                                    sLine = sLine.Substring(1, sLine.Length - 2);
                                    sLine = Regex.Replace(sLine, @"\\u.{4}", "");
                                }
                                if (values[j] == null)
                                {
                                    values[j] = sLine;
                                }
                                else
                                {
                                    values[j] = values[j] + "," + sLine;
                                }
                            }
                            reader.Close();
                            reader.Dispose();
                            break;
                        case "List": String[] lists = s_randomStrings[k].Split(',');
                            int length = lists.Length;
                            for (int j = 0; j < period; j++)
                            {
                                if (values[j] == null)
                                {
                                    values[j] = lists[ran.Next(0, length)];
                                }
                                else
                                {
                                    values[j] = values[j] + "," + lists[ran.Next(0, length)];
                                }
                            }
                            break;
                        case "Mix": String str = s_randomStrings[k];
                            Regex r = new Regex(@"\[.*?\]");
                            MatchCollection mc = r.Matches(str);
                            String[] randoms = new String[mc.Count];
                            str = r.Replace(str, "%s");
                            for (int j = 0; j < period; j++)
                            {
                                for (int p = 0; p < mc.Count; p++)
                                {
                                    randoms[p] = random(mc[p].Value, ran, keyCount[k]);
                                }
                                if (values[j] == null)
                                {
                                    values[j] = format(str, randoms);
                                }
                                else
                                {
                                    values[j] = values[j] + "," + format(str, randoms);
                                }
                                keyCount[k]++;
                            }
                            break;
                    }
                    int all = runTimes * attributeCount;
                    int current = i * attributeCount + k + 1;
                    backgroundWorker1.ReportProgress(100 * current / all);
                }
                foreach (String value in values)
                {
                    sw.WriteLine(value);
                }
                values = new String[period];
            }
            sw.Close();
            sw.Dispose();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dataGridView1.Enabled = true;
            numericUpDown1.Enabled = true;
            resultFilename.Enabled = true;
            generate.Enabled = true;
            saveButton.Enabled = true;
            loadButton.Enabled = true;
        }

        private void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            DataGridView grid = (DataGridView)sender;
            try
            {
                if (grid.Rows[e.RowIndex - 1].Cells[0].Value == null)
                {
                    grid.Rows[e.RowIndex - 1].Cells[0].ErrorText = "This cell can not be blank.";
                }
                if (grid.Rows[e.RowIndex - 1].Cells[1].Value == null)
                {
                    grid.Rows[e.RowIndex - 1].Cells[1].ErrorText = "This cell can not be blank.";
                }
                if (grid.Rows[e.RowIndex - 1].Cells[2].Value == null)
                {
                    grid.Rows[e.RowIndex - 1].Cells[2].ErrorText = "This cell can not be blank.";
                }
            }
            catch (Exception ex)
            {
            }
            generate.Enabled = true;
        }

        private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridView grid = (DataGridView)sender;
            if (!grid.CurrentRow.IsNewRow)
            {
                if (e.FormattedValue.ToString().Equals(""))
                {
                    grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = "This cell can not be blank.";
                }
                else
                {
                    grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = "";
                }
            }
        }

        private bool isErrorPresent()
        {
            for (int i = 0; i < dataGridView1.RowCount - 1; i++)
            {
                for (int j = 0; j < dataGridView1.ColumnCount; j++)
                {
                    if (!dataGridView1.Rows[i].Cells[j].ErrorText.Equals(""))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dataGridView1.Rows.Clear();
                XmlDocument xml = new XmlDocument();
                xml.Load(openFileDialog1.FileName);
                XmlNode root = xml.FirstChild;
                foreach (XmlNode attributeNode in root.SelectNodes("./attribute"))
                {
                    String nameStr = attributeNode.SelectSingleNode("./name").InnerText;
                    String typeStr = attributeNode.SelectSingleNode("./type").InnerText;
                    String stringStr = attributeNode.SelectSingleNode("./string").InnerText;
                    dataGridView1.Rows.Add(1);
                    dataGridView1.Rows[dataGridView1.RowCount - 2].Cells[0].Value = nameStr;
                    dataGridView1.Rows[dataGridView1.RowCount - 2].Cells[1].Value = typeStr;
                    dataGridView1.Rows[dataGridView1.RowCount - 2].Cells[2].Value = stringStr;
                }
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                XmlDocument xml = new XmlDocument();
                XmlNode root = xml.CreateNode(XmlNodeType.Element, "config", "");
                xml.AppendChild(root);
                for (int i = 0; i < dataGridView1.RowCount - 1; i++)
                {
                    String nameStr = dataGridView1.Rows[i].Cells[0].Value.ToString();
                    String typeStr = dataGridView1.Rows[i].Cells[1].Value.ToString();
                    String stringStr = dataGridView1.Rows[i].Cells[2].Value.ToString();
                    XmlNode attributeNode = xml.CreateNode(XmlNodeType.Element, "attribute", "");
                    root.AppendChild(attributeNode);
                    XmlNode nameNode = xml.CreateNode(XmlNodeType.Element, "name", "");
                    nameNode.InnerText = nameStr;
                    XmlNode typeNode = xml.CreateNode(XmlNodeType.Element, "type", "");
                    typeNode.InnerText = typeStr;
                    XmlNode stringNode = xml.CreateNode(XmlNodeType.Element, "string", "");
                    stringNode.InnerText = stringStr;
                    attributeNode.AppendChild(nameNode);
                    attributeNode.AppendChild(typeNode);
                    attributeNode.AppendChild(stringNode);
                }
                xml.Save(saveFileDialog1.FileName);
            }
        }

        private String random(String stringIn, Random ran, int keyCount)
        {
            String str = stringIn.Substring(1, stringIn.Length - 2);
            String[] args = str.Split(',');
            if (args.Length == 1)
            {
                if (args[0].Equals("key"))
                {
                    return keyCount.ToString();
                }
                else
                {
                    return args[0];
                }
            }
            else if (args.Length == 2)
            {
                return ran.Next(int.Parse(args[0]), int.Parse(args[1]) + 1).ToString();
            }
            else if (args.Length == 3)
            {
                if (args[0].Contains('-'))
                {
                    string[] sArray = args[0].Split('-');
                    int iBeginYear = int.Parse(sArray[0]);
                    int iBeginMonth = int.Parse(sArray[1]);
                    int iBeginDay = int.Parse(sArray[2]);

                    DateTime dtTime1 = new DateTime(iBeginYear, iBeginMonth, iBeginDay);

                    sArray = args[1].Split('-');
                    int iYear = int.Parse(sArray[0]);
                    int iMonth = int.Parse(sArray[1]);
                    int iDay = int.Parse(sArray[2]);

                    DateTime dtTime2 = new DateTime(iYear, iMonth, iDay);

                    TimeSpan tsSpan = dtTime2.Subtract(dtTime1);
                    int iMinusDay = tsSpan.Days;
                    return dtTime1.AddDays(ran.Next(0, iMinusDay + 1)).ToString(args[2]);
                }
                else
                {
                    double multiple = Math.Pow(10, int.Parse(args[2]));
                    int min = (int)(double.Parse(args[0]) * multiple);
                    int max = (int)(double.Parse(args[1]) * multiple + 1);
                    return ((double)ran.Next(min, max) / multiple).ToString();
                }
            }
            return "";
        }

        private String format(String formatIn, String[] randoms)
        {
            String formatOut = formatIn;
            int i = 0;
            while (formatOut.IndexOf("%s") != -1)
            {
                int index = formatOut.IndexOf("%s");
                String random;
                try
                {
                    random = randoms[i];
                }
                catch
                {
                    random = "";
                }
                formatOut = formatOut.Substring(0, index) + random + formatOut.Substring(index + 2, formatOut.Length - index - 2);
                i++;
            }
            return formatOut;
        }
    }
}
