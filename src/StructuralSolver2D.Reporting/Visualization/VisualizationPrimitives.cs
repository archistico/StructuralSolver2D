using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Reporting.Visualization;

/// <summary>
/// Represents a two-dimensional point prepared for graphical rendering.
/// Coordinates are expressed in the same model units used by StructuralSolver2D.
/// </summary>
/// <param name="X">Horizontal coordinate.</param>
/// <param name="Y">Vertical coordinate.</param>
public sealed record VisualizationPoint(double X, double Y);

/// <summary>
/// Represents one structural node with both original and scaled deformed coordinates.
/// </summary>
/// <param name="NodeId">Source structural node identifier.</param>
/// <param name="Label">Optional user-facing node label.</param>
/// <param name="Position">Original undeformed node position.</param>
/// <param name="DeformedPosition">Scaled deformed node position.</param>
/// <param name="Ux">Raw horizontal displacement in meters.</param>
/// <param name="Uy">Raw vertical displacement in meters.</param>
/// <param name="Rz">Raw in-plane rotation in radians.</param>
public sealed record VisualizationNode(
    string NodeId,
    string? Label,
    VisualizationPoint Position,
    VisualizationPoint DeformedPosition,
    double Ux,
    double Uy,
    double Rz);

/// <summary>
/// Represents one undeformed member axis segment prepared for rendering.
/// </summary>
/// <param name="MemberId">Source structural member identifier.</param>
/// <param name="StartNodeId">Start node identifier.</param>
/// <param name="EndNodeId">End node identifier.</param>
/// <param name="MemberType">Source structural member type.</param>
/// <param name="Start">Original start point.</param>
/// <param name="End">Original end point.</param>
public sealed record VisualizationMember(
    string MemberId,
    string StartNodeId,
    string EndNodeId,
    MemberType MemberType,
    VisualizationPoint Start,
    VisualizationPoint End);

/// <summary>
/// Represents the scaled deformed shape of a member as a polyline.
/// </summary>
/// <param name="MemberId">Source structural member identifier.</param>
/// <param name="Points">Polyline points of the deformed member axis.</param>
public sealed record DeformedMemberShape(
    string MemberId,
    IReadOnlyList<VisualizationPoint> Points);

/// <summary>
/// Identifies the result quantity represented by an internal-force diagram polyline.
/// </summary>
public enum VisualizationDiagramKind
{
    /// <summary>
    /// Axial normal-force diagram N.
    /// </summary>
    NormalForce,

    /// <summary>
    /// Shear-force diagram V.
    /// </summary>
    ShearForce,

    /// <summary>
    /// Bending-moment diagram M.
    /// </summary>
    BendingMoment,
}

/// <summary>
/// Represents one internal-force diagram polyline for a member.
/// </summary>
/// <param name="MemberId">Source structural member identifier.</param>
/// <param name="Kind">Diagram quantity.</param>
/// <param name="Points">Polyline points already offset from the member axis.</param>
/// <param name="MaxAbsValue">Maximum absolute raw diagram value used for labels and scaling controls.</param>
public sealed record MemberDiagramPolyline(
    string MemberId,
    VisualizationDiagramKind Kind,
    IReadOnlyList<VisualizationPoint> Points,
    double MaxAbsValue);

/// <summary>
/// Represents the drawing extents required to display model and result geometry.
/// </summary>
/// <param name="MinX">Minimum X coordinate.</param>
/// <param name="MinY">Minimum Y coordinate.</param>
/// <param name="MaxX">Maximum X coordinate.</param>
/// <param name="MaxY">Maximum Y coordinate.</param>
public sealed record VisualizationBounds(
    double MinX,
    double MinY,
    double MaxX,
    double MaxY)
{
    /// <summary>
    /// Gets an empty bounds value used when no points are available.
    /// </summary>
    public static VisualizationBounds Empty { get; } = new(0.0, 0.0, 0.0, 0.0);

    /// <summary>
    /// Gets the horizontal bounds size.
    /// </summary>
    public double Width => MaxX - MinX;

    /// <summary>
    /// Gets the vertical bounds size.
    /// </summary>
    public double Height => MaxY - MinY;
}


/// <summary>
/// Identifies the support symbol to render.
/// </summary>
public enum SupportGlyphKind
{
    /// <summary>
    /// Standard simple support / roller symbol.
    /// </summary>
    SimpleSupport,

    /// <summary>
    /// Standard hinge / pin support symbol.
    /// </summary>
    Hinge,

    /// <summary>
    /// Standard fixed-end symbol.
    /// </summary>
    Fixed,

    /// <summary>
    /// Generic fallback symbol for custom restraints.
    /// </summary>
    Custom,
}

/// <summary>
/// Identifies a reaction-result component.
/// </summary>
public enum ReactionComponentKind
{
    /// <summary>
    /// Horizontal reaction force.
    /// </summary>
    ForceX,

    /// <summary>
    /// Vertical reaction force.
    /// </summary>
    ForceY,

    /// <summary>
    /// In-plane reaction moment.
    /// </summary>
    MomentZ,
}

/// <summary>
/// Represents a support symbol to be rendered at a node.
/// </summary>
public sealed record VisualizationSupport(
    string SupportId,
    string NodeId,
    SupportGlyphKind Kind,
    VisualizationPoint Position,
    string? Label,
    double OrientationDegrees = 0.0);

/// <summary>
/// Represents one support reaction force arrow.
/// </summary>
public sealed record VisualizationReactionArrow(
    string SupportId,
    string NodeId,
    ReactionComponentKind ComponentKind,
    VisualizationPoint Start,
    VisualizationPoint End,
    double Value);

/// <summary>
/// Represents one support reaction moment glyph.
/// </summary>
public sealed record VisualizationReactionMoment(
    string SupportId,
    string NodeId,
    VisualizationPoint Center,
    double Radius,
    bool Clockwise,
    double Value);

/// <summary>
/// Represents a member-length dimension annotation.
/// </summary>
public sealed record MemberDimensionAnnotation(
    string MemberId,
    VisualizationPoint Start,
    VisualizationPoint End,
    double Distance);

/// <summary>
/// Represents the maximum translational displacement annotation.
/// </summary>
public sealed record VisualizationDisplacementAnnotation(
    string NodeId,
    VisualizationPoint UndeformedPoint,
    VisualizationPoint DeformedPoint,
    double Magnitude,
    double Ux = 0.0,
    double Uy = 0.0,
    double Rz = 0.0);

/// <summary>
/// Represents a value label associated with an internal-force diagram extremum.
/// </summary>
public sealed record DiagramValueAnnotation(
    string MemberId,
    VisualizationDiagramKind Kind,
    VisualizationPoint Position,
    double Value,
    double AbsoluteValue);


/// <summary>
/// Represents a nodal displacement label rendered near the scaled deformed node position.
/// Raw translational displacement values are expressed in meters; rotation is expressed in radians.
/// </summary>
public sealed record VisualizationNodeDisplacementLabel(
    string NodeId,
    string? Label,
    VisualizationPoint Position,
    double Ux,
    double Uy,
    double ResultantDisplacement,
    double Rz);


/// <summary>
/// Represents a displacement label at a standard station along a member, for example L/4, L/2 or 3L/4.
/// Raw translational displacement values are expressed in meters; local rotation is expressed in radians when available.
/// </summary>
public sealed record VisualizationMemberDisplacementLabel(
    string MemberId,
    string StationLabel,
    double NormalizedPosition,
    double Distance,
    VisualizationPoint Position,
    double GlobalUx,
    double GlobalUy,
    double ResultantDisplacement,
    double LocalRz);


/// <summary>
/// Identifies the graphical load representation kind.
/// </summary>
public enum VisualizationLoadGlyphKind
{
    /// <summary>
    /// Concentrated force represented by a straight arrow.
    /// </summary>
    ForceArrow,

    /// <summary>
    /// Concentrated moment represented by a circular arrow.
    /// </summary>
    MomentArrow,

    /// <summary>
    /// Distributed load represented by repeated arrows along a member.
    /// </summary>
    DistributedLoad,
}

/// <summary>
/// Represents a concentrated nodal or member load arrow.
/// </summary>
public sealed record VisualizationLoadArrow(
    string LoadId,
    string LoadCaseId,
    VisualizationLoadGlyphKind Kind,
    VisualizationPoint Start,
    VisualizationPoint End,
    double Value,
    string Unit,
    string? Label);

/// <summary>
/// Represents a concentrated moment load glyph.
/// </summary>
public sealed record VisualizationLoadMoment(
    string LoadId,
    string LoadCaseId,
    VisualizationPoint Center,
    double Radius,
    bool Clockwise,
    double Value,
    string Unit,
    string? Label);

/// <summary>
/// Represents a distributed load along a structural member.
/// </summary>
public sealed record VisualizationDistributedLoad(
    string LoadId,
    string LoadCaseId,
    string MemberId,
    VisualizationPoint StartAxisPoint,
    VisualizationPoint EndAxisPoint,
    VisualizationPoint StartOffsetPoint,
    VisualizationPoint EndOffsetPoint,
    double StartValue,
    double EndValue,
    string Unit,
    string? Label);
