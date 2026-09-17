using ColorConverter.ViewModels;
using Core.Services;
using System.Windows;

namespace ColorConverter;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel(new RgbConverter(), new XyzConverter(), new LabConverter());
    }
}