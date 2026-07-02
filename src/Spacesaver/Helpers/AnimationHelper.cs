using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;

namespace Spacesaver.Helpers;

public static class AnimationHelper
{
    private const float EntranceDurationSeconds = 0.38f;
    private const float HoverDurationSeconds = 0.18f;
    private const float PulseDurationSeconds = 0.28f;

    private static readonly Vector2 EaseOut = new(0.1f, 0.9f);
    private static readonly Vector2 EaseInOut = new(0.4f, 0f);

    public static void AttachHoverLift(
        UIElement element,
        float hoverLift = -3f,
        float hoverScale = 1.015f,
        float pressScale = 0.985f)
    {
        element.PointerEntered += (_, _) =>
            AnimateVisual(element, hoverLift, hoverScale, HoverDurationSeconds);

        element.PointerExited += (_, _) =>
            AnimateVisual(element, 0f, 1f, HoverDurationSeconds);

        element.PointerPressed += (_, _) =>
            AnimateVisual(element, hoverLift * 0.5f, pressScale, 0.1f);

        element.PointerReleased += (_, _) =>
            AnimateVisual(element, hoverLift, hoverScale, HoverDurationSeconds);
    }

    public static void ResetCompositionVisual(UIElement element)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        visual.Opacity = 1f;
        visual.Offset = Vector3.Zero;
        visual.Scale = Vector3.One;
    }

    public static async Task PlayOpacityEntranceAsync(UIElement element, int delayMs = 0)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        ResetCompositionVisual(element);
        visual.Opacity = 0f;

        if (delayMs > 0)
            await Task.Delay(delayMs);

        var opacity = compositor.CreateScalarKeyFrameAnimation();
        opacity.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        opacity.Duration = TimeSpan.FromSeconds(EntranceDurationSeconds);

        visual.StartAnimation("Opacity", opacity);
        await Task.Delay(TimeSpan.FromSeconds(EntranceDurationSeconds));
        visual.Opacity = 1f;
    }

    public static async Task PlayEntranceAsync(
        UIElement element,
        int delayMs = 0,
        float fromOffsetY = 20f)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        ResetCompositionVisual(element);
        visual.Opacity = 0f;
        visual.Offset = new Vector3(0, fromOffsetY, 0);

        if (delayMs > 0)
            await Task.Delay(delayMs);

        var opacity = compositor.CreateScalarKeyFrameAnimation();
        opacity.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        opacity.Duration = TimeSpan.FromSeconds(EntranceDurationSeconds);

        var offset = compositor.CreateVector3KeyFrameAnimation();
        offset.InsertKeyFrame(1f, Vector3.Zero, compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        offset.Duration = TimeSpan.FromSeconds(EntranceDurationSeconds);

        visual.StartAnimation("Opacity", opacity);
        visual.StartAnimation("Offset", offset);

        await Task.Delay(TimeSpan.FromSeconds(EntranceDurationSeconds));

        visual.Offset = Vector3.Zero;
        visual.Opacity = 1f;
    }

    public static Task PlayStaggerEntranceAsync(IEnumerable<UIElement> elements, int staggerMs = 55) =>
        Task.WhenAll(elements.Select((element, index) => PlayEntranceAsync(element, index * staggerMs)));

    public static async Task AnimateDoubleAsync(
        Action<double> setter,
        double from,
        double to,
        int durationMs = 550)
    {
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

    public static void Pulse(UIElement element, float peakScale = 1.04f)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        var scale = compositor.CreateVector3KeyFrameAnimation();
        scale.InsertKeyFrame(0f, Vector3.One);
        scale.InsertKeyFrame(0.45f, new Vector3(peakScale, peakScale, 1f), compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        scale.InsertKeyFrame(1f, Vector3.One, compositor.CreateCubicBezierEasingFunction(EaseInOut, new Vector2(1f, 1f)));
        scale.Duration = TimeSpan.FromSeconds(PulseDurationSeconds);

        visual.StartAnimation("Scale", scale);
    }

    public static void FadeSlideIn(UIElement element, float fromOffsetY = 12f)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        visual.Opacity = 0f;
        visual.Offset = new Vector3(0, fromOffsetY, 0);

        var opacity = compositor.CreateScalarKeyFrameAnimation();
        opacity.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        opacity.Duration = TimeSpan.FromSeconds(HoverDurationSeconds);

        var offset = compositor.CreateVector3KeyFrameAnimation();
        offset.InsertKeyFrame(1f, Vector3.Zero, compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f)));
        offset.Duration = TimeSpan.FromSeconds(HoverDurationSeconds);

        visual.StartAnimation("Opacity", opacity);
        visual.StartAnimation("Offset", offset);
    }

    public static void AnimateListItemEntrance(ListViewBase listView)
    {
        var delay = 0;
        for (var i = 0; i < listView.Items.Count; i++)
        {
            if (listView.ContainerFromIndex(i) is not ListViewItem container)
                continue;

            _ = PlayEntranceAsync(container, delay, fromOffsetY: 16f);
            delay += 40;
        }
    }

    private static void AnimateVisual(UIElement element, float translateY, float scale, float durationSeconds)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        var easing = compositor.CreateCubicBezierEasingFunction(EaseOut, new Vector2(0.2f, 1f));

        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(1f, new Vector3(scale, scale, 1f), easing);
        scaleAnimation.Duration = TimeSpan.FromSeconds(durationSeconds);

        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.InsertKeyFrame(1f, new Vector3(0, translateY, 0), easing);
        offsetAnimation.Duration = TimeSpan.FromSeconds(durationSeconds);

        visual.StartAnimation("Scale", scaleAnimation);
        visual.StartAnimation("Offset", offsetAnimation);
    }

    private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

    public static void WirePageLoaded(
        Page page,
        IEnumerable<UIElement> entranceElements,
        IEnumerable<UIElement>? hoverElements = null,
        float hoverScale = 1.015f)
    {
        page.Loaded += async (_, _) =>
        {
            await PlayStaggerEntranceAsync(entranceElements);

            if (hoverElements is null)
                return;

            foreach (var element in hoverElements)
                AttachHoverLift(element, hoverScale: hoverScale);
        };
    }
}
