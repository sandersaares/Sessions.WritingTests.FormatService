using Axinom.Toolkit;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace FormatService
{
    public sealed class ImageMagick : IDisposable
    {
        public Task ResizeAsync(string sourcePath, string destinationPath, int width, int height)
        {
            using (var inputFile = File.OpenRead(sourcePath))
            using (var reader = new BinaryReader(inputFile))
            {
                if (inputFile.Length >= 5)
                {
                    var prefix = reader.ReadBytes(5);

                    if (prefix[0] == 'G' && prefix[1] == 'I' && prefix[2] == 'F' && prefix[3] == '8' && prefix[4] == '9')
                        throw new NotSupportedException("GIF are not supported");
                }
            }

            return ExternalTool.ExecuteAsync(ExePath, $"\"{sourcePath}\" -resize {width}x{height}^ -gravity center -extent {width}x{height} \"{destinationPath}\"");
        }

        private string ExePath => Path.Combine(_files.Path, "convert.exe");

        public ImageMagick()
        {
            _files = new EmbeddedPackage(Assembly.GetExecutingAssembly(), "FormatService.EmbeddedBinaries", "convert.exe",
                "configure.xml", "delegates.xml", "policy.xml", "thresholds.xml", "type.xml", "type-ghostscript.xml", "colors.xml", "english.xml", "locale.xml", "log.xml", "magic.xml", "mime.xml", "quantization-table.xml");
        }

        private readonly EmbeddedPackage _files;

        public void Dispose()
        {
            _files.Dispose();
        }
    }
}
