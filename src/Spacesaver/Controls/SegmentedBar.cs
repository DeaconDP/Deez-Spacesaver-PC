using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Spacesaver.Controls;

public sealed record BarSegment(double Weight, Windows.UI.Color Color, string Label);

/// <summary>
/// One horizontal bar split into proportional colored segments (one per drive).
/// Each segment carries a tooltip and automation name so color is never the only signal.
/// </summary>
public sealed partial class SegmentedBar : Grid
{
    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
        nameof(Segments),
        typeof(IReadOnlyList<BarSegment>),
        typeof(SegmentedBar),
        new PropertyMetadata(null, OnSegmentsChanged));

    public IReadOnlyList<BarSegment>? Segments
    {
        get => (IReadOnlyList<BarSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    private static void OnSegmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((SegmentedBar)d).Rebuild();

    private void Rebuild()
    {
        Children.Clear();
        ColumnDefinitions.Clear();

        var segments = Segments?.Where(s => s.Weight > 0).ToList();

        if (segments is null || segments.Count == 0)
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var placeholder = new Rectangle
            {
                Fill = (Brush)Application.Current.Resources["AppSurfaceBrush"],
                Stroke = (Brush)Application.Current.Resources["AppBorderBrush"],
                StrokeThickness = 1,
                RadiusX = 1,
                RadiusY = 1
            };
            AutomationProperties.SetName(placeholder, "No size data yet");
            Children.Add(placeholder);
            return;
        }

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(segment.Weight, GridUnitType.Star),
                MinWidth = 4
            });

            var rect = new Rectangle
            {
                Fill = new SolidColorBrush(segment.Color),
                RadiusX = 1,
                RadiusY = 1,
                Margin = new Thickness(i == 0 ? 0 : 1, 0, 0, 0)
            };
            ToolTipService.SetToolTip(rect, segment.Label);
            AutomationProperties.SetName(rect, segment.Label);
            SetColumn(rect, i);
            Children.Add(rect);
        }
    }
}
