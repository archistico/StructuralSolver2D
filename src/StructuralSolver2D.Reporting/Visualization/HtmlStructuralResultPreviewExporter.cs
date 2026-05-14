using System.Globalization;
using System.Text;

namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Exports a <see cref="StructuralVisualizationModel"/> to a lightweight standalone HTML preview page.
/// </summary>
public sealed class HtmlStructuralResultPreviewExporter
{
    /// <summary>
    /// Exports the supplied visualization model to a complete HTML document with embedded inline SVG.
    /// </summary>
    public string Export(StructuralVisualizationModel model, HtmlPreviewExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        options ??= new HtmlPreviewExportOptions();
        options.SvgOptions.Title = options.Title;

        string svg = new SvgStructuralResultExporter().Export(model, options.SvgOptions);
        string title = EscapeHtml(options.Title);
        string description = EscapeHtml(options.Description);
        StringBuilder builder = new();

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\" />");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine($"  <title>{title}</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { margin: 0; font-family: Arial, Helvetica, sans-serif; background: #f8fafc; color: #0f172a; }");
        builder.AppendLine("    .page { max-width: 1320px; margin: 0 auto; padding: 24px; }");
        builder.AppendLine("    .card { background: #ffffff; border: 1px solid #e5e7eb; border-radius: 12px; padding: 16px; box-shadow: 0 8px 24px rgba(15, 23, 42, 0.06); }");
        builder.AppendLine("    h1 { margin: 0 0 12px 0; font-size: 28px; }");
        builder.AppendLine("    p { margin: 0 0 16px 0; color: #475569; }");
        builder.AppendLine("    dl { display: grid; grid-template-columns: repeat(4, minmax(120px, 1fr)); gap: 12px 16px; margin: 0 0 16px 0; }");
        builder.AppendLine("    dt { font-size: 12px; color: #64748b; text-transform: uppercase; letter-spacing: 0.04em; }");
        builder.AppendLine("    dd { margin: 4px 0 0 0; font-size: 16px; font-weight: 600; }");
        builder.AppendLine("    .svg-container svg { width: 100%; height: auto; display: block; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <div class=\"page\">");
        builder.AppendLine("    <div class=\"card\">");
        builder.AppendLine($"      <h1>{title}</h1>");

        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine($"      <p>{description}</p>");
        }

        builder.AppendLine("      <dl>");
        builder.AppendLine($"        <div><dt>Nodes</dt><dd>{model.Nodes.Count}</dd></div>");
        builder.AppendLine($"        <div><dt>Members</dt><dd>{model.Members.Count}</dd></div>");
        builder.AppendLine($"        <div><dt>Diagrams</dt><dd>{model.Diagrams.Count}</dd></div>");
        builder.AppendLine($"        <div><dt>Deformation scale</dt><dd>{Format(model.DeformationScale)}</dd></div>");
        builder.AppendLine("      </dl>");
        builder.AppendLine("      <div class=\"svg-container\">");
        builder.AppendLine(svg);
        builder.AppendLine("      </div>");
        builder.AppendLine("    </div>");
        builder.AppendLine("  </div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static string EscapeHtml(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&#39;", StringComparison.Ordinal);

    private static string Format(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);
}
