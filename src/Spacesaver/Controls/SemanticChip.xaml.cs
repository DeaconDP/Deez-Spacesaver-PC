using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Spacesaver.Controls;

public sealed partial class SemanticChip : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SemanticChip), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AccentBrushProperty =
        DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(SemanticChip), new PropertyMetadata(null));

    public static readonly DependencyProperty TintBrushProperty =
        DependencyProperty.Register(nameof(TintBrush), typeof(Brush), typeof(SemanticChip), new PropertyMetadata(null));

    public SemanticChip()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public Brush? AccentBrush
    {
        get => (Brush?)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush? TintBrush
    {
        get => (Brush?)GetValue(TintBrushProperty);
        set => SetValue(TintBrushProperty, value);
    }
}
