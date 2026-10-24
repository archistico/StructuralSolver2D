using StructuralSolver2D.Analysis.Dynamics.Transient;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Tests.Dynamics.M59;

public sealed class TimeHistoryM59Tests
{
    private static StructuralModel Cantilever()
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 77))
            .AddSection(new StructuralSection("SEC", "Section", 0.01, 0.0001));
        for (int i = 0; i <= 4; i++) model.AddNode(new StructuralNode($"N{i}", i, 0));
        for (int i = 0; i < 4; i++) model.AddMember(new StructuralMember($"E{i}", $"N{i}", $"N{i+1}", "MAT", "SEC"));
        model.AddSupport(StructuralSupport.Fixed("S", "N0"));
        return model;
    }
    private static readonly StructuralHarmonicDamping Damp = new() { UniformModalRatio = .05 };
    private static StructuralTimeLoadPattern Constant(double fy, string node="N4") =>
        new(new[] { new StructuralHarmonicNodalForce(node, Fy: fy) },
            new[] { new StructuralTimeLoadKnot(0,1), new StructuralTimeLoadKnot(1,1) });

    [Fact]
    public void Newmark_UndampedFreeSdof_ConservesEnergy_AndConvergesToCosine()
    {
        const double omega=10, u0=.02, end=.5;
        double Run(double dt)
        {
            var state = new NewmarkAverageAcceleration(omega, 0, dt, u0, 0, 0);
            double energy0=.5*omega*omega*u0*u0;
            for (int i=0; i<(int)Math.Round(end/dt); i++)
            {
                state.Advance(0);
                double energy=.5*(state.Velocity*state.Velocity+omega*omega*state.Displacement*state.Displacement);
                Assert.InRange(Math.Abs(energy-energy0),0,1e-11);
            }
            return Math.Abs(state.Displacement-u0*Math.Cos(omega*end));
        }
        double coarse=Run(.01),fine=Run(.005);
        Assert.True(fine<coarse*.30, $"Expected second order convergence: coarse={coarse}, fine={fine}");
    }

    [Fact]
    public void Newmark_DampedFreeSdof_LosesEnergy_AndHasCorrectInitialAcceleration()
    {
        var m=new NewmarkAverageAcceleration(12,0.08,.003,.025,.1,0);
        Assert.InRange(Math.Abs(m.Acceleration-(-2*.08*12*.1-144*.025)),0,1e-12);
        double initial=.5*(m.Velocity*m.Velocity+144*m.Displacement*m.Displacement);
        for(int i=0;i<500;i++) m.Advance(0);
        double last=.5*(m.Velocity*m.Velocity+144*m.Displacement*m.Displacement);
        Assert.True(last<initial*.2);
        Assert.True(last>=0);
    }

    [Fact]
    public void Newmark_StepForce_ApproachesStaticDisplacement()
    {
        var m = new NewmarkAverageAcceleration(10,.2,.001,0,0,0);
        for(int i=0;i<5000;i++)m.Advance(2);
        Assert.InRange(Math.Abs(m.Displacement-.02),0,1e-5);
        Assert.InRange(Math.Abs(m.Velocity),0,1e-4);
    }

    [Fact]
    public void TimeService_ReconstructsPhysicalNodalMotionAndEnergy()
    {
        var request=new StructuralTimeHistoryAnalysisRequest(Cantilever(),new StructuralDynamicsDefinition(),
            .002,100,Damp,new[] { new StructuralTimeLoadPattern(
                new[] { new StructuralHarmonicNodalForce("N4",Fy:-1) },
                new[] { new StructuralTimeLoadKnot(0,0),new StructuralTimeLoadKnot(.02,1),
                    new StructuralTimeLoadKnot(.04,0),new StructuralTimeLoadKnot(.2,0) }) });
        var result = new StructuralTimeHistoryAnalysisService().Analyze(request);
        Assert.Equal(101,result.Steps.Count);
        Assert.Equal(5,result.Envelopes.Count);
        Assert.Equal(0,result.Steps[0].Nodes[^1].Uy,12);
        Assert.True(result.Envelopes[^1].MaxAbsUy>0);
        Assert.All(result.Steps,s => {
            Assert.InRange(Math.Abs(s.Nodes[0].Ux),0,1e-10);
            Assert.InRange(Math.Abs(s.Nodes[0].Uy),0,1e-10);
            Assert.InRange(Math.Abs(s.Nodes[0].Rz),0,1e-10);
            Assert.True(double.IsFinite(s.Energy.BalanceResidual));
            Assert.InRange(Math.Abs(s.Energy.BalanceResidual),0,1e-8);
            Assert.True(s.Energy.DissipatedEnergy>=-1e-12);
        });
        Assert.InRange(Math.Abs(result.Steps[^1].TimeSeconds-.2),0,1e-12);
    }

    [Fact]
    public void TimeService_MultiplePatterns_ObeyLinearSuperposition()
    {
        var model=Cantilever();
        var first=Constant(-1);var second=Constant(.25,"N3");
        StructuralTimeHistoryAnalysisOutput Compute(params StructuralTimeLoadPattern[] loads) =>
            new StructuralTimeHistoryAnalysisService().Analyze(new(model,new StructuralDynamicsDefinition(),
                .002,35,Damp,loads));
        var a=Compute(first);var b=Compute(second);var combined=Compute(first,second);
        for(int i=0;i<combined.Steps.Count;i++)
        {
            double expected=a.Steps[i].Nodes[^1].Uy+b.Steps[i].Nodes[^1].Uy;
            Assert.InRange(Math.Abs(combined.Steps[i].Nodes[^1].Uy-expected),0,1e-11);
        }
    }

    [Fact]
    public void TimeService_ReducingTimeStep_ConvergesForTheSameLinearLoadHistory()
    {
        var model = Cantilever();
        var pattern = new StructuralTimeLoadPattern(
            new[] {new StructuralHarmonicNodalForce("N4", Fy:-1)},
            new[] {new StructuralTimeLoadKnot(0,0),new StructuralTimeLoadKnot(.02,1),
                new StructuralTimeLoadKnot(.12,1)});
        double Tip(double dt, int steps) => new StructuralTimeHistoryAnalysisService()
            .Analyze(new(model, new StructuralDynamicsDefinition(), dt,steps, Damp,new[]{pattern}))
            .Steps[^1].Nodes[^1].Uy;
        double coarse=Tip(.004,30), medium=Tip(.002,60), fine=Tip(.001,120);
        double coarseGap=Math.Abs(coarse-medium), fineGap=Math.Abs(medium-fine);
        Assert.True(coarseGap>1e-12);
        Assert.True(fineGap<coarseGap*.8,
            $"M59 timestep convergence: coarse-medium={coarseGap}, medium-fine={fineGap}");
    }

    [Fact]
    public void TimeService_FreeInitialVelocity_AndSupportCompatibility()
    {
        var model=Cantilever();
        var input=new StructuralTimeHistoryAnalysisRequest(model,new StructuralDynamicsDefinition(),
            .001,10,Damp,Array.Empty<StructuralTimeLoadPattern>(),
            new[] { new StructuralTimeInitialNodeState("N4",Vy:.01) });
        var result=new StructuralTimeHistoryAnalysisService().Analyze(input);
        Assert.True(result.Steps[0].Energy.Kinetic>0);
        Assert.True(Math.Abs(result.Steps[1].Nodes[^1].Uy)>0);
        Assert.Throws<ArgumentException>(() => new StructuralTimeHistoryAnalysisService().Analyze(input with {
            InitialStates=new[] { new StructuralTimeInitialNodeState("N0",Uy:.01) }
        }));
    }

    [Fact]
    public void TimeService_RejectsBadGridKnotsNodesAndUnsupportedMasses()
    {
        var model=Cantilever();
        var good=new StructuralTimeHistoryAnalysisRequest(model,new StructuralDynamicsDefinition(),
            .02,10,Damp,new[] { Constant(-1) });
        var service=new StructuralTimeHistoryAnalysisService();
        Assert.Throws<ArgumentException>(()=>service.Analyze(good with {TimeStepSeconds=0}));
        Assert.Throws<ArgumentException>(()=>service.Analyze(good with {StepCount=2001}));
        Assert.Throws<ArgumentException>(()=>service.Analyze(good with {
            Loads=new[] {new StructuralTimeLoadPattern(new[] {new StructuralHarmonicNodalForce("MISSING",Fy:1)},
                new[]{new StructuralTimeLoadKnot(0,1),new StructuralTimeLoadKnot(1,1)})} }));
        Assert.Throws<ArgumentException>(()=>service.Analyze(good with {
            Loads=new[] {new StructuralTimeLoadPattern(new[] {new StructuralHarmonicNodalForce("N4",Fy:1)},
                new[]{new StructuralTimeLoadKnot(0,1),new StructuralTimeLoadKnot(.1,1)})} }));
        Assert.Throws<ArgumentException>(()=>service.Analyze(good with {
            Loads=new[] {new StructuralTimeLoadPattern(new[] {new StructuralHarmonicNodalForce("N4",Fy:1)},
                new[]{new StructuralTimeLoadKnot(0,0),new StructuralTimeLoadKnot(0,1),new StructuralTimeLoadKnot(1,0)})} }));
        model.Members[0]=model.Members[0] with {ReleaseEndMoment=true};
        var error=Assert.Throws<StructuralSolver2D.Analysis.StructuralAnalysisException>(()=>service.Analyze(good));
        Assert.Contains("M53_RELEASE_UNSUPPORTED",error.Message);
    }
}
