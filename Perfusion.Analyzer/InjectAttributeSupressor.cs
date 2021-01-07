using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Perfusion.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InjectAttributeSuppressor : DiagnosticSuppressor
    {
        public static readonly SuppressionDescriptor SupressUnused = new(
            "PRF0001",
            "CS0649",
            "Members marked with InjectAttribute will be assigned to/called by the container."
        );

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(SupressUnused);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                if (diagnostic.Id == SupressUnused.SuppressedDiagnosticId)
                {
                    var node = diagnostic.Location.SourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
                    if (node is not null)
                    {
                        var model = context.GetSemanticModel(node.SyntaxTree);
                        var sym = model.GetDeclaredSymbol(node, context.CancellationToken);
                        if (sym.Kind == SymbolKind.Field || sym.Kind == SymbolKind.Method || sym.Kind == SymbolKind.Property)
                            if (sym.GetAttributes().Any(a => a.AttributeClass.Name == "InjectAttribute"))
                            {
                                context.ReportSuppression(Suppression.Create(SupressUnused, diagnostic));
                            }
                    }
                }
            }
        }
    }
}
