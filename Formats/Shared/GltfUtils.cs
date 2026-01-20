// Adapted from the IO.NET library

using System.Numerics;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using AttributeFormat = SharpGLTF.Memory.AttributeFormat;

namespace MithrilToolbox.Formats.Shared;

/// <summary>
/// GLTF parsing utilities
/// </summary>
public class GltfUtils
{
    public static void SetIndexData(ModelRoot root, MeshPrimitive prim, List<int> indices)
    {
        var view = root.CreateBufferView(4 * indices.Count, 0, BufferMode.ELEMENT_ARRAY_BUFFER);
        var array = new IntegerArray(view.Content);
        array.Fill(indices);

        var accessor = root.CreateAccessor();
        accessor.SetIndexData(view, 0, indices.Count, IndexEncodingType.UNSIGNED_INT);
        prim.SetIndexAccessor(accessor);
    }

    public static void SetVertexData(ModelRoot root, MeshPrimitive prim, string attribute, List<Vector2> vecs)
    {
        var view = root.CreateBufferView(8 * vecs.Count, 0, BufferMode.ARRAY_BUFFER);
        var array = new Vector2Array(view.Content);
        array.Fill(vecs);

        var accessor = root.CreateAccessor();
        accessor.SetVertexData(view, 0, vecs.Count, AttributeFormat.Float2);
        prim.SetVertexAccessor(attribute, accessor);
    }

    public static void SetVertexData(ModelRoot root, MeshPrimitive prim, string attribute, List<Vector3> vecs)
    {
        var view = root.CreateBufferView(12 * vecs.Count, 0, BufferMode.ARRAY_BUFFER);
        var array = new Vector3Array(view.Content);
        array.Fill(vecs);

        var accessor = root.CreateAccessor();
        accessor.SetVertexData(view, 0, vecs.Count, AttributeFormat.Float3);
        prim.SetVertexAccessor(attribute, accessor);
    }

    public static void SetVertexData(ModelRoot root, MeshPrimitive prim, string attribute, List<Vector4> vecs)
    {
        var view = root.CreateBufferView(16 * vecs.Count, 0, BufferMode.ARRAY_BUFFER);
        var array = new Vector4Array(view.Content);
        array.Fill(vecs);

        var accessor = root.CreateAccessor();
        accessor.SetVertexData(view, 0, vecs.Count, AttributeFormat.Float4);
        prim.SetVertexAccessor(attribute, accessor);
    }

    public static void SetVertexDataBoneIndices(ModelRoot root, MeshPrimitive prim, string attribute, List<Vector4> vecs)
    {
        var view = root.CreateBufferView(8 * vecs.Count, 0, BufferMode.ARRAY_BUFFER);
        var array = new Vector4Array(view.Content, 0, EncodingType.UNSIGNED_SHORT);
        array.Fill(vecs);

        var accessor = root.CreateAccessor();
        accessor.SetVertexData(view, 0, vecs.Count, DimensionType.VEC4, EncodingType.UNSIGNED_SHORT, false);
        prim.SetVertexAccessor(attribute, accessor);
    }
}