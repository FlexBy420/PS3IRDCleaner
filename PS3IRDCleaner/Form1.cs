using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using IsoTools.Iso9660;

namespace IsoTools {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        struct PS3File {
            public string Filename;
            public long Lenght;
        }

        private void bStart_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog() { Title = "Select an IRD File", Filter = "IRD files (*.ird)|*.ird" };
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK || Path.GetExtension(ofd.FileName) != ".ird") {
                MessageBox.Show("Not and IRD File?");
                return;
            }

            FolderBrowserDialog fbd = new FolderBrowserDialog() { Description = "Select a JB PS3 Folder" } ;
            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK || !Directory.Exists(fbd.SelectedPath + "\\PS3_GAME\\")) {
                MessageBox.Show("Not and JB Folder?");
                return;
            }
            this.Enabled = false;

            List<PS3File> lvFilesRaw = new List<PS3File>();
            IrdFile irdFile = IrdFile.Load(ofd.FileName);
            PS3CDReader pS3CDReader = new PS3CDReader(irdFile.Header);
            ICollection<DirectoryMemberInformation> members = pS3CDReader.Members;
            foreach (DirectoryMemberInformation directoryMemberInformation in (from d in members
                                                                               where d.IsFile
                                                                               select d).Distinct<DirectoryMemberInformation>())
                lvFilesRaw.Add(new PS3File() { Lenght = directoryMemberInformation.Length, Filename = directoryMemberInformation.Path });

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int untouchedItems = 0, createdItems = 0, resizedItems = 0;
            foreach (PS3File F in lvFilesRaw) {
                string currentFile = fbd.SelectedPath + F.Filename.Replace('/', '\\');
                if (File.Exists(currentFile)) {
                    using (FileStream fs = File.Open(currentFile, FileMode.Open, FileAccess.ReadWrite)) {
                        if (fs.Length != F.Lenght) {
                            fs.SetLength(F.Lenght);
                            sb.AppendLine(F.Filename + " resized");
                            ++resizedItems;
                        } else {
                            ++untouchedItems;
                        }
                    }
                } else {
                    // Create the Directories if needed.
                    if (!Directory.Exists(Path.GetDirectoryName(currentFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(currentFile));

                    // Create the file if needed.
                    using (FileStream fs = File.Create(currentFile)) {
                        fs.SetLength(F.Lenght);
                        sb.AppendLine(F.Filename + " created");
                        ++createdItems;
                    }
                }
            }

            sb.AppendLine(String.Format(Environment.NewLine + "Summary: {0} files untouched, {1} files created, {2} files resized", untouchedItems, createdItems, resizedItems));
            tbResults.Text = sb.ToString();
            this.Enabled = true;
        }
    }
}
