using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Reporting.Pdf;

/// <summary>Small vector layout engine: all coordinates are PDF points on an A4 page.</summary>
internal sealed class PdfReportLayout
{
    private const double Left = 48, Width = 499, Top = 788, Bottom = 58;
    private const string Ink = "0.12 0.19 0.28", Muted = "0.36 0.42 0.48", Teal = "0.00 0.43 0.46";
    private readonly List<StringBuilder> pages = new();
    private readonly string title;
    private StringBuilder page = null!;
    private double y;
    private int section;

    public PdfReportLayout(string title)
    {
        this.title = title;
        NewPage();
    }

    public void Hero(string reportTitle, string? description, string analysis, string? source)
    {
        Text("STRUCTURAL ANALYSIS / TECHNICAL REPORT", Left, y, 9, "F3", Teal);
        y -= 28;
        foreach (string line in PdfTextLayout.Wrap(reportTitle, 18, false, Width - 2))
        {
            Ensure(24);
            Text(line, Left, y, 18);
            y -= 24;
        }
        y -= 3;
        if (!string.IsNullOrWhiteSpace(description)) Paragraph(description);
        Paragraph($"Analysis id: {analysis}", muted: true);
        if (!string.IsNullOrWhiteSpace(source)) Paragraph($"Source: {source}", muted: true);
        Paragraph($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm zzz}", muted: true);
        y -= 4;
    }

    public void Heading(string text)
    {
        Ensure(76); // Keep the section label with its first content block or table header and row.
        y -= 9;
        Rectangle(Left, y - 17, 24, 22, Teal);
        Text((++section).ToString("00", CultureInfo.InvariantCulture), Left + 5, y - 10, 9, "F3", "1 1 1");
        Text(text, Left + 35, y - 10, 12, "F3");
        Line(Left + 35, y - 19, Left + Width, y - 19, "0.83 0.87 0.89", .5);
        y -= 34;
    }

    public void Paragraph(string text, bool muted = false)
    {
        double size = muted ? 8.5 : 10;
        double leading = muted ? 12 : 14;
        foreach (string line in PdfTextLayout.Wrap(text, size, false, Width - 2))
        {
            Ensure(leading);
            Text(line, Left, y - size, size, color: muted ? Muted : Ink);
            y -= leading;
        }
        y -= 5;
    }

    public void Statistics(IReadOnlyList<(string Label, int Value)> values)
    {
        Ensure(56);
        double cell = Width / values.Count;
        Rectangle(Left, y - 44, Width, 44, "0.95 0.97 0.98");
        for (int i = 0; i < values.Count; i++)
        {
            double x = Left + i * cell + 10;
            Text(values[i].Value.ToString(CultureInfo.InvariantCulture), x, y - 20, 16, "F3");
            Text(values[i].Label, x, y - 34, 8, color: Muted);
            if (i > 0) Line(x - 10, y - 34, x - 10, y - 10, "0.83 0.87 0.89", .5);
        }
        y -= 56;
    }

    public void Highlights(IReadOnlyList<(string Label, string Value)> values)
    {
        double cell = (Width - 16) / values.Count;
        var lines = values.Select(value => PdfTextLayout.Wrap(value.Value, 12, true, cell - 22).ToArray()).ToArray();
        double height = 30 + lines.Max(value => value.Length) * 15;
        Ensure(height + 10);
        for (int i = 0; i < values.Count; i++)
        {
            double x = Left + i * (cell + 8);
            Rectangle(x, y - height, cell, height, "0.92 0.97 0.96");
            Rectangle(x, y - height, 2, height, Teal);
            Text(values[i].Label, x + 11, y - 14, 8, "F3", Teal);
            for (int j = 0; j < lines[i].Length; j++)
                Text(lines[i][j], x + 11, y - 32 - j * 15, 12, "F2");
        }
        y -= height + 10;
    }

    public void Table(string name, string[] headers, double[] widths, IEnumerable<string[]> rows, int rightAlignedFrom)
    {
        const double size = 8, leading = 10, rowPadding = 8, padding = 7, headerHeight = 24;
        Ensure(headerHeight + 22);
        Header();
        int rowIndex = 0;
        foreach (string[] row in rows)
        {
            string[][] cells = row.Select((cell, index) => PdfTextLayout.Wrap(cell, size, index >= rightAlignedFrom,
                widths[index] - padding * 2).ToArray()).ToArray();
            int lineCount = cells.Max(cell => cell.Length);
            int offset = 0;
            double rowHeight = lineCount * leading + rowPadding;
            // Keep ordinary multiline rows together. Oversized rows still flow across pages.
            if (rowHeight <= Top - Bottom - 22 - headerHeight && y - rowHeight < Bottom)
                Continue();
            // A very long identifier can span pages. Repeat headers and retain every cell line.
            while (offset < lineCount)
            {
                int capacity = (int)Math.Floor((y - Bottom - rowPadding) / leading);
                if (capacity < 1)
                {
                    Continue();
                    capacity = (int)Math.Floor((y - Bottom - rowPadding) / leading);
                }
                int count = Math.Min(lineCount - offset, capacity);
                double height = count * leading + rowPadding;
                Rectangle(Left, y - height, Width, height, rowIndex % 2 == 0 ? "0.96 0.97 0.98" : "1 1 1");
                double x = Left;
                for (int column = 0; column < cells.Length; column++)
                {
                    for (int line = 0; line < count && offset + line < cells[column].Length; line++)
                    {
                        string value = cells[column][offset + line];
                        bool numeric = column >= rightAlignedFrom;
                        double tx = numeric ? x + widths[column] - padding - PdfTextLayout.Width(value, size, true) : x + padding;
                        Text(value, tx, y - 12 - line * leading, size, numeric ? "F2" : "F1");
                    }
                    x += widths[column];
                }
                Line(Left, y - height, Left + Width, y - height, "0.88 0.90 0.92", .35);
                y -= height;
                offset += count;
            }
            rowIndex++;
        }
        if (rowIndex == 0) Paragraph("No results available.", muted: true);
        y -= 8;

        void Continue()
        {
            NewPage();
            Text(name + " / continued", Left, y - 9, 9, color: Muted);
            y -= 22;
            Header();
        }

        void Header()
        {
            Rectangle(Left, y - headerHeight, Width, headerHeight, Ink);
            double x = Left;
            for (int i = 0; i < headers.Length; i++)
            {
                Text(headers[i], x + padding, y - 15, 8, "F3", "1 1 1");
                x += widths[i];
            }
            y -= headerHeight;
        }
    }

    public void ModelFigure(StructuralModel model, IReadOnlyList<MemberDisplacementDiagram> diagrams)
    {
        if (model.Nodes.Count == 0) return;
        const double height = 146;
        Ensure(height + 10);
        Rectangle(Left, y - height, Width, height, "0.98 0.99 0.99");
        Text("GEOMETRY & DEFORMATION", Left + 12, y - 16, 8, "F3", Muted);
        // Normalize first to avoid overflowing the extent calculation for extreme coordinates.
        double coordinateScale = model.Nodes.Max(node => Math.Max(Math.Abs(node.X), Math.Abs(node.Y)));
        if (coordinateScale == 0) coordinateScale = 1;
        double minX = model.Nodes.Min(node => node.X / coordinateScale), maxX = model.Nodes.Max(node => node.X / coordinateScale);
        double minY = model.Nodes.Min(node => node.Y / coordinateScale), maxY = model.Nodes.Max(node => node.Y / coordinateScale);
        double sx = maxX == minX ? double.PositiveInfinity : (Width - 86) / (maxX - minX);
        double sy = maxY == minY ? double.PositiveInfinity : 65 / (maxY - minY);
        double scale = Math.Min(sx, sy);
        if (!double.IsFinite(scale)) scale = 1;
        double cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
        var nodes = model.Nodes.ToDictionary(node => node.Id);
        var members = model.Members.ToDictionary(member => member.Id);
        var projected = model.Nodes.ToDictionary(node => node.Id, node => Project(node.X / coordinateScale, node.Y / coordinateScale));
        double maxDisplacement = diagrams.SelectMany(diagram => diagram.Samples)
            .Select(sample => Math.Max(Math.Abs(sample.GlobalUx), Math.Abs(sample.GlobalUy))).DefaultIfEmpty(0).Max();
        page.AppendLine($"q {F(Left)} {F(y - height + 26)} {F(Width)} {F(height - 48)} re W n");
        foreach (StructuralMember member in model.Members)
        {
            if (!projected.TryGetValue(member.StartNodeId, out var a) || !projected.TryGetValue(member.EndNodeId, out var b)) continue;
            Line(a.X, a.Y, b.X, b.Y, "0.62 0.68 0.72", 1.5);
        }
        foreach (var diagram in diagrams)
        {
            if (!members.TryGetValue(diagram.MemberId, out var member) ||
                !nodes.TryGetValue(member.StartNodeId, out var a) || !nodes.TryGetValue(member.EndNodeId, out var b)) continue;
            (double X, double Y)? previous = null;
            foreach (var sample in diagram.Samples.OrderBy(sample => sample.NormalizedPosition))
            {
                double t = sample.NormalizedPosition;
                var p = Project((1 - t) * (a.X / coordinateScale) + t * (b.X / coordinateScale),
                    (1 - t) * (a.Y / coordinateScale) + t * (b.Y / coordinateScale));
                if (maxDisplacement > 0)
                    p = (p.X + 15 * (sample.GlobalUx / maxDisplacement), p.Y + 15 * (sample.GlobalUy / maxDisplacement));
                if (previous is { } last) Line(last.X, last.Y, p.X, p.Y, Teal, 1.7);
                previous = p;
            }
        }
        foreach (var node in model.Nodes)
        {
            var p = projected[node.Id];
            Rectangle(p.X - 2, p.Y - 2, 4, 4, Ink);
            if (model.Nodes.Count <= 12 && PdfTextLayout.Width(node.Id, 7) < 46)
                Text(node.Id, Math.Min(p.X + 5, Left + Width - 50), p.Y + 7, 7, color: Muted);
        }
        page.AppendLine("Q");
        Text("Grey: original geometry   /   Teal: auto-amplified deformation", Left + 12, y - height + 12, 8, color: Muted);
        y -= height + 10;

        (double X, double Y) Project(double x, double yy) => (Left + Width / 2 + (x - cx) * scale, y - 76 + (yy - cy) * scale);
    }

    public IReadOnlyList<string> Finish()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            page = pages[i];
            Line(Left, 38, Left + Width, 38, "0.83 0.87 0.89", .5);
            Text("StructuralSolver2D  /  Linear elastic 2D analysis", Left, 23, 8, color: Muted);
            string number = $"Page {i + 1} of {pages.Count}";
            Text(number, Left + Width - PdfTextLayout.Width(number, 8), 23, 8, color: Muted);
        }
        return pages.Select(value => value.ToString()).ToArray();
    }

    private void Ensure(double height)
    {
        if (y - height < Bottom) NewPage();
    }

    private void NewPage()
    {
        page = new StringBuilder();
        pages.Add(page);
        y = Top;
        Rectangle(0, 838, 595, 4, Teal);
        Text("StructuralSolver2D", Left, 817, 9, "F3", Teal);
        string running = PdfTextLayout.Wrap(title, 8, false, 330).First();
        if (running.Length < title.Length) running += "...";
        Text(running, 205, 817, 8, color: Muted);
        Line(Left, 804, Left + Width, 804, "0.83 0.87 0.89", .5);
    }

    private void Text(string value, double x, double yy, double size, string font = "F1", string color = Ink)
    {
        page.AppendLine($"BT /{font} {F(size)} Tf {color} rg");
        page.AppendLine($"{F(x)} {F(yy)} Td ({PdfTextLayout.Escape(value)}) Tj");
        page.AppendLine("ET");
    }

    private void Rectangle(double x, double yy, double width, double height, string color) =>
        page.AppendLine($"{color} rg {F(x)} {F(yy)} {F(width)} {F(height)} re f");

    private void Line(double x1, double y1, double x2, double y2, string color, double width) =>
        page.AppendLine($"{color} RG {F(width)} w {F(x1)} {F(y1)} m {F(x2)} {F(y2)} l S");

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
