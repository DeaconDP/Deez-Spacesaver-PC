using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Helpers;
using Spacesaver.Models;

namespace Spacesaver.Controls;

public sealed partial class CategoryTile : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(CategoryTile), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(nameof(Caption), typeof(string), typeof(CategoryTile), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(CategoryTile),
            new PropertyMetadata(string.Empty, OnVisualChanged));

    public static readonly DependencyProperty CategoryProperty =
        DependencyProperty.Register(nameof(Category), typeof(TaskCategory), typeof(CategoryTile),
            new PropertyMetadata(TaskCategory.QuickWins, OnVisualChanged));

    public event RoutedEventHandler? Click;

    public CategoryTile()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyVisuals();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public TaskCategory Category
    {
        get => (TaskCategory)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public Button TileButtonElement => TileButton;

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CategoryTile tile)
            tile.ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        IconElement.Glyph = IconGlyph;
        SemanticColorHelper.ApplyCategoryTile(TileButton, Category);
    }

    private void TileButton_Click(object sender, RoutedEventArgs e) =>
        Click?.Invoke(this, e);
}
