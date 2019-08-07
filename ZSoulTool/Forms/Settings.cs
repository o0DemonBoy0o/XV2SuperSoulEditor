using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XV2SSEdit.Forms
{
    public partial class Settings : Form
    {
        public ToolSettings settings { get; set; }
        public bool Finished { get; set; }

        public Settings(ToolSettings _settings)
        {
            settings = _settings;
            InitializeComponent();
            InitLanguageList();
            directoryTextBox.Text = settings.GameDir;
        }

        private void InitLanguageList()
        {
            comboBox1.Items.Add(Language.English);
            comboBox1.Items.Add(Language.Spanish_CA);
            comboBox1.Items.Add(Language.Spanish_ES);
            comboBox1.Items.Add(Language.French);
            comboBox1.Items.Add(Language.German);
            comboBox1.Items.Add(Language.Italian);
            comboBox1.Items.Add(Language.Portuguese);
            comboBox1.Items.Add(Language.Polish);
            comboBox1.Items.Add(Language.Russian);
            comboBox1.Items.Add(Language.Chinese_TW);
            comboBox1.Items.Add(Language.Chinese_ZH);
            comboBox1.Items.Add(Language.Korean);

            comboBox1.SelectedIndex = (int)settings.GameLanguage;
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowse = new FolderBrowserDialog();
            folderBrowse.Description = "Browse for DB Xenoverse 2 Directory";
            folderBrowse.ShowDialog();

            if (!Directory.Exists(folderBrowse.SelectedPath) || !File.Exists(String.Format("{0}/bin/DBXV2.exe", folderBrowse.SelectedPath)))
            {
                if(MessageBox.Show(this, "The selected directory does not seem to be valid. Are you sure you selected the correct one?", "Browse", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    return;
                }
            }

            directoryTextBox.Text = folderBrowse.SelectedPath;
        }

        private void buttonDone_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(directoryTextBox.Text))
            {
                MessageBox.Show(this, "The game directory cannot be empty.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(directoryTextBox.Text) || !File.Exists(String.Format("{0}/bin/DBXV2.exe", directoryTextBox.Text)))
            {
                if (MessageBox.Show(this, "The selected directory does not seem to be valid. Are you sure you selected the correct one?", "Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    return;
                }
            }

            settings.GameLanguage = (Language)comboBox1.SelectedIndex;
            settings.GameDir = directoryTextBox.Text;

            Finished = true;
            Close();
        }

        private void Settings_Load(object sender, EventArgs e)
        {

        }
    }
}
