namespace MithrilToolbox.Formats.Model;

public class VertexAttribute
{
    /// <summary><list type="bullet">
    /// <item><description>0x00 = Empty</description></item>
    /// <item><description>0x02 = Floats</description></item>
    /// <item><description>0x03 = Halfs</description></item>
    /// <item><description>0x04 = Bytes (Weights)</description></item>
    /// <item><description>0x07 = Bytes (Bone Indices)</description></item>
    /// <item><description>0x14 = Bytes (Colors)</description></item>
    /// </list></summary>
    public enum AttributeFormat : byte
    {
        Empty = 0x00,
        Floats = 0x02,
        Halfs = 0x03,
        BytesWeights = 0x04,
        BytesIndices = 0x07,
        BytesColors = 0x14
    }

    /// <summary><list type="bullet">
    /// <item><description>0x00 = Positions</description></item>
    /// <item><description>0x01 = Weights</description></item>
    /// <item><description>0x02 = Normals</description></item>
    /// <item><description>0x03 = Color (Layer 0)</description></item>
    /// <item><description>0x04 = Color (Layer 1)</description></item>
    /// <item><description>0x07 = Bone Indices</description></item>
    /// <item><description>0x08 = UV (Layer 0)</description></item>
    /// <item><description>0x09 = UV (Layer 1)</description></item>
    /// <item><description>0x0A = UV (Layer 2)</description></item>
    /// <item><description>0x0B = UV (Layer 3)</description></item>
    /// <item><description>0x0C = Binormals (Layer 1)</description></item>
    /// <item><description>0x0D = Tangents (Layer 1)</description></item>
    /// <item><description>0x0E = Binormals (Layer 0)</description></item>
    /// <item><description>0x0F = Tangents (Layer 0)</description></item>
    /// </list></summary>
    public enum AttributeType : byte
    {
        Position = 0x00,
        Weights = 0x01,
        Normals = 0x02,
        Color0 = 0x03,
        Color1 = 0x04,
        BoneIndices = 0x07,
        UV0 = 0x08,
        UV1 = 0x09,
        UV2 = 0x0A,
        UV3 = 0x0B,
        Tangents1 = 0x0C,
        Binormals1 = 0x0D,
        Tangents0 = 0x0E,
        Binormals0 = 0x0F
    }

    public short EndCheck, StrideStart;
    /// <summary>
    /// Attribute size
    /// </summary>
    /// <example>When Type is Position, Format is Floats and Amount 
    /// is 3, that means the attribute is stored as a Vector3</example>
    public byte VertexAmount;
    public byte Unknown;
    public AttributeFormat VertexFormat;
    public AttributeType VertexType;

    public static List<VertexAttribute> BuildAttributes(Vertex v)
    {
        List<VertexAttribute> attributes = [];
        short strideOffset = 0;

        void AddAttribute(byte amount, AttributeFormat format, AttributeType type, byte size)
        {
            attributes.Add(new()
            {
                EndCheck = 0,
                StrideStart = strideOffset,
                VertexAmount = amount,
                VertexFormat = format,
                VertexType = type,
                Unknown = 0
            });
            strideOffset += size;
        }

        AddAttribute(3, AttributeFormat.Floats, AttributeType.Position, 0xC);
        AddAttribute(3, AttributeFormat.Floats, AttributeType.Normals, 0xC);
        AddAttribute(3, AttributeFormat.Floats, AttributeType.Tangents0, 0xC);
        if (v.Tangents1 != null)
        {
            AddAttribute(3, AttributeFormat.Floats, AttributeType.Tangents1, 0xC);
        }
        AddAttribute(3, AttributeFormat.Floats, AttributeType.Binormals0, 0xC);
        if (v.Binormals1 != null)
        {
            AddAttribute(3, AttributeFormat.Floats, AttributeType.Binormals1, 0xC);
        }
        if (v.Color0 != null && v.Color0.Length > 0)
        {
            AddAttribute(4, AttributeFormat.BytesColors, AttributeType.Color0, 0x4);
        }
        if (v.Color1 != null && v.Color1.Length > 0)
        {
            AddAttribute(4, AttributeFormat.BytesColors, AttributeType.Color1, 0x4);
        }
        AddAttribute(2, AttributeFormat.Floats, AttributeType.UV0, 0x8);
        if (v.UV1 != null)
        {
            AddAttribute(2, AttributeFormat.Floats, AttributeType.UV1, 0x8);
        }
        if (v.UV2 != null)
        {
            AddAttribute(2, AttributeFormat.Floats, AttributeType.UV2, 0x8);
        }
        if (v.UV3 != null)
        {
            AddAttribute(2, AttributeFormat.Floats, AttributeType.UV3, 0x8);
        }
        if (v.BoneIndices != null && v.BoneIndices.Length > 0)
        {
            AddAttribute(4, AttributeFormat.BytesIndices, AttributeType.BoneIndices, 0x4);
        }
        if (v.Weights != null)
        {
            AddAttribute(4, AttributeFormat.BytesWeights, AttributeType.Weights, 0x4);
        }

        return attributes;
    }
}