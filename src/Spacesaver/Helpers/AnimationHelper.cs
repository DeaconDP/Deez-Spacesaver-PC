using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using Windows.UI.ViewManagement;

namespace Spacesaver.Helpers;

public static class AnimationHelper
{
    private const float EntranceDurationSeconds = 0.24f;
    private const float HoverDurationSeconds = 0.16f;
    private const float PulseDurationSeconds = 0.22f;

    private static readonly Vector2 EaseOut = new(0.1f, 0.9f);
    private static readonly Vector2 EaseInOut = new(0.4f, 0f);

    public static bool PrefersReducedMotion
    {
        get
        {
            try
            {
                return new UISettings().AnimationsEnabled == false;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Subtle hover: 1px rise, no scale pop (cyberpunk motion budget).
    /// </summary>
    public static void AttachHoverLift(
        UIElement element,
        float hoverLift = -1f,
        float hoverScale = 1f,
        float pressScale = 1f)
    {
        if (PrefersReducedMotion)
            return;

        element.PointerEntered += (_, _) =>
            AnimateVisual(element, hoverLift, hoverScale, HoverDurationSeconds);

        element.PointerExited += (_, _) =>
            AnimateVisual(element, 0f, 1f, HoverDurationSeconds);

        element.PointerPressed += (_, _) =>
            AnimateVisual(element, hoverLift * 0.5f, pressScale, 0.1f);

        element.PointerReleased += (_, _) =>
            AnimateVisual(element, hoverLift, hoverScale, HoverDurationSeconds);
    }

    public static async Task PlayEntranceAsync(
        UIElement element,
        int delayMs = 0,
        float fromOffsetY = 10f)
    {
        if (PrefersReducedMotion)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Opacity = 1f;
            SetTranslation(element, Vector3.Zero);
            return;
        }

        // Animate the Translation facade, never Visual.Offset — XAML layout owns Offset.
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        var visualAnim = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visualAnim.Compositor;

        visualAnim.Opacity = 0f;
        visualAnim.Properties.InsertVector3("Translation", new Vector3(0, fromOffsetY, 0));

        if (delayMs > 0)
            await Task.Delay(delayMs);

        var opacity = compositor.CreateScalarKeyFrameAnimation();
        opacity.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        opacity.Duration = TimeSpan.FromSeconds(EntranceDurationSeconds);

        var translation = compositor.CreateVector3KeyFrameAnimation();
        translation.InsertKeyFrame(1f, Vector3.Zero, compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        translation.Duration = TimeSpan.FromSeconds(EntranceDurationSeconds);

        visualAnim.StartAnimation("Opacity", opacity);
        visualAnim.StartAnimation("Translation", translation);

        await Task.Delay(TimeSpan.FromSeconds(EntranceDurationSeconds));
    }

    private static void SetTranslation(UIElement element, Vector3 value)
    {
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        ElementCompositionPreview.GetElementVisual(element).Properties.InsertVector3("Translation", value);
    }

    public static Task PlayStaggerEntranceAsync(IEnumerable<UIElement> elements, int staggerMs = 50) =>
        Task.WhenAll(elements.Select((element, index) => PlayEntranceAsync(element, index * staggerMs)));

    public static async Task AnimateDoubleAsync(
        Action<double> setter,
        double from,
        double to,
        int durationMs = 450)
    {
        if (PrefersReducedMotion)
        {
            setter(to);
            return;
        }

        const int steps = 24;
        var stepDelay = Math.Max(1, durationMs / steps);

        for (var i = 1; i <= steps; i++)
        {
            var t = EaseOutCubic(i / (double)steps);
            setter(from + (to - from) * t);
            await Task.Delay(stepDelay);
        }

        setter(to);
    }

    public static void Pulse(UIElement element, float peakScale = 1.02f)
    {
        if (PrefersReducedMotion)
            return;

        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        var scale = compositor.CreateVector3KeyFrameAnimation();
        scale.InsertKeyFrame(0f, Vector3.One);
        scale.InsertKeyFrame(0.45f, new Vector3(peakScale, peakScale, 1f), compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        scale.InsertKeyFrame(1f, Vector3.One, compositor.CreateCubicBezierEasingFunction(EaseInOut, new Vector2(1f, 1f)));
        scale.Duration = TimeSpan.FromSeconds(PulseDurationSeconds);

        visual.StartAnimation("Scale", scale);
    }

    public static void FadeSlideIn(UIElement element, float fromOffsetY = 8f)
    {
        if (PrefersReducedMotion)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Opacity = 1f;
            SetTranslation(element, Vector3.Zero);
            return;
        }

        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        var visualAnim = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visualAnim.Compositor;

        visualAnim.Opacity = 0f;
        visualAnim.Properties.InsertVector3("Translation", new Vector3(0, fromOffsetY, 0));

        var opacity = compositor.CreateScalarKeyFrameAnimation();
        opacity.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        opacity.Duration = TimeSpan.FromSeconds(HoverDurationSeconds);

        var translation = compositor.CreateVector3KeyFrameAnimation();
        translation.InsertKeyFrame(1f, Vector3.Zero, compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        translation.Duration = TimeSpan.FromSeconds(HoverDurationSeconds);

        visualAnim.StartAnimation("Opacity", opacity);
        visualAnim.StartAnimation("Translation", translation);
    }

    public static void AnimateListItemEntrance(ListViewBase listView)
    {
        if (PrefersReducedMotion)
            return;

        var delay = 0;
        for (var i = 0; i < listView.Items.Count; i++)
        {
            if (listView.ContainerFromIndex(i) is not ListViewItem container)
                continue;

            _ = PlayEntranceAsync(container, delay, fromOffsetY: 10f);
            delay += 40;
            if (delay > 240)
                break;
        }
    }

    private static void AnimateVisual(UIElement element, float translateY, float scale, float durationSeconds)
    {
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f));

        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(1f, new Vector3(scale, scale, 1f), easing);
        scaleAnimation.Duration = TimeSpan.FromSeconds(durationSeconds);

        var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
        translationAnimation.InsertKeyFrame(1f, new Vector3(0, translateY, 0), easing);
        translationAnimation.Duration = TimeSpan.FromSeconds(durationSeconds);

        visual.StartAnimation("Scale", scaleAnimation);
        visual.StartAnimation("Translation", translationAnimation);
    }

    private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);
}
