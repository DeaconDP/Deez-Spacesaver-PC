using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;
using Spacesaver.ViewModels;

namespace Spacesaver.Pages;

public sealed partial class TaskGuidePage : Page
{
    public TaskGuidePage()
    {
        InitializeComponent();
        AppServices.Progress.ProgressChanged += OnProgressChanged;
        Unloaded += (_, _) => AppServices.Progress.ProgressChanged -= OnProgressChanged;

        TaskListView.ContainerContentChanging += TaskListView_ContainerContentChanging;
        AnimationHelper.WirePageLoaded(this, [PageHeaderControl]);
    }

    private void TaskListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || args.Phase > 0)
            return;

        if (args.ItemContainer is ListViewItem { Content: Microsoft.UI.Xaml.FrameworkElement content })
            AnimationHelper.AttachHoverLift(content, hoverLift: -2f, hoverScale: 1.008f);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is TaskGuideNavigationArgs args && args.InitialCategory is TaskCategory category)
            SelectCategoryTab(category);

        LoadTasksForSelectedTab();
    }

    private void SelectCategoryTab(TaskCategory category)
    {
        foreach (var tab in CategoryTabs.TabItems)
        {
            if (tab is not TabViewItem item)
                continue;

            if (string.Equals(item.Tag as string, category.ToString(), StringComparison.Ordinal))
            {
                CategoryTabs.SelectedItem = item;
                return;
            }
        }
    }

    private void CategoryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        LoadTasksForSelectedTab();

    private void CategoryTabs_AddTabButtonClick(TabView sender, object args) { }

    private void CategoryTabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) { }

    private void OnProgressChanged() => LoadTasksForSelectedTab();

    private TaskCategory? GetSelectedCategory()
    {
        if (CategoryTabs.SelectedItem is not TabViewItem tab)
            return null;

        var tag = tab.Tag as string;
        return tag switch
        {
            "all" or null => null,
            nameof(TaskCategory.QuickWins) => TaskCategory.QuickWins,
            nameof(TaskCategory.SystemCleanup) => TaskCategory.SystemCleanup,
            nameof(TaskCategory.LargeSpaceWins) => TaskCategory.LargeSpaceWins,
            nameof(TaskCategory.ManualReview) => TaskCategory.ManualReview,
            _ => Enum.TryParse<TaskCategory>(tag, out var parsed) ? parsed : null
        };
    }

    private void LoadTasksForSelectedTab()
    {
        var category = GetSelectedCategory();
        var tasks = TaskCatalog.GetByCategory(category);

        PageHeaderControl.Subtitle = category is null
            ? "All 20 cleanup techniques in recommended order."
            : $"{tasks.Count} tasks in this category.";

        if (category is TaskCategory cat)
        {
            var tone = SemanticColorHelper.ForTaskCategory(cat);
            PageHeaderControl.TitleTextBlock.Foreground = SemanticColorHelper.GetForegroundBrush(tone);
        }
        else
        {
            PageHeaderControl.TitleTextBlock.ClearValue(TextBlock.ForegroundProperty);
        }

        TaskListView.ItemsSource = tasks
            .Select(t => new TaskRowViewModel(t, AppServices.Progress.IsCompleted(t.Id)))
            .ToList();
    }

    private void TaskListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is TaskRowViewModel row)
            NavigationHelper.NavigateToTaskDetail(this, row.Task.Id);
    }
}
