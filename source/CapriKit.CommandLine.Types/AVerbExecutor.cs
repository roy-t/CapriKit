namespace CapriKit.CommandLine.Types;

public abstract class AVerbExecutor
{
    protected readonly List<string> Verbs = [];
    protected readonly Dictionary<string, Dictionary<string, string>> VerbToFlagToDocs = [];   

    public IReadOnlyList<string> GetVerbs()
    {
        return Verbs;
    }

    // Empty key means documentation for the verb itself, the other keys are the names of the flags
    public IReadOnlyDictionary<string, string>? GetDocumentationForVerb(string verb)
    {
        if (VerbToFlagToDocs.TryGetValue(verb, out var documentation))
        {
            return documentation;
        }

        return null;
    }    
}
