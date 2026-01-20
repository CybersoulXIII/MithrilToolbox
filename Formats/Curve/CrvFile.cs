using System.Numerics;
using System.Text;
using System.Text.Json;
using static MithrilToolbox.Formats.Shared.NumericUtils;

namespace MithrilToolbox.Formats.Curve;

/// <summary>
/// 2D animation file format
/// </summary>
public class CrvFile
{
    private readonly struct Header
    {
        public const int Magic = 0x63727600;
        public const int Unknown0 = 0x02000000;
        public const int Unknown1 = 0x06000000;
        public const int Unknown2 = 0x02000000;
        public const byte Padding0Size = 24;
        public const int Unknown3 = 0x01000000;
        public const byte Padding1Size = 84;
    }

    private readonly struct TableOfContents
    {
        public const uint DataStartSignal = 0x0000006E;
        public const uint SubTypeMagic = 0xF948B8E7;
    }

    /// <summary>
    /// Mutable element of the table of contents. Initialized with 3 as a value to account 
    /// for table of contents, the header and the closer nodes.
    /// </summary>
    private static uint SectionCount = 3;

    private enum NodeType : uint
    {
        Header = 0xE965A9E5,
        KeyData = 0x30AC7D05,
        CurveData = 0xC11E6676,
        Closer = 0x31AA6033
    }

    /// <summary>Curve animation data
    /// <list type="table"><item>
    /// <c>byte[]</c> : Key data
    /// </item><item>
    /// <c>Vector3[]</c> : Curve data
    /// </item></list></summary>
    private Dictionary<byte[], Vector3[]?> DataBlocks = [];

    public CrvFile() { }

    public CrvFile(string filePath)
    {
        Read(filePath);
    }

    private void Read(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(stream);

        // Skip file header (0x80), table of contents (0xC) and first node header (0xC)
        reader.BaseStream.Seek(0x98, SeekOrigin.Begin);

        uint dataEntryCount = reader.ReadUInt32();

        for (int i = 0; i < dataEntryCount; i++)
        {
            // Skip node tag, node index and unknown value
            reader.BaseStream.Seek(0xC, SeekOrigin.Current);
            
            byte[] keyData = reader.ReadBytes(4);
            uint curveDataCount = reader.ReadUInt32();
            if (curveDataCount == 0)
            {
                DataBlocks.Add(keyData, null);
            }
            else
            {
                Vector3[] curveArray = new Vector3[curveDataCount];
                for (int j = 0; j < curveDataCount; j++)
                {
                    reader.BaseStream.Seek(0xC, SeekOrigin.Current);
                    curveArray[j] = ReadVector3(reader);
                }
                DataBlocks.Add(keyData, curveArray);
            }
        }
    }

    public static void Write(CrvFile curve, string outputPath)
    {
        using FileStream stream = new(outputPath, FileMode.Create, FileAccess.Write);
        using BinaryWriter writer = new(stream);

        foreach (var entry in curve.DataBlocks)
        {
            SectionCount++;
            if (entry.Value != null)
            {           
                foreach (var vector in entry.Value)
                {
                    SectionCount++;
                }
            }
        }

        writer.Write(Header.Magic);
        writer.Write(Header.Unknown0);
        writer.Write(Header.Unknown1);
        writer.Write(Header.Unknown2);
        writer.Write(new byte[Header.Padding0Size]);
        writer.Write(Header.Unknown3);
        writer.Write(new byte[Header.Padding1Size]);

        writer.Write(TableOfContents.DataStartSignal);
        writer.Write(TableOfContents.SubTypeMagic);
        writer.Write(SectionCount);

        uint nodeIndex = 1;
        List<uint> keyNodeIndices = [];

        writer.Write((uint)NodeType.Header);
        writer.Write(nodeIndex);
        writer.Write(1);
        writer.Write(curve.DataBlocks.Count);

        foreach(var entry in curve.DataBlocks)
        {
            nodeIndex++;
            writer.Write((uint)NodeType.KeyData);
            writer.Write(nodeIndex);
            keyNodeIndices.Add(nodeIndex);
            writer.Write(1);
            writer.Write(entry.Key);
            if (entry.Value == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(entry.Value.Length);
                foreach (Vector3 vector in entry.Value)
                {
                    nodeIndex++;
                    writer.Write((uint)NodeType.CurveData);
                    writer.Write(nodeIndex);
                    writer.Write(1);
                    WriteVector3(writer, vector);
                }
            }
        }

        nodeIndex++;
        writer.Write((uint)NodeType.Closer);
        writer.Write(nodeIndex);
        writer.Write(1);
        writer.Write(curve.DataBlocks.Count);
        foreach (uint index in keyNodeIndices)
        {
            writer.Write((uint)NodeType.KeyData);
            writer.Write(index);
            writer.Write(0);
        }
        writer.Write(curve.DataBlocks.Count);

        writer.Flush();
    }

    public static void Export(CrvFile animation, string outputPath)
    {
        outputPath = Path.ChangeExtension(outputPath, ".json");

        JsonWriterOptions options = new()
        {
            Indented = true
        };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, options);

        writer.WriteStartArray();
        for (int i = 0; i < animation.DataBlocks.Count; i++)
        {
            var dataSegment = animation.DataBlocks.ElementAt(i);

            writer.WriteStartObject();
            writer.WritePropertyName("keyData");
            writer.WriteRawValue($"[{string.Join(", ", dataSegment.Key)}]");
            writer.WritePropertyName("curveData");

            if (dataSegment.Value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStartArray();
                for (int j = 0; j < dataSegment.Value.Length; j++)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("x", dataSegment.Value[j].X);
                    writer.WriteNumber("y", dataSegment.Value[j].Y);
                    writer.WriteNumber("z", dataSegment.Value[j].Z);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.Flush();

        string jsonString = Encoding.UTF8.GetString(stream.ToArray());

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        File.WriteAllText(outputPath, jsonString);
    }

    public static CrvFile Import(string inputPath)
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(inputPath));
        JsonElement root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new Exception("Expected top-level array");
        }

        CrvFile animation = new()
        {
            DataBlocks = []
        };

        foreach (JsonElement block in root.EnumerateArray())
        {
            if (block.ValueKind != JsonValueKind.Object)
            {
                throw new Exception("Expected data block object");
            }

            byte[] keyData = new byte[4];
            List<Vector3> curveData = [];

            if (block.TryGetProperty("keyData", out JsonElement keyArray))
            {
                if (keyArray.ValueKind != JsonValueKind.Array || keyArray.GetArrayLength() != 4)
                {
                    throw new Exception("keyData must be an array of 4 bytes");
                }

                for (int i = 0; i < 4; i++)
                {
                    keyData[i] = keyArray[i].GetByte();
                }
            }
            else
            {
                throw new Exception("Missing keyData property");
            }

            if (block.TryGetProperty("curveData", out JsonElement curveDataElement))
            {
                if (curveDataElement.ValueKind != JsonValueKind.Null)
                {
                    if (curveDataElement.ValueKind != JsonValueKind.Array)
                    {
                        throw new Exception("Expected curveData array");
                    }

                    foreach (JsonElement vector in curveDataElement.EnumerateArray())
                    {
                        if (vector.ValueKind != JsonValueKind.Object || vector.GetPropertyCount() != 3)
                        {
                            throw new Exception("Curve entry must be an object containing 3 childs");
                        }

                        float x = vector.GetProperty("x").GetSingle();
                        float y = vector.GetProperty("y").GetSingle();
                        float z = vector.GetProperty("z").GetSingle();

                        curveData.Add(new(x, y, z));

                    }
                }
            }

            animation.DataBlocks.Add(
                keyData,
                curveData.Count == 0 ? null : [.. curveData]
            );
        }

        return animation;
    }
}
