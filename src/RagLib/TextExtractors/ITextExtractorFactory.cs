namespace RagLib.TextExtractors
{
    public interface ITextExtractorFactory
    {
        ITextExtractor GetTextExtractor(string extention);
    }
}
