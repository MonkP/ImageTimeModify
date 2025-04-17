using System;
using System.IO;
using System.Linq;
using ExifLibrary;

namespace ImageDateTimeModifier
{
    class Program
    {
        static void Main(string[] args)
        {
            // 设置要修改的目标日期时间（默认为2000-01-01 00:00:00）
            DateTime targetDateTime = new DateTime(1992, 1, 1, 0, 0, 0);

            Console.WriteLine($"将修改所有图片的拍摄时间为: {targetDateTime}");
            Console.WriteLine("警告：此操作不可逆！请确认要继续 (Y/N)?");
            var key = Console.ReadKey();
            if (key.Key != ConsoleKey.Y)
            {
                Console.WriteLine("\n操作已取消");
                return;
            }

            Console.WriteLine("\n开始处理...");

            // 支持的图片扩展名
            var imageExtensions = new[] {".jpg", ".jpeg", ".tiff" }; // PNG/BMP/GIF通常不支持EXIF

            try
            {
                // 获取当前目录
                string rootDirectory = Directory.GetCurrentDirectory();

                // 递归获取所有图片文件
                var imageFiles = Directory.GetFiles(rootDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                if (!imageFiles.Any())
                {
                    Console.WriteLine("没有找到任何支持的图片文件");
                    return;
                }

                Console.WriteLine($"找到 {imageFiles.Count} 个图片文件");

                int successCount = 0;
                int failCount = 0;
                int totalFiles = imageFiles.Count;
                int currentFile = 0;

                foreach (var imagePath in imageFiles)
                {
                    currentFile++;
                    try
                    {
                        Console.WriteLine($"[{currentFile}/{totalFiles}] 处理: {GetRelativePath(rootDirectory, imagePath)}");

                        // 修改图片日期时间
                        ModifyImageDateTime(imagePath, targetDateTime);

                        Console.WriteLine("  修改成功");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  处理失败: {ex.Message}");
                        failCount++;
                    }
                }

                Console.WriteLine($"\n处理完成!");
                Console.WriteLine($"总文件数: {totalFiles}");
                Console.WriteLine($"成功: {successCount}");
                Console.WriteLine($"失败: {failCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        static void ModifyImageDateTime(string imagePath, DateTime newDateTime)
        {
            // 使用新版ExifLibrary API
            var exif = ImageFile.FromFile(imagePath);
            // 设置日期时间属性
           // exif.Properties.Set(ExifTag.DateTimeDigitized, newDateTime.ToString("yyyy:MM:dd HH:mm:ss"));
            exif.Properties.Set(ExifTag.DateTime, newDateTime.ToString("yyyy:MM:dd HH:mm:ss"));

            exif.Properties.Set(ExifTag.DateTimeOriginal, newDateTime.ToString("yyyy:MM:dd HH:mm:ss"));
            // 保存修改
            exif.Save(imagePath);
        }

        static string GetRelativePath(string rootPath, string fullPath)
        {
            // 获取相对于根目录的相对路径
            if (fullPath.StartsWith(rootPath))
            {
                return fullPath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            return fullPath;
        }
    }
}