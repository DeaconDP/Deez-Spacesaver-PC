using Spacesaver.Helpers;

namespace Spacesaver.ViewModels;

public sealed class StepRowViewModel
{
    public StepRowViewModel(int number, string text, SemanticTone tone)
    {
        StepNumber = number;
        StepText = text;
        Tone = tone;
    }

    public int StepNumber { get; }

    public string StepText { get; }

    public SemanticTone Tone { get; }
}
