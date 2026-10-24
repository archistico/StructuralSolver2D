using System.Net;
using System.Reflection;
using System.Text.Json;
using StructuralSolver2D.Analysis.PublicApi;

namespace StructuralSolver2D.Reporting.Nonlinear;

/// <summary>
/// M69: offline viewer for accepted states of the M68 nonlinear Truss2D research model.
/// This exporter NEVER solves, interpolates constitutive states, or includes rejected Newton trials.
/// </summary>
public sealed class InteractiveHtmlNonlinearTruss2DViewerExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string ExportStatic(NonlinearTruss2DModel model, StructuralNonlinearStaticOutput output, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        CheckModel(model, output.NodeIds, output.MemberIds);
        foreach (var step in output.Steps)
            CheckStep(step.Displacements, step.Reactions, step.AxialForces, step.MaterialStates,
                output.NodeIds.Count, output.MemberIds.Count);
        var steps = output.Steps.Select(s => new
        {
            index = s.Index, position = s.LoadFactor, loadFactor = s.LoadFactor,
            displacements = s.Displacements, reactions = s.Reactions,
            axialForces = s.AxialForces,
            plasticStrain = s.MaterialStates.Select(x => x.PlasticStrain).ToArray(),
            accumulatedPlasticStrain = s.MaterialStates.Select(x => x.AccumulatedPlasticStrain).ToArray(),
            plasticDissipation = s.CumulativePlasticDissipation,
            residual = s.ResidualNorm, iterations = s.NewtonIterations
        }).ToArray();
        return Render(model, title, "static", output.Completed, output.Failure, steps);
    }

    public string ExportTime(NonlinearTruss2DModel model, StructuralNonlinearTimeOutput output, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        CheckModel(model, output.NodeIds, output.MemberIds);
        foreach (var step in output.Steps)
            CheckStep(step.Displacements, step.Reactions, step.AxialForces, step.MaterialStates,
                output.NodeIds.Count, output.MemberIds.Count);
        var steps = output.Steps.Select(s => new
        {
            index = s.Index, position = s.Time, loadFactor = s.LoadFactor,
            displacements = s.Displacements, reactions = s.Reactions,
            axialForces = s.AxialForces,
            plasticStrain = s.MaterialStates.Select(x => x.PlasticStrain).ToArray(),
            accumulatedPlasticStrain = s.MaterialStates.Select(x => x.AccumulatedPlasticStrain).ToArray(),
            plasticDissipation = s.CumulativePlasticDissipation,
            residual = s.ReducedResidualNorm, iterations = s.NewtonIterations,
            kinetic = s.KineticEnergy, recoverable = s.RecoverableEnergy,
            hardening = s.HardeningEnergy, viscous = s.CumulativeViscousDissipation,
            physicalEnergyResidual = s.PhysicalEnergyResidual
        }).ToArray();
        return Render(model, title, "time", output.Completed, output.Failure, steps);
    }

    private static void CheckModel(NonlinearTruss2DModel model, IReadOnlyList<string> nodeIds,
        IReadOnlyList<string> memberIds)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(nodeIds);
        ArgumentNullException.ThrowIfNull(memberIds);
        if (model.Nodes.Count != nodeIds.Count || model.Members.Count != memberIds.Count ||
            !model.Nodes.Select(n => n.Id).SequenceEqual(nodeIds) ||
            !model.Members.Select(m => m.Id).SequenceEqual(memberIds))
            throw new ArgumentException("M69_MODEL_ORDER: The viewer model must match the result node/member order.");
    }

    private static void CheckStep(IReadOnlyList<double> displacements, IReadOnlyList<double> reactions,
        IReadOnlyList<double> forces, IReadOnlyList<StructuralNonlinearMaterialState> states,
        int nodeCount, int memberCount)
    {
        if (displacements.Count != 2 * nodeCount || reactions.Count != 2 * nodeCount ||
            forces.Count != memberCount || states.Count != memberCount)
            throw new ArgumentException("M69_HISTORY_SHAPE: Nonlinear history has inconsistent nodal/member dimensions.");
    }

    private static string Render<T>(NonlinearTruss2DModel model, string? title,
        string mode, bool completed, string? failure, T steps)
    {
        var data = new
        {
            title = title ?? "StructuralSolver2D — Reticolare elasto-plastica",
            mode, completed, failure,
            nodes = model.Nodes.Select(n => new { n.Id, n.X, n.Y }).ToArray(),
            members = model.Members.Select(m => new { m.Id, m.StartNodeId, m.EndNodeId }).ToArray(),
            loads = model.Loads.Select(l => new { l.NodeId, l.Fx, l.Fy }).ToArray(),
            steps
        };
        // System.Text.Json default encoder escapes <, > and & inside the script data element.
        string json = JsonSerializer.Serialize(data, JsonOptions);
        string css = ReadEmbedded("StructuralSolver2D.Reporting.nonlinear-viewer.css");
        string script = ReadEmbedded("StructuralSolver2D.Reporting.nonlinear-viewer.js");
        string status = completed ? "Completa" : "PARZIALE — solo passi convergenti";
        return "<!doctype html><html lang=\"it\"><head><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
            "<title>StructuralSolver2D — M69</title><style>" + css + "</style></head><body><main>" +
            "<header><h1>M69 · Viewer non lineare Truss2D</h1><p id=\"title\"></p>" +
            "<p id=\"status\" role=\"status\" class=\"status\">" + WebUtility.HtmlEncode(status) + "</p></header>" +
            "<p class=\"disclaimer\">Risultati di ricerca: piccole deformazioni, incrudimento isotropo, nessuna verifica normativa, danno o collasso. " +
            "Le deformate sono amplificate solo per la visualizzazione; i dati dei grafici restano fisici. " +
            "Le curve collegano campioni convergenti: non rappresentano stati intermedi risolti.</p>" +
            "<section class=\"panel controls\"><button id=\"play\" type=\"button\">Riproduci</button> " +
            "<label>Passo <input id=\"step\" type=\"range\" min=\"0\" max=\"0\" step=\"1\" value=\"0\"></label> " +
            "<label>Amplificazione grafica <input id=\"scale\" type=\"range\" min=\"0\" max=\"100\" value=\"35\"></label> " +
            "<span id=\"step-label\"></span></section>" +
            "<section class=\"panel\"><div class=\"legend\"><span>━━ Geometria iniziale</span><span class=\"elastic\">━━ Elastico</span>" +
            "<span class=\"plastic\">━━ Deformazione plastica accumulata</span><span class=\"selected\">━━ Asta selezionata</span></div>" +
            "<svg id=\"structure\" viewBox=\"0 0 900 400\" role=\"img\" aria-label=\"Reticolare e deformata ingrandita\"></svg>" +
            "<p id=\"member-value\"></p></section>" +
            "<section class=\"panel\"><label>Nodo <select id=\"node\"></select></label> " +
            "<label>Componente <select id=\"dof\"><option value=\"0\">Ux [m]</option><option value=\"1\">Uy [m]</option></select></label> " +
            "<label>Asta <select id=\"member\"></select></label>" +
            "<p id=\"node-value\"></p><div class=\"charts\"><div><h2>Carico–spostamento</h2><svg id=\"load-chart\" viewBox=\"0 0 600 270\" role=\"img\" aria-label=\"Fattore di carico e spostamento nodale\"></svg></div>" +
            "<div><h2>Forza assiale dell’asta [kN]</h2><svg id=\"force-chart\" viewBox=\"0 0 600 270\" role=\"img\" aria-label=\"Forza assiale nel corso dei passi accettati\"></svg></div></div>" +
            "<div class=\"charts\"><div><h2>Deformazione plastica accumulata</h2><svg id=\"plastic-chart\" viewBox=\"0 0 600 270\" role=\"img\" aria-label=\"Deformazione plastica accumulata\"></svg></div>" +
            "<div><h2>Dissipazione plastica cumulata [kN·m]</h2><svg id=\"energy-chart\" viewBox=\"0 0 600 270\" role=\"img\" aria-label=\"Dissipazione nel corso dei passi accettati\"></svg></div></div></section>" +
            "<p class=\"disclaimer\" id=\"footer-note\"></p>" +
            "<script type=\"application/json\" id=\"dataset\">" + json + "</script>" +
            "<script>" + script + "</script></main></body></html>";
    }

    private static string ReadEmbedded(string name)
    {
        using Stream stream = typeof(InteractiveHtmlNonlinearTruss2DViewerExporter).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"M69_ASSET_MISSING: {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
