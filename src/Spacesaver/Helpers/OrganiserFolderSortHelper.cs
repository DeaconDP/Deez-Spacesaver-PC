using Spacesaver.Models;

namespace Spacesaver.Helpers;

public static class OrganiserFolderSortHelper
{
    public static int Compare(OrganiserFolder a, OrganiserFolder b, OrganiserFolderSortMode mode) =>
        mode switch
        {
            OrganiserFolderSortMode.Tag => CompareTag(a, b),
            OrganiserFolderSortMode.Size => CompareSize(a, b),
            OrganiserFolderSortMode.DateCreated => CompareDate(a, b, f => f.CreatedUtc),
            OrganiserFolderSortMode.DateModified => CompareDate(a, b, f => f.ModifiedUtc),
            _ => CompareLocation(a, b)
        };

    public static IOrderedEnumerable<T> OrderByMode<T>(
        IEnumerable<T> items,
        Func<T, OrganiserFolder> folderSelector,
        OrganiserFolderSortMode mode) =>
        items.OrderBy(item => folderSelector(item), Comparer<OrganiserFolder>.Create((a, b) => Compare(a, b, mode)));

    private static int CompareTag(OrganiserFolder a, OrganiserFolder b)
    {
        var tag = string.Compare(
            ProjectFolderTypeHelper.GetLabel(a.FolderType),
            ProjectFolderTypeHelper.GetLabel(b.FolderType),
            StringComparison.OrdinalIgnoreCase);
        return tag != 0 ? tag : CompareLocation(a, b);
    }

    private static int CompareLocation(OrganiserFolder a, OrganiserFolder b) =>
        string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);

    private static int CompareSize(OrganiserFolder a, OrganiserFolder b)
    {
        // Descending measured size; null/unscanned last.
        var aHas = a.LastMeasuredBytes is not null;
        var bHas = b.LastMeasuredBytes is not null;
        if (aHas != bHas)
            return aHas ? -1 : 1;

        if (aHas && bHas)
        {
            var size = b.LastMeasuredBytes!.Value.CompareTo(a.LastMeasuredBytes!.Value);
            if (size != 0)
                return size;
        }

        return CompareLocation(a, b);
    }

    private static int CompareDate(
        OrganiserFolder a,
        OrganiserFolder b,
        Func<OrganiserFolder, DateTimeOffset?> selector)
    {
        // Newest first; missing dates last.
        var aVal = selector(a);
        var bVal = selector(b);
        var aHas = aVal is not null;
        var bHas = bVal is not null;
        if (aHas != bHas)
            return aHas ? -1 : 1;

        if (aHas && bHas)
        {
            var date = bVal!.Value.CompareTo(aVal!.Value);
            if (date != 0)
                return date;
        }

        return CompareLocation(a, b);
    }
}
