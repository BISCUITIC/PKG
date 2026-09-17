using System.Windows.Media;
using Core.Interfaces;
using Core.Models.Colors;

namespace ColorConverter.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IRgbConverter _rgbConverter;
    private readonly IXyzConverter _xyzConverter;
    private readonly ILabConverter _labConverter;


    public RgbViewModel RGB { get; }
    public XyzViewModel XYZ { get; }
    public LabViewModel LAB { get; }


    private bool _isUpdating;


    private SolidColorBrush _previewBrush = Brushes.Black;

    public SolidColorBrush PreviewBrush
    {
        get => _previewBrush;
        private set
        {
            _previewBrush = value;
            OnPropertyChanged();
        }
    }


    private string _warningMessage = "";

    public string WarningMessage
    {
        get => _warningMessage;
        private set
        {
            _warningMessage = value;
            OnPropertyChanged();
        }
    }



    public MainViewModel(
        IRgbConverter rgbConverter,
        IXyzConverter xyzConverter,
        ILabConverter labConverter)
    {
        _rgbConverter = rgbConverter;
        _xyzConverter = xyzConverter;
        _labConverter = labConverter;


        RGB = new RgbViewModel();
        XYZ = new XyzViewModel();
        LAB = new LabViewModel();


        RGB.Changed += RgbChanged;
        XYZ.Changed += XyzChanged;
        LAB.Changed += LabChanged;


        SetRgb(new RgbColor(120, 80, 200));
    }



    private void RgbChanged()
    {
        if (_isUpdating)
            return;


        var rgb = new RgbColor(
            RGB.R.Value,
            RGB.G.Value,
            RGB.B.Value);


        SetRgb(rgb);
    }



    private void XyzChanged()
    {
        if (_isUpdating)
            return;


        var xyz = new XyzColor(
            XYZ.X.Value,
            XYZ.Y.Value,
            XYZ.Z.Value);


        SetXyz(xyz);
    }



    private void LabChanged()
    {
        if (_isUpdating)
            return;


        var lab = new LabColor(
            LAB.L.Value,
            LAB.A.Value,
            LAB.B.Value);


        SetLab(lab);
    }



    private void SetRgb(RgbColor rgb)
    {
        var xyz = _rgbConverter.ToXyz(rgb);

        var lab = _rgbConverter.ToLab(rgb);


        UpdateAll(
            rgb,
            xyz,
            lab,
            false);
    }



    private void SetXyz(XyzColor xyz)
    {
        var rgb = _xyzConverter.ToRgb(xyz);

        var lab = _xyzConverter.ToLab(xyz);


        UpdateAll(
            rgb.Value,
            xyz,
            lab,
            rgb.WasClipped);
    }



    private void SetLab(LabColor lab)
    {
        var rgb = _labConverter.ToRgb(lab);

        var xyz = _labConverter.ToXyz(lab);


        UpdateAll(
            rgb.Value,
            xyz,
            lab,
            rgb.WasClipped);
    }



    private void UpdateAll(
        RgbColor rgb,
        XyzColor xyz,
        LabColor lab,
        bool clipped)
    {
        _isUpdating = true;

        try
        {
            RGB.R.Value = rgb.R;
            RGB.G.Value = rgb.G;
            RGB.B.Value = rgb.B;


            XYZ.X.Value = xyz.X;
            XYZ.Y.Value = xyz.Y;
            XYZ.Z.Value = xyz.Z;


            LAB.L.Value = lab.L;
            LAB.A.Value = lab.A;
            LAB.B.Value = lab.B;


            PreviewBrush = CreateBrush(rgb);


            WarningMessage = clipped
                ? "Цвет выходит за пределы RGB и был обрезан."
                : "";
        }
        finally
        {
            _isUpdating = false;
        }
    }



    private static SolidColorBrush CreateBrush(RgbColor rgb)
    {
        var color = Color.FromRgb(
            (byte)Math.Clamp(Math.Round(rgb.R), 0, 255),
            (byte)Math.Clamp(Math.Round(rgb.G), 0, 255),
            (byte)Math.Clamp(Math.Round(rgb.B), 0, 255));


        var brush = new SolidColorBrush(color);

        brush.Freeze();

        return brush;
    }
}