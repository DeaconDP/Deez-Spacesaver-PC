using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Spacesaver.Helpers;

namespace Spacesaver.Controls;

public sealed partial class StepRow : UserControl
{
    public static readonly DependencyProperty StepNumberProperty =
        DependencyProperty.Register(nameof(StepNumber), typeof(int), typeof(StepRow), new PropertyMetadata(1));

    public static readonly DependencyProperty StepTextProperty =
        DependencyProperty.Register(nameof(StepText), typeof(string), typeof(StepRow), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ToneProperty =
        DependencyProperty.Register(nameof(Tone), typeof(SemanticTone), typeof(StepRow),
            new PropertyMetadata(SemanticTone.Informative, OnToneChanged));

    public StepRow()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyTone(Tone);
    }

    public int StepNumber
    {
        get => (int)GetValue(StepNumberProperty);
        set => SetValue(StepNumberProperty, value);
    }

    public string StepText
    {
        get => (string)GetValue(StepTextProperty);
        set => SetValue(StepTextProperty, value);
    }

    public SemanticTone Tone
    {
        get => (SemanticTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    private static void OnToneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StepRow row && e.NewValue is SemanticTone tone)
            row.ApplyTone(tone);
    }

    private void ApplyTone(SemanticTone tone)
    {
        NumberBadge.Background = SemanticColorHelper.GetTintBrush(tone);
        NumberBadge.BorderBrush = SemanticColorHelper.GetAccentBrush(tone);
        NumberText.Foreground = SemanticColorHelper.GetForegroundBrush(tone);
    }
}
