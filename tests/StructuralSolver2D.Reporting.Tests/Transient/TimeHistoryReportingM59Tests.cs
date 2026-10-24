using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Reporting.Transient;

namespace StructuralSolver2D.Reporting.Tests.Transient;

public sealed class TimeHistoryReportingM59Tests
{
    [Fact]
    public void ReportCsvAndViewer_AreSelfContainedAndCarryUnits()
    {
        var model=new StructuralModel().AddMaterial(new StructuralMaterial("M","Steel",210_000_000,77))
            .AddSection(new StructuralSection("S","Section",.01,.0001))
            .AddNode(new StructuralNode("A",0,0)).AddNode(new StructuralNode("B",1,0))
            .AddMember(new StructuralMember("E","A","B","M","S"))
            .AddSupport(StructuralSupport.Fixed("SUP","A"));
        var result=new StructuralTimeHistoryAnalysisService().Analyze(new(model,new StructuralDynamicsDefinition(),
            .005,12,new StructuralHarmonicDamping {UniformModalRatio=.03},
            new[]{new StructuralTimeLoadPattern(new[]{new StructuralHarmonicNodalForce("B",Fy:-1)},
                new[]{new StructuralTimeLoadKnot(0,0),new StructuralTimeLoadKnot(.01,1),new StructuralTimeLoadKnot(.06,1)})}));
        var report=new TimeHistoryReportExporter();
        Assert.Contains("Balance residual [kN*m]",report.ExportMarkdown(result,"M59 test"));
        Assert.Contains("Ay_m_s2",report.ExportCsv(result));
        Assert.Contains("Dissipated_kNm",report.ExportEnergyCsv(result));
        string html=new InteractiveHtmlTimeHistoryViewerExporter().Export(model,result,"M59 test");
        Assert.Contains("id=\"history\"",html);
        Assert.Contains("id=\"structure\"",html);
        Assert.Contains("M59 test",html);
        Assert.Contains("const frames = data.frames",html);
    }
}
