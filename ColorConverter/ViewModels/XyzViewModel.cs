namespace ColorConverter.ViewModels;

public class XyzViewModel : ViewModelBase
{
    public ColorComponentViewModel X { get; }

    public ColorComponentViewModel Y { get; }

    public ColorComponentViewModel Z { get; }


    public event Action? Changed;


    public XyzViewModel()
    {
        X = new ColorComponentViewModel(
            "X",
            0,
            0,
            100);


        Y = new ColorComponentViewModel(
            "Y",
            0,
            0,
            100);


        Z = new ColorComponentViewModel(
            "Z",
            0,
            0,
            100);



        X.Changed += OnComponentChanged;
        Y.Changed += OnComponentChanged;
        Z.Changed += OnComponentChanged;
    }


    private void OnComponentChanged()
    {
        Changed?.Invoke();
    }
}