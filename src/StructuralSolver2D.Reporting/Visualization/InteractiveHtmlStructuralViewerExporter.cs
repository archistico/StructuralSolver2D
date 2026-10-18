using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>Exports a standalone viewer with result modes, inspection, and pan/zoom controls.</summary>
public sealed class InteractiveHtmlStructuralViewerExporter
{
    private static readonly string Styles = ReadAsset("viewer.css");
    private static readonly string Script = ReadAsset("viewer.js");

    public string Export(StructuralVisualizationModel model, InteractiveViewerExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        options ??= new InteractiveViewerExportOptions();
        options.Validate();
        SvgExportOptions svgOptions = options.SvgOptions.Copy();
        svgOptions.Title = options.Title;
        svgOptions.IncludeNodeLabels = true;
        svgOptions.IncludeNodeDisplacementLabels = true;
        svgOptions.IncludeMemberDisplacementLabels = true;
        svgOptions.IncludeLegend = false;
        string svg = new SvgStructuralResultExporter().Export(model, svgOptions);
        svg = svg[(svg.IndexOf("?>", StringComparison.Ordinal) + 2)..].TrimStart();
        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\" />");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine($"<title>{Html(options.Title)}</title><style>{Styles}</style></head><body>");
        builder.AppendLine("<main class=\"viewer-page\"><header class=\"viewer-header\">");
        builder.AppendLine($"<h1>{Html(options.Title)}</h1>");
        if (!string.IsNullOrWhiteSpace(options.Description))
            builder.AppendLine($"<details class=\"model-description\"><summary>About this model</summary><p>{Html(options.Description)}</p></details>");
        builder.AppendLine("</header>");
        AppendToolbar(builder, options);
        builder.AppendLine("<section class=\"viewer-workspace\" aria-label=\"Interactive structural result viewer\"><div class=\"viewer-shell\">");
        builder.AppendLine("<div class=\"viewer-canvas\" id=\"viewerCanvas\" tabindex=\"0\" aria-label=\"Diagram. Arrow keys pan; plus and minus zoom; Home fits the visible model.\">");
        builder.AppendLine(svg);
        builder.AppendLine("</div></div><aside id=\"inspector\" class=\"inspector\" hidden aria-label=\"Selected element\">");
        builder.AppendLine("<button type=\"button\" data-action=\"close-inspector\">Close details</button><div id=\"selection-details\" aria-live=\"polite\"></div></aside></section>");
        builder.AppendLine($"<div class=\"view-status\"><span data-view-status></span><span>Amplification <strong data-effective-scale>×{Number(model.DeformationScale)}</strong></span>");
        if (model.MaximumDisplacement is not null)
            builder.AppendLine($"<span>Max nodal disp. <strong>{(model.MaximumDisplacement.Magnitude * 1000).ToString("G6", CultureInfo.InvariantCulture)} mm</strong></span>");
        builder.AppendLine("</div><p class=\"hint\">Select a member or node for values. Wheel to zoom, drag to pan; keyboard: arrows, +/−, Home.</p>");
        builder.AppendLine("<details class=\"model-summary\"><summary>Model summary</summary><dl class=\"summary\">");
        foreach (var (label, value) in new[] { ("Nodes", model.Nodes.Count), ("Members", model.Members.Count), ("Supports", model.Supports.Count), ("Diagrams", model.Diagrams.Count) })
            builder.AppendLine($"<div><dt>{label}</dt><dd>{value}</dd></div>");
        builder.AppendLine($"<div><dt>Base deformation scale</dt><dd>{Number(model.DeformationScale)}</dd></div></dl></details></main>");
        builder.AppendLine($"<script type=\"application/json\" id=\"viewer-data\">{BuildData(model, options)}</script>");
        builder.AppendLine($"<script>{Script}</script></body></html>");
        return builder.ToString();
    }

    private static void AppendToolbar(StringBuilder builder, InteractiveViewerExportOptions options)
    {
        builder.AppendLine("<nav class=\"toolbar\" aria-label=\"Viewer controls\"><div class=\"primary-controls\">");
        builder.AppendLine("<label>View <select data-view aria-label=\"Result view\"><option value=\"model\">Model</option><option value=\"displacements\">Displacements</option><option value=\"n\">Normal force N</option><option value=\"v\">Shear force V</option><option value=\"m\">Bending moment M</option><option value=\"all\">All layers</option><option value=\"custom\" hidden>Custom layers</option></select></label>");
        builder.AppendLine("<button type=\"button\" data-action=\"zoom-in\" aria-label=\"Zoom in\">Zoom +</button><button type=\"button\" data-action=\"zoom-out\" aria-label=\"Zoom out\">Zoom −</button><button type=\"button\" data-action=\"fit\">Fit view</button><button type=\"button\" data-action=\"reset\">Reset view</button></div>");
        builder.AppendLine("<details class=\"viewer-details\"><summary>Layers and display</summary><div class=\"secondary-controls\">");
        Layer(builder, "Undeformed", "#undeformed-model", true);
        Layer(builder, "Deformed", "#deformed-shape", false);
        Layer(builder, "Loads", "#loads", options.ShowLoadsByDefault);
        Layer(builder, "Load labels", ".load-label", false);
        Layer(builder, "N", DiagramSelector("normal-force"), false);
        Layer(builder, "V", DiagramSelector("shear-force"), false);
        Layer(builder, "M", DiagramSelector("bending-moment"), false);
        Layer(builder, "Supports", "#supports", true);
        Layer(builder, "Reactions", "#reactions", false);
        Layer(builder, "Dimensions", "#dimensions", false);
        Layer(builder, "Node names", ".node-label,.support-label", false);
        Layer(builder, "Diagram values", "#diagrams .annotation-label,#diagrams .annotation-point", false);
        Layer(builder, "Nodal displacement labels", ".displacement-label,.displacement-label-anchor", options.ShowNodeDisplacementLabelsByDefault);
        Layer(builder, "Member station labels", ".member-displacement-label,.member-displacement-label-anchor", options.ShowMemberDisplacementLabelsByDefault);
        builder.AppendLine("</div><div class=\"secondary-controls\">");
        builder.AppendLine($"<label class=\"control-group\">Deformed scale <input type=\"range\" min=\"0\" max=\"500\" step=\"any\" value=\"{Number(options.InitialDeformationVisualScalePercent)}\" data-scale=\"deformed\" /><output data-scale-output=\"deformed\"></output></label>");
        builder.AppendLine($"<label class=\"control-group\">Text size <input type=\"range\" min=\"50\" max=\"160\" step=\"any\" value=\"{Number(options.InitialTextScalePercent)}\" data-text-scale=\"labels\" /><output data-text-scale-output=\"labels\"></output></label>");
        builder.AppendLine($"<label class=\"control-group\">Anim. speed <input type=\"range\" min=\"0.25\" max=\"4\" step=\"any\" value=\"{Number(options.InitialAnimationSpeed)}\" data-animation=\"speed\" /><output data-animation-output=\"speed\"></output></label>");
        builder.AppendLine("<button type=\"button\" data-action=\"play-animation\">Play deformation</button><button type=\"button\" data-action=\"pause-animation\" disabled>Pause</button><span class=\"hint\">Static result preview: undeformed → deformed → undeformed.</span></div></details></nav>");
    }

    private static string DiagramSelector(string kind) => $".diagram.{kind},.diagram-fill.{kind},.annotation-point.{kind},.annotation-label.{kind}";
    private static void Layer(StringBuilder builder, string label, string selector, bool visible) =>
        builder.AppendLine($"<label><input type=\"checkbox\" data-layer=\"{selector}\"{(visible ? " checked" : "")} /> {label}</label>");

    private static string BuildData(StructuralVisualizationModel model, InteractiveViewerExportOptions options) => JsonSerializer.Serialize(new
    {
        baseScale = model.DeformationScale,
        initialView = options.InitialView.ToString().ToLowerInvariant(),
        nodes = model.Nodes.ToDictionary(node => node.NodeId, node => new { id = node.NodeId, label = node.Label, x = node.Position.X, y = node.Position.Y, ux = node.Ux, uy = node.Uy, rz = node.Rz }),
        members = model.Members.ToDictionary(member => member.MemberId, member => new
        {
            id = member.MemberId,
            type = member.MemberType.ToString(),
            start = member.StartNodeId,
            end = member.EndNodeId,
            length = Math.Sqrt(Math.Pow(member.End.X - member.Start.X, 2) + Math.Pow(member.End.Y - member.Start.Y, 2)),
            forces = model.Diagrams.Where(diagram => diagram.MemberId == member.MemberId).ToDictionary(diagram => diagram.Kind.ToString(), diagram => Math.Abs(diagram.MaxAbsValue)),
            stations = model.MemberDisplacementLabels.Where(label => label.MemberId == member.MemberId).Select(label => new { label = label.StationLabel, x = label.Distance, ux = label.GlobalUx, uy = label.GlobalUy, rz = label.LocalRz })
        })
    });

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string ReadAsset(string name)
    {
        using Stream stream = typeof(InteractiveHtmlStructuralViewerExporter).Assembly.GetManifestResourceStream("StructuralSolver2D.Reporting." + name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
