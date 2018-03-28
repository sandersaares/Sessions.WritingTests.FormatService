using FormatService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public sealed class ImageFormattingTests
    {
        private static readonly Uri DefaultImageUrl = new Uri("http://images.local/picture.jpg");
        private const string DefaultPublishContainerName = "FormattedImages";
        private const string DefaultNamePrefix = "Image";

        [DataTestMethod]
        [DataRow(10, 10, 100, 100, 10, 10)]
        [DataRow(10, 10, 1000, 1000, 10, 10)]
        [DataRow(10, 10, 20, 20, 10, 10)]
        [DataRow(10, 10, 300, 100, 10, 4)]
        public async Task Format_WithVariousFormatsAndInputs_GeneratesExpectedImageContents(int squaresH, int squaresV, int formatW, int formatH, int expectedSquaresH, int expectedSquaresV)
        {
            // Arrange
            var testImage = GenerateGrid(squaresH, squaresV);

            var format = new ImageFormat
            {
                Name = "test",
                Width = formatW,
                Height = formatH
            };

            IImageDownloader downloader = new FakeImageDownloader
            {
                ImageBytes = testImage
            };

            FakeFormattedImagePublisher publisher = new FakeFormattedImagePublisher();

            // Act
            var formattedImages = await ImageFormatting.FormatAsync(DefaultImageUrl, DefaultPublishContainerName, DefaultNamePrefix, new[] { format }, downloader, publisher, default);

            // Assert
            using (var image = Image.Load(publisher.PublishedImageBytes))
            {
                // Quick sanity check.
                Assert.AreEqual(formatW, image.Width);
                Assert.AreEqual(formatH, image.Height);

                // Count number of different colors found in first row/column.
                // We only look at once channel to keep it simple.
                //
                // As we encode into JPEG, there can be slight deviation and resizing will blend.
                // Therefore we just round to black or white to keep it simple.
                int horizontalColorCount = 1;

                for (var x = 1; x < image.Width; x++)
                {
                    if (!PixelBlackWhiteMatch(image[x, 0], image[x - 1, 0]))
                        horizontalColorCount++;
                }

                int verticalColorCount = 1;

                for (var y = 1; y < image.Height; y++)
                {
                    if (!PixelBlackWhiteMatch(image[0, y], image[0, y - 1]))
                        verticalColorCount++;
                }

                Assert.AreEqual(expectedSquaresH, horizontalColorCount);
                Assert.AreEqual(expectedSquaresV, verticalColorCount);
            }
        }

        private static bool PixelBlackWhiteMatch(Rgba32 a, Rgba32 b)
        {
            var roundedA = (int)Math.Round(a.R / 255.0);
            var roundedB = (int)Math.Round(b.R / 255.0);

            return roundedA == roundedB;
        }

        private static readonly GraphicsOptions _drawOptions = new GraphicsOptions
        {
            Antialias = false
        };

        public static byte[] GenerateGrid(int squaresH, int squaresV)
        {
            using (var image = new Image<Rgba32>(squaresH * 10, squaresV * 10))
            {
                image.Mutate(x => x.Fill(Rgba32.White));

                for (var y = 0; y < squaresV; y++)
                    for (var x = 0; x < squaresH; x++)
                    {
                        // We fill with black if the evenness is the same on both axes.
                        if (y % 2 != x % 2)
                            continue;

                        image.Mutate(_ => _.FillPolygon(Rgba32.Black, new[]
                        {
                            new PointF(x * 10, y * 10),
                            new PointF((x + 1) * 10, y * 10),
                            new PointF((x + 1) * 10, (y + 1) * 10),
                            new PointF(x * 10, (y + 1) * 10),
                        }, _drawOptions));
                    }

                using (var buffer = new MemoryStream())
                {
                    image.SaveAsPng(buffer);

                    return buffer.ToArray();
                }
            }
        }
    }
}
