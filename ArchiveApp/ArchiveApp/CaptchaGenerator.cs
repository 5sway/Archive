using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Globalization;

namespace ArchiveApp
{
    public static class CaptchaGenerator
    {
        private static Random _random = new Random();

        public static string GenerateCaptchaText(int length = 5)
        {
            // Генерация текста CAPTCHA заданной длины
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Используемые символы (исключены похожие символы)
            char[] captcha = new char[length];
            for (int i = 0; i < length; i++)
            {
                // Случайный выбор символа из доступных
                captcha[i] = chars[_random.Next(chars.Length)];
            }
            return new string(captcha);
        }

        public static BitmapImage GenerateCaptchaImage(string captchaText)
        {
            // Создание изображения CAPTCHA с заданным текстом
            int width = 150, height = 50;
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                // Рисуем белый фон
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                // Настройки для текста CAPTCHA
                Typeface typeface = new Typeface("Arial");
                FormattedText formattedText = new FormattedText(
                    captchaText,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    30, // Размер шрифта
                    Brushes.Black, // Цвет текста
                    1.0); // Плотность пикселей

                // Рисуем текст
                dc.DrawText(formattedText, new Point(15, 5));

                // Добавляем шум (точки)
                for (int i = 0; i < 20; i++)
                {
                    double x = _random.Next(width);
                    double y = _random.Next(height);
                    dc.DrawRectangle(Brushes.Gray, null, new Rect(x, y, 2, 2));
                }
            }

            // Создаем растровое изображение
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                width, height, // Размеры
                96, 96, // DPI
                PixelFormats.Pbgra32); // Формат пикселей
            bitmap.Render(visual);

            // Конвертируем в BitmapImage
            return ConvertBitmapToBitmapImage(bitmap);
        }

        private static BitmapImage ConvertBitmapToBitmapImage(BitmapSource bitmap)
        {
            // Конвертация BitmapSource в BitmapImage
            using (MemoryStream memory = new MemoryStream())
            {
                // Используем PNG кодировщик
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(memory);

                // Создаем и настраиваем BitmapImage
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(memory.ToArray());
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Загрузка сразу
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Делаем неизменяемым для потокобезопасности

                return bitmapImage;
            }
        }
    }
}
