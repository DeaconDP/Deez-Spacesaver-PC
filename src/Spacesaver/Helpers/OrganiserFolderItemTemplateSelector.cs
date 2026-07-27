using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.ViewModels;

namespace Spacesaver.Helpers;

public sealed class OrganiserFolderItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? HeaderTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is OrganiserFolderSectionHeader ? HeaderTemplate : FolderTemplate;

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}
