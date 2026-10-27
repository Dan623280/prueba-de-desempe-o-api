using PdfSharp.Fonts;

namespace SkillHub.Infrastructure.Service;

public class CertificateFontResolver : IFontResolver
{
    public string DefaultFontName => "DejaVu Sans";
    public byte[] GetFont(string faceName)
    {
        return File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fonts", faceName + ".ttf"));
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new(isBold ? "DejaVuSans-Bold" : "DejaVuSans");
    }
}
