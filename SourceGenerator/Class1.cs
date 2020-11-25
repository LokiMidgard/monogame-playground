using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerator
{
    [Generator]
    public class Generator : ISourceGenerator
    {

        private static string EmitInterface(string @namespace) => $@"
namespace {@namespace}
{{

    public partial record AnimationSequence;
    public partial record AnimationSequence<TDirection, TAnimation> : AnimationSequence
        where TDirection : struct, System.Enum
        where TAnimation : struct, System.Enum;

    public partial interface ISprite<TDirection, TAnimation>
        where TDirection : struct, System.Enum
        where TAnimation : struct, System.Enum
    {{
        Microsoft.Xna.Framework.Graphics.Texture2D Texture {{ get; }}
        float FrameSpeed {{ get; set; }}
        AnimationSequence<TDirection, TAnimation> this[TDirection direction, TAnimation animation] {{ get; }}
    }}
}}
";



        private static string EmitBeginning(string @namespace) => $@"namespace {@namespace}
{{ 
    internal partial class Sprite
    {{
        public static ISprite<TDirection,TAnimation> Create<TDirection, TAnimation>(global::Microsoft.Xna.Framework.Graphics.Texture2D sheet, System.Func<TDirection, TAnimation, AnimationSequence<TDirection, TAnimation>> animationSelection)
            where TDirection : struct, System.Enum
            where TAnimation : struct, System.Enum
        {{
";
        private const string EmitEnding = @"            throw new System.NotImplementedException();
        }
    }
}
";

        private const string EmitAttribute = @"
namespace SpriteGenerator 
{
    [System.AttributeUsage(System.AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    internal sealed class SpriteGeneratorNamespaceAttribute : System.Attribute
    {
        public SpriteGeneratorNamespaceAttribute(string generatedNamespace)
        {
            this.GeneratedNamespace = generatedNamespace;
               
        }
        public string GeneratedNamespace { get; }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            // I should probably put something here
        }

        public void Execute(GeneratorExecutionContext context)
        {

            var attributeCompilation = GenerateHelperAttributes(context);


            var @namespace = attributeCompilation.Assembly
                    .GetAttributes()
                    .SingleOrDefault(x => x.AttributeClass.Name == "SpriteGeneratorNamespaceAttribute" && x.ConstructorArguments.Length == 1)
                    ?.ConstructorArguments
                    .FirstOrDefault().Value as string
                ?? "GeneratedSprites";


            //System.Diagnostics.Debugger.Launch();
            var compilation = GenerateHelperClasses(context, @namespace);

            var enumValidatorType = compilation.GetTypeByMetadataName($@"{@namespace}.Sprite")!;

            var infos = GetEnumValidationInfo(compilation, enumValidatorType).Distinct();


            //if (infos.Any())
            {
                var sb = new StringBuilder();

                sb.AppendLine(EmitInterface(@namespace));
                sb.AppendLine();
                sb.AppendLine(EmitAttribute);
                sb.AppendLine();

                sb.AppendLine(EmitBeginning(@namespace));

                foreach (var info in infos)
                {
                    this.GenerateValidator(sb, info, "            ");
                    sb.AppendLine();
                }
                sb.AppendLine(EmitEnding);
                sb.AppendLine($"namespace {@namespace} {{");
                sb.AppendLine("    internal partial class Sprite {");

                foreach (var info in infos)
                {
                    this.GenerateClass(sb, info, "        ");
                    sb.AppendLine();
                }
                sb.AppendLine("    }");
                sb.AppendLine("}");

                context.AddSource("Validation.cs", sb.ToString());
            }
            //else
            //{
            //    context.AddSource("Validator.cs", EnumValidatorStub);
            //}
        }

        private void GenerateValidator(StringBuilder sb, EnumValidationInfo info, string indent)
        {
            sb.AppendLine($"{indent}if(typeof(TDirection) == typeof({info.DirectionType}) && typeof(TAnimation) == typeof({info.AnimationType}))");
            sb.AppendLine($"{indent}  return new Sprite_{info.DirectionType.ToString().Replace('.', '_')}__{info.AnimationType.ToString().Replace('.', '_')}(sheet, (System.Func<{info.DirectionType}, {info.AnimationType}, AnimationSequence<{info.DirectionType}, {info.AnimationType}>>)(object)animationSelection) as ISprite<TDirection, TAnimation>;");
        }
        private void GenerateClass(StringBuilder sb, EnumValidationInfo info, string indent)
        {
            //sb.AppendLine($"{indent}if(TDirection == typeof({info.DirectionType}) && TAnimation == typeof({info.AnimationType}))");
            sb.AppendLine($"{indent}private class Sprite_{info.DirectionType.ToString().Replace('.', '_')}__{info.AnimationType.ToString().Replace('.', '_')} : Sprite, ISprite<{info.DirectionType},{info.AnimationType}> {{");
            sb.AppendLine($"{indent}  public global::Microsoft.Xna.Framework.Graphics.Texture2D Texture {{ get; }}");
            sb.AppendLine($"{indent}  public float FrameSpeed {{ get; set; }} = 1.0f;");
            sb.AppendLine($"{indent}  public AnimationSequence<{info.DirectionType}, {info.AnimationType}> this[{info.DirectionType} direction, {info.AnimationType} animation]");
            sb.AppendLine($"{indent}    => this.animations[({info.DirectionType.GetMembers().OfType<IFieldSymbol>().First().ConstantValue.GetType().FullName})direction, ({info.AnimationType.GetMembers().OfType<IFieldSymbol>().First().ConstantValue.GetType().FullName})animation];");

            var directionFields = new List<IFieldSymbol>();
            foreach (var directionMember in info.DirectionType.GetMembers())
            {
                if (directionMember is IFieldSymbol
                    {
                        IsStatic: true,
                        IsConst: true,
                        ConstantValue: int _
                    } field)
                {
                    directionFields.Add(field);
                }
            }
            var animationFields = new List<IFieldSymbol>();
            foreach (var animationMember in info.AnimationType.GetMembers())
            {
                if (animationMember is IFieldSymbol
                    {
                        IsStatic: true,
                        IsConst: true,
                        ConstantValue: int _
                    } field)
                {
                    animationFields.Add(field);
                }
            }

            sb.AppendLine($"{indent}  private readonly AnimationSequence<{info.DirectionType}, {info.AnimationType}>[,] animations;");
            sb.AppendLine($"{indent}  internal Sprite_{info.DirectionType.ToString().Replace('.', '_')}__{info.AnimationType.ToString().Replace('.', '_')}(global::Microsoft.Xna.Framework.Graphics.Texture2D sheet, System.Func<{info.DirectionType}, {info.AnimationType}, AnimationSequence<{info.DirectionType}, {info.AnimationType}>> animationSelection){{");

            sb.AppendLine($"{indent}    this.animations = new AnimationSequence<{info.DirectionType}, {info.AnimationType}>[{directionFields.Count},{animationFields.Count}];");
            sb.AppendLine($"{indent}    this.Texture = sheet;");

            foreach (var direction in directionFields)
                foreach (var animaiton in animationFields)
                {
                    sb.AppendLine($"{indent}    this.animations[{direction.ConstantValue},{animaiton.ConstantValue}] = animationSelection({info.DirectionType}.{direction.Name},{info.AnimationType}.{animaiton.Name});");

                }

            sb.AppendLine($"{indent}  }}");
            sb.AppendLine($"{indent}}}");
        }


        private static IEnumerable<EnumValidationInfo> GetEnumValidationInfo(Compilation compilation, INamedTypeSymbol enumValidatorType)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                foreach (var invocation in tree.GetRoot().DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                {
                    var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol == null)
                    {
                        continue;
                    }

                    // Its called Create
                    if (symbol.Name != "Create")
                        continue;

                    // we search a static Method
                    if (!symbol.IsStatic)
                        continue;

                    // our method has two type arguments
                    var args = symbol.TypeArguments;
                    if (args.Length != 2)
                        continue;


                    if (SymbolEqualityComparer.Default.Equals(symbol.ContainingType, enumValidatorType))
                    {


                        // Note: This assumes the only method on enumValidatorType is the one we want.
                        // ie, I'm too lazy to check which invocation is being made :)

                        var enumType1 = args[0];
                        var enumType2 = args[1];
                        if (enumType1 == null || enumType2 == null)
                        {
                            continue;
                        }

                        var info = new EnumValidationInfo(enumType1, enumType2);
                        // foreach (var member in enumType.GetMembers())
                        // {
                        //     if (member is IFieldSymbol
                        //         {
                        //             IsStatic: true,
                        //             IsConst: true,
                        //             ConstantValue: int value
                        //         } field)
                        //     {
                        //         info.Elements.Add((field.Name, value));
                        //     }
                        // }

                        // info.Elements.Sort((e1, e2) => e1.Value.CompareTo(e2.Value));

                        yield return info;
                    }
                }
            }
        }

        private static Compilation GenerateHelperClasses(GeneratorExecutionContext context, string @namespace)
        {
            var compilation = context.Compilation;

            var options = (compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
            var tempCompilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(EmitInterface(@namespace) + EmitBeginning(@namespace) + EmitEnding, Encoding.UTF8), options));

            return tempCompilation;
        }

        private static Compilation GenerateHelperAttributes(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;

            var options = (compilation as CSharpCompilation)?.SyntaxTrees[0].Options as CSharpParseOptions;
            var tempCompilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(EmitAttribute, Encoding.UTF8), options));

            return tempCompilation;
        }

        private class EnumValidationInfo
        {
            public EnumValidationInfo(ITypeSymbol DirectionType, ITypeSymbol AnimationType)
            {
                this.DirectionType = DirectionType;
                this.AnimationType = AnimationType;
            }

            public ITypeSymbol DirectionType { get; }
            public ITypeSymbol AnimationType { get; }

            public override bool Equals(object obj)
            {
                return obj is EnumValidationInfo info &&
                       EqualityComparer<ITypeSymbol>.Default.Equals(this.DirectionType, info.DirectionType) &&
                       EqualityComparer<ITypeSymbol>.Default.Equals(this.AnimationType, info.AnimationType);
            }

            public override int GetHashCode()
            {
                var hashCode = -1554953905;
                hashCode = hashCode * -1521134295 + EqualityComparer<ITypeSymbol>.Default.GetHashCode(this.DirectionType);
                hashCode = hashCode * -1521134295 + EqualityComparer<ITypeSymbol>.Default.GetHashCode(this.AnimationType);
                return hashCode;
            }
        }
    }
}
