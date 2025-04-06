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
        private static Random _random = new Random(); // Генератор случайных чисел

        public static string GenerateCaptchaText(int length = 5)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Допустимые символы без похожих (I, O, 1, 0)
            char[] captcha = new char[length];      // Массив для хранения символов капчи
            for (int i = 0; i < length; i++)        // Заполнение массива случайными символами
                captcha[i] = chars[_random.Next(chars.Length)]; // Выбор случайного символа
            return new string(captcha);             // Преобразование массива в строку
        }

        public static BitmapImage GenerateCaptchaImage(string captchaText)
        {
            int width = 150, height = 50;           // Размеры изображения капчи
            DrawingVisual visual = new DrawingVisual(); // Объект для рисования
            using (DrawingContext dc = visual.RenderOpen()) // Открытие контекста рисования
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height)); // Отрисовка белого фона
                Typeface typeface = new Typeface("Arial"); // Шрифт для текста капчи
                FormattedText formattedText = new FormattedText( // Форматированный текст капчи
                    captchaText,                    // Текст для отображения
                    CultureInfo.InvariantCulture,   // Инвариантная культура
                    FlowDirection.LeftToRight,      // Направление текста слева направо
                    typeface,                       // Выбранный шрифт
                    30,                             // Размер шрифта
                    Brushes.Black,                  // Цвет текста
                    1.0);                           // Плотность пикселей
                dc.DrawText(formattedText, new Point(15, 5)); // Отрисовка текста на изображении

                for (int i = 0; i < 20; i++)        // Добавление шума в виде случайных точек
                {
                    double x = _random.Next(width); // Случайная X-координата
                    double y = _random.Next(height);// Случайная Y-координата
                    dc.DrawRectangle(Brushes.Gray, null, new Rect(x, y, 2, 2)); // Отрисовка серой точки
                }
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap( // Создание растрового изображения
                width, height,                      // Размеры изображения
                96, 96,                             // Разрешение DPI
                PixelFormats.Pbgra32);              // Формат пикселей с альфа-каналом
            bitmap.Render(visual);                  // Рендеринг визуального объекта в битмап

            return ConvertBitmapToBitmapImage(bitmap); // Конвертация в BitmapImage
        }

        private static BitmapImage ConvertBitmapToBitmapImage(BitmapSource bitmap)
        {
            using (MemoryStream memory = new MemoryStream()) // Создание потока памяти
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder(); // Кодировщик для PNG формата
                encoder.Frames.Add(BitmapFrame.Create(bitmap)); // Добавление кадра из битмапа
                encoder.Save(memory);               // Сохранение в поток памяти

                BitmapImage bitmapImage = new BitmapImage(); // Создание объекта BitmapImage
                bitmapImage.BeginInit();            // Начало инициализации
                bitmapImage.StreamSource = new MemoryStream(memory.ToArray()); // Установка источника данных
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Загрузка в память сразу
                bitmapImage.EndInit();              // Завершение инициализации
                bitmapImage.Freeze();               // Заморозка для потокобезопасности

                return bitmapImage;                 // Возврат готового изображения
            }
        }
    }
}