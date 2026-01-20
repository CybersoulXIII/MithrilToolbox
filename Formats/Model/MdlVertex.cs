using System.Numerics;

using static MithrilToolbox.Formats.Model.VertexAttribute;
using static MithrilToolbox.Formats.Shared.NumericUtils;

namespace MithrilToolbox.Formats.Model;

public class Vertex
{
    public Vector3 Position, Normals, Binormals0, Tangents0;
    public Vector3? Binormals1, Tangents1;
    public Vector2 UV0;
    public Vector2? UV1, UV2, UV3;
    public Vector4? Weights;
    /// <summary>
    /// 0-based bone indices; 4 values per entry.
    /// </summary>
    public ushort[]? BoneIndices;
    /// <summary>
    /// Stored as 4 color channels (probably RGBA).
    /// </summary>
    public byte[]? Color0 = [], Color1 = [];

    public static Vertex ReadVertex(BinaryReader reader, List<ushort> nodeArray, List<VertexAttribute> attributeSet)
    {
        Vertex vertex = new();

        foreach (var attribute in attributeSet)
        {
            switch (attribute.VertexType, attribute.VertexFormat)
            {
                case (AttributeType.Position, AttributeFormat.Floats):
                    vertex.Position = ReadVector3(reader);
                    break;
                case (AttributeType.Position, AttributeFormat.Halfs):
                    vertex.Position = ReadVector4Half(reader);
                    break;

                case (AttributeType.Normals, AttributeFormat.Floats):
                    vertex.Normals = ReadVector3(reader);
                    break;
                case (AttributeType.Normals, AttributeFormat.Halfs):
                    vertex.Normals = ReadVector4Half(reader);
                    break;

                case (AttributeType.Tangents0, AttributeFormat.Floats):
                    vertex.Tangents0 = ReadVector3(reader);
                    break;
                case (AttributeType.Tangents0, AttributeFormat.Halfs):
                    vertex.Tangents0 = ReadVector4Half(reader);
                    break;

                case (AttributeType.Tangents1, AttributeFormat.Floats):
                    vertex.Tangents1 = ReadVector3(reader);
                    break;
                case (AttributeType.Tangents1, AttributeFormat.Halfs):
                    vertex.Tangents1 = ReadVector4Half(reader);
                    break;

                case (AttributeType.Binormals0, AttributeFormat.Floats):
                    vertex.Binormals0 = ReadVector3(reader);
                    break;
                case (AttributeType.Binormals0, AttributeFormat.Halfs):
                    vertex.Binormals0 = ReadVector4Half(reader);
                    break;

                case (AttributeType.Binormals1, AttributeFormat.Floats):
                    vertex.Binormals1 = ReadVector3(reader);
                    break;
                case (AttributeType.Binormals1, AttributeFormat.Halfs):
                    vertex.Binormals1 = ReadVector4Half(reader);
                    break;

                case (AttributeType.UV0, AttributeFormat.Floats):
                    vertex.UV0 = ReadVector2(reader);
                    break;
                case (AttributeType.UV0, AttributeFormat.Halfs):
                    vertex.UV0 = ReadVector3Half(reader);
                    break;

                case (AttributeType.UV1, AttributeFormat.Floats):
                    vertex.UV1 = ReadVector2(reader);
                    break;
                case (AttributeType.UV1, AttributeFormat.Halfs):
                    vertex.UV1 = ReadVector3Half(reader);
                    break;

                case (AttributeType.UV2, AttributeFormat.Floats):
                    vertex.UV2 = ReadVector2(reader);
                    break;
                case (AttributeType.UV2, AttributeFormat.Halfs):
                    vertex.UV2 = ReadVector3Half(reader);
                    break;

                case (AttributeType.UV3, AttributeFormat.Floats):
                    vertex.UV3 = ReadVector2(reader);
                    break;
                case (AttributeType.UV3, AttributeFormat.Halfs):
                    vertex.UV3 = ReadVector3Half(reader);
                    break;

                case (AttributeType.Color0, AttributeFormat.BytesColors):
                    vertex.Color0 = [reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()];
                    break;

                case (AttributeType.Color1, AttributeFormat.BytesColors):
                    vertex.Color1 = [reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()];
                    break;

                case (AttributeType.BoneIndices, AttributeFormat.BytesIndices):
                    ushort bone1 = reader.ReadByte();
                    ushort bone2 = reader.ReadByte();
                    ushort bone3 = reader.ReadByte();
                    ushort bone4 = reader.ReadByte();
                    vertex.BoneIndices = [nodeArray[bone1], nodeArray[bone2], nodeArray[bone3], nodeArray[bone4]];
                    break;

                case (AttributeType.Weights, AttributeFormat.BytesWeights):
                    float weight1 = reader.ReadByte() / 255f;
                    float weight2 = reader.ReadByte() / 255f;
                    float weight3 = reader.ReadByte() / 255f;
                    float weight4 = reader.ReadByte() / 255f;
                    vertex.Weights = new(weight1, weight2, weight3, weight4);
                    break;
            }
        }

        return vertex;
    }


    public static void WriteVertex(BinaryWriter writer, Vertex vertex, List<VertexAttribute> attributeSet)
    {
        foreach (var attribute in attributeSet)
        {
            switch (attribute.VertexType, attribute.VertexFormat)
            {
                case (AttributeType.Position, AttributeFormat.Floats):
                    WriteVector3(writer, vertex.Position);
                    break;

                case (AttributeType.Normals, AttributeFormat.Floats):
                    WriteVector3(writer, vertex.Normals);
                    break;

                case (AttributeType.Tangents0, AttributeFormat.Floats):
                    WriteVector3(writer, vertex.Tangents0);
                    break;
                case (AttributeType.Tangents1, AttributeFormat.Floats):
                    if (vertex.Tangents1 != null)
                    {
                        WriteVector3(writer, (Vector3)vertex.Tangents1);
                    }
                    break;

                case (AttributeType.Binormals0, AttributeFormat.Floats):
                    WriteVector3(writer, vertex.Binormals0);
                    break;
                case (AttributeType.Binormals1, AttributeFormat.Floats):
                    if (vertex.Binormals1 != null)
                    {
                        WriteVector3(writer, (Vector3)vertex.Binormals1);
                    }
                    break;

                case (AttributeType.Color0, AttributeFormat.BytesColors):
                    if (vertex.Color0 != null && vertex.Color0.Length > 0)
                    {
                        writer.Write(vertex.Color0);
                    }
                    break;
                case (AttributeType.Color1, AttributeFormat.BytesColors):
                    if (vertex.Color1 != null && vertex.Color1.Length > 0)
                    {
                        writer.Write(vertex.Color1);
                    }
                    break;

                case (AttributeType.UV0, AttributeFormat.Floats):
                    WriteVector2(writer, vertex.UV0);
                    break;
                case (AttributeType.UV1, AttributeFormat.Floats):
                    if (vertex.UV1 != null)
                    {
                        WriteVector2(writer, (Vector2)vertex.UV1);
                    }
                    break;
                case (AttributeType.UV2, AttributeFormat.Floats):
                    if (vertex.UV2 != null)
                    {
                        WriteVector2(writer, (Vector2)vertex.UV2);
                    }
                    break;
                case (AttributeType.UV3, AttributeFormat.Floats):
                    if (vertex.UV3 != null)
                    {
                        WriteVector2(writer, (Vector2)vertex.UV3);
                    }
                    break;

                case (AttributeType.BoneIndices, AttributeFormat.BytesIndices):
                    if (vertex.BoneIndices != null && vertex.BoneIndices.Length > 0)
                    {
                        writer.Write((byte)vertex.BoneIndices[0]);
                        writer.Write((byte)vertex.BoneIndices[1]);
                        writer.Write((byte)vertex.BoneIndices[2]);
                        writer.Write((byte)vertex.BoneIndices[3]);
                    }
                    break;
                case (AttributeType.Weights, AttributeFormat.BytesWeights):
                    if (vertex.Weights != null)
                    {
                        writer.Write((byte)(vertex.Weights?[0] * 255f));
                        writer.Write((byte)(vertex.Weights?[1] * 255f));
                        writer.Write((byte)(vertex.Weights?[2] * 255f));
                        writer.Write((byte)(vertex.Weights?[3] * 255f));
                    }
                    break;
            }
        }
    }
}