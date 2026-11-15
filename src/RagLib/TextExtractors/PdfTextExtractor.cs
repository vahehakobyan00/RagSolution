using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace RagLib.TextExtractors
{
    /// <summary>
    /// PdfPig-based extractor that concatenates page text and saves images.
    /// Use with PdfPig 0.1.11 where IPdfImage exposes:
    ///   - bool TryGetPng(out ReadOnlySpan&lt;byte&gt;)
    ///   - ReadOnlySpan&lt;byte&gt; RawBytes
    ///
    /// Constructor:
    ///     new PdfTextExtractor(imagesRoot, imagesBaseUrl)
    /// imagesRoot:    Absolute folder on disk where images will be saved (or null to skip saving).
    /// imagesBaseUrl: URL prefix (e.g., "/rag-images/{docId}") to expose saved files (or null to skip).
    ///
    /// Returned meta keys:
    ///     meta["page_spans"] = List<Dictionary<string, object>> { { "page", int }, { "start", int }, { "end", int } }
    ///     meta["images"]     = List<Dictionary<string, object>> { { "page", int }, { "url", string }, { "file", string } }
    /// </summary>
    public sealed class PdfTextExtractor : ITextExtractor
    {
        private readonly string? _imagesRoot;     // absolute disk path where images are written
        private readonly string? _imagesBaseUrl;  // public URL prefix for served images

        public PdfTextExtractor(/*string? imagesRoot = null, string? imagesBaseUrl = null*/)
        {
            _imagesRoot = System.IO.Path.Combine("D:\\", "files");
            _imagesBaseUrl = System.IO.Path.Combine("D:\\", "files");//imagesBaseUrl?.TrimEnd('/');
        }

        public Task<(string, Dictionary<string, object>)> ExtractAsync(string path, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException("PDF not found", path);
            //path = System.IO.Path.Combine("D:\\", "file.pdf");
            var sb = new StringBuilder(capacity: 1 << 18);
            var pageSpans = new List<Dictionary<string, object>>(256);
            var imagesMeta = new List<Dictionary<string, object>>(256);

            using var doc = PdfDocument.Open(path);
            var pageCount = doc.NumberOfPages;

            for (int p = 1; p <= pageCount; p++)
            {
                ct.ThrowIfCancellationRequested();

                var page = doc.GetPage(p);

                // mark start
                int start = sb.Length;

                // header + text
                sb.AppendLine($"[Page {p}/{pageCount}]");
                var pageText = ContentOrderTextExtractor.GetText(page);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    sb.AppendLine(pageText.TrimEnd());
                }
                sb.AppendLine();

                // mark end
                int end = sb.Length;

                pageSpans.Add(new Dictionary<string, object>
                {
                    ["page"] = p,
                    ["start"] = start,
                    ["end"] = end
                });

                // images
                int i = 0;
                foreach (var img in page.GetImages())
                {
                    ct.ThrowIfCancellationRequested();

                    // Fallback to raw embedded bytes (often JPEG/JP2/JBIG2/Flate/etc.)
                    ReadOnlySpan<byte> raw = img.RawBytes;
                    if (!raw.IsEmpty)
                    {
                        var ext = GuessExt(raw); // .jpg/.png/.jp2/.j2c/.jbig2/.bin
                        if (ext != ".bin")
                        {
                            imagesMeta.Add(new Dictionary<string, object>
                            {
                                ["page"] = p,
                                ["bytes"] = Convert.ToBase64String(img.RawBytes)
                            });
                        }
                    }

                    i++;
                }
            }

            var meta = new Dictionary<string, object>
            {
                ["source"] = "pdf",
                ["file"] = Path.GetFileName(path),
                ["pages"] = pageCount,
                ["page_spans"] = pageSpans,
                ["images"] = imagesMeta
            };

            return Task.FromResult((sb.ToString(), meta));
        }

        public Task<(string, Dictionary<string, object>)> ExtractAsyncWithSavingImagesForTesting(string path, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException("PDF not found", path);
            path =  System.IO.Path.Combine("D:\\","file.pdf");
            var sb = new StringBuilder(capacity: 1 << 18);
            var pageSpans = new List<Dictionary<string, object>>(256);
            var imagesMeta = new List<Dictionary<string, object>>(256);

            using var doc = PdfDocument.Open(path);
            var pageCount = doc.NumberOfPages;

            // Ensure images folder exists if we plan to write images
            if (_imagesRoot is not null && _imagesBaseUrl is not null)
            {
                Directory.CreateDirectory(_imagesRoot);
            }

            for (int p = 1; p <= pageCount; p++)
            {
                ct.ThrowIfCancellationRequested();

                var page = doc.GetPage(p);

                // mark start
                int start = sb.Length;

                // header + text
                sb.AppendLine($"[Page {p}/{pageCount}]");
                var pageText = ContentOrderTextExtractor.GetText(page);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    sb.AppendLine(pageText.TrimEnd());
                }
                sb.AppendLine();

                // mark end
                int end = sb.Length;

                pageSpans.Add(new Dictionary<string, object>
                {
                    ["page"] = p,
                    ["start"] = start,
                    ["end"] = end
                });

                // images
                if (_imagesRoot is not null && _imagesBaseUrl is not null)
                {
                    int i = 0;
                    foreach (var img in page.GetImages())
                    {
                        ct.ThrowIfCancellationRequested();

                        var baseFile = $"p{p}_{i}";
                        string? writtenPath = null;

                        // Prefer decoded PNG if available
                        //if (img.TryGetPng(out ReadOnlySpan<byte> png) && !png.IsEmpty)
                        //{
                        //    writtenPath = Path.Combine(_imagesRoot, baseFile + ".png");
                        //    WriteSpan(writtenPath, png);
                        //}
                        //else
                        {
                            // Fallback to raw embedded bytes (often JPEG/JP2/JBIG2/Flate/etc.)
                            ReadOnlySpan<byte> raw = img.RawBytes;
                            if (!raw.IsEmpty)
                            {
                                var ext = GuessExt(raw); // .jpg/.png/.jp2/.j2c/.jbig2/.bin
                                if (ext != ".bin")
                                {
                                    writtenPath = Path.Combine(_imagesRoot, baseFile + ext);
                                    WriteSpan(writtenPath, raw);
                                }
                            }
                        }

                        if (writtenPath is not null)
                        {
                            var fileName = Path.GetFileName(writtenPath);
                            var url = $"{_imagesBaseUrl}/{fileName}";

                            imagesMeta.Add(new Dictionary<string, object>
                            {
                                ["page"] = p,
                                ["url"] = url,
                                ["file"] = writtenPath,
                                ["bytes"] = Convert.ToBase64String(img.RawBytes)
                            });
                        }

                        i++;
                    }
                }
            }

            var meta = new Dictionary<string, object>
            {
                ["source"] = "pdf",
                ["file"] = Path.GetFileName(path),
                ["pages"] = pageCount,
                ["page_spans"] = pageSpans,
                ["images"] = imagesMeta
            };

            return Task.FromResult((sb.ToString(), meta));
        }

        public Task<(string, Dictionary<string, object>)> ExtractAsync(Stream stream, string fileName, CancellationToken ct)
        {
            using var doc = PdfDocument.Open(stream);
            var sb = new StringBuilder();

            foreach (var page in doc.GetPages())
            {
                var text = ContentOrderTextExtractor.GetText(page);
                sb.AppendLine(text);
                sb.AppendLine();
            }

            var meta = new Dictionary<string, object>
            {
                ["source"] = "pdf",
                ["file"] = fileName,
                ["pages"] = doc.NumberOfPages
            };

            return Task.FromResult((sb.ToString(), meta));
        }

        // --- Helpers ---

        /// <summary>
        /// File.WriteAllBytes requires byte[]; convert once from ReadOnlySpan&lt;byte&gt;.
        /// </summary>
        private static void WriteSpan(string path, ReadOnlySpan<byte> data)
            => File.WriteAllBytes(path, data.ToArray());

        /// <summary>
        /// Lightweight signature sniffing to choose a reasonable extension for raw streams.
        /// </summary>
        private static string GuessExt(ReadOnlySpan<byte> data)
        {
            // JPEG SOI: FF D8
            if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8) return ".jpg";
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return ".png";
            // JPEG2000 JP2 box: bytes 4..7 = 'jP  '
            if (data.Length >= 12 && data[4] == 0x6A && data[5] == 0x50 && data[6] == 0x20 && data[7] == 0x20) return ".jp2";
            // JPEG2000 codestream: FF 4F FF 51
            if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0x4F && data[2] == 0xFF && data[3] == 0x51) return ".j2c";
            // JBIG2 magic: 97 4A 42 32 0D 0A 1A 0A
            if (data.Length >= 8 && data[0] == 0x97 && data[1] == 0x4A && data[2] == 0x42 && data[3] == 0x32 &&
                data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A) return ".jbig2";

            // Unknown/unsupported container (e.g., CCITT/Flate raw bitmap)
            return ".bin";
        }
    }
}



