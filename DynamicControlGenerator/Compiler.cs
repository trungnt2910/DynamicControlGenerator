using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DynamicControlGenerator
{
    public static class Compiler
    {
        const string DefaultNamespace = "DynamicControlGenerator";
        private static readonly string[] CsFileNames = new[] { "ToastNotificationLoader.cs", "ScreenInterop.cs", "IconInterop.cs" };
        const string XamlFileName = "ToastNotificationXaml.xaml";

        /// <summary>
        /// Compiles the dynamic control, and references Framework-specific assemblies.
        /// This method is slow, takes about 2 seconds on all targets.
        /// Therefore, it is recommended that the assembly should be compiled as soon as possible
        /// on a background thread.
        /// </summary>
        /// <returns>The Assembly that contains the dynamic control</returns>
        public static Assembly Compile()
        {
            bool isDebug = Debugger.IsAttached;
            
            var syntaxTrees = CsFileNames.Select(name => CSharpSyntaxTree.ParseText(ReadResource(name)));

            var compilation = CSharpCompilation.Create(
                "ToastNotification.Runtime.Wpf",
                syntaxTrees,
                GetReferences().ToArray(),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: isDebug ? OptimizationLevel.Debug : OptimizationLevel.Release)
                );

            var stream = OpenResource(XamlFileName);

            var resourceDescription = new ResourceDescription(
                "Uno.Extras.ToastNotification.ToastNotificationXaml.xaml",
                () => stream,
                true);

            var memoryStream = new MemoryStream();

            Assembly dynamicallyCompiledAssembly;

            if (isDebug)
            {
                var pdbStream = new MemoryStream();
                var emitOptions = new EmitOptions(
                    debugInformationFormat: DebugInformationFormat.PortablePdb);
                var embeddedTexts = CsFileNames.Select(name =>
                {
                    return EmbeddedText.FromSource(name, SourceText.From(ReadResource(name), Encoding.UTF8));
                });
                var result = compilation.Emit(memoryStream, pdbStream,
                    manifestResources: new[] { resourceDescription },
                    embeddedTexts: embeddedTexts,
                    options: emitOptions);

                // If it was not successful, throw an exception to fail the test
                if (!result.Success)
                {
                    var stringBuilder = new StringBuilder();
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        stringBuilder.AppendLine(diagnostic.ToString());
                    }

                    throw new InvalidOperationException(stringBuilder.ToString());
                }

                dynamicallyCompiledAssembly = Assembly.Load(memoryStream.ToArray(), pdbStream.ToArray());
            }
            else
            {
                var result = compilation.Emit(memoryStream, manifestResources: new[] { resourceDescription });
                dynamicallyCompiledAssembly = Assembly.Load(memoryStream.ToArray());
            }

            stream.Dispose();

            return dynamicallyCompiledAssembly;
        }

        private static string ReadResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceCsName = $"{DefaultNamespace}.{resourceName}";

            using (var stream = assembly.GetManifestResourceStream(resourceCsName))
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }

        private static Stream OpenResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceCsName = $"{DefaultNamespace}.{resourceName}";

            return assembly.GetManifestResourceStream(resourceCsName);
        }

        private static IEnumerable<MetadataReference> GetReferences()
        {
            // Tested on .NET 4.8, .NET Core 3.1, and the latest .NET 5.0

            var entry = Assembly.GetEntryAssembly();
            var applicationType = entry.GetTypes().FirstOrDefault(t =>
            {
                // Clients might be using libraries that extends
                // System.Windows.Application
                // so we must check the whole class tree.
                do
                {
                    Debug.WriteLine(t.Name);
                    if (t.FullName == "System.Windows.Application")
                    {
                        return true;
                    }
                    t = t.BaseType;
                }
                while (t != null);

                return false;
            });

            if (applicationType == null)
            {
                throw new InvalidOperationException("This program is not a valid WPF application. Check your app's App.xaml");
            }

            var presentationFrameworkAssembly = applicationType.Assembly;

            // This is safe, `Assembly`s have their `GetHashCode` method overridden.
            var result = new HashSet<Assembly>();

            // Running a BFS, a classic thing for CPers.
            var q = new Queue<Assembly>();
            q.Enqueue(presentationFrameworkAssembly);

            while (q.Count > 0)
            {
                var asm = q.Dequeue();
                foreach (var childName in asm.GetReferencedAssemblies())
                {
                    try
                    {
                        var childAsm = Assembly.Load(childName);
                        if (result.Contains(childAsm))
                        {
                            continue;
                        }

                        q.Enqueue(childAsm);
                        result.Add(childAsm);
                    }
                    catch (FileNotFoundException)
                    {
                        Debug.WriteLine($"Cannot find {childName}");
                    }
                }
            }

            return result.Select(asm => { try { return asm.ToMetadataReference(); } catch { return null; } })
                         .Where(meta => meta != null);
        }

        private static MetadataReference ToMetadataReference(this Assembly asm)
        {
            return MetadataReference.CreateFromFile(asm.Location);
        }
    }
}
