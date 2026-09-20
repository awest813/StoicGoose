using StoicGoose.Common.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StoicGoose.Common.Utilities
{
    public static class Resources
    {
        private static Stream GetEmbeddedResourceStream(string name)
        {
            var assemblies = new Assembly[] { Assembly.GetEntryAssembly(), typeof(Resources).Assembly }
                .Where(assembly => assembly != null)
                .Distinct();

            foreach (var assembly in assemblies)
            {
                var resourceName = $"{assembly.GetName().Name}.{name}";
                var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                    return stream;
            }

            return null;
        }

        public static RgbaFile GetEmbeddedRgbaFile(string name)
        {
            using var stream = GetEmbeddedResourceStream(name);
            if (stream == null) return null;
            return new RgbaFile(stream);
        }

        public static string GetEmbeddedText(string name)
        {
            using var stream = GetEmbeddedResourceStream(name);
            if (stream == null) return string.Empty;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static byte[] GetEmbeddedRawData(string name)
        {
            using var stream = GetEmbeddedResourceStream(name);
            if (stream == null) return null;
            var buffer = new byte[stream.Length];
            stream.ReadExactly(buffer);
            return buffer;
        }
    }
}
