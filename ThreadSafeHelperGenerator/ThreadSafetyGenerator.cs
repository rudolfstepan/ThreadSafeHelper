using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace ThreadSafeHelperGenerator
{
    [Generator]
    public class ThreadSafetyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            foreach (var method in receiver.Methods)
            {
                var model = context.Compilation.GetSemanticModel(method.SyntaxTree);
                var methodSymbol = model.GetDeclaredSymbol(method) as IMethodSymbol;

                var classDeclaration = (ClassDeclarationSyntax)method.Parent;
                var namespaceDeclaration = (NamespaceDeclarationSyntax)classDeclaration.Parent;

                var source = GenerateMethodWrapper(namespaceDeclaration.Name.ToString(), classDeclaration.Identifier.Text, methodSymbol);
                context.AddSource($"{classDeclaration.Identifier.Text}_{method.Identifier.Text}_Generated.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }

        private string GenerateMethodWrapper(string @namespace, string className, IMethodSymbol methodSymbol)
        {
            var methodName = methodSymbol.Name;
            var returnType = methodSymbol.ReturnType.ToDisplayString();
            var parameters = string.Join(", ", methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
            var arguments = string.Join(", ", methodSymbol.Parameters.Select(p => p.Name));

            var attributes = methodSymbol.GetAttributes();
            var threadSafeAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "ThreadSafeAttribute");
            var singleExecutionAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "SingleExecutionAttribute");
            var debounceAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "DebounceAttribute");
            var readWriteLockAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "ReadWriteLockAttribute");

            var sb = new StringBuilder($@"
using System;
using System.Threading;

namespace {@namespace}
{{
    public partial class {className}
    {{
");

            if (threadSafeAttribute != null)
            {
                var maxConcurrentThreads = (int)threadSafeAttribute.ConstructorArguments[0].Value;
                var waitForAvailability = (bool)threadSafeAttribute.ConstructorArguments[1].Value;

                sb.Append($@"
        private static readonly object {methodName}_lockObject = new object();
        private static int {methodName}_currentConcurrentThreads = 0;

        public {returnType} {methodName}_ThreadSafe({parameters})
        {{
            while (true)
            {{
                lock ({methodName}_lockObject)
                {{
                    if ({methodName}_currentConcurrentThreads < {maxConcurrentThreads})
                    {{
                        {methodName}_currentConcurrentThreads++;
                        break;
                    }}
                    else if (!{waitForAvailability.ToString().ToLower()})
                    {{
                        Console.WriteLine(""Thread "" + Thread.CurrentThread.ManagedThreadId + "" wird abgebrochen."");
");
                if (returnType == "void")
                {
                    sb.Append($@"
                        return;
");
                }
                else
                {
                    sb.Append($@"
                        return default;
");
                }
                sb.Append($@"
                    }}
                }}

                Thread.Sleep(100);
            }}

            try
            {{
                {(returnType == "void" ? string.Empty : "return ")}{methodName}_Implementation({arguments});
            }}
            finally
            {{
                lock ({methodName}_lockObject)
                {{
                    {methodName}_currentConcurrentThreads--;
                }}
            }}
        }}
");
            }

            if (singleExecutionAttribute != null)
            {
                sb.Append($@"
        private static bool {methodName}_hasExecuted = false;

        public {returnType} {methodName}_SingleExecution({parameters})
        {{
            if (!{methodName}_hasExecuted)
            {{
                lock (this)
                {{
                    if (!{methodName}_hasExecuted)
                    {{
                        {methodName}_hasExecuted = true;
                        {(returnType == "void" ? string.Empty : "return ")}{methodName}_Implementation({arguments});
                    }}
                }}
            }}
");
                if (returnType == "void")
                {
                    sb.Append($@"
            return;
");
                }
                else
                {
                    sb.Append($@"
            return default;
");
                }
                sb.Append($@"
        }}
");
            }

            if (debounceAttribute != null)
            {
                var milliseconds = (int)debounceAttribute.ConstructorArguments[0].Value;

                sb.Append($@"
        private static DateTime {methodName}_lastInvocation = DateTime.MinValue;

        public {returnType} {methodName}_Debounce({parameters})
        {{
            var now = DateTime.Now;
            if ((now - {methodName}_lastInvocation).TotalMilliseconds >= {milliseconds})
            {{
                {methodName}_lastInvocation = now;
                {(returnType == "void" ? string.Empty : "return ")}{methodName}_Implementation({arguments});
            }}
");
                if (returnType == "void")
                {
                    sb.Append($@"
            return;
");
                }
                else
                {
                    sb.Append($@"
            return default;
");
                }
                sb.Append($@"
        }}
");
            }

            if (readWriteLockAttribute != null)
            {
                var isReadLock = (bool)readWriteLockAttribute.ConstructorArguments[0].Value;

                sb.Append($@"
        private static readonly ReaderWriterLockSlim {methodName}_rwLock = new ReaderWriterLockSlim();

        public {returnType} {methodName}_ReadWriteLock({parameters})
        {{
            if ({isReadLock.ToString().ToLower()})
            {{
                {methodName}_rwLock.EnterReadLock();
                try
                {{
                    {(returnType == "void" ? string.Empty : "return ")}{methodName}_Implementation({arguments});
                }}
                finally
                {{
                    {methodName}_rwLock.ExitReadLock();
                }}
            }}
            else
            {{
                {methodName}_rwLock.EnterWriteLock();
                try
                {{
                    {(returnType == "void" ? string.Empty : "return ")}{methodName}_Implementation({arguments});
                }}
                finally
                {{
                    {methodName}_rwLock.ExitWriteLock();
                }}
            }}
");
                if (returnType == "void")
                {
                    sb.Append($@"
            return;
");
                }
                else
                {
                    sb.Append($@"
            return default;
");
                }
                sb.Append($@"
        }}
");
            }

            sb.Append($@"
    }}
}}
");

            return sb.ToString();
        }

        public class SyntaxReceiver : ISyntaxReceiver
        {
            public List<MethodDeclarationSyntax> Methods { get; } = new List<MethodDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is MethodDeclarationSyntax methodDeclaration &&
                    methodDeclaration.AttributeLists.Count > 0)
                {
                    Methods.Add(methodDeclaration);
                }
            }
        }
    }

}
