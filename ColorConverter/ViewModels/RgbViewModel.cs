using ColorConverter.ViewModels;

public class RgbViewModel : ViewModelBase
{
    public ColorComponentViewModel R { get; }
    public ColorComponentViewModel G { get; }
    public ColorComponentViewModel B { get; }


    public event Action? Changed;


    public RgbViewModel()
    {
        R = new ColorComponentViewModel(
            "R",
            120,
            0,
            255);


        G = new ColorComponentViewModel(
            "G",
            80,
            0,
            255);


        B = new ColorComponentViewModel(
            "B",
            200,
            0,
            255);



        R.Changed += OnComponentChanged;
        G.Changed += OnComponentChanged;
        B.Changed += OnComponentChanged;
    }


    private void OnComponentChanged()
    {
        Changed?.Invoke();
    }
}