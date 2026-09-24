using ColorConverter.ViewModels;
using Core.Services;
using Core.Models.Colors;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace ColorConverter;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private int[] _customColors = [];

    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel(new RgbConverter(), new XyzConverter(), new LabConverter());
    }

    private void PickColor_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        var currentColor = viewModel.PreviewBrush.Color;

        using var dialog = new Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
            Color = System.Drawing.Color.FromArgb(
                currentColor.R, currentColor.G, currentColor.B),
            CustomColors = _customColors
        };

        var owner = new DialogOwner(new WindowInteropHelper(this).Handle);
        var result = dialog.ShowDialog(owner);
        _customColors = dialog.CustomColors;

        if (result != Forms.DialogResult.OK)
            return;

        var selectedColor = dialog.Color;
        viewModel.SetRgb(new RgbColor(
            selectedColor.R, selectedColor.G, selectedColor.B));
    }

    private sealed class DialogOwner : Forms.IWin32Window
    {
        public IntPtr Handle { get; }

        public DialogOwner(IntPtr handle)
        {
            Handle = handle;
        }
    }
}