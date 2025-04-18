using System;
using System.IO;
using System.Linq;
using ExifLibrary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Tiff.Constants;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageDateTimeModifier
{
    class Program
    {
        static void Main(string[] args)
        {
            #region 将png转为tif
            string rootDirectory;
            // 设置要修改的目标日期时间（默认为2000-01-01 00:00:00）
            DateTime targetDateTime = new DateTime(2000, 1, 1, 0, 0, 0);
            int dateArgIndex = 0;
            //首先尝试从参数中获取要处理的目录，此时第二个参数为修改后的图片日期
            if (args.Length > 0 && Directory.Exists(args[0]))
            {
                rootDirectory = args[0];
                dateArgIndex = 1;
            }
            else
            {
                // 获取当前目录
                rootDirectory = Directory.GetCurrentDirectory();
            }
            if(dateArgIndex < args.Length)
            {
                DateTime.TryParse(args[dateArgIndex], out targetDateTime);
            }
            try
            {
                // 支持的图片扩展名
                var toConvertExtensions = new[] { ".png", ".tiff", ".tif" }; // PNG/BMP/GIF通常不支持EXIF
                                                                                      // 递归获取所有图片文件
                var toConvertFiles = Directory.GetFiles(rootDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(f => toConvertExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .ToList();

                if (toConvertFiles.Count == 0)
                {
                    Console.WriteLine("未找到任何待转换文件");
                }

                int successCount = 0;
                int failCount = 0;

                foreach (string toConvert in toConvertFiles)
                {
                    try
                    {
                        // 生成对应的jpeg路径（相同目录，相同文件名，不同扩展名）
                        string jpegPath = Path.ChangeExtension(toConvert, ".jpg");

                        Console.WriteLine($"正在转换: {Path.GetFileName(toConvert)}");

                        // 使用ImageSharp加载并保存为TIFF
                        using (Image image = Image.Load(toConvert))
                        {
                            // 使用无损压缩的LZW编码保存为TIFF
                            var encoder = new JpegEncoder
                            {
                                Quality = 100
                            };

                            image.Save(jpegPath, encoder);
                        }

                        Console.WriteLine($"已保存jpg: {Path.GetFileName(jpegPath)}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"转换失败({Path.GetFileName(toConvert)}): {ex.Message}");
                        failCount++;
                    }
                }

                Console.WriteLine($"\n转换完成！成功: {successCount}, 失败: {failCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"png、tif转换发生全局错误: {ex.Message}");
            }
            #endregion

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
            var imageExtensions = new[] {".jpg", ".jpeg"}; // PNG/BMP/GIF通常不支持EXIF

            try
            {

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
            exif.Properties.Set(ExifTag.DateTimeDigitized, newDateTime.ToString("yyyy:MM:dd HH:mm:ss"));
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