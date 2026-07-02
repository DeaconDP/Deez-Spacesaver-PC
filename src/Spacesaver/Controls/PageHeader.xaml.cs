using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Spacesaver.Controls;

public sealed partial class PageHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(PageHeader), new PropertyMetadata(string.Empty, OnSubtitleChanged));

    public static readonly DependencyProperty ShowProgressBarProperty =
        DependencyProperty.Register(nameof(ShowProgressBar), typeof(bool), typeof(PageHeader), new PropertyMetadata(false, OnProgressChanged));

    public static readonly DependencyProperty ProgressValueProperty =
        DependencyProperty.Register(nameof(ProgressValue), typeof(double), typeof(PageHeader), new PropertyMetadata(0d));

    public PageHeader()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool ShowProgressBar
    {
        get => (bool)GetValue(ShowProgressBarProperty);
        set => SetValue(ShowProgressBarProperty, value);
    }

    public double ProgressValue
    {
        get => (double)GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }

    public ProgressBar UsageProgressBar => UsageBar;

    public TextBlock TitleTextBlock => TitleBlock;

    public TextBlock SubtitleTextBlock => SubtitleBlock;

    public Visibility SubtitleVisibility =>
        string.IsNullOrWhiteSpace(Subtitle) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility ProgressVisibility =>
        ShowProgressBar ? Visibility.Visible : Visibility.Collapsed;

    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PageHeader)d).Bindings.Update();

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PageHeader)d).Bindings.Update();
}
