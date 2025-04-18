using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExifLibrary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Tiff.Constants;
using SixLabors.ImageSharp.PixelFormats;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace SetImageDatetime
{
    public partial class MainForm : Form
    {
        readonly string _toConvert = "待转换";
        readonly string _converting = "转换中";
        readonly string _toModify = "待修改";
        readonly string _complete = "完成";
        readonly string _error = "错误";
        public MainForm()
        {
            InitializeComponent();
        }

        string[] allowAddExts = new string[] { ".png", ".jpeg", ".jpg", ".tif", ".tiff" };
        string[] canModifyExts = new string[] { ".jpeg", ".jpg" };

        /// <summary>
        /// 输出路径
        /// </summary>
        private string outputDir { get; set; }
        /// <summary>
        /// 记录输出路径的文件路径
        /// </summary>
        private string outputDirFilePath
        {
            get
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "outputPath.txt");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //读取上次选择的输出目录，如果没有，就使用当前目录
            if (File.Exists(outputDirFilePath))
            {
                outputDir = File.ReadAllText(outputDirFilePath);
            }

            if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir)) 
            { 
                outputDir = Directory.GetCurrentDirectory();
                File.WriteAllText(outputDirFilePath, outputDir);
            }
            txtOutputPath.Text = outputDir;
        }

        private Dictionary<string, ListViewItem> filePathToListItem = new Dictionary<string, ListViewItem>();

        #region 拖放支持
        private void FileListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void FileListView_DragDrop(object sender, DragEventArgs e)
        {
            var dropData = (System.Array)e.Data.GetData(DataFormats.FileDrop);
            foreach (string path in dropData)
            {
                FileInfo file = new FileInfo(path);
                string ext = file.Extension.ToLower();
                //只接受指定的扩展名
                if (file.Exists && allowAddExts.Contains(ext))
                {
                    //处理重复添加
                    if (filePathToListItem.ContainsKey(file.FullName))
                    {
                        continue;
                    }
                    //在列表中添加项
                    var item = new ListViewItem(new string[] {
                        file.Name,
                        file.FullName,
                        //根据扩展名确定后续处理
                        canModifyExts.Contains(ext)?_toModify:_toConvert
                    });
                    FileListView.Items.Add(item);
                    filePathToListItem.Add(file.FullName, item);
                }
            }
        }
        #endregion

        private void btnCleanList_Click(object sender, EventArgs e)
        {
            FileListView.Items.Clear();
            filePathToListItem.Clear();
        }

        private void btnExec_Click(object sender, EventArgs e)
        {
            //检查目标路径是否存在
            if (!Directory.Exists(outputDir))
            {
                MessageBox.Show(this, "输出路径不存在", "错误");
                outputDir = Directory.GetCurrentDirectory();
                File.WriteAllText(outputDirFilePath, outputDir);
                btnSelectOutputFolder_Click(sender, e);
                return;
            }
            //开始处理
            dealImage();
        }

        private void btnSelectOutputFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = outputDir;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    outputDir = dialog.SelectedPath;
                    txtOutputPath.Text = outputDir;
                    File.WriteAllText(outputDirFilePath, outputDir);
                }
            }
        }

        private void txtOutputPath_TextChanged(object sender, EventArgs e)
        {
            outputDir = txtOutputPath.Text;
            if (!Directory.Exists(outputDir))
            {
                MessageBox.Show(this, "输出路径不存在", "错误");
                btnSelectOutputFolder_Click(sender, e);
            }
        }

        private void dealImage()
        {
            DateTime newDateTime = targetTime.Value;
            foreach(ListViewItem item in FileListView.Items)
            {
                try
                {
                    string srcPath = item.SubItems[1].Text;
                    //跳过已处理的
                    if (item.SubItems[2].Text == _complete || item.SubItems[2].Text == _converting)
                    {
                        continue;
                    }
                    else if (item.SubItems[2].Text == _toConvert)
                    {
                        string jpegPath = Path.ChangeExtension(srcPath, ".jpg");
                        item.SubItems[2].Text = _converting;

                        // 使用ImageSharp加载并保存为TIFF
                        using (Image image = Image.Load(srcPath))
                        {
                            // 使用无损压缩的LZW编码保存为TIFF
                            var encoder = new JpegEncoder
                            {
                                Quality = 100
                            };

                            image.Save(jpegPath, encoder);
                        }
                        item.SubItems[2].Text = _toModify;
                        srcPath = jpegPath;
                    }

                    if (item.SubItems[2].Text == _toModify)
                    {
                        // 使用新版ExifLibrary API
                        var exif = ImageFile.FromFile(srcPath);
                        if (exif.Properties.Get(ExifTag.DateTime) !=null && exif.Properties.Get(ExifTag.DateTimeOriginal)!=null)
                        {
                            item.SubItems[2].Text = _complete;
                        }
                        // 设置日期时间属性
                        exif.Properties.Set(ExifTag.DateTimeDigitized, newDateTime.ToString("yyyy:MM:dd HH:mm:ss"));
                        exif.Properties.Set(ExifTag.DateTime, newDateTime.ToString("yyyy:MM:dd HH:mm:ss"));

                        exif.Properties.Set(ExifTag.DateTimeOriginal, newDateTime.ToString("yyyy:MM:dd HH:mm:ss"));
                        // 保存修改
                        var targetPath = Path.Combine(outputDir, Path.GetFileName(srcPath));
                        exif.Save(targetPath);
                        item.SubItems[2].Text = _complete;
                    }
                }
                catch (Exception ex)
                {
                    item.SubItems[2].Text = _error + ex.Message;
                }
            }
        }
    }
}
