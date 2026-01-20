using System.Numerics;
using static MithrilToolbox.Formats.Shared.NumericUtils;

namespace MithrilToolbox.Formats.Model;

/// <summary>
/// Visibility groups. Relates to actual meshes.
/// </summary>
public class VisGroup
{
    public int VisNum, VertexStart, VertexCount, FaceStart, FaceCount;
    /// <summary>
    /// In a model without LODs, should always be -1
    /// </summary>
    public short LodNum;
    /// <summary>
    /// W is always 0
    /// </summary>
    public Matrix3x4 BoundingTransform;
    /// <summary>
    /// W is always 0
    /// </summary>
    public Vector4 BoundingMax, BoundingMin;
}