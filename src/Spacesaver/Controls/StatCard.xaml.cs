using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Spacesaver.Controls;

public sealed partial class StatCard : ContentControl
{
    public static readonly DependencyProperty AccentBrushProperty =
        DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(StatCard),
            new PropertyMetadata(null, OnAccentChanged));

    private Border? _cardBorder;

    public StatCard()
    {
        InitializeComponent();
    }

    public Brush? AccentBrush
    {
        get => (Brush?)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Border? CardBorderElement => _cardBorder;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _cardBorder = GetTemplateChild("CardBorder") as Border;
        ApplyAccentBrush();
    }

    private static void OnAccentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatCard card)
            card.ApplyAccentBrush();
    }

    private void ApplyAccentBrush()
    {
        if (_cardBorder is null || AccentBrush is not Brush brush)
            return;

        _cardBorder.BorderBrush = brush;
        _cardBorder.BorderThickness = new Thickness(1);
    }
}
