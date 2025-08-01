using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace CapriKit.CommandLine;

[Generator]
public class VerbGenerator : IIncrementalGenerator
{    
    private static readonly string RootNameSpace = "CapriKit.CommandLine";

    private static readonly string VerbAttributeFullName = "CapriKit.CommandLine.VerbAttribute";
    private static readonly string VerbAttributeName = "VerbAttribute";

    private static readonly string FlagAttributeFullName = "CapriKit.CommandLine.FlagAttribute";
    private static readonly string FlagAttributeName = "FlagAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {        
        // Get all classes annotated with [Verb(..)]
        var verbDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            VerbAttributeFullName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, ct) => GetSemanticTargetForVerbGeneration(ctx, ct))
            .Collect();

        // Get all properties annotated with [Flag(..)], even if they are not in class with annotated with [Verb(..)]
        var flagDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            FlagAttributeFullName,
            static (node, _) => node is PropertyDeclarationSyntax,
            static (ctx, ct) => GetSemanticTargetForFlagGeneration(ctx, ct))
            .Collect();

        // Generate codes based on all verb classes and flag properties
        var verbsAndFlags = verbDeclarations.Combine(flagDeclarations);
        context.RegisterSourceOutput(verbsAndFlags, static (spc, source) => Execute(spc, source));
    }

    private static void Execute(SourceProductionContext context, (ImmutableArray<(ClassDeclarationSyntax Syntax, AttributeData Data, string Namespace)> VerbDeclarations, ImmutableArray<(PropertyDeclarationSyntax Syntax, AttributeData Data, string ParentClass)> FlagDeclarations) source)
    {
        var flags = new List<FlagProperty>();
        foreach (var flagDeclaration in source.FlagDeclarations)
        {
            var flag = GetFlag(flagDeclaration.Syntax, flagDeclaration.Data, flagDeclaration.ParentClass);
            flags.Add(flag);            
        }

        var verbs = new List<VerbClass>();
        foreach(var verbDeclaration in source.VerbDeclarations)
        {
            var verb = GetVerb(verbDeclaration.Syntax, verbDeclaration.Data, verbDeclaration.Namespace);
            verbs.Add(verb);
        }

        GenerateVerbFiles(context, verbs, flags);
        GeneratePrinter(context, verbs, flags);
    }

    private static void GeneratePrinter(SourceProductionContext context, List<VerbClass> verbs, List<FlagProperty> flags)
    {               
        var verbDictionary = string.Join(", ", verbs.Select(v => $"{{{v.TypeNamespace}.{v.TypeName}.VerbName, (new VerbInfo({v.TypeNamespace}.{v.TypeName}.VerbName, {v.TypeNamespace}.{v.TypeName}.Documentation), {v.TypeNamespace}.{v.TypeName}.Flags)}}"));

        var file = $$"""
                namespace {{RootNameSpace}};                
                public static class CommandLineVerbs
                {                    
                    /// <summary>
                    /// The keys in this dictionary represent all recognized verbs. The values refer to the availble flags for the verb.
                    /// Use this property together with the CommandLineHelp class to print help information for your CLI.
                    /// </summary>                    
                    public static readonly IReadOnlyDictionary<string, (VerbInfo, IReadOnlyList<FlagInfo>)> AllVerbs = new Dictionary<string, (VerbInfo, IReadOnlyList<FlagInfo>)>()
                    {
                        {{verbDictionary}}
                    };
                }
                """;
        context.AddSource($"VerbGenerator.CommandLineHelp.g.cs", file);
    }
    
    private static void GenerateVerbFiles(SourceProductionContext context, IReadOnlyList<VerbClass> verbs, IReadOnlyList<FlagProperty> flags)
    {        
        foreach (var verb in verbs)
        {            
            var flagsForVerb = flags.Where(f => f.ParentTypeName == verb.TypeName).ToList();

            var builder = new StringBuilder();

            var classIntro = $$"""
                using {{RootNameSpace}};
                namespace {{verb.TypeNamespace}};
                public partial class {{verb.TypeName}}
                {
                    public static string VerbName => {{Utilities.ToLiteral(verb.VerbName)}};
                    public static string Documentation => {{Utilities.ToLiteral(verb.Documentation)}};

                """;
            builder.AppendLine(classIntro);

            var flagInfos = string.Join(", ", flagsForVerb.Select(f => $"new FlagInfo({Utilities.ToLiteral(f.FlagName)}, {Utilities.ToLiteral(f.PropertyType)}, {Utilities.ToLiteral(f.Documentation)})"));            
            var flagList = $$"""
                    /// <summary>
                    /// Name, type and description of each flag available for this command.
                    /// </summary>
                    public static readonly IReadOnlyList<FlagInfo> Flags = [{{flagInfos}}];
                    
                """;
            builder.AppendLine(flagList);

            foreach (var flag in flagsForVerb)
            {
                var name = flag.PropertyName;
                var type = flag.PropertyType;
                var docs = flag.Documentation;
                var fields = $$"""
                        private {{type}} _{{name}} = default;
                        public bool Has{{name}} { get; private set; }                        
                        public partial {{type}} {{name}} { get => _{{name}}; }
                        public void Set{{name}}({{type}} value){ _{{name}} = value; Has{{name}} = true; }

                    """;

                builder.AppendLine(fields);
            }

            var verbName = verb.TypeName;
            var parseIntro = $$"""
                #nullable enable
                    /// <summary>
                    /// Tries parsing the given arguments as this verb and its flags.
                    ///
                    /// Throws an UnmatchedArgumentsException if the verb matched, but any of the arguments could not be matched to a flag.
                    /// </summary>
                    public static bool TryParse(out {{verb.TypeName}} value, params string[] args)
                    {
                        var argsParser = new ArgsParser(args);
                        if (argsParser.PopVerb("{{verb.VerbName}}"))
                        {
                            var verb = new {{verb.TypeName}}();                            
                """;
            builder.AppendLine(parseIntro);
            
            foreach (var flag in flagsForVerb)
            {
                var flagName = flag.FlagName;
                var flagPropertyName = flag.PropertyName;
                var flagType = flag.PropertyType;
                if (flag.PropertyType == "bool")
                {                    
                    builder.AppendLine($"            if(argsParser.PopBoolFlag(\"{flagName}\")) {{ verb.Set{flagPropertyName}(true); }}");
                }
                else
                {                    
                    builder.AppendLine($"            if(argsParser.PopFlag<{flagType}>(\"{flagName}\", out {flagType} __{flagPropertyName})) {{ verb.Set{flagPropertyName}(__{flagPropertyName}); }}");
                }
            }

            var parseOutro = $$"""
                            var unmatched = argsParser.GetUnmatchedArguments();
                            if (unmatched.Any())
                            {
                                throw new UnmatchedArgumentsException(unmatched);
                            }
                            value = verb;
                            return true;
                        }
                        value = null!;
                        return false;
                    }
                #nullable restore                
                """;
            builder.AppendLine(parseOutro);
                      
            var classOutro = """                
                }
                """;
            builder.AppendLine(classOutro);
            
            context.AddSource($"VerbGenerator.{verb.TypeNamespace}.{verb.TypeName}.g.cs", builder.ToString());
        }               
    }

    private static VerbClass GetVerb(ClassDeclarationSyntax syntax, AttributeData data, string @namespace)
    {        
        if (data.ConstructorArguments.Length != 1)
        {
            throw new Exception($"{VerbAttributeFullName} should have exactly one constructor argument");
        }

        var typeName = syntax.Identifier.ValueText;
        var verbName = data.ConstructorArguments[0].Value?.ToString() ?? string.Empty;        
        var documentation = Utilities.GetDocumentationFromLeadingTrivia(syntax);

        return new VerbClass(typeName, @namespace, verbName, documentation);
    }

    private static FlagProperty GetFlag(PropertyDeclarationSyntax syntax, AttributeData data, string parentTypeName)
    {        
        // Limit support to built-in-types: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types        
        if (syntax.Type is not PredefinedTypeSyntax predefinedType)
        {
            throw new NotSupportedException($"The flag attribute can only be used on built in types");
        } 

        // Ignore built-in reference types, except for string
        var propertyType = predefinedType.Keyword.ValueText;
        if (propertyType is "object" or "delegate" or "dynamic")
        {
            throw new NotSupportedException($"The flag attribute cannot be used on 'object', 'delegate, or 'dynamic' types");
        }

        if (data.ConstructorArguments.Length != 1)
        {
            throw new Exception($"{FlagAttributeFullName} should have exactly one constructor argument");
        }        

        var flagName = data.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        var propertyName = syntax.Identifier.ValueText ?? string.Empty;
        
        var documentation = Utilities.GetDocumentationFromLeadingTrivia(syntax);       

        return new FlagProperty(propertyType, propertyName, parentTypeName, flagName, documentation);
    }
    
    private static (ClassDeclarationSyntax Syntax, AttributeData Data, string Namespace) GetSemanticTargetForVerbGeneration(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetNode is not ClassDeclarationSyntax classDeclaration)
        {
            throw new NotSupportedException($"Target of [{VerbAttributeFullName}] should be a class");
        }

        // Technically the syntax provider should never give us a node without the attribute we requested
        var attribute = context.Attributes.FirstOrDefault(a => a.AttributeClass?.Name == VerbAttributeName);
        if (attribute == null)
        {
            throw new Exception($"Could not find attribute");
        }

        var ns = Utilities.GetNamespace(classDeclaration);

        return (classDeclaration, attribute, ns);
    }

    private static (PropertyDeclarationSyntax Syntax, AttributeData Data, string ParentClass) GetSemanticTargetForFlagGeneration(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetNode is not PropertyDeclarationSyntax properyDeclaration)
        {
            throw new NotSupportedException($"Target of [{FlagAttributeFullName}] should be a property");
        }

        // Technically the syntax provider should never give us a node without the attribute we requested
        var attribute = context.Attributes.First(a => a.AttributeClass?.Name == FlagAttributeName);
        if (attribute == null)
        {
            throw new Exception($"Could not find attribute");
        }

        // This can happen if the property is defined on a struct, which we currently do not supported
        var parentClass = context.TargetNode.FirstAncestorOrSelf<ClassDeclarationSyntax>()?.Identifier.ValueText;
        if (parentClass == null)
        {
            throw new NotSupportedException($"Properties attributed with [{FlagAttributeFullName}] should be defined in a class");
        }

        return (properyDeclaration, attribute, parentClass);        
    }    
}
