using System;
using System.Windows.Media.Imaging;

namespace SmartKithen
{
    /// <summary>
    /// Вспомогательный класс для загрузки изображений рецептов с fallback на placeholder
    /// </summary>
    public static class ImageLoader
    {
        private const string PlaceholderFileName = "placeholder.jpg";

        /// <summary>
        /// Загружает изображение рецепта с fallback на placeholder если путь пуст или файл не найден
        /// </summary>
        /// <param name="imagePath">Путь к изображению из БД (может быть null или пусто)</param>
        /// <returns>BitmapImage с загруженным изображением или placeholder</returns>
        public static BitmapImage LoadRecipeImage(string imagePath)
        {
            // Если путь указан, пытаемся загрузить изображение
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                var image = TryLoadImage(imagePath);
                if (image != null)
                    return image;
            }

            // Fallback на placeholder
            return LoadPlaceholder();
        }

        /// <summary>
        /// Пытается загрузить изображение по заданному пути
        /// </summary>
        private static BitmapImage TryLoadImage(string imagePath)
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var relativePath = imagePath.TrimStart('/').Replace('/', '\\');
                var fullPath = System.IO.Path.Combine(appDir, relativePath);

                // Попытка загрузки файла с диска (если изображения скопированы в выходную папку)
                if (System.IO.File.Exists(fullPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }

                // Если изображения включены как ресурсы в проект (Build Action = Resource), используем pack URI
                try
                {
                    // Приводим путь к виду Images/xxx.jpg
                    var packRelative = relativePath.Replace('\\', '/');
                    if (!packRelative.StartsWith("Images/", StringComparison.OrdinalIgnoreCase) && 
                        !packRelative.StartsWith("/Images/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Попробуем добавить папку Images если её нет
                        packRelative = "Images/" + packRelative.TrimStart('/');
                    }

                    var packUri = new Uri($"pack://application:,,,/{packRelative}", UriKind.Absolute);
                    var bitmap = new BitmapImage(packUri);
                    bitmap.Freeze();
                    return bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Не удалось загрузить изображение по pack URI: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке изображения {imagePath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Загружает изображение-заглушку
        /// </summary>
        private static BitmapImage LoadPlaceholder()
        {
            try
            {
                // Сначала пытаемся файл на диске
                var placeholderPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Images", PlaceholderFileName);

                if (System.IO.File.Exists(placeholderPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(placeholderPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }

                // Попытка загрузить placeholder как ресурс
                try
                {
                    var packPlaceholder = new Uri($"pack://application:,,,/Images/{PlaceholderFileName}", UriKind.Absolute);
                    var bitmap = new BitmapImage(packPlaceholder);
                    bitmap.Freeze();
                    return bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Не удалось загрузить placeholder: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке placeholder: {ex.Message}");
            }

            // Если всё не получилось, вернём пустое изображение
            return new BitmapImage();
        }
    }
}
