using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;


namespace ThreadSafeHelperGenerator
{
    [Generator]
    public class CombinedSourceGenerator : ISourceGenerator
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
            var sb = new StringBuilder($@"
using System;
using System.Threading;
using System.Timers;

namespace {@namespace}
{{
    public partial class {className}
    {{
");

            var methodName = methodSymbol.Name;
            var returnType = methodSymbol.ReturnType.ToDisplayString();
            var parameters = string.Join(", ", methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
            var arguments = string.Join(", ", methodSymbol.Parameters.Select(p => p.Name));

            var attributes = methodSymbol.GetAttributes();

            sb.Append(HandleThreadSafeAttribute(methodName, returnType, parameters, arguments, attributes));
            sb.Append(HandleSingleExecutionAttribute(methodName, returnType, parameters, arguments, attributes));
            sb.Append(HandleDebounceAttribute(methodName, returnType, parameters, arguments, attributes));
            sb.Append(HandleReadWriteLockAttribute(methodName, returnType, parameters, arguments, attributes));
            sb.Append(HandleTimedExecutionAttribute(methodName, attributes));
            sb.Append(HandleCacheAttribute(methodName, returnType, parameters, arguments, attributes));
            sb.Append(HandleRetryFallbackAttributes(methodName, returnType, parameters, arguments, attributes));

            sb.Append($@"
    }}
}}
");

            return sb.ToString();
        }

        private string HandleThreadSafeAttribute(string methodName, string returnType, string parameters, string arguments, ImmutableArray<AttributeData> attributes)
        {
            var threadSafeAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "ThreadSafeAttribute");
            if (threadSafeAttribute == null) return string.Empty;

            var maxConcurrentThreads = (int)threadSafeAttribute.ConstructorArguments[0].Value;
            var waitForAvailability = (bool)threadSafeAttribute.ConstructorArguments[1].Value;

            var sb = new StringBuilder($@"
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

            return sb.ToString();
        }

        private string HandleSingleExecutionAttribute(string methodName, string returnType, string parameters, string arguments, ImmutableArray<AttributeData> attributes)
        {
            var singleExecutionAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "SingleExecutionAttribute");
            if (singleExecutionAttribute == null) return string.Empty;

            var sb = new StringBuilder($@"
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

            return sb.ToString();
        }

        private string HandleDebounceAttribute(string methodName, string returnType, string parameters, string arguments, ImmutableArray<AttributeData> attributes)
        {
            var debounceAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "DebounceAttribute");
            if (debounceAttribute == null) return string.Empty;

            var milliseconds = (int)debounceAttribute.ConstructorArguments[0].Value;

            var sb = new StringBuilder($@"
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

            return sb.ToString();
        }

        private string HandleReadWriteLockAttribute(string methodName, string returnType, string parameters, string arguments, ImmutableArray<AttributeData> attributes)
        {
            var readWriteLockAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "ReadWriteLockAttribute");
            if (readWriteLockAttribute == null) return string.Empty;

            var isReadLock = (bool)readWriteLockAttribute.ConstructorArguments[0].Value;

            var sb = new StringBuilder($@"
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

            return sb.ToString();
        }

        private string HandleTimedExecutionAttribute(string methodName, ImmutableArray<AttributeData> attributes)
        {
            var timedExecutionAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "TimedExecutionAttribute");
            if (timedExecutionAttribute == null) return string.Empty;

            var intervalMilliseconds = (int)timedExecutionAttribute.ConstructorArguments[0].Value;
            var runInBackground = (bool)timedExecutionAttribute.ConstructorArguments[1].Value;

            var sb = new StringBuilder($@"
        private System.Timers.Timer _{methodName}_timer;
        private Thread _{methodName}_backgroundThread;

        public void Start{methodName}Timer()
        {{
            _{methodName}_timer = new System.Timers.Timer({intervalMilliseconds});
            _{methodName}_timer.Elapsed += (sender, e) =>
            {{
                if ({runInBackground.ToString().ToLower()})
                {{
                    _{methodName}_backgroundThread = new Thread(() => {methodName}_Implementation());
                    _{methodName}_backgroundThread.IsBackground = true;
                    _{methodName}_backgroundThread.Start();
                }}
                else
                {{
                    {methodName}_Implementation();
                }}
            }};
            _{methodName}_timer.AutoReset = true;
            _{methodName}_timer.Start();
        }}

        public void Stop{methodName}Timer()
        {{
            _{methodName}_timer?.Stop();
            _{methodName}_timer?.Dispose();
        }}
");

            return sb.ToString();
        }

        private string HandleCacheAttribute(string methodName, string returnType, string parameters, string arguments, ImmutableArray<AttributeData> attributes)
        {
            var cacheAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "CacheAttribute");
            if (cacheAttribute == null) return string.Empty;

            var cacheDuration = (int)cacheAttribute.ConstructorArguments[0].Value;

            var sb = new StringBuilder($@"
        private {returnType} {methodName}_cachedValue;
        private DateTime {methodName}_cacheExpiration = DateTime.MinValue;

        public {returnType} {methodName}_Cached({parameters})
        {{
            if(DateTime.Now > {methodName}_cacheExpiration)
            {{
                {methodName}_cachedValue = {methodName}_Implementation({arguments});
                {methodName}_cacheExpiration = DateTime.Now.AddSeconds({cacheDuration});
            }}

            return {methodName}_cachedValue;
        }}
");

            return sb.ToString();
        }

        private string HandleRetryFallbackAttributes(string methodName, string returnType, string parameters, string arguments, ImmutableArray<AttributeData> attributes)
        {
            var retryAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "RetryAttribute");
            var fallbackAttribute = attributes.FirstOrDefault(ad => ad.AttributeClass?.Name == "FallbackAttribute");

            if (retryAttribute == null && fallbackAttribute == null) return string.Empty;

            var sb = new StringBuilder();

            if (retryAttribute != null && fallbackAttribute != null)
            {
                var maxRetries = retryAttribute.ConstructorArguments[0].Value;
                var retryInterval = retryAttribute.ConstructorArguments[1].Value;
                var fallbackMethodName = (string)fallbackAttribute.ConstructorArguments[0].Value;

                bool isVoid = returnType.ToLower() == "void";

                sb.Append($@"
        public {returnType} {methodName}_RetryFallback({parameters})
        {{
            for (int i = 0; i < {maxRetries}; i++)
            {{
                try
                {{
                    {(isVoid ? $"{methodName}_Implementation({arguments});" : $"return {methodName}_Implementation({arguments});")}
                }}
                catch
                {{
                    Thread.Sleep({retryInterval});
                }}
            }}

            try
            {{
                return {fallbackMethodName}({arguments});
            }}
            catch
            {{
                {(isVoid ? "return;" : "return default;")}
            }}
        }}
");
            }
            else if (retryAttribute != null && fallbackAttribute == null)
            {
                var maxRetries = retryAttribute.ConstructorArguments[0].Value;
                var retryInterval = retryAttribute.ConstructorArguments[1].Value;

                bool isVoid = returnType.ToLower() == "void";

                sb.Append($@"
        public {returnType} {methodName}_Retry({parameters})
        {{
            for (int i = 0; i < {maxRetries}; i++)
            {{
                try
                {{
                    {(isVoid ? $"{methodName}_Implementation({arguments});" : $"return {methodName}_Implementation({arguments});")}
                }}
                catch
                {{
                    Thread.Sleep({retryInterval});
                }}
            }}
    
            {(isVoid ? "return;" : "return default;")}
        }}
");
            }
            else if (fallbackAttribute != null && retryAttribute == null)
            {
                var fallbackMethodName = (string)fallbackAttribute.ConstructorArguments[0].Value;

                bool isVoid = returnType.ToLower() == "void";


                sb.Append($@"
        public {returnType} {methodName}_Fallback({parameters})
        {{
            try
            {{
                {(isVoid ? $"{methodName}_Implementation({arguments});" : $"return {methodName}_Implementation({arguments});")}
            }}
            catch
            {{
                {(isVoid ? $"{fallbackMethodName}({arguments});" : $"return {fallbackMethodName}({arguments});")}
            }}
        }}
");
            }

            return sb.ToString();
        }

        private class SyntaxReceiver : ISyntaxReceiver
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
