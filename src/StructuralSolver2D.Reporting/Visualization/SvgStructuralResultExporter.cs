using System.Globalization;
using System.Text;

namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Exports a <see cref="StructuralVisualizationModel"/> to a standalone SVG document.
/// </summary>
public sealed class SvgStructuralResultExporter
{
    /// <summary>
    /// Exports the supplied visualization model to SVG markup.
    /// </summary>
    public string Export(StructuralVisualizationModel model, SvgExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        options ??= new SvgExportOptions();
        options.Validate();

        CoordinateMapper mapper = CoordinateMapper.Create(model.Bounds, options.Width, options.Height, options.Padding);
        StringBuilder builder = new();
        string title = EscapeXml(options.Title);

        builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Format(options.Width)}\" height=\"{Format(options.Height)}\" viewBox=\"0 0 {Format(options.Width)} {Format(options.Height)}\" role=\"img\" aria-labelledby=\"title desc\">");
        builder.AppendLine($"  <title id=\"title\">{title}</title>");
        builder.AppendLine("  <desc id=\"desc\">Static structural result preview exported from StructuralSolver2D.</desc>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    .background { fill: #ffffff; }");
        builder.AppendLine("    .frame { fill: none; stroke: #d1d5db; stroke-width: 1; }");
        builder.AppendLine("    .member.undeformed { stroke: #64748b; stroke-width: 2; fill: none; }");
        builder.AppendLine("    .member.deformed { stroke: #dc2626; stroke-width: 2.2; fill: none; }");
        builder.AppendLine("    .diagram.normal-force { stroke: #2563eb; stroke-width: 1.8; fill: none; }");
        builder.AppendLine("    .diagram.shear-force { stroke: #059669; stroke-width: 1.8; fill: none; }");
        builder.AppendLine("    .diagram.bending-moment { stroke: #7c3aed; stroke-width: 1.8; fill: none; }");
        builder.AppendLine("    .node { fill: #0f172a; }");
        builder.AppendLine("    .node-label { fill: #111827; font-family: Arial, Helvetica, sans-serif; font-size: 12px; }");
        builder.AppendLine("    .title { fill: #111827; font-family: Arial, Helvetica, sans-serif; font-size: 20px; font-weight: bold; }");
        builder.AppendLine("    .caption { fill: #4b5563; font-family: Arial, Helvetica, sans-serif; font-size: 12px; }");
        builder.AppendLine("    .legend-label { fill: #111827; font-family: Arial, Helvetica, sans-serif; font-size: 12px; }");
        builder.AppendLine("  </style>");
        builder.AppendLine($"  <rect class=\"background\" x=\"0\" y=\"0\" width=\"{Format(options.Width)}\" height=\"{Format(options.Height)}\"/>");
        builder.AppendLine($"  <rect class=\"frame\" x=\"0.5\" y=\"0.5\" width=\"{Format(options.Width - 1.0)}\" height=\"{Format(options.Height - 1.0)}\"/>");
        builder.AppendLine($"  <text class=\"title\" x=\"{Format(options.Padding)}\" y=\"26\">{title}</text>");
        builder.AppendLine($"  <text class=\"caption\" x=\"{Format(options.Padding)}\" y=\"44\">Deformation scale: {Format(model.DeformationScale)} | Nodes: {model.Nodes.Count} | Members: {model.Members.Count}</text>");

        if (options.IncludeUndeformedModel)
        {
            builder.AppendLine("  <g id=\"undeformed-model\">");
            foreach (VisualizationMember member in model.Members)
            {
                builder.AppendLine($"    <line class=\"member undeformed\" x1=\"{Format(mapper.MapX(member.Start.X))}\" y1=\"{Format(mapper.MapY(member.Start.Y))}\" x2=\"{Format(mapper.MapX(member.End.X))}\" y2=\"{Format(mapper.MapY(member.End.Y))}\"/>");
            }

            foreach (VisualizationNode node in model.Nodes)
            {
                builder.AppendLine($"    <circle class=\"node\" cx=\"{Format(mapper.MapX(node.Position.X))}\" cy=\"{Format(mapper.MapY(node.Position.Y))}\" r=\"3\"/>");
                if (options.IncludeNodeLabels)
                {
                    builder.AppendLine($"    <text class=\"node-label\" x=\"{Format(mapper.MapX(node.Position.X) + 6.0)}\" y=\"{Format(mapper.MapY(node.Position.Y) - 6.0)}\">{EscapeXml(node.Label ?? node.NodeId)}</text>");
                }
            }

            builder.AppendLine("  </g>");
        }

        if (options.IncludeInternalForceDiagrams)
        {
            builder.AppendLine("  <g id=\"diagrams\">");
            foreach (MemberDiagramPolyline diagram in model.Diagrams)
            {
                string className = diagram.Kind switch
                {
                    VisualizationDiagramKind.NormalForce => "diagram normal-force",
                    VisualizationDiagramKind.ShearForce => "diagram shear-force",
                    VisualizationDiagramKind.BendingMoment => "diagram bending-moment",
                    _ => "diagram",
                };

                builder.AppendLine($"    <polyline class=\"{className}\" points=\"{BuildPoints(diagram.Points, mapper)}\"/>");
            }

            builder.AppendLine("  </g>");
        }

        if (options.IncludeDeformedShape)
        {
            builder.AppendLine("  <g id=\"deformed-shape\">");
            foreach (DeformedMemberShape shape in model.DeformedShapes)
            {
                builder.AppendLine($"    <polyline class=\"member deformed\" points=\"{BuildPoints(shape.Points, mapper)}\"/>");
            }

            builder.AppendLine("  </g>");
        }

        if (options.IncludeLegend)
        {
            AppendLegend(builder, options, model);
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static void AppendLegend(StringBuilder builder, SvgExportOptions options, StructuralVisualizationModel model)
    {
        double legendX = options.Width - 220.0;
        double legendY = options.Padding;
        double lineX1 = legendX;
        double lineX2 = legendX + 28.0;
        double textX = legendX + 36.0;
        double currentY = legendY + 4.0;

        builder.AppendLine("  <g id=\"legend\">");
        builder.AppendLine($"    <text class=\"legend-label\" x=\"{Format(legendX)}\" y=\"{Format(currentY)}\">Legend</text>");
        currentY += 18.0;

        if (options.IncludeUndeformedModel)
        {
            builder.AppendLine($"    <line class=\"member undeformed\" x1=\"{Format(lineX1)}\" y1=\"{Format(currentY)}\" x2=\"{Format(lineX2)}\" y2=\"{Format(currentY)}\"/>");
            builder.AppendLine($"    <text class=\"legend-label\" x=\"{Format(textX)}\" y=\"{Format(currentY + 4.0)}\">Undeformed model</text>");
            currentY += 18.0;
        }

        if (options.IncludeDeformedShape)
        {
            builder.AppendLine($"    <line class=\"member deformed\" x1=\"{Format(lineX1)}\" y1=\"{Format(currentY)}\" x2=\"{Format(lineX2)}\" y2=\"{Format(currentY)}\"/>");
            builder.AppendLine($"    <text class=\"legend-label\" x=\"{Format(textX)}\" y=\"{Format(currentY + 4.0)}\">Deformed shape</text>");
            currentY += 18.0;
        }

        if (options.IncludeInternalForceDiagrams)
        {
            if (model.Diagrams.Any(item => item.Kind == VisualizationDiagramKind.NormalForce))
            {
                builder.AppendLine($"    <line class=\"diagram normal-force\" x1=\"{Format(lineX1)}\" y1=\"{Format(currentY)}\" x2=\"{Format(lineX2)}\" y2=\"{Format(currentY)}\"/>");
                builder.AppendLine($"    <text class=\"legend-label\" x=\"{Format(textX)}\" y=\"{Format(currentY + 4.0)}\">Normal force N</text>");
                currentY += 18.0;
            }

            if (model.Diagrams.Any(item => item.Kind == VisualizationDiagramKind.ShearForce))
            {
                builder.AppendLine($"    <line class=\"diagram shear-force\" x1=\"{Format(lineX1)}\" y1=\"{Format(currentY)}\" x2=\"{Format(lineX2)}\" y2=\"{Format(currentY)}\"/>");
                builder.AppendLine($"    <text class=\"legend-label\" x=\"{Format(textX)}\" y=\"{Format(currentY + 4.0)}\">Shear force V</text>");
                currentY += 18.0;
            }

            if (model.Diagrams.Any(item => item.Kind == VisualizationDiagramKind.BendingMoment))
            {
                builder.AppendLine($"    <line class=\"diagram bending-moment\" x1=\"{Format(lineX1)}\" y1=\"{Format(currentY)}\" x2=\"{Format(lineX2)}\" y2=\"{Format(currentY)}\"/>");
                builder.AppendLine($"    <text class=\"legend-label\" x=\"{Format(textX)}\" y=\"{Format(currentY + 4.0)}\">Bending moment M</text>");
            }
        }

        builder.AppendLine("  </g>");
    }

    private static string BuildPoints(IReadOnlyList<VisualizationPoint> points, CoordinateMapper mapper)
    {
        StringBuilder builder = new();

        for (int index = 0; index < points.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            VisualizationPoint point = points[index];
            builder.Append(Format(mapper.MapX(point.X)));
            builder.Append(',');
            builder.Append(Format(mapper.MapY(point.Y)));
        }

        return builder.ToString();
    }

    private static string EscapeXml(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&apos;", StringComparison.Ordinal);

    private static string Format(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private readonly record struct CoordinateMapper(
        double MinX,
        double MinY,
        double Scale,
        double Padding,
        double Height)
    {
        public static CoordinateMapper Create(VisualizationBounds bounds, double width, double height, double padding)
        {
            double boundsWidth = bounds.Width <= 0.0 ? 1.0 : bounds.Width;
            double boundsHeight = bounds.Height <= 0.0 ? 1.0 : bounds.Height;
            double availableWidth = width - (padding * 2.0);
            double availableHeight = height - (padding * 2.0) - 24.0;
            double scale = Math.Min(availableWidth / boundsWidth, availableHeight / boundsHeight);

            if (scale <= 0.0 || double.IsNaN(scale) || double.IsInfinity(scale))
            {
                scale = 1.0;
            }

            return new CoordinateMapper(bounds.MinX, bounds.MinY, scale, padding, height - 8.0);
        }

        public double MapX(double x) => Padding + ((x - MinX) * Scale);

        public double MapY(double y) => Height - Padding - ((y - MinY) * Scale);
    }
}
