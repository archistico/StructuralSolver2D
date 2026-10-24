using System.Net;
using System.Text;
using System.Text.Json;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Dedicated, self-contained modal visualization: no static loads or deformations
/// are shown or implied. All animation amplitudes and playback speeds are illustrative.
/// </summary>
public sealed class InteractiveHtmlModalViewerExporter
{
    private static readonly string Styles = ReadAsset("modal-viewer.css");
    private static readonly string Script = ReadAsset("modal-viewer.js");

    public string Export(StructuralModel model, StructuralModalAnalysisOutput result, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(result);
        if (result.Modes.Count == 0) throw new ArgumentException("A modal spectrum with at least one mode is required.", nameof(result));
        var nodes = model.Nodes.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var mode in result.Modes)
        {
            if (mode.Shape.Count != model.Nodes.Count ||
                mode.Shape.Select(shape => shape.NodeId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != model.Nodes.Count ||
                mode.Shape.Any(shape => !nodes.ContainsKey(shape.NodeId)))
                throw new ArgumentException("Each modal vector must contain exactly all model nodes.", nameof(result));
        }
        foreach (var member in model.Members)
            if (!nodes.ContainsKey(member.StartNodeId) || !nodes.ContainsKey(member.EndNodeId))
                throw new ArgumentException($"Member '{member.Id}' has an invalid endpoint.", nameof(model));

        string modelTitle = title ?? "Modal shapes — StructuralSolver2D";
        // System.Text.Json's default encoder escapes HTML-sensitive code points even
        // inside JSON script elements; labels are also inserted into DOM via textContent.
        string payload = JsonSerializer.Serialize(new
        {
            nodes = model.Nodes.Select(node => new { id = node.Id, x = node.X, y = node.Y }),
            members = model.Members.Select(member => new {
                id = member.Id, a = nodes[member.StartNodeId].Id, b = nodes[member.EndNodeId].Id,
                frame = member.Type == MemberType.Frame2D
            }),
            modes = result.Modes.Select(mode => new {
                index = mode.ModeNumber,
                hz = mode.FrequencyHertz,
                period = mode.PeriodSeconds,
                residual = mode.RelativeResidual,
                shape = mode.Shape.Select(n => new { id = nodes[n.NodeId].Id, ux = n.Ux, uy = n.Uy, rz = n.Rz })
            }),
            warnings = result.Warnings
        });
        var b = new StringBuilder();
        b.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        b.Append("<title>").Append(WebUtility.HtmlEncode(modelTitle)).Append("</title><style>").Append(Styles).AppendLine("</style></head><body>");
        b.Append("<main><header><h1>").Append(WebUtility.HtmlEncode(modelTitle)).AppendLine("</h1><p>Free vibration · linear elastic · 2D</p></header>");
        b.AppendLine("<section class=\"controls\" aria-label=\"Modal controls\"><label>Mode <select id=\"modal-mode\" aria-label=\"Natural vibration mode\"></select></label>");
        b.AppendLine("<label>Illustrative amplitude <input id=\"amplitude\" type=\"range\" min=\"0\" max=\"200\" value=\"75\" step=\"5\"><output id=\"amplitude-label\">75%</output></label>");
        b.AppendLine("<label>Display speed <input id=\"speed\" type=\"range\" min=\"0.25\" max=\"3\" value=\"1\" step=\"0.25\"><output id=\"speed-label\">1 ×</output></label>");
        b.AppendLine("<button id=\"play\" type=\"button\">Play</button><button id=\"pause\" type=\"button\">Pause</button><button id=\"reset\" type=\"button\">Reset view</button></section>");
        b.AppendLine("<section class=\"view\"><svg id=\"modal-svg\" xmlns=\"http://www.w3.org/2000/svg\" role=\"img\" aria-label=\"Normalized modal shape\"><g id=\"model-baseline\"></g><g id=\"modal-shape\"></g><g id=\"model-nodes\"></g></svg><aside aria-live=\"polite\"><h2 id=\"mode-title\">Mode</h2><dl id=\"mode-metrics\"></dl><h3>Nodal contributions</h3><table><thead><tr><th>Node</th><th>Relative translation</th></tr></thead><tbody id=\"node-contributions\"></tbody></table></aside></section>");
        b.AppendLine("<p class=\"warning\">Illustrative normalized oscillation only. Amplitude and playback speed do not represent actual displacement or physical elapsed time. Free vibration does not predict a loaded response. Mode sign is arbitrary; repeated frequencies have non-unique basis vectors.</p>");
        b.AppendLine("<details><summary>Diagnostics and limitations</summary><ul id=\"warnings\"></ul></details></main>");
        b.Append("<script type=\"application/json\" id=\"modal-data\">").Append(payload).AppendLine("</script>");
        b.Append("<script>").Append(Script).AppendLine("</script></body></html>");
        return b.ToString();
    }

    private static string ReadAsset(string name)
    {
        using Stream stream = typeof(InteractiveHtmlModalViewerExporter).Assembly
            .GetManifestResourceStream("StructuralSolver2D.Reporting." + name)
            ?? throw new InvalidOperationException($"Missing modal asset {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
