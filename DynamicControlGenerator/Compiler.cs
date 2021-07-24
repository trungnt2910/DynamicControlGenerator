using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        private static readonly string[] CsFileNames = new[] { "ToastNotificationLoader.cs", "Interop.cs", "IconFinder.cs" };
        const string XamlFileName = "ToastNotificationXaml.xaml";

        public static Assembly Compile()
        {
            var syntaxTrees = CsFileNames.Select(name => CSharpSyntaxTree.ParseText(ReadResource(name)));

            var compilation = CSharpCompilation.Create(
                "ToastNotification.Runtime.Wpf",
                syntaxTrees,
                GetReferences().ToArray(),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release)
                );

            var stream = OpenResource(XamlFileName);

            var resourceDescription = new ResourceDescription(
                "Uno.Extras.ToastNotification.ToastNotificationXaml.xaml",
                () => stream,
                true);

            var memoryStream = new MemoryStream();
            var result = compilation.Emit(memoryStream, manifestResources: new[] { resourceDescription });

            stream.Dispose();

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

            // Otherwise load the assembly, instantiate the type via reflection and call CalculateSomething
            var dynamicallyCompiledAssembly = Assembly.Load(memoryStream.ToArray());

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
            yield return GetAssemblyOfSystemPredefined().ToMetadataReference();

            var entry = Assembly.GetEntryAssembly();

            //var wpfCore = new[] { "System.Runtime", "System.Xaml" };

            //foreach (var asmName in entry.GetReferencedAssemblies())
            //{
            //    if (wpfCore.Contains(asmName.Name))
            //    {
            //        yield return Assembly.Load(asmName).ToMetadataReference();
            //    }
            //}

            foreach (var asm in GetAssembliesOfWpf())
            {
                yield return asm.ToMetadataReference();
            }
        }

        private static MetadataReference ToMetadataReference(this Assembly asm)
        {
            return MetadataReference.CreateFromFile(asm.Location);
        }

        private static Assembly GetAssemblyOfSystemPredefined()
        {
            var assembly = Assembly.GetEntryAssembly();

            // The assembly should contain at least a type?
            var type = assembly.GetTypes().FirstOrDefault();

            // All types should inherit System.Object
            while (type.BaseType != null)
            {
                type = type.BaseType;
            }

            return type.Assembly;
        }

        private static IEnumerable<Assembly> GetAssembliesOfWpf()
        {
            //EVEN WORKS ON .NET FRAMEWORK!
            //Tested on .NET Core 3.1, .NET 5.0 and .NET Framework 4.8

            var assembly = Assembly.GetEntryAssembly();

            // This is guaranteed.
            var presentationFrameworkName = assembly.GetReferencedAssemblies()
                .FirstOrDefault(asm => asm.Name == "PresentationFramework");

            if (presentationFrameworkName == null)
            {
                throw new PlatformNotSupportedException("This function must be called from a .NET (Core) WPF app.");
            }

            var presentationFramework = Assembly.Load(presentationFrameworkName);

            yield return presentationFramework;

            foreach (var asm in presentationFramework.GetReferencedAssemblies())
            {
                var asmLoad = Assembly.Load(asm);
                yield return asmLoad;
                if (asm.Name == "System.Xml.ReaderWriter")
                {
                    foreach (var asmChild in asmLoad.GetReferencedAssemblies())
                    {
                        yield return Assembly.Load(asmChild);
                    }
                }
            }
        }
    }
}
