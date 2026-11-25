using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1.Helpers
{
    /// <summary>
    /// Фотографии лежат в папке Images рядом с приложением, в БД хранится только имя файла.
    /// Если файла нет — показываем заглушку no_image.png.
    /// </summary>
    public static class ImageLoader
    {
        public const string Placeholder = "no_image.png";
        private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static string ImagesDir => Path.Combine(AppContext.BaseDirectory, "Images");

        public static ImageSource? Load(string? fileName)
        {
            // Path.GetFileName отбрасывает любые папки из значения в БД (защита от "..\..\file")
            string name = string.IsNullOrWhiteSpace(fileName) ? "" : Path.GetFileName(fileName.Trim());
            string path = Path.Combine(ImagesDir, name);
            if (name.Length == 0 || !File.Exists(path))
                path = Path.Combine(ImagesDir, Placeholder);

            if (Cache.TryGetValue(path, out var cached)) return cached;

            ImageSource? result = null;
            try
            {
                if (File.Exists(path))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad; // не держим файл открытым
                    bmp.DecodePixelWidth = 500;
                    bmp.UriSource = new Uri(path, UriKind.Absolute);
                    bmp.EndInit();
                    bmp.Freeze();
                    result = bmp;
                }
            }
            catch
            {
                result = null; // битый файл — просто без картинки
            }
            Cache[path] = result;
            return result;
        }

        /// <summary>Сбросить кэш (после замены файла администратором).</summary>
        public static void Invalidate() => Cache.Clear();

        /// <summary>
        /// Копирует выбранную администратором фотографию в папку Images (рядом с .exe и, если найдётся,
        /// в папку проекта), чтобы она «хранилась прямо в проекте». Возвращает имя файла для БД.
        /// </summary>
        public static string Import(string sourcePath)
        {
            Directory.CreateDirectory(ImagesDir);
            string fileName = Path.GetFileName(sourcePath);
            string target = Path.Combine(ImagesDir, fileName);

            bool alreadyThere = string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(target),
                StringComparison.OrdinalIgnoreCase);

            if (!alreadyThere)
            {
                if (File.Exists(target))
                {
                    // Файл с таким именем уже есть — не затираем чужую фотографию
                    fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";
                    target = Path.Combine(ImagesDir, fileName);
                }
                File.Copy(sourcePath, target);
            }

            // Дополнительно кладём копию в папку проекта (при запуске из Visual Studio), чтобы фото не пропало при пересборке
            try
            {
                var projectImages = FindProjectImagesDir();
                if (projectImages != null)
                {
                    Directory.CreateDirectory(projectImages);
                    string projectFile = Path.Combine(projectImages, fileName);
                    if (!File.Exists(projectFile)) File.Copy(target, projectFile);
                }
            }
            catch
            {
                // не критично: главное, что файл есть рядом с приложением
            }

            Invalidate();
            return fileName;
        }

        private static string? FindProjectImagesDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (dir.GetFiles("*.csproj").Length > 0) return Path.Combine(dir.FullName, "Images");
                dir = dir.Parent;
            }
            return null;
        }
    }
}
