using Microsoft.UI.Xaml.Media;

namespace Spacesaver.Helpers;

/// <summary>
/// Stable per-drive accent colors so "D:" is the same color everywhere in the organiser.
/// Cyberpunk-aligned palette; color is always paired with a text label in the UI.
/// </summary>
public static class DriveColorHelper
{
    private static readonly Windows.UI.Color[] Palette =
    [
        Windows.UI.Color.FromArgb(255, 0, 229, 192),   // teal
        Windows.UI.Color.FromArgb(255, 107, 158, 255), // blue
        Windows.UI.Color.FromArgb(255, 255, 138, 76),  // orange
        Windows.UI.Color.FromArgb(255, 255, 61, 129),  // pink
        Windows.UI.Color.FromArgb(255, 255, 209, 102), // amber
        Windows.UI.Color.FromArgb(255, 125, 220, 92),  // green
        Windows.UI.Color.FromArgb(255, 76, 201, 240),  // cyan
        Windows.UI.Color.FromArgb(255, 139, 144, 160)  // slate
    ];

    public static Windows.UI.Color ForDriveRoot(string? root)
    {
        if (string.IsNullOrEmpty(root))
            return Palette[^1];

        if (char.IsAsciiLetter(root[0]))
            return Palette[(char.ToUpperInvariant(root[0]) - 'A') % Palette.Length];

        var hash = 0;
        foreach (var c in root.ToUpperInvariant())
            hash = unchecked(hash * 31 + c);

        return Palette[Math.Abs(hash) % Palette.Length];
    }

    public static Brush GetBrush(string? root) => new SolidColorBrush(ForDriveRoot(root));
}
