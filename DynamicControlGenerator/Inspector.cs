using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DynamicControlGenerator
{
    public class Inspector
    {
        public static void Inspect()
        {
            var currentAssembly = Assembly.GetCallingAssembly();
            foreach (var type in currentAssembly.GetTypes())
            {
                Debug.WriteLine(type);
            }
            //foreach (var asm in currentAssembly.GetReferencedAssemblies())
            //{
            //    Debug.WriteLine(asm);
            //}
        }

        public static void Print()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceCsName = "DynamicControlGenerator.ToastNotificationLoader.cs";
            var resourceXamlName = "DynamicControlGenerator.ToastNotificationXaml.xaml";

            using (var stream = assembly.GetManifestResourceStream(resourceCsName))
            using (var sr = new StreamReader(stream))
            {
                Debug.WriteLine(sr.ReadToEnd());
            }

            using (var stream = assembly.GetManifestResourceStream(resourceXamlName))
            using (var sr = new StreamReader(stream))
            {
                Debug.WriteLine(sr.ReadToEnd());
            }
        }
    }
}
