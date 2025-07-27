using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace CapriKit.CommandLine;

[Generator]
public class VerbGenerator : IIncrementalGenerator
{
    private static readonly string UtilitiesNameSpace = "CapriKit.CommandLine.Types";
    private static readonly string ExecutorBaseClass = "AVerbExector";

    private static readonly string VerbAttributeFullName = "CapriKit.CommandLine.Types.VerbAttribute";
    private static readonly string VerbAttributeName = "VerbAttribute";

    private static readonly string FlagAttributeFullName = "CapriKit.CommandLine.Types.FlagAttribute";
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

        GenerateCode(context, verbs, flags);
    }

    private static void GenerateCode(SourceProductionContext context, IReadOnlyList<VerbClass> verbs, IReadOnlyList<FlagProperty> flags)
    {        
        foreach (var verb in verbs)
        {
            var flagsForVerb = flags.Where(f => f.ParentTypeName == verb.TypeName).ToList();

            var builder = new StringBuilder();
            builder.AppendLine($"#nullable enable");
            builder.AppendLine($"using {UtilitiesNameSpace};");
            builder.AppendLine($"namespace {verb.TypeNamespace};");
            builder.Append($"public partial class {verb.TypeName}(");
            for (var i = 0; i < flagsForVerb.Count; i++)
            {
                var initFlag = flagsForVerb[i];
                builder.Append($"bool _Has{initFlag.PropertyName}, {initFlag.PropertyType} _{initFlag.PropertyName}");
                if( i < flagsForVerb.Count - 1 )
                {
                    builder.Append(", ");
                }
            }

            builder.AppendLine($")");
            builder.AppendLine("{");

            foreach (var flag in flagsForVerb)
            {
                builder.AppendLine($"  public bool Has{flag.PropertyName} {{ get => _Has{flag.PropertyName}; }}");
                builder.AppendLine($"  public partial {flag.PropertyType} {flag.PropertyName} {{ get => _{flag.PropertyName}; }}");
            }

            // TODO: use ArgsParser IsVerb and then TryParseFlag to try and parse each flag in a new parser method!

            builder.AppendLine($"  public static {verb.TypeName}? Parse(params string[] args)");
            builder.AppendLine("  {");
            builder.AppendLine($"    if (ArgsParser.IsVerb(\"{verb.VerbName}\", args))");
            builder.AppendLine("    {");

            foreach(var flag in flagsForVerb)
            {
                builder.AppendLine($"      var _Has{flag.PropertyName} = ArgsParser.TryParseFlag<{flag.PropertyType}>(\"{flag.FlagName}\", out {flag.PropertyType} __{flag.PropertyName}, args);");
            }

            builder.Append($"      return new {verb.TypeName}(");
            for (var i = 0; i < flagsForVerb.Count; i++)
            {
                var conFlag = flagsForVerb[i];
                builder.Append($"_Has{conFlag.PropertyName}, __{conFlag.PropertyName}");
                if (i < flagsForVerb.Count - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.AppendLine($");");

            builder.AppendLine("    }");
            builder.AppendLine("    return null;");

            builder.AppendLine("  }");
            builder.AppendLine("}");

            builder.AppendLine($"#nullable restore");
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
