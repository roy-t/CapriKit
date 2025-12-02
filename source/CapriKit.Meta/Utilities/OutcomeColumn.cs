using Spectre.Console;
using Spectre.Console.Rendering;

namespace CapriKit.Meta.Utilities;

internal sealed class OutcomeColumn : ProgressColumn
{
    public const string OutcomeKey = "outcome";

    public enum Outcome
    {
        Indeterminate,
        Success,
        Failure,
        Skipped,
    }

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        return task.State.Get<Outcome>(OutcomeKey) switch
        {
            Outcome.Success => new Markup(Emoji.Known.CheckMark),
            Outcome.Failure => new Markup(Emoji.Known.CrossMark),
            Outcome.Skipped => new Markup(Emoji.Known.FastForwardButton),
            _ => new Markup(string.Empty),
        };
    }
}
