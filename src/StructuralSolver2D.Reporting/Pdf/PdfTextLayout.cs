using System.Globalization;
using System.Text;

namespace StructuralSolver2D.Reporting.Pdf;

internal static class PdfTextLayout
{
    // Helvetica advances in 1/1000 em (Adobe AFM); Courier has a fixed 600-unit advance.
    // https://adobe-type-tools.github.io/font-tech-notes/pdfs/5004.AFM_Spec.pdf
    private static readonly int[] HelveticaAscii =
    {
        278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278,
        556,556,556,556,556,556,556,556,556,556,278,278,584,584,584,556,
        1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,
        667,778,722,667,611,722,667,944,667,667,611,278,278,278,469,556,
        333,556,556,500,556,556,278,556,556,222,222,500,222,833,556,556,
        556,556,333,500,278,556,500,722,500,500,500,334,260,334,584
    };
    private static readonly Encoding WinAnsi = CodePagesEncodingProvider.Instance.GetEncoding(1252,
        EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)!;

    public static double Width(string text, double fontSize, bool monospace = false) =>
        text.Sum(character => monospace ? 600 : Advance(character)) * fontSize / 1000;

    private static int Advance(char character)
    {
        if (character is >= ' ' and <= '~') return HelveticaAscii[character - ' '];
        if (character is >= '\u00ec' and <= '\u00ef') return 278;
        string decomposed = character.ToString().Normalize(NormalizationForm.FormD);
        if (decomposed.Length > 1 && decomposed[0] is >= ' ' and <= '~') return HelveticaAscii[decomposed[0] - ' '];
        // Conservative maximum for other WinAnsi glyphs; never underestimate the right edge.
        return character == '\u00a0' ? 278 : 1015;
    }

    public static IEnumerable<string> Wrap(string text, double fontSize, bool monospace, double availableWidth)
    {
        foreach (string logicalLine in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Replace('\t', ' ').Split('\n'))
        {
            string remaining = logicalLine;
            while (Width(remaining, fontSize, monospace) > availableWidth)
            {
                double width = 0;
                int count = 0;
                while (count < remaining.Length)
                {
                    double next = Width(remaining[count].ToString(), fontSize, monospace);
                    if (width + next > availableWidth) break;
                    width += next;
                    count++;
                }
                count = Math.Max(1, count);
                int space = remaining.LastIndexOf(' ', count - 1, count);
                int split = space > 0 ? space : count;
                yield return remaining[..split].TrimEnd();
                remaining = remaining[split..].TrimStart();
            }
            yield return remaining;
        }
    }

    public static string Escape(string text)
    {
        byte[] bytes;
        try { bytes = WinAnsi.GetBytes(text); }
        catch (EncoderFallbackException exception)
        {
            throw new NotSupportedException("PDF text must use Windows-1252 characters. Use Markdown or XLSX for other scripts.", exception);
        }
        var result = new StringBuilder();
        foreach (byte value in bytes)
        {
            if (value is (byte)'(' or (byte)')' or (byte)'\\') result.Append('\\').Append((char)value);
            else if (value is >= 32 and <= 126) result.Append((char)value);
            else result.Append('\\').Append(Convert.ToString(value, 8).PadLeft(3, '0'));
        }
        return result.ToString();
    }
}
