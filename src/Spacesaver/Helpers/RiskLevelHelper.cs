using Microsoft.UI.Xaml.Media;
using Spacesaver.Models;

namespace Spacesaver.Helpers;

public static class RiskLevelHelper
{
    public static Brush GetBadgeBrush(RiskLevel level) =>
        SemanticColorHelper.GetAccentBrush(SemanticColorHelper.ForRiskLevel(level));

    public static Brush GetBadgeTintBrush(RiskLevel level) =>
        SemanticColorHelper.GetTintBrush(SemanticColorHelper.ForRiskLevel(level));
}
