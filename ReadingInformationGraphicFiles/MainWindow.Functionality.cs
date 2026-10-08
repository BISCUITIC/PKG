using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace ReadingInformationGraphicFiles;

public partial class MainWindow
{
    private const int MaxFiles = 100_000;

    private CancellationTokenSource? _cts;
    private DispatcherTimer? _progressTimer;
    private int _processed;
    private int _errors;

    private void Browse_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выберите папку с изображениями",
            Multiselect = false
        };

        if (Directory.Exists(FolderTextBox.Text))
            dialog.InitialDirectory = FolderTextBox.Text;

        if (dialog.ShowDialog() == true)
            FolderTextBox.Text = dialog.FolderName;
    }

    private async void Start_Click(object? sender, RoutedEventArgs e)
    {
        string folder = FolderTextBox.Text.Trim();
        if (!Directory.Exists(folder))
        {
            MessageBox.Show(
                "Укажите существующую папку.",
                "Проверка",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _cts?.Cancel();
        using var cts = new CancellationTokenSource();
        _cts = cts;
        CancellationToken token = cts.Token;

        SetRunning(true);
        ResultsGrid.ItemsSource = null;
        _processed = 0;
        _errors = 0;
        ProgressBar.IsIndeterminate = true;
        ProgressBar.Value = 0;
        StatusText.Text = "Поиск файлов...";

        string[] files = Array.Empty<string>();
        ImageInfo?[] results = Array.Empty<ImageInfo?>();
        bool limitReached = false;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var enumeration = await EnumerateImageFilesAsync(
                folder,
                RecursiveCheckBox.IsChecked == true,
                token);

            files = enumeration.Files;
            limitReached = enumeration.LimitReached;
            results = new ImageInfo?[files.Length];

            ProgressBar.IsIndeterminate = false;
            ProgressBar.Maximum = Math.Max(files.Length, 1);
            ProgressBar.Value = 0;

            if (files.Length == 0)
            {
                StatusText.Text = "В выбранной папке поддерживаемые изображения не найдены.";
                ResultsGrid.ItemsSource = Array.Empty<ImageInfo>();
                return;
            }

            StatusText.Text = limitReached
                ? $"Найдено не менее {files.Length:N0} поддерживаемых файлов. Анализируются первые {MaxFiles:N0}."
                : $"Найдено поддерживаемых файлов: {files.Length:N0}. Анализ...";

            StartProgressTimer(stopwatch, files.Length);

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 2, 8)
            };

            await Parallel.ForEachAsync(
                Enumerable.Range(0, files.Length),
                parallelOptions,
                (index, currentToken) =>
                {
                    currentToken.ThrowIfCancellationRequested();

                    ImageInfo info;
                    try
                    {
                        info = ImageMetadataReader.Read(files[index]);
                    }
                    catch (Exception ex)
                    {
                        info = CreateErrorInfo(files[index], ex.Message);
                    }

                    results[index] = info;
                    Interlocked.Increment(ref _processed);
                    if (info.IsError)
                        Interlocked.Increment(ref _errors);

                    return ValueTask.CompletedTask;
                });

            StopProgressTimer();
            ShowResults(results);
            stopwatch.Stop();
            ProgressBar.Value = files.Length;
            StatusText.Text = limitReached
                ? $"Готово. Обработано: {_processed:N0}. Ошибок: {_errors:N0}. Показаны первые {MaxFiles:N0} файлов. Время: {stopwatch.Elapsed.TotalSeconds:0.##} с."
                : $"Готово. Обработано: {_processed:N0}. Ошибок: {_errors:N0}. Время: {stopwatch.Elapsed.TotalSeconds:0.##} с.";
        }
        catch (OperationCanceledException)
        {
            StopProgressTimer();
            stopwatch.Stop();
            ShowResults(results);
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Maximum = Math.Max(files.Length, 1);
            ProgressBar.Value = Math.Min(Volatile.Read(ref _processed), ProgressBar.Maximum);
            StatusText.Text = $"Остановлено. Обработано: {_processed:N0}. Ошибок: {_errors:N0}. Время: {stopwatch.Elapsed.TotalSeconds:0.##} с.";
        }
        catch (Exception ex)
        {
            StopProgressTimer();
            stopwatch.Stop();
            ShowResults(results);
            MessageBox.Show(
                ex.Message,
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            StatusText.Text = "Ошибка при сканировании.";
        }
        finally
        {
            StopProgressTimer();
            SetRunning(false);
            if (ReferenceEquals(_cts, cts))
                _cts = null;
        }
    }

    private static Task<(string[] Files, bool LimitReached)> EnumerateImageFilesAsync(
        string folder,
        bool recursive,
        CancellationToken token)
    {
        return Task.Run(() =>
        {
            var files = new List<string>(Math.Min(MaxFiles, 4096));
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = recursive,
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = false,
                AttributesToSkip = FileAttributes.System
            };

            foreach (string file in Directory.EnumerateFiles(folder, "*.*", options))
            {
                token.ThrowIfCancellationRequested();

                if (!ImageMetadataReader.IsSupported(file))
                    continue;

                if (files.Count == MaxFiles)
                    return (files.ToArray(), true);

                files.Add(file);
            }

            return (files.ToArray(), false);
        }, token);
    }

    private static ImageInfo CreateErrorInfo(string path, string error)
    {
        return new ImageInfo
        {
            FileName = Path.GetFileName(path),
            FullPath = path,
            Format = "Ошибка",
            Dimensions = "—",
            Resolution = "—",
            ColorDepth = "—",
            Compression = "—",
            FileSize = TryGetFileSize(path),
            Error = error,
            IsError = true
        };
    }

    private void StartProgressTimer(Stopwatch stopwatch, int total)
    {
        StopProgressTimer();
        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };

        _progressTimer.Tick += (_, _) =>
        {
            int processed = Volatile.Read(ref _processed);
            int errors = Volatile.Read(ref _errors);
            ProgressBar.Value = Math.Min(processed, total);

            double speed = stopwatch.Elapsed.TotalSeconds > 0
                ? processed / stopwatch.Elapsed.TotalSeconds
                : 0;

            StatusText.Text = $"Анализ: {processed:N0}/{total:N0}. Ошибок: {errors:N0}. Скорость: {speed:N0} файлов/с.";
        };

        _progressTimer.Start();
    }

    private void StopProgressTimer()
    {
        _progressTimer?.Stop();
        _progressTimer = null;
    }

    private void ShowResults(IEnumerable<ImageInfo?> results)
    {
        var items = results
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderBy(x => x.FileName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.FullPath, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        ResultsGrid.ItemsSource = items;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    private void SetRunning(bool running)
    {
        StartButton.IsEnabled = !running;
        CancelButton.IsEnabled = running;
        BrowseButton.IsEnabled = !running;
        FolderTextBox.IsEnabled = !running;
        RecursiveCheckBox.IsEnabled = !running;
    }

    private void ResultsGrid_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
    {
        if (ResultsGrid.SelectedItem is not ImageInfo info || string.IsNullOrWhiteSpace(info.FullPath))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{info.FullPath}\"",
                UseShellExecute = true
            });
        }
        catch
        {
            // Дополнительная функция: ошибка открытия расположения не влияет на анализ.
        }
    }

    private static string TryGetFileSize(string path)
    {
        try
        {
            long bytes = new FileInfo(path).Length;
            const double k = 1024;
            return bytes switch
            {
                < 1024 => $"{bytes} Б",
                < 1024 * 1024 => $"{bytes / k:0.##} КБ",
                < 1024L * 1024 * 1024 => $"{bytes / (k * k):0.##} МБ",
                _ => $"{bytes / (k * k * k):0.##} ГБ"
            };
        }
        catch
        {
            return "—";
        }
    }
}
