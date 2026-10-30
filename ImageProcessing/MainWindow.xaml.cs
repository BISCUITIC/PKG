using ImageProcessing.Models;
using ImageProcessing.Services;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ImageProcessing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _currentImagePath;
        private GrayImage _currentImage;        
        private GrayImage _resultImage;        

        public MainWindow()
        {
            InitializeComponent();
        }

        private OpenFileDialog CreateOpenFileDialog()
        {
            return new OpenFileDialog
            {
                Title = "Выберите изображение",
                Filter = "Файлы изображений|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif|Все файлы|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
        }

        private BitmapImage CreateBitmapImage(OpenFileDialog dialog)
        {
            BitmapImage bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.UriSource = new Uri(dialog.FileName, UriKind.Absolute);
            bitmap.EndInit();

            bitmap.Freeze();

            return bitmap;
        }

        private void ShowLoadedImage(BitmapImage bitmap)
        {
            OriginalImage.Source = bitmap;
            ResultImage.Source = null;

            OriginalPlaceholder.Visibility = Visibility.Collapsed;
            ResultPlaceholder.Visibility = Visibility.Visible;

            FileInfoText.Text = $"{Path.GetFileName(bitmap.UriSource.LocalPath)}\n{_currentImage.Width:N0} × {_currentImage.Height:N0} пикселей";

            ApplyButton.IsEnabled = true;
            SaveButton.IsEnabled = false;

            StatusText.Text = $"Загружено изображение: {_currentImage.Width} × {_currentImage.Height}. Выберите метод обработки.";
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = CreateOpenFileDialog();

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                BitmapImage bitmap = CreateBitmapImage(dialog);

                _currentImage = GrayImage.FromBitmapSource(bitmap);
                _currentImagePath = bitmap.UriSource.LocalPath;    

                ShowLoadedImage(bitmap);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Не удалось открыть изображение. Возможно, формат не поддерживается или файл повреждён.\n\n{ex.Message}",
                    "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ParameterBox is null || ParameterLabel is null || MethodDescription is null || WindowSizeBox is null)
                return;

            if (MethodComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
                return;

            if (!MethodMetafataMap.Map.TryGetValue(tag, out var meta))
                return;
            
            WindowSizeBox.IsEnabled = meta.UsesWindow;
            ParameterLabel.Text = meta.ParameterLabel;
            ParameterBox.Text = meta.DefaultParameter;
            MethodDescription.Text = meta.Description;
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImage is null)
            {
                MessageBox.Show(this, "Сначала откройте изображение.", "Нет изображения", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MethodComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string tag ||
                !ImageProcessingFactory.TryGetStrategyFromTag(tag, out var strategy) || strategy is null)
            {
                MessageBox.Show(this, "Выберите корректный метод обработки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(WindowSizeBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int requestedWindow))
            {
                MessageBox.Show(this, "Размер окна должен быть целым числом.", "Некорректный размер окна", MessageBoxButton.OK, MessageBoxImage.Warning);
                WindowSizeBox.Focus();
                return;
            }
            
            if (!TryParseDouble(ParameterBox.Text, out double parameter) || !double.IsFinite(parameter))
            {
                MessageBox.Show(this, "Введите корректное числовое значение параметра. Десятичный разделитель: точка или запятая.",
                    "Некорректный параметр", MessageBoxButton.OK, MessageBoxImage.Warning);
                ParameterBox.Focus();
                return;
            }

            int windowSize = ImageProcessor.NormalizeWindowSize(requestedWindow);
            WindowSizeBox.Text = windowSize.ToString(CultureInfo.InvariantCulture); // Обновляем текст, если размер скорректировался

            SetUIEnabled(false);
            StatusText.Text = "Выполняется обработка…";

            try
            {                
                GrayImage resultImage = await Task.Run(() => strategy.Process(_currentImage, windowSize, parameter));

                _resultImage = resultImage;
                ResultImage.Source = resultImage.ToBitmapSource();
                ResultPlaceholder.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = true;

                StatusText.Text = "Готово!";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Во время обработки произошла ошибка.\n\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка обработки.";
            }
            finally
            {
                SetUIEnabled(true);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем наличие результата (теперь это GrayImage _resultImage)
            if (_resultImage is null)
                return;

            var dialog = new SaveFileDialog
            {
                Title = "Сохранить результат обработки",
                Filter = "Изображение PNG (*.png)|*.png",
                DefaultExt = ".png",
                AddExtension = true,
                FileName = Path.GetFileNameWithoutExtension(_currentImagePath) + "_result.png",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                var encoder = new PngBitmapEncoder();

                // Используем метод из нашего класса GrayImage вместо ручного вызова CreateGrayBitmap
                encoder.Frames.Add(BitmapFrame.Create(_resultImage.ToBitmapSource()));

                using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write);
                encoder.Save(stream);

                StatusText.Text = $"Результат сохранён: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Не удалось сохранить файл.\n\n{ex.Message}", "Ошибка сохранения",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SetUIEnabled(bool isEnabled)
        {
            OpenButton.IsEnabled = isEnabled;
            ApplyButton.IsEnabled = isEnabled && _currentImage is not null;
        }

        private static bool TryParseDouble(string text, out double value)
        {
            string normalized = text.Trim().Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

    }
}