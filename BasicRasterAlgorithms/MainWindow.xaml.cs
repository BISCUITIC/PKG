using RasterAlgorithms;
using RasterEngine.Algorithms;
using System.Windows;

namespace BasicRasterAlgorithms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            VirtualGrid grid = new VirtualGrid(101, 101);

            //BresenhamLine.Draw(
            //    grid,
            //    0, 0,
            //    12, 2);

            //StepByStepLine.Draw(
            //    grid,
            //    -10, -5,
            //    10, 5);

            //DdaLine.Draw(
            //    grid,
            //    0, 0,
            //    12, 2);

            BresenhamCircle.Draw(
                grid,
                0,
                0,
                10);

            GridPainter.Source = grid;
            GridPainter.InvalidateVisual();
        }
    }
}