using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Presentation;
using ExcelDataReader;
using UglyToad.PdfPig;
using RtfPipe;

namespace OllamaChat.Services;

/// <summary>
/// Service for processing and extracting text from various document formats
/// </summary>
public class DocumentProcessingService : IDocumentProcessingService
{
    private static readonly Dictionary<string, DocumentType> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Plain text
        { ".txt", DocumentType.PlainText },
        { ".log", DocumentType.PlainText },
        { ".ini", DocumentType.PlainText },
        { ".cfg", DocumentType.PlainText },
        { ".conf", DocumentType.PlainText },

        // Markdown
        { ".md", DocumentType.Markdown },
        { ".markdown", DocumentType.Markdown },

        // Data formats
        { ".json", DocumentType.Json },
        { ".xml", DocumentType.Xml },
        { ".html", DocumentType.Html },
        { ".htm", DocumentType.Html },
        { ".csv", DocumentType.Csv },
        { ".tsv", DocumentType.Csv },

        // PDF
        { ".pdf", DocumentType.Pdf },

        // Word documents
        { ".docx", DocumentType.WordDocument },
        { ".doc", DocumentType.WordDocumentLegacy },

        // Excel spreadsheets
        { ".xlsx", DocumentType.ExcelSpreadsheet },
        { ".xls", DocumentType.ExcelSpreadsheetLegacy },

        // PowerPoint presentations
        { ".pptx", DocumentType.PowerPointPresentation },
        { ".ppt", DocumentType.PowerPointPresentationLegacy },

        // Rich text
        { ".rtf", DocumentType.RichText },

        // Source code
        { ".cs", DocumentType.SourceCode },
        { ".py", DocumentType.SourceCode },
        { ".js", DocumentType.SourceCode },
        { ".ts", DocumentType.SourceCode },
        { ".jsx", DocumentType.SourceCode },
        { ".tsx", DocumentType.SourceCode },
        { ".java", DocumentType.SourceCode },
        { ".cpp", DocumentType.SourceCode },
        { ".c", DocumentType.SourceCode },
        { ".h", DocumentType.SourceCode },
        { ".hpp", DocumentType.SourceCode },
        { ".go", DocumentType.SourceCode },
        { ".rs", DocumentType.SourceCode },
        { ".rb", DocumentType.SourceCode },
        { ".php", DocumentType.SourceCode },
        { ".swift", DocumentType.SourceCode },
        { ".kt", DocumentType.SourceCode },
        { ".scala", DocumentType.SourceCode },
        { ".sql", DocumentType.SourceCode },
        { ".sh", DocumentType.SourceCode },
        { ".bash", DocumentType.SourceCode },
        { ".ps1", DocumentType.SourceCode },
        { ".yaml", DocumentType.SourceCode },
        { ".yml", DocumentType.SourceCode },
        { ".toml", DocumentType.SourceCode },
        { ".vue", DocumentType.SourceCode },
        { ".svelte", DocumentType.SourceCode },
        { ".css", DocumentType.SourceCode },
        { ".scss", DocumentType.SourceCode },
        { ".sass", DocumentType.SourceCode },
        { ".less", DocumentType.SourceCode }
    };

    static DocumentProcessingService()
    {
        // Required for ExcelDataReader to work with .xls files
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public bool CanProcess(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return ExtensionMap.ContainsKey(extension);
    }

    public DocumentType GetDocumentType(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return ExtensionMap.TryGetValue(extension, out var docType) ? docType : DocumentType.Unknown;
    }

    public IReadOnlyCollection<string> GetSupportedExtensions()
    {
        return ExtensionMap.Keys.ToList().AsReadOnly();
    }

    public async Task<DocumentProcessingResult> ExtractTextAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new DocumentProcessingResult
            {
                IsSuccess = false,
                ErrorMessage = $"File not found: {filePath}"
            };
        }

        var content = await File.ReadAllBytesAsync(filePath);
        return await ExtractTextAsync(content, Path.GetFileName(filePath));
    }

    public async Task<DocumentProcessingResult> ExtractTextAsync(byte[] content, string fileName)
    {
        var documentType = GetDocumentType(fileName);
        var result = new DocumentProcessingResult { DocumentType = documentType };

        try
        {
            result.ExtractedText = documentType switch
            {
                DocumentType.Pdf => await ExtractFromPdfAsync(content, result),
                DocumentType.WordDocument => await ExtractFromDocxAsync(content),
                DocumentType.WordDocumentLegacy => ExtractFromLegacyDoc(content),
                DocumentType.ExcelSpreadsheet => await ExtractFromXlsxAsync(content, result),
                DocumentType.ExcelSpreadsheetLegacy => await ExtractFromXlsAsync(content, result),
                DocumentType.PowerPointPresentation => await ExtractFromPptxAsync(content),
                DocumentType.PowerPointPresentationLegacy => ExtractFromLegacyPpt(content),
                DocumentType.RichText => ExtractFromRtf(content),
                DocumentType.Html => ExtractFromHtml(content),
                DocumentType.PlainText or DocumentType.Markdown or DocumentType.Json
                    or DocumentType.Xml or DocumentType.Csv or DocumentType.SourceCode
                    => await ExtractTextFileAsync(content),
                _ => await ExtractTextFileAsync(content)
            };

            result.IsSuccess = true;
            result.CharacterCount = result.ExtractedText.Length;
            result.WordCount = CountWords(result.ExtractedText);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"Failed to extract text: {ex.Message}";
        }

        return result;
    }

    private static async Task<string> ExtractFromPdfAsync(byte[] content, DocumentProcessingResult result)
    {
        return await Task.Run(() =>
        {
            using var stream = new MemoryStream(content);
            using var document = PdfDocument.Open(stream);

            result.PageCount = document.NumberOfPages;
            var textBuilder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                textBuilder.AppendLine(pageText);
                textBuilder.AppendLine(); // Separator between pages
            }

            return textBuilder.ToString().Trim();
        });
    }

    private static async Task<string> ExtractFromDocxAsync(byte[] content)
    {
        return await Task.Run(() =>
        {
            using var stream = new MemoryStream(content);
            using var document = WordprocessingDocument.Open(stream, false);

            var body = document.MainDocumentPart?.Document?.Body;
            if (body == null)
                return string.Empty;

            var textBuilder = new StringBuilder();

            foreach (var paragraph in body.Elements<Paragraph>())
            {
                var paragraphText = GetParagraphText(paragraph);
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    textBuilder.AppendLine(paragraphText);
                }
            }

            // Also extract from tables
            foreach (var table in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>())
            {
                textBuilder.AppendLine(ExtractTableText(table));
            }

            return textBuilder.ToString().Trim();
        });
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        var textBuilder = new StringBuilder();

        foreach (var run in paragraph.Elements<Run>())
        {
            foreach (var text in run.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>())
            {
                textBuilder.Append(text.Text);
            }
        }

        return textBuilder.ToString();
    }

    private static string ExtractTableText(DocumentFormat.OpenXml.Wordprocessing.Table table)
    {
        var textBuilder = new StringBuilder();

        foreach (var row in table.Elements<TableRow>())
        {
            var cells = new List<string>();
            foreach (var cell in row.Elements<TableCell>())
            {
                var cellText = new StringBuilder();
                foreach (var paragraph in cell.Elements<Paragraph>())
                {
                    cellText.Append(GetParagraphText(paragraph));
                }
                cells.Add(cellText.ToString().Trim());
            }
            textBuilder.AppendLine(string.Join(" | ", cells));
        }

        return textBuilder.ToString();
    }

    private static string ExtractFromLegacyDoc(byte[] content)
    {
        // Legacy .doc files are binary and require more complex parsing
        // For now, try to extract any readable text
        try
        {
            var text = Encoding.UTF8.GetString(content);
            // Filter out binary data, keeping only printable characters
            var printableText = new StringBuilder();
            var wordBuffer = new StringBuilder();

            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c))
                {
                    wordBuffer.Append(c);
                }
                else if (wordBuffer.Length > 0)
                {
                    var word = wordBuffer.ToString().Trim();
                    if (word.Length > 2 && word.Any(char.IsLetter))
                    {
                        printableText.Append(word);
                        printableText.Append(' ');
                    }
                    wordBuffer.Clear();
                }
            }

            return printableText.ToString().Trim();
        }
        catch
        {
            return "[Legacy .doc format - limited text extraction available]";
        }
    }

    private static async Task<string> ExtractFromXlsxAsync(byte[] content, DocumentProcessingResult result)
    {
        return await Task.Run(() =>
        {
            using var stream = new MemoryStream(content);
            using var document = SpreadsheetDocument.Open(stream, false);

            var workbookPart = document.WorkbookPart;
            if (workbookPart == null)
                return string.Empty;

            var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>().ToList();
            result.SheetCount = sheets?.Count ?? 0;

            var textBuilder = new StringBuilder();
            var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable
                .Elements<SharedStringItem>()
                .Select(s => s.InnerText)
                .ToArray() ?? Array.Empty<string>();

            foreach (var sheet in sheets ?? Enumerable.Empty<Sheet>())
            {
                textBuilder.AppendLine($"=== Sheet: {sheet.Name} ===");

                var worksheetPart = workbookPart.GetPartById(sheet.Id!) as WorksheetPart;
                if (worksheetPart?.Worksheet?.GetFirstChild<SheetData>() is not { } sheetData)
                    continue;

                foreach (var row in sheetData.Elements<Row>())
                {
                    var rowValues = new List<string>();
                    foreach (var cell in row.Elements<Cell>())
                    {
                        var cellValue = GetCellValue(cell, sharedStrings);
                        rowValues.Add(cellValue);
                    }
                    textBuilder.AppendLine(string.Join("\t", rowValues));
                }

                textBuilder.AppendLine();
            }

            return textBuilder.ToString().Trim();
        });
    }

    private static string GetCellValue(Cell cell, string[] sharedStrings)
    {
        var value = cell.CellValue?.Text ?? string.Empty;

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            if (int.TryParse(value, out var index) && index < sharedStrings.Length)
            {
                return sharedStrings[index];
            }
        }

        return value;
    }

    private static async Task<string> ExtractFromXlsAsync(byte[] content, DocumentProcessingResult result)
    {
        return await Task.Run(() =>
        {
            using var stream = new MemoryStream(content);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var textBuilder = new StringBuilder();
            var sheetIndex = 0;

            do
            {
                textBuilder.AppendLine($"=== Sheet {++sheetIndex}: {reader.Name} ===");

                while (reader.Read())
                {
                    var rowValues = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.GetValue(i)?.ToString() ?? string.Empty;
                        rowValues.Add(value);
                    }
                    textBuilder.AppendLine(string.Join("\t", rowValues));
                }

                textBuilder.AppendLine();
            } while (reader.NextResult());

            result.SheetCount = sheetIndex;
            return textBuilder.ToString().Trim();
        });
    }

    private static async Task<string> ExtractFromPptxAsync(byte[] content)
    {
        return await Task.Run(() =>
        {
            using var stream = new MemoryStream(content);
            using var document = PresentationDocument.Open(stream, false);

            var presentationPart = document.PresentationPart;
            if (presentationPart == null)
                return string.Empty;

            var textBuilder = new StringBuilder();
            var slideIndex = 0;

            foreach (var slidePart in presentationPart.SlideParts)
            {
                textBuilder.AppendLine($"=== Slide {++slideIndex} ===");

                var slide = slidePart.Slide;
                foreach (var shape in slide.Descendants<DocumentFormat.OpenXml.Presentation.Shape>())
                {
                    var textBody = shape.TextBody;
                    if (textBody != null)
                    {
                        foreach (var paragraph in textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>())
                        {
                            var paragraphText = string.Join("",
                                paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>()
                                    .SelectMany(r => r.Elements<DocumentFormat.OpenXml.Drawing.Text>())
                                    .Select(t => t.Text));

                            if (!string.IsNullOrWhiteSpace(paragraphText))
                            {
                                textBuilder.AppendLine(paragraphText);
                            }
                        }
                    }
                }

                // Extract notes if present
                var notesSlidePart = slidePart.NotesSlidePart;
                if (notesSlidePart != null)
                {
                    textBuilder.AppendLine("[Notes:]");
                    foreach (var text in notesSlidePart.NotesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                    {
                        if (!string.IsNullOrWhiteSpace(text.Text))
                        {
                            textBuilder.AppendLine(text.Text);
                        }
                    }
                }

                textBuilder.AppendLine();
            }

            return textBuilder.ToString().Trim();
        });
    }

    private static string ExtractFromLegacyPpt(byte[] content)
    {
        // Legacy .ppt files are binary and require more complex parsing
        try
        {
            var text = Encoding.UTF8.GetString(content);
            var printableText = new StringBuilder();
            var wordBuffer = new StringBuilder();

            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c))
                {
                    wordBuffer.Append(c);
                }
                else if (wordBuffer.Length > 0)
                {
                    var word = wordBuffer.ToString().Trim();
                    if (word.Length > 2 && word.Any(char.IsLetter))
                    {
                        printableText.Append(word);
                        printableText.Append(' ');
                    }
                    wordBuffer.Clear();
                }
            }

            return printableText.ToString().Trim();
        }
        catch
        {
            return "[Legacy .ppt format - limited text extraction available]";
        }
    }

    private static string ExtractFromRtf(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content);
            var html = Rtf.ToHtml(stream);
            return StripHtmlTags(html);
        }
        catch
        {
            // Fallback to basic text extraction
            var text = Encoding.UTF8.GetString(content);
            return StripRtfFormatting(text);
        }
    }

    private static string StripRtfFormatting(string rtfText)
    {
        // Remove RTF control words and groups
        var result = Regex.Replace(rtfText, @"\\[a-z]+(-?\d+)?[ ]?", string.Empty);
        result = Regex.Replace(result, @"[{}]", string.Empty);
        result = Regex.Replace(result, @"\\\'[0-9a-f]{2}", string.Empty);
        return result.Trim();
    }

    private static string ExtractFromHtml(byte[] content)
    {
        var html = Encoding.UTF8.GetString(content);
        return StripHtmlTags(html);
    }

    private static string StripHtmlTags(string html)
    {
        // Remove script and style content
        var result = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"<style[^>]*>[\s\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);

        // Replace block elements with newlines
        result = Regex.Replace(result, @"<(p|div|br|h\d|li|tr)[^>]*>", "\n", RegexOptions.IgnoreCase);

        // Remove remaining tags
        result = Regex.Replace(result, @"<[^>]+>", string.Empty);

        // Decode HTML entities
        result = System.Net.WebUtility.HtmlDecode(result);

        // Clean up whitespace
        result = Regex.Replace(result, @"\n\s*\n", "\n\n");
        result = Regex.Replace(result, @"[ \t]+", " ");

        return result.Trim();
    }

    private static async Task<string> ExtractTextFileAsync(byte[] content)
    {
        // Try UTF-8 first, then fallback to other encodings
        try
        {
            return await Task.FromResult(Encoding.UTF8.GetString(content));
        }
        catch
        {
            try
            {
                return Encoding.Default.GetString(content);
            }
            catch
            {
                return Encoding.ASCII.GetString(content);
            }
        }
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
