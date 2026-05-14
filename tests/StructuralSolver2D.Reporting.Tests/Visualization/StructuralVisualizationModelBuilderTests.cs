using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Reporting.Visualization;

namespace StructuralSolver2D.Reporting.Tests.Visualization;

public sealed class StructuralVisualizationModelBuilderTests
{
    [Fact]
    public void Build_ShouldCreateNodesMembersAndScaledDeformedPositions()
    {
        StructuralModel model = CreateTwoNodeModel();
        var result = new StructuralAnalysisResult(
            "LC1",
            new[]
            {
                new NodalDisplacementResult("A", 0.001, 0.0, 0.01),
                new NodalDisplacementResult("B", 0.0, -0.002, -0.02),
            },
            Array.Empty<SupportReactionResult>(),
            Array.Empty<MemberEndForceResult>());

        var visualization = new StructuralVisualizationModelBuilder().Build(
            model,
            result,
            Array.Empty<MemberInternalForceDiagram>(),
            Array.Empty<MemberDisplacementDiagram>(),
            new VisualizationOptions { DeformationScale = 100.0, BoundsPadding = 0.0 });

        Assert.Equal(2, visualization.Nodes.Count);
        Assert.Single(visualization.Members);
        VisualizationNode nodeA = Assert.Single(visualization.Nodes, node => node.NodeId == "A");
        VisualizationNode nodeB = Assert.Single(visualization.Nodes, node => node.NodeId == "B");

        Assert.Equal(0.1, nodeA.DeformedPosition.X, precision: 12);
        Assert.Equal(0.0, nodeA.DeformedPosition.Y, precision: 12);
        Assert.Equal(4.0, nodeB.DeformedPosition.X, precision: 12);
        Assert.Equal(-0.2, nodeB.DeformedPosition.Y, precision: 12);
        Assert.Equal(-0.02, nodeB.Rz, precision: 12);
    }

    [Fact]
    public void Build_ShouldUseDisplacementSamplesForCurvedDeformedShape()
    {
        StructuralModel model = CreateTwoNodeModel();
        var result = new StructuralAnalysisResult(
            "LC1",
            Array.Empty<NodalDisplacementResult>(),
            Array.Empty<SupportReactionResult>(),
            Array.Empty<MemberEndForceResult>());
        var displacementDiagrams = new[]
        {
            new MemberDisplacementDiagram(
                "M1",
                4.0,
                new[]
                {
                    new MemberDisplacementSample("M1", 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0),
                    new MemberDisplacementSample("M1", 0.5, 2.0, 0.0, -0.010, 0.0, 0.0, -0.010),
                    new MemberDisplacementSample("M1", 1.0, 4.0, 0.0, 0.0, 0.0, 0.0, 0.0),
                }),
        };

        StructuralVisualizationModel visualization = new StructuralVisualizationModelBuilder().Build(
            model,
            result,
            Array.Empty<MemberInternalForceDiagram>(),
            displacementDiagrams,
            new VisualizationOptions { DeformationScale = 50.0, BoundsPadding = 0.0 });

        DeformedMemberShape shape = Assert.Single(visualization.DeformedShapes);
        Assert.Equal(3, shape.Points.Count);
        Assert.Equal(2.0, shape.Points[1].X, precision: 12);
        Assert.Equal(-0.5, shape.Points[1].Y, precision: 12);
    }

    [Fact]
    public void Build_ShouldCreateScalableInternalForceDiagramPolylines()
    {
        StructuralModel model = CreateTwoNodeModel();
        var result = new StructuralAnalysisResult(
            "LC1",
            Array.Empty<NodalDisplacementResult>(),
            Array.Empty<SupportReactionResult>(),
            Array.Empty<MemberEndForceResult>());
        var forceDiagrams = new[]
        {
            new MemberInternalForceDiagram(
                "M1",
                4.0,
                new[]
                {
                    new MemberInternalForceSample("M1", 0.0, 0.0, 0.0, 10.0, 0.0),
                    new MemberInternalForceSample("M1", 0.5, 2.0, 0.0, -20.0, 30.0),
                    new MemberInternalForceSample("M1", 1.0, 4.0, 0.0, 0.0, -40.0),
                }),
        };

        StructuralVisualizationModel visualization = new StructuralVisualizationModelBuilder().Build(
            model,
            result,
            forceDiagrams,
            Array.Empty<MemberDisplacementDiagram>(),
            new VisualizationOptions
            {
                IncludeNormalForceDiagram = false,
                ShearForceDiagramScale = 0.01,
                BendingMomentDiagramScale = 0.02,
                BoundsPadding = 0.0,
            });

        Assert.Equal(2, visualization.Diagrams.Count);
        MemberDiagramPolyline shear = Assert.Single(visualization.Diagrams, diagram => diagram.Kind == VisualizationDiagramKind.ShearForce);
        MemberDiagramPolyline moment = Assert.Single(visualization.Diagrams, diagram => diagram.Kind == VisualizationDiagramKind.BendingMoment);

        Assert.Equal(20.0, shear.MaxAbsValue, precision: 12);
        Assert.Equal(40.0, moment.MaxAbsValue, precision: 12);
        Assert.Equal(-0.2, shear.Points[1].Y, precision: 12);
        Assert.Equal(-0.8, moment.Points[2].Y, precision: 12);
    }



    [Fact]
    public void Build_ShouldCreateSupportGlyphsReactionsDimensionsAndAnnotations()
    {
        StructuralModel model = CreateSupportedModel();
        var result = new StructuralAnalysisResult(
            "LC1",
            new[]
            {
                new NodalDisplacementResult("A", 0.0, 0.0, 0.0),
                new NodalDisplacementResult("B", 0.0, -0.002, -0.01),
            },
            new[]
            {
                new SupportReactionResult("S1", "A", 10.0, 25.0, 12.0),
                new SupportReactionResult("S2", "B", 0.0, 15.0, 0.0),
            },
            Array.Empty<MemberEndForceResult>());
        var forceDiagrams = new[]
        {
            new MemberInternalForceDiagram(
                "M1",
                4.0,
                new[]
                {
                    new MemberInternalForceSample("M1", 0.0, 0.0, 5.0, 0.0, 0.0),
                    new MemberInternalForceSample("M1", 0.5, 2.0, -8.0, -20.0, 30.0),
                    new MemberInternalForceSample("M1", 1.0, 4.0, 1.0, 10.0, -40.0),
                }),
        };

        StructuralVisualizationModel visualization = new StructuralVisualizationModelBuilder().Build(
            model,
            result,
            forceDiagrams,
            Array.Empty<MemberDisplacementDiagram>(),
            new VisualizationOptions
            {
                DeformationScale = 100.0,
                ReactionForceScale = 0.01,
                ReactionMomentScale = 0.01,
                MinimumReactionMomentRadius = 0.1,
                BoundsPadding = 0.0,
            });

        Assert.Equal(2, visualization.Supports.Count);
        VisualizationSupport rotatedSupport = Assert.Single(visualization.Supports, support => support.SupportId == "S2");
        Assert.Equal(30.0, rotatedSupport.OrientationDegrees, precision: 12);
        Assert.Equal(3, visualization.ReactionArrows.Count);
        Assert.Single(visualization.ReactionMoments);
        MemberDimensionAnnotation dimension = Assert.Single(visualization.MemberDimensions);
        Assert.Equal(4.0, dimension.Distance, precision: 12);
        Assert.NotNull(visualization.MaximumDisplacement);
        Assert.Equal("B", visualization.MaximumDisplacement!.NodeId);
        Assert.Equal(0.002, visualization.MaximumDisplacement.Magnitude, precision: 12);
        Assert.Equal(-0.002, visualization.MaximumDisplacement.Uy, precision: 12);
        Assert.Equal(-0.01, visualization.MaximumDisplacement.Rz, precision: 12);
        VisualizationNodeDisplacementLabel displacementLabel = Assert.Single(visualization.NodeDisplacementLabels);
        Assert.Equal("B", displacementLabel.NodeId);
        Assert.Equal(0.002, displacementLabel.ResultantDisplacement, precision: 12);
        Assert.Equal(-0.002, displacementLabel.Uy, precision: 12);
        Assert.Equal(3, visualization.MemberDisplacementLabels.Count);
        VisualizationMemberDisplacementLabel halfSpanLabel = Assert.Single(visualization.MemberDisplacementLabels, label => label.StationLabel == "L/2");
        Assert.Equal("M1", halfSpanLabel.MemberId);
        Assert.Equal(0.50, halfSpanLabel.NormalizedPosition, precision: 12);
        Assert.Equal(2.0, halfSpanLabel.Distance, precision: 12);
        Assert.Equal(0.001, halfSpanLabel.ResultantDisplacement, precision: 12);
        Assert.Equal(3, visualization.DiagramValueAnnotations.Count);
    }

    [Fact]
    public void Build_WithAnimationFrameCount_ShouldPrepareCyclicDeformedShapeFrames()
    {
        StructuralModel model = CreateTwoNodeModel();
        var result = new StructuralAnalysisResult(
            "LC1",
            new[]
            {
                new NodalDisplacementResult("A", 0.0, 0.0, 0.0),
                new NodalDisplacementResult("B", 0.0, -0.002, 0.0),
            },
            Array.Empty<SupportReactionResult>(),
            Array.Empty<MemberEndForceResult>());

        StructuralVisualizationModel visualization = new StructuralVisualizationModelBuilder().Build(
            model,
            result,
            Array.Empty<MemberInternalForceDiagram>(),
            Array.Empty<MemberDisplacementDiagram>(),
            new VisualizationOptions
            {
                DeformationScale = 100.0,
                AnimationFrameCount = 4,
                BoundsPadding = 0.0,
            });

        Assert.Equal(4, visualization.AnimationFrames.Count);
        Assert.Equal(0.0, visualization.AnimationFrames[0].Factor, precision: 12);
        Assert.Equal(1.0, visualization.AnimationFrames[1].Factor, precision: 12);
        Assert.Equal(0.0, visualization.AnimationFrames[2].Factor, precision: 12);
        Assert.Equal(-1.0, visualization.AnimationFrames[3].Factor, precision: 12);

        VisualizationNode positivePeak = Assert.Single(visualization.AnimationFrames[1].Nodes, node => node.NodeId == "B");
        VisualizationNode negativePeak = Assert.Single(visualization.AnimationFrames[3].Nodes, node => node.NodeId == "B");

        Assert.Equal(-0.2, positivePeak.DeformedPosition.Y, precision: 12);
        Assert.Equal(0.2, negativePeak.DeformedPosition.Y, precision: 12);
    }

    [Fact]
    public void Build_ShouldThrowForNegativeScales()
    {
        StructuralModel model = CreateTwoNodeModel();
        var result = new StructuralAnalysisResult(
            "LC1",
            Array.Empty<NodalDisplacementResult>(),
            Array.Empty<SupportReactionResult>(),
            Array.Empty<MemberEndForceResult>());

        var options = new VisualizationOptions { DeformationScale = -1.0 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StructuralVisualizationModelBuilder().Build(
                model,
                result,
                Array.Empty<MemberInternalForceDiagram>(),
                Array.Empty<MemberDisplacementDiagram>(),
                options));
    }


    [Fact]
    public void Build_WithInvalidAnimationFrameCount_ShouldThrowClearException()
    {
        StructuralModel model = CreateTwoNodeModel();
        var result = new StructuralAnalysisResult(
            "LC1",
            Array.Empty<NodalDisplacementResult>(),
            Array.Empty<SupportReactionResult>(),
            Array.Empty<MemberEndForceResult>());

        var options = new VisualizationOptions { AnimationFrameCount = 1 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StructuralVisualizationModelBuilder().Build(
                model,
                result,
                Array.Empty<MemberInternalForceDiagram>(),
                Array.Empty<MemberDisplacementDiagram>(),
                options));
    }

    private static StructuralModel CreateTwoNodeModel() =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 4.0, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic", 210_000_000.0))
            .AddSection(new StructuralSection("SEC", "Generic", 0.01, 0.0001))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D));

    private static StructuralModel CreateSupportedModel() =>
        CreateTwoNodeModel()
            .AddSupport(StructuralSupport.Fixed("S1", "A", "Incastro"))
            .AddSupport(StructuralSupport.SimpleSupport("S2", "B", "Carrello", orientationDegrees: 30.0));
}
