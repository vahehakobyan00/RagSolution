namespace RagLib.TextExtractors
{
    public class TextExtractorFactory : ITextExtractorFactory
    {
        public ITextExtractor GetTextExtractor(string extention)
        {
            ITextExtractor extractor = extention switch
            {
                ".pdf" => new PdfTextExtractor(),
                ".docx" => new DocxTextExtractorOpenXml(),
                ".txt" => new TxtTextExtractor(),
                _ => throw new NotSupportedException("Only .txt, .pdf and .docx.")
            };
            return extractor;
        }
    }
}
