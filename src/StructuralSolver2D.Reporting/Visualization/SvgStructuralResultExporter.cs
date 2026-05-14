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
        AppendDefinitions(builder);
        AppendStyles(builder);
        builder.AppendLine($"  <rect class=\"background\" x=\"0\" y=\"0\" width=\"{Format(options.Width)}\" height=\"{Format(options.Height)}\"/>");
        builder.AppendLine($"  <rect class=\"frame\" x=\"0.5\" y=\"0.5\" width=\"{Format(options.Width - 1.0)}\" height=\"{Format(options.Height - 1.0)}\"/>");
        builder.AppendLine($"  <text class=\"title\" x=\"{Format(options.Padding)}\" y=\"26\">{title}</text>");
        builder.AppendLine($"  <text class=\"caption\" x=\"{Format(options.Padding)}\" y=\"44\">Deformation scale: {Format(model.DeformationScale)} | Nodes: {model.Nodes.Count} | Members: {model.Members.Count}</text>");

        if (options.IncludeUndeformedModel)
        {
            AppendUndeformedModel(builder, model, options, mapper);
        }

        if (options.IncludeInternalForceDiagrams)
        {
            AppendDiagrams(builder, model, options, mapper);
        }

        if (options.IncludeDeformedShape)
        {
            AppendDeformedShape(builder, model, options, mapper);
        }

        if (options.IncludeLegend)
        {
            AppendLegend(builder, options, model);
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    private static void AppendDefinitions(StringBuilder builder)
    {
        builder.AppendLine("  <defs>");
        builder.AppendLine("    <marker id=\"reactionArrow\" markerWidth=\"8\" markerHeight=\"8\" refX=\"7\" refY=\"4\" orient=\"auto\" markerUnits=\"userSpaceOnUse\">");
        builder.AppendLine("      <path d=\"M0,0 L8,4 L0,8 z\" fill=\"#0ea5e9\" />");
        builder.AppendLine("    </marker>");
        builder.AppendLine("    <marker id=\"dimensionArrow\" markerWidth=\"8\" markerHeight=\"8\" refX=\"4\" refY=\"4\" orient=\"auto\" markerUnits=\"userSpaceOnUse\">");
        builder.AppendLine("      <path d=\"M8,0 L0,4 L8,8\" fill=\"none\" stroke=\"#6b7280\" stroke-width=\"1.2\" />");
        builder.AppendLine("    </marker>");
        builder.AppendLine("  </defs>");
    }

    private static void AppendStyles(StringBuilder builder)
    {
        builder.AppendLine("  <style>");
        builder.AppendLine("    .background { fill: #ffffff; }");
        builder.AppendLine("    .frame { fill: none; stroke: #d1d5db; stroke-width: 1; }");
        builder.AppendLine("    .member.undeformed { stroke: #64748b; stroke-width: 2; fill: none; }");
        builder.AppendLine("    .member.deformed { stroke: #dc2626; stroke-width: 2.2; fill: none; }");
        builder.AppendLine("    .diagram.normal-force { stroke: #2563eb; stroke-width: 1.8; fill: none; }");
        builder.AppendLine("    .diagram.shear-force { stroke: #059669; stroke-width: 1.8; fill: none; }");
        builder.AppendLine("    .diagram.bending-moment { stroke: #7c3aed; stroke-width: 1.8; fill: none; }");
        builder.AppendLine("    .node { fill: #0f172a; }");
        builder.AppendLine("    .node-label, .support-label, .dimension-label, .annotation-label, .reaction-label, .legend-label, .displacement-label, .member-displacement-label { fill: #111827; font-family: Arial, Helvetica, sans-serif; font-size: 12px; }");
        builder.AppendLine("    .title { fill: #111827; font-family: Arial, Helvetica, sans-serif; font-size: 20px; font-weight: bold; }");
        builder.AppendLine("    .caption { fill: #4b5563; font-family: Arial, Helvetica, sans-serif; font-size: 12px; }");
        builder.AppendLine("    .support-line { stroke: #1f2937; stroke-width: 1.5; fill: none; }");
        builder.AppendLine("    .support-fill { fill: #f3f4f6; stroke: #1f2937; stroke-width: 1.5; }");
        builder.AppendLine("    .reaction { stroke: #0ea5e9; stroke-width: 1.8; fill: none; }");
        builder.AppendLine("    .reaction-label { fill: #0369a1; }");
        builder.AppendLine("    .displacement-label { fill: #991b1b; font-size: 11px; }");
        builder.AppendLine("    .displacement-label-anchor { fill: #dc2626; stroke: #ffffff; stroke-width: 1; }");
        builder.AppendLine("    .member-displacement-label { fill: #7f1d1d; font-size: 10.5px; }");
        builder.AppendLine("    .member-displacement-label-anchor { fill: #f97316; stroke: #ffffff; stroke-width: 1; }");
        builder.AppendLine("    .dimension { stroke: #6b7280; stroke-width: 1.2; fill: none; }");
        builder.AppendLine("    .dimension-extension { stroke: #9ca3af; stroke-width: 1; }");
        builder.AppendLine("    .annotation-line { stroke: #dc2626; stroke-width: 1.2; stroke-dasharray: 4 3; fill: none; }");
        builder.AppendLine("    .annotation-label { font-weight: 600; }");
        builder.AppendLine("    .annotation-point { fill: #ffffff; stroke-width: 1.6; }");
        builder.AppendLine("    .annotation-point.normal-force { stroke: #2563eb; }");
        builder.AppendLine("    .annotation-point.shear-force { stroke: #059669; }");
        builder.AppendLine("    .annotation-point.bending-moment { stroke: #7c3aed; }");
        builder.AppendLine("  </style>");
    }

    private static void AppendUndeformedModel(StringBuilder builder, StructuralVisualizationModel model, SvgExportOptions options, CoordinateMapper mapper)
    {
        builder.AppendLine("  <g id=\"undeformed-model\">");

        foreach (VisualizationMember member in model.Members)
        {
            builder.AppendLine($"    <line class=\"member undeformed\" x1=\"{Format(mapper.MapX(member.Start.X))}\" y1=\"{Format(mapper.MapY(member.Start.Y))}\" x2=\"{Format(mapper.MapX(member.End.X))}\" y2=\"{Format(mapper.MapY(member.End.Y))}\"/>");
        }

        if (options.IncludeMemberDimensions)
        {
            foreach (MemberDimensionAnnotation dimension in model.MemberDimensions)
            {
                AppendDimension(builder, mapper, dimension);
            }
        }

        if (options.IncludeSupportSymbols)
        {
            foreach (VisualizationSupport support in model.Supports)
            {
                AppendSupportGlyph(builder, mapper, support);
            }
        }

        foreach (VisualizationNode node in model.Nodes)
        {
            double x = mapper.MapX(node.Position.X);
            double y = mapper.MapY(node.Position.Y);
            builder.AppendLine($"    <circle class=\"node\" cx=\"{Format(x)}\" cy=\"{Format(y)}\" r=\"3\"/>");
            if (options.IncludeNodeLabels)
            {
                builder.AppendLine($"    <text class=\"node-label\" x=\"{Format(x + 6.0)}\" y=\"{Format(y - 6.0)}\">{EscapeXml(node.Label ?? node.NodeId)}</text>");
            }
        }

        if (options.IncludeReactions)
        {
            foreach (VisualizationReactionArrow arrow in model.ReactionArrows)
            {
                AppendReactionArrow(builder, mapper, arrow);
            }

            foreach (VisualizationReactionMoment moment in model.ReactionMoments)
            {
                AppendReactionMoment(builder, mapper, moment);
            }
        }

        builder.AppendLine("  </g>");
    }

    private static void AppendDiagrams(StringBuilder builder, StructuralVisualizationModel model, SvgExportOptions options, CoordinateMapper mapper)
    {
        builder.AppendLine("  <g id=\"diagrams\">");
        foreach (MemberDiagramPolyline diagram in model.Diagrams)
        {
            string className = GetDiagramCssClass(diagram.Kind);
            builder.AppendLine($"    <polyline class=\"{className}\" points=\"{BuildPoints(diagram.Points, mapper)}\"/>");
        }

        if (options.IncludeDiagramValueLabels)
        {
            foreach (DiagramValueAnnotation annotation in model.DiagramValueAnnotations)
            {
                AppendDiagramAnnotation(builder, mapper, annotation);
            }
        }

        builder.AppendLine("  </g>");
    }

    private static void AppendDeformedShape(StringBuilder builder, StructuralVisualizationModel model, SvgExportOptions options, CoordinateMapper mapper)
    {
        builder.AppendLine("  <g id=\"deformed-shape\">");
        Dictionary<string, VisualizationMember> membersById = model.Members.ToDictionary(member => member.MemberId, StringComparer.OrdinalIgnoreCase);

        foreach (DeformedMemberShape shape in model.DeformedShapes)
        {
            string deformationData = string.Empty;
            if (membersById.TryGetValue(shape.MemberId, out VisualizationMember? member))
            {
                deformationData = $" data-base-points=\"{BuildInterpolatedBasePoints(member, shape.Points.Count, mapper)}\" data-displacement-points=\"{BuildDisplacementPoints(member, shape.Points, mapper)}\"";
            }

            builder.AppendLine($"    <polyline class=\"member deformed\" data-deformed-polyline=\"true\"{deformationData} points=\"{BuildPoints(shape.Points, mapper)}\"/>");
        }

        if (options.IncludeNodeDisplacementLabels)
        {
            foreach (VisualizationNodeDisplacementLabel label in model.NodeDisplacementLabels)
            {
                AppendNodeDisplacementLabel(builder, mapper, label, model.DeformationScale);
            }
        }

        if (options.IncludeMemberDisplacementLabels)
        {
            foreach (VisualizationMemberDisplacementLabel label in model.MemberDisplacementLabels)
            {
                AppendMemberDisplacementLabel(builder, mapper, label, model.DeformationScale);
            }
        }

        if (options.IncludeMaximumDisplacementAnnotation && model.MaximumDisplacement is not null)
        {
            AppendMaximumDisplacement(builder, mapper, model.MaximumDisplacement);
        }

        builder.AppendLine("  </g>");
    }

    private static void AppendSupportGlyph(StringBuilder builder, CoordinateMapper mapper, VisualizationSupport support)
    {
        double x = mapper.MapX(support.Position.X);
        double y = mapper.MapY(support.Position.Y);
        const double size = 12.0;
        double displayRotation = -support.OrientationDegrees;

        builder.AppendLine($"    <g class=\"support\" data-support-id=\"{EscapeXml(support.SupportId)}\" data-orientation-degrees=\"{Format(support.OrientationDegrees)}\">");
        builder.AppendLine($"      <g class=\"support-symbol\" transform=\"rotate({Format(displayRotation)} {Format(x)} {Format(y)})\">");

        switch (support.Kind)
        {
            case SupportGlyphKind.SimpleSupport:
                builder.AppendLine($"        <polygon class=\"support-fill\" points=\"{Format(x)},{Format(y)} {Format(x - size)},{Format(y + size)} {Format(x + size)},{Format(y + size)}\"/>");
                builder.AppendLine($"        <circle class=\"support-fill\" cx=\"{Format(x - 5.0)}\" cy=\"{Format(y + size + 4.0)}\" r=\"3\"/>");
                builder.AppendLine($"        <circle class=\"support-fill\" cx=\"{Format(x + 5.0)}\" cy=\"{Format(y + size + 4.0)}\" r=\"3\"/>");
                builder.AppendLine($"        <line class=\"support-line\" x1=\"{Format(x - size - 4.0)}\" y1=\"{Format(y + size + 8.0)}\" x2=\"{Format(x + size + 4.0)}\" y2=\"{Format(y + size + 8.0)}\"/>");
                break;
            case SupportGlyphKind.Hinge:
                builder.AppendLine($"        <polygon class=\"support-fill\" points=\"{Format(x)},{Format(y)} {Format(x - size)},{Format(y + size)} {Format(x + size)},{Format(y + size)}\"/>");
                builder.AppendLine($"        <line class=\"support-line\" x1=\"{Format(x - size - 4.0)}\" y1=\"{Format(y + size + 4.0)}\" x2=\"{Format(x + size + 4.0)}\" y2=\"{Format(y + size + 4.0)}\"/>");
                break;
            case SupportGlyphKind.Fixed:
                builder.AppendLine($"        <line class=\"support-line\" x1=\"{Format(x)}\" y1=\"{Format(y - 12.0)}\" x2=\"{Format(x)}\" y2=\"{Format(y + 12.0)}\"/>");
                for (int index = -2; index <= 2; index++)
                {
                    double y0 = y + (index * 5.0);
                    builder.AppendLine($"        <line class=\"support-line\" x1=\"{Format(x - 12.0)}\" y1=\"{Format(y0 - 4.0)}\" x2=\"{Format(x)}\" y2=\"{Format(y0 + 4.0)}\"/>");
                }
                break;
            default:
                builder.AppendLine($"        <rect class=\"support-fill\" x=\"{Format(x - 8.0)}\" y=\"{Format(y - 8.0)}\" width=\"16\" height=\"16\" rx=\"2\"/>");
                break;
        }

        builder.AppendLine("      </g>");
        builder.AppendLine($"      <text class=\"support-label\" x=\"{Format(x + 14.0)}\" y=\"{Format(y + 16.0)}\">{EscapeXml(GetSupportLabel(support))}</text>");
        builder.AppendLine("    </g>");
    }

    private static string GetSupportLabel(VisualizationSupport support)
    {
        string typeLabel = support.Kind switch
        {
            SupportGlyphKind.SimpleSupport => "Simple support",
            SupportGlyphKind.Hinge => "Hinge",
            SupportGlyphKind.Fixed => "Fixed",
            _ => "Custom support",
        };

        return string.IsNullOrWhiteSpace(support.Label)
            ? typeLabel
            : $"{support.Label} ({typeLabel})";
    }

    private static void AppendReactionArrow(StringBuilder builder, CoordinateMapper mapper, VisualizationReactionArrow arrow)
    {
        double x1 = mapper.MapX(arrow.Start.X);
        double y1 = mapper.MapY(arrow.Start.Y);
        double x2 = mapper.MapX(arrow.End.X);
        double y2 = mapper.MapY(arrow.End.Y);
        double mx = (x1 + x2) / 2.0;
        double my = (y1 + y2) / 2.0;

        builder.AppendLine($"    <line class=\"reaction\" x1=\"{Format(x1)}\" y1=\"{Format(y1)}\" x2=\"{Format(x2)}\" y2=\"{Format(y2)}\" marker-end=\"url(#reactionArrow)\"/>");
        builder.AppendLine($"    <text class=\"reaction-label\" x=\"{Format(mx + 6.0)}\" y=\"{Format(my - 6.0)}\">{EscapeXml(GetReactionLabel(arrow))}</text>");
    }

    private static string GetReactionLabel(VisualizationReactionArrow arrow)
    {
        string prefix = arrow.ComponentKind switch
        {
            ReactionComponentKind.ForceX => "Rx",
            ReactionComponentKind.ForceY => "Ry",
            _ => "R",
        };

        return $"{prefix} = {Format(arrow.Value)} kN";
    }

    private static void AppendReactionMoment(StringBuilder builder, CoordinateMapper mapper, VisualizationReactionMoment moment)
    {
        double cx = mapper.MapX(moment.Center.X);
        double cy = mapper.MapY(moment.Center.Y);
        double radius = moment.Radius * mapper.Scale;
        radius = Math.Max(12.0, radius);

        (double startAngle, double endAngle, int sweepFlag) = moment.Clockwise
            ? (-45.0, 230.0, 1)
            : (225.0, -50.0, 0);

        (double x1, double y1) = PolarPoint(cx, cy, radius, startAngle);
        (double x2, double y2) = PolarPoint(cx, cy, radius, endAngle);

        builder.AppendLine($"    <path class=\"reaction\" d=\"M {Format(x1)} {Format(y1)} A {Format(radius)} {Format(radius)} 0 1 {sweepFlag} {Format(x2)} {Format(y2)}\" marker-end=\"url(#reactionArrow)\"/>");
        builder.AppendLine($"    <text class=\"reaction-label\" x=\"{Format(cx + radius + 4.0)}\" y=\"{Format(cy - radius)}\">Mz = {Format(moment.Value)} kNm</text>");
    }

    private static void AppendDimension(StringBuilder builder, CoordinateMapper mapper, MemberDimensionAnnotation dimension)
    {
        double x1 = mapper.MapX(dimension.Start.X);
        double y1 = mapper.MapY(dimension.Start.Y);
        double x2 = mapper.MapX(dimension.End.X);
        double y2 = mapper.MapY(dimension.End.Y);
        double dx = x2 - x1;
        double dy = y2 - y1;
        double length = Math.Sqrt((dx * dx) + (dy * dy));
        if (length <= 0.0)
        {
            return;
        }

        double nx = -dy / length;
        double ny = dx / length;
        const double offset = 18.0;
        const double extension = 10.0;
        double sx = x1 + (nx * offset);
        double sy = y1 + (ny * offset);
        double ex = x2 + (nx * offset);
        double ey = y2 + (ny * offset);
        double tx = (sx + ex) / 2.0;
        double ty = (sy + ey) / 2.0 - 4.0;

        builder.AppendLine($"    <line class=\"dimension-extension\" x1=\"{Format(x1)}\" y1=\"{Format(y1)}\" x2=\"{Format(sx + (nx * extension))}\" y2=\"{Format(sy + (ny * extension))}\"/>");
        builder.AppendLine($"    <line class=\"dimension-extension\" x1=\"{Format(x2)}\" y1=\"{Format(y2)}\" x2=\"{Format(ex + (nx * extension))}\" y2=\"{Format(ey + (ny * extension))}\"/>");
        builder.AppendLine($"    <line class=\"dimension\" x1=\"{Format(sx)}\" y1=\"{Format(sy)}\" x2=\"{Format(ex)}\" y2=\"{Format(ey)}\" marker-start=\"url(#dimensionArrow)\" marker-end=\"url(#dimensionArrow)\"/>");
        builder.AppendLine($"    <text class=\"dimension-label\" x=\"{Format(tx)}\" y=\"{Format(ty)}\">L = {Format(dimension.Distance)} m</text>");
    }

    private static void AppendMaximumDisplacement(StringBuilder builder, CoordinateMapper mapper, VisualizationDisplacementAnnotation annotation)
    {
        double x1 = mapper.MapX(annotation.UndeformedPoint.X);
        double y1 = mapper.MapY(annotation.UndeformedPoint.Y);
        double x2 = mapper.MapX(annotation.DeformedPoint.X);
        double y2 = mapper.MapY(annotation.DeformedPoint.Y);
        double tx = ((x1 + x2) / 2.0) + 6.0;
        double ty = ((y1 + y2) / 2.0) - 8.0;
        string line1 = $"umax = {FormatMillimeters(annotation.Magnitude)} @ {annotation.NodeId}";
        string line2 = $"Ux = {FormatMillimeters(annotation.Ux)}, Uy = {FormatMillimeters(annotation.Uy)}";
        string line3 = $"Rz = {Format(annotation.Rz)} rad ({FormatRadiansAsDegrees(annotation.Rz)}°)";

        builder.AppendLine($"    <line class=\"annotation-line\" x1=\"{Format(x1)}\" y1=\"{Format(y1)}\" x2=\"{Format(x2)}\" y2=\"{Format(y2)}\" marker-end=\"url(#reactionArrow)\"/>");
        builder.AppendLine($"    <circle class=\"annotation-point\" cx=\"{Format(x2)}\" cy=\"{Format(y2)}\" r=\"3.5\" style=\"stroke:#dc2626\"/>");
        builder.AppendLine($"    <text class=\"annotation-label\" x=\"{Format(tx)}\" y=\"{Format(ty)}\">{EscapeXml(line1)}</text>");
        builder.AppendLine($"    <text class=\"annotation-label\" x=\"{Format(tx)}\" y=\"{Format(ty + 14.0)}\">{EscapeXml(line2)}</text>");
        builder.AppendLine($"    <text class=\"annotation-label\" x=\"{Format(tx)}\" y=\"{Format(ty + 28.0)}\">{EscapeXml(line3)}</text>");
    }

    private static void AppendNodeDisplacementLabel(StringBuilder builder, CoordinateMapper mapper, VisualizationNodeDisplacementLabel label, double deformationScale)
    {
        double x = mapper.MapX(label.Position.X);
        double y = mapper.MapY(label.Position.Y);
        (double deltaX, double deltaY) = GetDisplayDisplacement(mapper, label.Position, label.Ux, label.Uy, deformationScale);
        string name = string.IsNullOrWhiteSpace(label.Label) ? label.NodeId : label.Label!;

        builder.AppendLine($"    <g class=\"displacement-label\" data-deformation-label=\"true\" data-dx=\"{Format(deltaX)}\" data-dy=\"{Format(deltaY)}\" data-node-id=\"{EscapeXml(label.NodeId)}\">");
        builder.AppendLine($"      <circle class=\"displacement-label-anchor\" cx=\"{Format(x)}\" cy=\"{Format(y)}\" r=\"3\"/>");
        builder.AppendLine($"      <text class=\"displacement-label\" x=\"{Format(x + 8.0)}\" y=\"{Format(y + 12.0)}\">{EscapeXml(name)}: u = {FormatMillimeters(label.ResultantDisplacement)}</text>");
        builder.AppendLine($"      <text class=\"displacement-label\" x=\"{Format(x + 8.0)}\" y=\"{Format(y + 25.0)}\">Ux = {FormatMillimeters(label.Ux)}, Uy = {FormatMillimeters(label.Uy)}</text>");
        builder.AppendLine($"      <text class=\"displacement-label\" x=\"{Format(x + 8.0)}\" y=\"{Format(y + 38.0)}\">Rz = {Format(label.Rz)} rad</text>");
        builder.AppendLine("    </g>");
    }

    private static void AppendMemberDisplacementLabel(StringBuilder builder, CoordinateMapper mapper, VisualizationMemberDisplacementLabel label, double deformationScale)
    {
        double x = mapper.MapX(label.Position.X);
        double y = mapper.MapY(label.Position.Y);
        (double deltaX, double deltaY) = GetDisplayDisplacement(mapper, label.Position, label.GlobalUx, label.GlobalUy, deformationScale);

        builder.AppendLine($"    <g class=\"member-displacement-label\" data-deformation-label=\"true\" data-dx=\"{Format(deltaX)}\" data-dy=\"{Format(deltaY)}\" data-member-id=\"{EscapeXml(label.MemberId)}\" data-station=\"{EscapeXml(label.StationLabel)}\">");
        builder.AppendLine($"      <circle class=\"member-displacement-label-anchor\" cx=\"{Format(x)}\" cy=\"{Format(y)}\" r=\"3\"/>");
        builder.AppendLine($"      <text class=\"member-displacement-label\" x=\"{Format(x + 7.0)}\" y=\"{Format(y - 6.0)}\">{EscapeXml(label.MemberId)} {EscapeXml(label.StationLabel)}: u = {FormatMillimeters(label.ResultantDisplacement)}</text>");
        builder.AppendLine($"      <text class=\"member-displacement-label\" x=\"{Format(x + 7.0)}\" y=\"{Format(y + 7.0)}\">Ux = {FormatMillimeters(label.GlobalUx)}, Uy = {FormatMillimeters(label.GlobalUy)}</text>");
        builder.AppendLine($"      <text class=\"member-displacement-label\" x=\"{Format(x + 7.0)}\" y=\"{Format(y + 20.0)}\">x = {Format(label.Distance)} m, Rz = {Format(label.LocalRz)} rad</text>");
        builder.AppendLine("    </g>");
    }

    private static void AppendDiagramAnnotation(StringBuilder builder, CoordinateMapper mapper, DiagramValueAnnotation annotation)
    {
        double x = mapper.MapX(annotation.Position.X);
        double y = mapper.MapY(annotation.Position.Y);
        string cssKind = GetDiagramCssSuffix(annotation.Kind);
        string prefix = annotation.Kind switch
        {
            VisualizationDiagramKind.NormalForce => "Nmax",
            VisualizationDiagramKind.ShearForce => "Vmax",
            _ => "Mmax",
        };
        string unit = annotation.Kind == VisualizationDiagramKind.BendingMoment ? "kNm" : "kN";

        builder.AppendLine($"    <circle class=\"annotation-point {cssKind}\" cx=\"{Format(x)}\" cy=\"{Format(y)}\" r=\"4\"/>");
        builder.AppendLine($"    <text class=\"annotation-label\" x=\"{Format(x + 8.0)}\" y=\"{Format(y - 6.0)}\">{prefix} = {Format(annotation.Value)} {unit}</text>");
    }

    private static string GetDiagramCssClass(VisualizationDiagramKind kind) =>
        kind switch
        {
            VisualizationDiagramKind.NormalForce => "diagram normal-force",
            VisualizationDiagramKind.ShearForce => "diagram shear-force",
            VisualizationDiagramKind.BendingMoment => "diagram bending-moment",
            _ => "diagram",
        };

    private static string GetDiagramCssSuffix(VisualizationDiagramKind kind) =>
        kind switch
        {
            VisualizationDiagramKind.NormalForce => "normal-force",
            VisualizationDiagramKind.ShearForce => "shear-force",
            VisualizationDiagramKind.BendingMoment => "bending-moment",
            _ => "diagram",
        };

    private static (double X, double Y) PolarPoint(double cx, double cy, double radius, double degrees)
    {
        double radians = degrees * Math.PI / 180.0;
        return (cx + (Math.Cos(radians) * radius), cy + (Math.Sin(radians) * radius));
    }

    private static void AppendLegend(StringBuilder builder, SvgExportOptions options, StructuralVisualizationModel model)
    {
        double legendX = options.Width - 230.0;
        double currentY = options.Padding + 4.0;
        double lineX1 = legendX;
        double lineX2 = legendX + 28.0;
        double textX = legendX + 36.0;

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

    private static string BuildInterpolatedBasePoints(VisualizationMember member, int pointCount, CoordinateMapper mapper)
    {
        if (pointCount <= 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        for (int index = 0; index < pointCount; index++)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            double t = pointCount == 1 ? 0.0 : (double)index / (pointCount - 1);
            double x = member.Start.X + ((member.End.X - member.Start.X) * t);
            double y = member.Start.Y + ((member.End.Y - member.Start.Y) * t);
            builder.Append(Format(mapper.MapX(x)));
            builder.Append(',');
            builder.Append(Format(mapper.MapY(y)));
        }

        return builder.ToString();
    }

    private static string BuildDisplacementPoints(VisualizationMember member, IReadOnlyList<VisualizationPoint> deformedPoints, CoordinateMapper mapper)
    {
        StringBuilder builder = new();
        for (int index = 0; index < deformedPoints.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            double t = deformedPoints.Count == 1 ? 0.0 : (double)index / (deformedPoints.Count - 1);
            double baseX = member.Start.X + ((member.End.X - member.Start.X) * t);
            double baseY = member.Start.Y + ((member.End.Y - member.Start.Y) * t);
            VisualizationPoint deformed = deformedPoints[index];
            double dx = mapper.MapX(deformed.X) - mapper.MapX(baseX);
            double dy = mapper.MapY(deformed.Y) - mapper.MapY(baseY);
            builder.Append(Format(dx));
            builder.Append(',');
            builder.Append(Format(dy));
        }

        return builder.ToString();
    }

    private static (double DeltaX, double DeltaY) GetDisplayDisplacement(
        CoordinateMapper mapper,
        VisualizationPoint deformedPosition,
        double ux,
        double uy,
        double deformationScale)
    {
        double baseX = deformedPosition.X - (ux * deformationScale);
        double baseY = deformedPosition.Y - (uy * deformationScale);
        return (
            mapper.MapX(deformedPosition.X) - mapper.MapX(baseX),
            mapper.MapY(deformedPosition.Y) - mapper.MapY(baseY));
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

    private static string FormatMillimeters(double meters) =>
        $"{Format(meters * 1000.0)} mm";

    private static string FormatRadiansAsDegrees(double radians) =>
        Format(radians * 180.0 / Math.PI);

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
