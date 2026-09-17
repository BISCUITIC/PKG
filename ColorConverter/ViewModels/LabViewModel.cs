namespace ColorConverter.ViewModels;

public class LabViewModel : ViewModelBase
{
    public ColorComponentViewModel L { get; }

    public ColorComponentViewModel A { get; }

    public ColorComponentViewModel B { get; }


    public event Action? Changed;


    public LabViewModel()
    {
        L = new ColorComponentViewModel(
            "L",
            0,
            0,
            100);


        A = new ColorComponentViewModel(
            "A",
            0,
            -128,
            127);


        B = new ColorComponentViewModel(
            "B",
            0,
            -128,
            127);



        L.Changed += OnComponentChanged;
        A.Changed += OnComponentChanged;
        B.Changed += OnComponentChanged;
    }


    private void OnComponentChanged()
    {
        Changed?.Invoke();
    }
}