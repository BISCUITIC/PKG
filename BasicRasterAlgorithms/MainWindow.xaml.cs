using BasicRasterAlgorithms.Models;
using RasterAlgorithms;
using RasterEngine.Algorithms;
using RasterEngine.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace BasicRasterAlgorithms;

public partial class MainWindow : Window
{
    private const int GRID_SIZE = 101;
    private const int GRID_RADIUS = GRID_SIZE / 2;

    public MainWindow()
    {
        InitializeComponent();

        InitializeAlgorithmSelectors();

        GridPainter.Source = new VirtualGrid(
            GRID_SIZE,
            GRID_SIZE);
    }

    private void InitializeAlgorithmSelectors()
    {
        LineAlgorithmSelector.ItemsSource =
            new AlgorithmOption<ILineRasterizationAlgorithm>[]
            {
                new("Пошаговый алгоритм", new StepByStepLine()),
                new("ЦДА", new DdaLine()),
                new("Брезенхем", new BresenhamLine())
            };

        LineAlgorithmSelector.SelectedIndex = 0;

        CircleAlgorithmSelector.ItemsSource =
            new AlgorithmOption<ICircleRasterizationAlgorithm>[]
            {
                new("Брезенхем", new BresenhamCircle())
            };

        CircleAlgorithmSelector.SelectedIndex = 0;
    }

    private void DrawLine_Click(object sender, RoutedEventArgs e)
    {
        if (LineAlgorithmSelector.SelectedItem is not
            AlgorithmOption<ILineRasterizationAlgorithm> option)
            return;

        if (!TryReadCoordinate(X1TextBox, "X1", out int x1))
            return;

        if (!TryReadCoordinate(Y1TextBox, "Y1", out int y1))
            return;

        if (!TryReadCoordinate(X2TextBox, "X2", out int x2))
            return;

        if (!TryReadCoordinate(Y2TextBox, "Y2", out int y2))
            return;

        Render(grid =>
            option.Algorithm.Draw(grid, x1, y1, x2, y2));
    }

    private void DrawCircle_Click(object sender, RoutedEventArgs e)
    {
        if (CircleAlgorithmSelector.SelectedItem is not
            AlgorithmOption<ICircleRasterizationAlgorithm> option)
            return;

        if (!TryReadCoordinate(
            CenterXTextBox, "X", out int centerX))
            return;

        if (!TryReadCoordinate(
            CenterYTextBox, "Y", out int centerY))
            return;

        if (!TryReadInteger(
            RadiusTextBox, "Радиус", out int radius))
            return;

        if (radius < 0)
        {
            ShowInputError(
                RadiusTextBox,
                "Радиус не может быть отрицательным.");

            return;
        }

        // Проверяем, что окружность полностью помещается
        // в рабочую область.
        if ((long)centerX - radius < -GRID_RADIUS ||
            (long)centerX + radius > GRID_RADIUS ||
            (long)centerY - radius < -GRID_RADIUS ||
            (long)centerY + radius > GRID_RADIUS)
        {
            ShowInputError(
                RadiusTextBox,
                "Окружность должна полностью помещаться в рабочую область.");

            return;
        }

        Render(grid =>
            option.Algorithm.Draw(
                grid,
                centerX,
                centerY,
                radius));
    }

    private static bool TryReadCoordinate(
        TextBox textBox,
        string parameterName,
        out int value)
    {
        if (!TryReadInteger(textBox, parameterName, out value))
            return false;

        if (value < -GRID_RADIUS || value > GRID_RADIUS)
        {
            ShowInputError(
                textBox,
                $"{parameterName} должна находиться в диапазоне " +
                $"{-GRID_RADIUS}..{GRID_RADIUS}.");

            return false;
        }

        return true;
    }

    private static bool TryReadInteger(
        TextBox textBox,
        string parameterName,
        out int value)
    {
        if (!int.TryParse(textBox.Text, out value))
        {
            ShowInputError(
                textBox,
                $"{parameterName} должна быть целым числом.");

            return false;
        }

        return true;
    }

    private static void ShowInputError(
        TextBox textBox,
        string message)
    {
        MessageBox.Show(
            message,
            "Некорректный ввод",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        textBox.Focus();
        textBox.SelectAll();
    }

    private void Render(Action<VirtualGrid> draw)
    {
        VirtualGrid grid = new VirtualGrid(
            GRID_SIZE,
            GRID_SIZE);

        draw(grid);

        GridPainter.Source = grid;
    }
}