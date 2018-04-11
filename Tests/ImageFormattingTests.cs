using Axinom.Toolkit;
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
    public sealed class ImageFormattingTests : TestClassBase
    {
        // Default image formats to use unless overridden by special test.
        private static readonly ImageFormat[] DefaultFormats = new[]
        {
            new ImageFormat
            {
                Name = "1",
                Width = 1,
                Height = 1
            },
            new ImageFormat
            {
                Name = "22",
                Width = 22,
                Height = 22
            },
            new ImageFormat
            {
                Name = "333",
                Width = 333,
                Height = 333
            },
            new ImageFormat
            {
                Name = "Banner",
                Width = 500,
                Height = 200
            },
            new ImageFormat
            {
                Name = "Banner square",
                Width = 200,
                Height = 200
            },
            new ImageFormat
            {
                Name = "Tall",
                Width = 100,
                Height = 1000
            },
            new ImageFormat
            {
                Name = "Wide",
                Width = 1000,
                Height = 100
            }
        };

        private static readonly Uri DefaultImageUrl = new Uri("http://images.local/picture.jpg");
        private const string DefaultPublishContainerName = "FormattedImages";
        private const string DefaultNamePrefix = "Image";

        [DataTestMethod]
        [DataRow("exit_left.png")]
        [DataRow("exit_left_tiny.jpg")]
        [DataRow("planet_mars_4k_8k-HD.jpg")]
        public async Task Format_WithVariousImages_GeneratesAndPublishForAllFormats(string filename)
        {
            var inputPath = ResolveTestDataPath(filename);
            var inputBytes = File.ReadAllBytes(inputPath);

            var downloader = new FakeImageDownloader();
            downloader.ImageBytes = inputBytes;

            var publisher = new FakeFormattedImagePublisher();

            var formattedImages = await ImageFormatting.FormatAsync(DefaultImageUrl, DefaultPublishContainerName, DefaultNamePrefix, DefaultFormats, downloader, publisher, default);

            Assert.AreEqual(DefaultFormats.Length, formattedImages.Length);
            Assert.AreEqual(DefaultFormats.Length, publisher.PublishedImages.Count);

            var filenames = publisher.PublishedImages.Select(i => i.publishFilename).ToArray();
            var uniqueFilenames = filenames.Distinct().ToArray();

            Assert.AreEqual(uniqueFilenames.Length, filenames.Length);

            foreach (var name in filenames)
                StringAssert.EndsWith(name, ".jpg");
        }

        [TestMethod]
        public async Task Format_With404Image_ThrowsExceptionAndPublishesNothing()
        {
            var downloader = new FakeImageDownloader();
            var publisher = new FakeFormattedImagePublisher();

            try
            {
                await ImageFormatting.FormatAsync(DefaultImageUrl, DefaultPublishContainerName, DefaultNamePrefix, DefaultFormats, downloader, publisher, default);
            }
            catch (Exception ex)
            {
                Log.Default.Debug($"Got expected exception: {ex}");

                Assert.AreEqual(0, publisher.PublishedImages.Count);

                return;
            }

            Assert.Fail("No exception was thrown for 404 image - silently ignored? Bad code!");
        }

        [TestMethod]
        public async Task Format_WithGif_ThrowsExceptionAndPublishesNothing()
        {
            var downloader = new FakeImageDownloader();
            downloader.ImageBytes = File.ReadAllBytes(ResolveTestDataPath("loading.gif"));

            var publisher = new FakeFormattedImagePublisher();

            try
            {
                await ImageFormatting.FormatAsync(DefaultImageUrl, DefaultPublishContainerName, DefaultNamePrefix, DefaultFormats, downloader, publisher, default);
            }
            catch (Exception ex)
            {
                Log.Default.Debug($"Got expected exception: {ex}");

                Assert.AreEqual(0, publisher.PublishedImages.Count);

                return;
            }

            Assert.Fail("No exception was thrown for GIF input - silently ignored? Bad code!");
        }

        [TestMethod]
        public async Task Format_WithFailedPublish_ThrowsException()
        {
            var downloader = new FakeImageDownloader();
            downloader.ImageBytes = File.ReadAllBytes(ResolveTestDataPath("exit_left.png"));

            var publisher = new FakeFormattedImagePublisher();
            publisher.AlwaysFails = true;

            try
            {
                await ImageFormatting.FormatAsync(DefaultImageUrl, DefaultPublishContainerName, DefaultNamePrefix, DefaultFormats, downloader, publisher, default);
            }
            catch (Exception ex)
            {
                Log.Default.Debug($"Got expected exception: {ex}");

                Assert.IsTrue(publisher.PublishedImages.Count > 0, "Did not find any attempts to publish any images");

                return;
            }

            Assert.Fail("No exception was thrown for failed publish - silently ignored? Bad code!");
        }

        [TestMethod]
        public async Task Format_WithNotAnImage_ThrowsException()
        {
            var downloader = new FakeImageDownloader();
            downloader.ImageBytes = File.ReadAllBytes(ResolveTestDataPath("dash.js"));

            var publisher = new FakeFormattedImagePublisher();

            try
            {
                await ImageFormatting.FormatAsync(DefaultImageUrl, DefaultPublishContainerName, DefaultNamePrefix, DefaultFormats, downloader, publisher, default);
            }
            catch (Exception ex)
            {
                Log.Default.Debug($"Got expected exception: {ex}");

                Assert.AreEqual(0, publisher.PublishedImages.Count);
                return;
            }

            Assert.Fail("No exception was thrown for non-image file - silently ignored? Bad code!");
        }

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
            using (var image = Image.Load(publisher.PublishedImages.Single().imageBytes))
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
