using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Spacesaver.Controls;

public sealed partial class ListCard : ContentControl
{
    public static readonly DependencyProperty AccentBrushProperty =
        DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(ListCard), new PropertyMetadata(null));

    public static readonly DependencyProperty TintBrushProperty =
        DependencyProperty.Register(nameof(TintBrush), typeof(Brush), typeof(ListCard), new PropertyMetadata(null));

    public ListCard()
    {
        InitializeComponent();
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
