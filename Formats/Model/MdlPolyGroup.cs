namespace MithrilToolbox.Formats.Model;

/// <summary>
/// Mesh setup data. Relates to mesh collections.
/// </summary>
public class PolyGroup
{
    public int VisGroupStart, VisGroupCount, VertexStart, VertexSize, VertexStrideSize, IndexStart, IndexSize;
    /// <summary>
    /// Seems to be always 1
    /// </summary>
    public int Unknown1;
    /// <summary>
    /// Values are always multiples of 8
    /// </summary>
    public int Unknown2;
    public int NodeIdStart, NodeIdCount, MaterialId;
    /// <summary>
    /// Flag that represents if the meshes in this group are static (0) or skinned (1)
    /// </summary>
    public int MeshType;
}