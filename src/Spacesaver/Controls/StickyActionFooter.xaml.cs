using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Spacesaver.Controls;

public sealed partial class StickyActionFooter : UserControl
{
    public static readonly DependencyProperty HasSelectionProperty =
        DependencyProperty.Register(nameof(HasSelection), typeof(bool), typeof(StickyActionFooter),
            new PropertyMetadata(false, OnHasSelectionChanged));

    public StickyActionFooter()
    {
        InitializeComponent();
    }

    public Border FooterBorderElement => FooterBorder;

    public TextBlock SummaryTextBlock => SummaryText;

    public TextBlock StatusTextBlock => StatusText;

    public Button PrimaryActionButton => PrimaryButton;

    public bool HasSelection
    {
        get => (bool)GetValue(HasSelectionProperty);
        set => SetValue(HasSelectionProperty, value);
    }

    private static void OnHasSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not StickyActionFooter footer)
            return;

        var accent = Application.Current.Resources["SemanticInformativeBrush"] as Brush;
        footer.FooterBorder.BorderBrush = (bool)e.NewValue
            ? accent ?? footer.FooterBorder.BorderBrush
            : Application.Current.Resources["CardStrokeColorDefaultBrush"] as Brush;
    }
}
