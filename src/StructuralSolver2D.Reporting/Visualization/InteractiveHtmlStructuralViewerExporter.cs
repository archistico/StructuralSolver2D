using System.Globalization;
using System.Text;

namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Exports a <see cref="StructuralVisualizationModel"/> to a standalone HTML viewer with basic pan, zoom and layer controls.
/// </summary>
public sealed class InteractiveHtmlStructuralViewerExporter
{
    /// <summary>
    /// Exports the supplied visualization model to a complete interactive HTML document.
    /// </summary>
    public string Export(StructuralVisualizationModel model, InteractiveViewerExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        options ??= new InteractiveViewerExportOptions();
        options.SvgOptions.Title = options.Title;
        options.SvgOptions.IncludeNodeDisplacementLabels = true;
        options.SvgOptions.IncludeMemberDisplacementLabels = true;

        string svg = StripXmlDeclaration(new SvgStructuralResultExporter().Export(model, options.SvgOptions));
        string title = EscapeHtml(options.Title);
        string description = EscapeHtml(options.Description);
        StringBuilder builder = new();

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\" />");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine($"  <title>{title}</title>");
        AppendStyles(builder);
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <main class=\"viewer-page\">");
        builder.AppendLine("    <header class=\"viewer-header\">");
        builder.AppendLine($"      <h1>{title}</h1>");
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine($"      <p>{description}</p>");
        }
        builder.AppendLine("    </header>");
        AppendToolbar(builder, options);
        builder.AppendLine("    <section class=\"viewer-shell\" aria-label=\"Interactive structural result viewer\">");
        builder.AppendLine("      <div class=\"viewer-canvas\" id=\"viewerCanvas\">");
        builder.AppendLine(svg);
        builder.AppendLine("      </div>");
        builder.AppendLine("    </section>");
        AppendSummary(builder, model);
        builder.AppendLine("  </main>");
        AppendScript(builder);
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static void AppendStyles(StringBuilder builder)
    {
        builder.AppendLine("  <style>");
        builder.AppendLine("    :root { color-scheme: light; }");
        builder.AppendLine("    body { margin: 0; font-family: Arial, Helvetica, sans-serif; background: #0f172a; color: #e5e7eb; }");
        builder.AppendLine("    .viewer-page { min-height: 100vh; padding: 20px; box-sizing: border-box; }");
        builder.AppendLine("    .viewer-header { max-width: 1500px; margin: 0 auto 12px auto; }");
        builder.AppendLine("    .viewer-header h1 { margin: 0 0 6px 0; font-size: 28px; }");
        builder.AppendLine("    .viewer-header p { margin: 0; color: #cbd5e1; }");
        builder.AppendLine("    .toolbar { max-width: 1500px; margin: 0 auto 12px auto; padding: 12px; display: flex; flex-wrap: wrap; gap: 8px 14px; align-items: center; background: #111827; border: 1px solid #334155; border-radius: 12px; }");
        builder.AppendLine("    .toolbar button { border: 1px solid #475569; background: #1f2937; color: #f8fafc; border-radius: 8px; padding: 7px 10px; cursor: pointer; }");
        builder.AppendLine("    .toolbar button:hover { background: #374151; }");
        builder.AppendLine("    .toolbar label { display: inline-flex; align-items: center; gap: 5px; font-size: 13px; color: #e2e8f0; }");
        builder.AppendLine("    .control-group { display: inline-flex; align-items: center; gap: 7px; padding: 6px 8px; border: 1px solid #334155; border-radius: 10px; background: #0f172a; }");
        builder.AppendLine("    .control-group span { color: #cbd5e1; font-size: 12px; min-width: 34px; text-align: right; }");
        builder.AppendLine("    .toolbar input[type=range] { width: 96px; }");
        builder.AppendLine("    .hint { color: #94a3b8; font-size: 12px; }");
        builder.AppendLine("    .viewer-shell { max-width: 1500px; margin: 0 auto; border: 1px solid #334155; border-radius: 14px; background: #020617; overflow: hidden; box-shadow: 0 12px 36px rgba(0, 0, 0, 0.35); }");
        builder.AppendLine("    .viewer-canvas { height: min(78vh, 920px); overflow: hidden; cursor: grab; touch-action: none; }");
        builder.AppendLine("    .viewer-canvas.dragging { cursor: grabbing; }");
        builder.AppendLine("    .viewer-canvas svg { width: 100%; height: 100%; display: block; background: #ffffff; }");
        builder.AppendLine("    .summary { max-width: 1500px; margin: 12px auto 0 auto; display: grid; grid-template-columns: repeat(6, minmax(110px, 1fr)); gap: 10px; }");
        builder.AppendLine("    .summary div { background: #111827; border: 1px solid #334155; border-radius: 10px; padding: 10px; }");
        builder.AppendLine("    .summary dt { color: #94a3b8; font-size: 11px; text-transform: uppercase; letter-spacing: 0.04em; }");
        builder.AppendLine("    .summary dd { margin: 4px 0 0 0; font-weight: 700; color: #f8fafc; }");
        builder.AppendLine("    .hidden-layer { display: none !important; }");
        builder.AppendLine("  </style>");
    }

    private static void AppendToolbar(StringBuilder builder, InteractiveViewerExportOptions options)
    {
        string deformationScale = FormatPercent(options.InitialDeformationVisualScalePercent);
        string animationAmplitude = FormatPercent(options.InitialAnimationAmplitudePercent);
        string animationSpeed = options.InitialAnimationSpeed.ToString("0.###", CultureInfo.InvariantCulture);

        builder.AppendLine("    <nav class=\"toolbar\" aria-label=\"Viewer controls\">");
        builder.AppendLine("      <button type=\"button\" data-action=\"zoom-in\">Zoom +</button>");
        builder.AppendLine("      <button type=\"button\" data-action=\"zoom-out\">Zoom -</button>");
        builder.AppendLine("      <button type=\"button\" data-action=\"reset\">Reset view</button>");
        builder.AppendLine("      <button type=\"button\" data-action=\"play-animation\">Play deformation</button>");
        builder.AppendLine("      <button type=\"button\" data-action=\"pause-animation\">Pause</button>");
        builder.AppendLine($"      <label class=\"control-group\">Deformed scale <input type=\"range\" min=\"0\" max=\"300\" step=\"5\" value=\"{deformationScale}\" data-scale=\"deformed\" /> <span data-scale-output=\"deformed\">{deformationScale}%</span></label>");
        builder.AppendLine($"      <label class=\"control-group\">Anim. amplitude <input type=\"range\" min=\"0\" max=\"200\" step=\"5\" value=\"{animationAmplitude}\" data-animation=\"amplitude\" /> <span data-animation-output=\"amplitude\">{animationAmplitude}%</span></label>");
        builder.AppendLine($"      <label class=\"control-group\">Anim. speed <input type=\"range\" min=\"0.25\" max=\"4\" step=\"0.25\" value=\"{animationSpeed}\" data-animation=\"speed\" /> <span data-animation-output=\"speed\">{animationSpeed}x</span></label>");
        builder.AppendLine("      <label><input type=\"checkbox\" data-layer=\"#undeformed-model\" checked /> Undeformed</label>");
        builder.AppendLine("      <label><input type=\"checkbox\" data-layer=\"#deformed-shape\" checked /> Deformed</label>");
        builder.AppendLine("      <label><input type=\"checkbox\" data-layer=\".diagram.normal-force,.diagram-fill.normal-force,.annotation-point.normal-force,.annotation-label.normal-force\" checked /> N</label>");
        builder.AppendLine("      <label><input type=\"checkbox\" data-layer=\".diagram.shear-force,.diagram-fill.shear-force,.annotation-point.shear-force,.annotation-label.shear-force\" checked /> V</label>");
        builder.AppendLine("      <label><input type=\"checkbox\" data-layer=\".diagram.bending-moment,.diagram-fill.bending-moment,.annotation-point.bending-moment,.annotation-label.bending-moment\" checked /> M</label>");
        builder.AppendLine("      <label><input type=\"checkbox\" data-layer=\"#diagrams\" checked /> Diagrams group</label>");
        builder.AppendLine("      <label><input type=\"checkbox\" data-layer=\".support\" checked /> Supports</label>");
        builder.AppendLine("      <label><input type=\"checkbox\" data-layer=\".reaction,.reaction-label\" checked /> Reactions</label>");
        builder.AppendLine("      <label><input type=\"checkbox\" data-layer=\".dimension,.dimension-extension,.dimension-label\" checked /> Dimensions</label>");
        string displacementLabelsChecked = options.ShowNodeDisplacementLabelsByDefault ? " checked" : string.Empty;
        builder.AppendLine($"      <label><input type=\"checkbox\" data-layer=\".displacement-label,.displacement-label-anchor\"{displacementLabelsChecked} /> Nodal displacement labels</label>");
        string memberDisplacementLabelsChecked = options.ShowMemberDisplacementLabelsByDefault ? " checked" : string.Empty;
        builder.AppendLine($"      <label><input type=\"checkbox\" data-layer=\".member-displacement-label,.member-displacement-label-anchor\"{memberDisplacementLabelsChecked} /> Member station labels</label>");
        builder.AppendLine("      <span class=\"hint\">Wheel to zoom, drag to pan. Deformed scale recomputes deformed coordinates from displacement data; N/V/M toggles only show or hide diagrams.</span>");
        builder.AppendLine("    </nav>");
    }

    private static void AppendSummary(StringBuilder builder, StructuralVisualizationModel model)
    {
        builder.AppendLine("    <dl class=\"summary\">");
        AppendSummaryItem(builder, "Nodes", model.Nodes.Count.ToString(CultureInfo.InvariantCulture));
        AppendSummaryItem(builder, "Members", model.Members.Count.ToString(CultureInfo.InvariantCulture));
        AppendSummaryItem(builder, "Supports", model.Supports.Count.ToString(CultureInfo.InvariantCulture));
        AppendSummaryItem(builder, "Reactions", (model.ReactionArrows.Count + model.ReactionMoments.Count).ToString(CultureInfo.InvariantCulture));
        AppendSummaryItem(builder, "Applied loads", (model.LoadArrows.Count + model.LoadMoments.Count + model.DistributedLoads.Count).ToString(CultureInfo.InvariantCulture));
        AppendSummaryItem(builder, "Diagrams", model.Diagrams.Count.ToString(CultureInfo.InvariantCulture));
        AppendSummaryItem(builder, "Deform. scale", model.DeformationScale.ToString("0.###", CultureInfo.InvariantCulture));
        if (model.MaximumDisplacement is not null)
        {
            AppendSummaryItem(builder, "Max disp.", FormatMillimeters(model.MaximumDisplacement.Magnitude));
        }
        builder.AppendLine("    </dl>");
    }

    private static void AppendSummaryItem(StringBuilder builder, string label, string value)
    {
        builder.AppendLine("      <div>");
        builder.AppendLine($"        <dt>{EscapeHtml(label)}</dt>");
        builder.AppendLine($"        <dd>{EscapeHtml(value)}</dd>");
        builder.AppendLine("      </div>");
    }

    private static void AppendScript(StringBuilder builder)
    {
        builder.AppendLine("  <script>");
        builder.AppendLine("    (() => {");
        builder.AppendLine("      const canvas = document.getElementById('viewerCanvas');");
        builder.AppendLine("      const svg = canvas.querySelector('svg');");
        builder.AppendLine("      const original = svg.viewBox.baseVal;");
        builder.AppendLine("      let viewBox = { x: original.x, y: original.y, width: original.width, height: original.height };");
        builder.AppendLine("      let dragging = false;");
        builder.AppendLine("      let last = { x: 0, y: 0 };");
        builder.AppendLine("      let animationRunning = false;");
        builder.AppendLine("      let animationStart = 0;");
        builder.AppendLine("      function applyViewBox() { svg.setAttribute('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.width} ${viewBox.height}`); }");
        builder.AppendLine("      function zoom(factor, clientX, clientY) {");
        builder.AppendLine("        const rect = svg.getBoundingClientRect();");
        builder.AppendLine("        const px = clientX === undefined ? rect.left + rect.width / 2 : clientX;");
        builder.AppendLine("        const py = clientY === undefined ? rect.top + rect.height / 2 : clientY;");
        builder.AppendLine("        const mx = viewBox.x + ((px - rect.left) / rect.width) * viewBox.width;");
        builder.AppendLine("        const my = viewBox.y + ((py - rect.top) / rect.height) * viewBox.height;");
        builder.AppendLine("        viewBox.x = mx - ((mx - viewBox.x) * factor);");
        builder.AppendLine("        viewBox.y = my - ((my - viewBox.y) * factor);");
        builder.AppendLine("        viewBox.width *= factor;");
        builder.AppendLine("        viewBox.height *= factor;");
        builder.AppendLine("        applyViewBox();");
        builder.AppendLine("      }");
        builder.AppendLine("      function setOutput(selector, value, suffix) { const output = document.querySelector(selector); if (output) output.textContent = `${value}${suffix}`; }");
        builder.AppendLine("      function parsePointList(value) {");
        builder.AppendLine("        return (value || '').trim().split(/\\s+/).filter(Boolean).map(pair => {");
        builder.AppendLine("          const parts = pair.split(',').map(Number);");
        builder.AppendLine("          return { x: parts[0], y: parts[1] };");
        builder.AppendLine("        });");
        builder.AppendLine("      }");
        builder.AppendLine("      function formatPointList(points) { return points.map(point => `${point.x.toFixed(3)},${point.y.toFixed(3)}`).join(' '); }");
        builder.AppendLine("      function getDeformationScaleValue() { const input = document.querySelector('[data-scale=deformed]'); return input ? Number(input.value) / 100 : 1; }");
        builder.AppendLine("      function applyDeformation(factor) {");
        builder.AppendLine("        svg.querySelectorAll('[data-deformed-polyline=true]').forEach(polyline => {");
        builder.AppendLine("          const base = parsePointList(polyline.dataset.basePoints);");
        builder.AppendLine("          const displacement = parsePointList(polyline.dataset.displacementPoints);");
        builder.AppendLine("          if (base.length === 0 || base.length !== displacement.length) return;");
        builder.AppendLine("          const points = base.map((point, index) => ({ x: point.x + displacement[index].x * factor, y: point.y + displacement[index].y * factor }));");
        builder.AppendLine("          polyline.setAttribute('points', formatPointList(points));");
        builder.AppendLine("        });");
        builder.AppendLine("        svg.querySelectorAll('[data-deformation-label=true]').forEach(label => {");
        builder.AppendLine("          const dx = Number(label.dataset.dx || '0');");
        builder.AppendLine("          const dy = Number(label.dataset.dy || '0');");
        builder.AppendLine("          label.setAttribute('transform', `translate(${(dx * (factor - 1)).toFixed(3)} ${(dy * (factor - 1)).toFixed(3)})`);");
        builder.AppendLine("        });");
        builder.AppendLine("      }");
        builder.AppendLine("      function applyVisualScales(animationFactor) {");
        builder.AppendLine("        const factor = animationFactor ?? getDeformationScaleValue();");
        builder.AppendLine("        applyDeformation(factor);");
        builder.AppendLine("      }");
        builder.AppendLine("      function animationAmplitude() { const input = document.querySelector('[data-animation=amplitude]'); return input ? Number(input.value) / 100 : 1; }");
        builder.AppendLine("      function animationSpeed() { const input = document.querySelector('[data-animation=speed]'); return input ? Number(input.value) : 1; }");
        builder.AppendLine("      function animationLoop(timestamp) {");
        builder.AppendLine("        if (!animationRunning) return;");
        builder.AppendLine("        if (!animationStart) animationStart = timestamp;");
        builder.AppendLine("        const elapsed = (timestamp - animationStart) / 1000;");
        builder.AppendLine("        const factor = getDeformationScaleValue() * animationAmplitude() * Math.sin(elapsed * animationSpeed() * Math.PI * 2);");
        builder.AppendLine("        applyVisualScales(factor);");
        builder.AppendLine("        requestAnimationFrame(animationLoop);");
        builder.AppendLine("      }");
        builder.AppendLine("      canvas.addEventListener('wheel', event => { event.preventDefault(); zoom(event.deltaY < 0 ? 0.9 : 1.1, event.clientX, event.clientY); }, { passive: false });");
        builder.AppendLine("      canvas.addEventListener('pointerdown', event => { dragging = true; last = { x: event.clientX, y: event.clientY }; canvas.classList.add('dragging'); canvas.setPointerCapture(event.pointerId); });");
        builder.AppendLine("      canvas.addEventListener('pointermove', event => {");
        builder.AppendLine("        if (!dragging) return;");
        builder.AppendLine("        const rect = svg.getBoundingClientRect();");
        builder.AppendLine("        viewBox.x -= (event.clientX - last.x) / rect.width * viewBox.width;");
        builder.AppendLine("        viewBox.y -= (event.clientY - last.y) / rect.height * viewBox.height;");
        builder.AppendLine("        last = { x: event.clientX, y: event.clientY };");
        builder.AppendLine("        applyViewBox();");
        builder.AppendLine("      });");
        builder.AppendLine("      canvas.addEventListener('pointerup', event => { dragging = false; canvas.classList.remove('dragging'); canvas.releasePointerCapture(event.pointerId); });");
        builder.AppendLine("      document.querySelector('[data-action=zoom-in]').addEventListener('click', () => zoom(0.85));");
        builder.AppendLine("      document.querySelector('[data-action=zoom-out]').addEventListener('click', () => zoom(1.15));");
        builder.AppendLine("      document.querySelector('[data-action=reset]').addEventListener('click', () => { viewBox = { x: original.x, y: original.y, width: original.width, height: original.height }; applyViewBox(); });");
        builder.AppendLine("      document.querySelector('[data-action=play-animation]').addEventListener('click', () => { animationRunning = true; animationStart = 0; requestAnimationFrame(animationLoop); });");
        builder.AppendLine("      document.querySelector('[data-action=pause-animation]').addEventListener('click', () => { animationRunning = false; applyVisualScales(); });");
        builder.AppendLine("      document.querySelectorAll('[data-scale]').forEach(input => {");
        builder.AppendLine("        function applyScaleInput() { setOutput(`[data-scale-output=${input.dataset.scale}]`, input.value, '%'); if (!animationRunning) applyVisualScales(); }");
        builder.AppendLine("        input.addEventListener('input', applyScaleInput);");
        builder.AppendLine("        applyScaleInput();");
        builder.AppendLine("      });");
        builder.AppendLine("      document.querySelectorAll('[data-animation]').forEach(input => {");
        builder.AppendLine("        function applyAnimationInput() { const suffix = input.dataset.animation === 'speed' ? 'x' : '%'; setOutput(`[data-animation-output=${input.dataset.animation}]`, input.value, suffix); }");
        builder.AppendLine("        input.addEventListener('input', applyAnimationInput);");
        builder.AppendLine("        applyAnimationInput();");
        builder.AppendLine("      });");
        builder.AppendLine("      document.querySelectorAll('[data-layer]').forEach(control => {");
        builder.AppendLine("        function applyLayerVisibility() {");
        builder.AppendLine("          control.dataset.layer.split(',').forEach(selector => {");
        builder.AppendLine("            svg.querySelectorAll(selector.trim()).forEach(element => element.classList.toggle('hidden-layer', !control.checked));");
        builder.AppendLine("          });");
        builder.AppendLine("        }");
        builder.AppendLine("        control.addEventListener('change', applyLayerVisibility);");
        builder.AppendLine("        applyLayerVisibility();");
        builder.AppendLine("      });");
        builder.AppendLine("      applyVisualScales();");
        builder.AppendLine("      applyViewBox();");
        builder.AppendLine("    })();");
        builder.AppendLine("  </script>");
    }

    private static string StripXmlDeclaration(string svg)
    {
        string trimmedStart = svg.TrimStart();
        if (!trimmedStart.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            return svg;
        }

        int end = trimmedStart.IndexOf("?>", StringComparison.Ordinal);
        return end < 0 ? svg : trimmedStart[(end + 2)..].TrimStart();
    }

    private static string FormatPercent(double percent) =>
        percent.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatMillimeters(double meters) =>
        $"{(meters * 1000.0).ToString("0.###", CultureInfo.InvariantCulture)} mm";

    private static string EscapeHtml(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&#39;", StringComparison.Ordinal);
}
