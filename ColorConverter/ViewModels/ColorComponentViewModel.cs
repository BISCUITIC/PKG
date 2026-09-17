namespace ColorConverter.ViewModels;

public class ColorComponentViewModel : ViewModelBase
{
    private double _value;


    public string Name { get; }

    public double Minimum { get; }

    public double Maximum { get; }


    public event Action? Changed;


    public double Value
    {
        get => _value;

        set
        {
            if (_value == value)
                return;


            _value = value;


            OnPropertyChanged();


            Changed?.Invoke();
        }
    }


    public ColorComponentViewModel(
        string name,
        double value,
        double min,
        double max)
    {
        Name = name;

        _value = value;

        Minimum = min;

        Maximum = max;
    }
}