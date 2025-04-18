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

namespace SetImageDatetime
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private Dictionary<string, ListViewItem> filePathToListItem = new Dictionary<string, ListViewItem>();
        private void FileListView_DragDrop(object sender, DragEventArgs e)
        {
            var dropData = (System.Array)e.Data.GetData(DataFormats.FileDrop);
            foreach (string path in dropData)
            {
                FileInfo file = new FileInfo(path);
                if (file.Exists && file.Extension.ToLower() == ".ncm")
                {
                    //处理重复添加
                    if (filePathToListItem.ContainsKey(file.FullName))
                    {
                        continue;
                    }
                    //在列表中添加项
                    var item = new ListViewItem(new string[] { file.Name, file.FullName, "" });
                    FileListView.Items.Add(item);
                    filePathToListItem.Add(file.FullName, item);
                }
            }
        }

        private void btnCleanList_Click(object sender, EventArgs e)
        {
            FileListView.Items.Clear();
            filePathToListItem.Clear();
        }

        private void btnExec_Click(object sender, EventArgs e)
        {

        }
    }
}
