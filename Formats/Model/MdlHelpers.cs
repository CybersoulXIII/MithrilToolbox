namespace MithrilToolbox.Formats.Model;

/// <summary>
/// Helper class used to ease the conversion of VisGroups
/// </summary>
public class Mesh
{
    public string Name = "";
    public List<Vertex> Vertices = [];
    /// <summary>
    /// Stored as tris
    /// </summary>
    public List<ushort[]> Faces = [];
    /// <summary>
    /// Default is 0
    /// </summary>
    public int VisibilityId;
}

/// <summary>
/// Helper class used to ease the conversion of MeshGroups
/// </summary>
public class MeshCollection
{
    public string Name = "";
    public List<VertexAttribute> Attributes = [];
    public List<Mesh> VisibleMeshes = [];
    public int MaterialId = 0;
}