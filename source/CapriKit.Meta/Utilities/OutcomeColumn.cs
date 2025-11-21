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
        Failure
    }

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        return task.State.Get<Outcome>(OutcomeKey) switch
        {
            Outcome.Success => new Markup(Emoji.Known.CheckMark),
            Outcome.Failure => new Markup(Emoji.Known.CrossMark),
            _ => new Markup(string.Empty),
        };
    }
}
