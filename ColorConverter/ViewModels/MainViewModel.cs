using System.Windows.Media;
using System.Collections.ObjectModel;
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

    public ObservableCollection<string> Warnings { get; } = new();

    private void ValidateColors(
        RgbColor rgb,
        XyzColor xyz,
        LabColor lab)
    {
        Warnings.Clear();

        if (rgb.R < 0 || rgb.R > 255 ||
            rgb.G < 0 || rgb.G > 255 ||
            rgb.B < 0 || rgb.B > 255)
        {
            Warnings.Add("⚠ RGB: значения должны находиться в диапазоне 0..255");
        }

        if (xyz.X < 0 || xyz.Y < 0 || xyz.Z < 0)
        {
            Warnings.Add("⚠ XYZ: координаты не могут быть отрицательными");
        }

        if (lab.L < 0 || lab.L > 100)
        {
            Warnings.Add("⚠ LAB: значение L должно быть в диапазоне 0..100");
        }

        if (lab.A < -128 || lab.A > 127 ||
            lab.B < -128 || lab.B > 127)
        {
            Warnings.Add("⚠ LAB: компоненты A и B выходят за рабочий диапазон");
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

    public void SetRgb(RgbColor rgb)
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


            ValidateColors(rgb, xyz, lab);

            if (clipped)
            {
                Warnings.Add("⚠ RGB: цвет выходит за пределы sRGB и был обрезан");
            }
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