using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Spacesaver.Controls;

public sealed partial class StepIndicator : UserControl
{
    public static readonly DependencyProperty CurrentStepProperty =
        DependencyProperty.Register(nameof(CurrentStep), typeof(int), typeof(StepIndicator),
            new PropertyMetadata(0, OnCurrentStepChanged));

    private readonly TextBlock[] _labels;

    public StepIndicator()
    {
        InitializeComponent();
        _labels = [Step0Label, Step1Label, Step2Label, Step3Label];
        UpdateLabels();
    }

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    private void UpdateLabels()
    {
        for (var i = 0; i < _labels.Length; i++)
        {
            _labels[i].FontWeight = CurrentStep == i ? FontWeights.SemiBold : FontWeights.Normal;
            _labels[i].Foreground = GetStepForeground(i);
        }
    }

    private Brush GetStepForeground(int step)
    {
        if (CurrentStep == step)
            return Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as Brush
                ?? new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);

        if (CurrentStep > step)
            return Application.Current.Resources["SemanticGoodBrush"] as Brush
                ?? new SolidColorBrush(Microsoft.UI.Colors.Green);

        return Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
            ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    private static void OnCurrentStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StepIndicator indicator)
        {
            indicator.UpdateLabels();
            indicator.Bindings.Update();
        }
    }
}
