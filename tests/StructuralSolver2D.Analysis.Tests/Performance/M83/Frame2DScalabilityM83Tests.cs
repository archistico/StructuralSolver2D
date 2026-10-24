using System.Text.Json;
using StructuralSolver2D.PerformanceM73;

namespace StructuralSolver2D.Analysis.Tests.Performance.M83;

/// <summary>Numerical and serialization gates only. Execution speed is never a unit-test threshold.</summary>
public sealed class Frame2DScalabilityM83Tests
{
    [Fact]
    public void RealFrame2DAndM82_QuickBenchmarksPassAnalyticAndEquilibriumChecks()
    {
        var report = M83ScalabilitySuite.Run(quick: true, repetitions: 1);
        Assert.True(report.Quick);
        Assert.Equal(4, report.Cases.Count);
        Assert.Equal(2, report.Cases.Count(x => x.Scenario == "Frame2D-first-order"));
        Assert.Equal(2, report.Cases.Count(x => x.Scenario == "Frame2D-M82-linearized"));
        foreach (var item in report.Cases)
        {
            Assert.InRange(item.MaxNumericalError, 0d, 1e-6);
            Assert.Equal(3 * (item.MemberCount + 1), item.PhysicalDofs);
            Assert.Equal(3 * item.MemberCount, item.FreeDofs);
            Assert.True(double.IsFinite(item.MedianMilliseconds) && item.MedianMilliseconds >= 0d);
            Assert.True(item.MedianAllocatedBytes >= 0L);
            Assert.True(double.IsFinite(item.TipUx) && item.TipUx > 0d);
            if (item.Scenario == "Frame2D-M82-linearized")
                Assert.True(item.CriticalMultiplier > 1d);
            else
                Assert.Null(item.CriticalMultiplier);
        }
        Assert.All(report.Cases.Where(item => item.Scenario == "Frame2D-first-order"),
            item => Assert.InRange(item.TipUx, 0.01015, 0.01017));
        Assert.All(report.Cases.Where(item => item.Scenario == "Frame2D-M82-linearized"),
            item => Assert.True(item.TipUx > 0.0101587301587d));
    }

    [Fact]
    public void M83Files_AreIndependentOfM73AndRoundTripWithInvariantNumbers()
    {
        var report = M83ScalabilitySuite.Run(quick: true, repetitions: 1);
        string path = Path.Combine(Path.GetTempPath(), "m83-scalability-" + Guid.NewGuid().ToString("N"));
        try
        {
            M83ScalabilitySuite.Save(report, path);
            string csv = File.ReadAllText(Path.Combine(path, "m83-scalability.csv"));
            Assert.Contains("scenario,members,physical_dofs", csv);
            Assert.Equal(report.Cases.Count + 1, csv.TrimEnd().Split('\n').Length);
            Assert.DoesNotContain(";", csv);
            Assert.False(File.Exists(Path.Combine(path, "m73-baseline.csv")));
            var deserialized = JsonSerializer.Deserialize<M83ScalabilitySuite.Report>(
                File.ReadAllText(Path.Combine(path, "m83-scalability.json")));
            Assert.NotNull(deserialized);
            Assert.Equal(report.Cases.Count, deserialized.Cases.Count);
            Assert.All(deserialized.Cases, item => Assert.True(double.IsFinite(item.MaxNumericalError)));
        }
        finally
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
    }

    [Fact]
    public void RepetitionsOutsideBound_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => M83ScalabilitySuite.Run(quick: true, repetitions: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => M83ScalabilitySuite.Run(quick: true, repetitions: 21));
    }
}
