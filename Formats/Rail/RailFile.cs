using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using static MithrilToolbox.Formats.Shared.NumericUtils;

namespace MithrilToolbox.Formats.Rail;

/// <summary>
/// Camera animation file format
/// </summary>
public class RailFile
{
    private readonly struct Header
    {
        public const int Magic = 0x00000000;
        public const int Unknown0 = 0x02000000;
        public const int Unknown1 = 0x00280416;
        public const int Unknown2 = 0x02000000;
        public const byte Padding0Size = 24;
        public const int Unknown3 = 0x01000000;
        public const byte Padding1Size = 84;
    }

    private readonly struct TableOfContents
    {
        public const uint DataStartSignal = 0x0000006E;
        public const uint SubTypeMagic = 0xB05C416E;
    }

    /// <summary>
    /// Mutable element of the table of contents. Initialized with 2 as a value to account 
    /// for the table of contents and the header node.
    /// </summary>
    private static uint SectionCount = 2;

    private enum NodeType : uint
    {
        RootNode = 0xBF88CB99,
        BlockHeader = 0x22FE430F,
        BlockTag = 0xC0F9C888,
        VectorEntry = 0xEC4618E2,
        Separator = 0xFFFFFFFF
    }

    /// <summary>Camera rail data
    /// <list type="table"><item>
    /// <c>Vector3[]</c> : Camera position data (5 elements)
    /// </item><item>
    /// <c>int[]</c> : Additional data (2 elements)
    /// </item></list></summary>
    private List<(Vector3[], int[])> DataBlocks = [];

    private RailFile() { }

    public RailFile(string filePath)
    {
        Read(filePath);
    }

    private void Read(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(stream);

        // Skip file header (0x80), table of contents (0xC) and first node header (0xC)
        reader.BaseStream.Seek(0x98, SeekOrigin.Begin);

        NodeType currentNode = NodeType.RootNode;
        while (true)
        {
            currentNode = (NodeType)reader.ReadUInt32();
            if (currentNode is NodeType.Separator)
            {
                break;
            }
            // Skip block tag nodes
            reader.BaseStream.Seek(0x14, SeekOrigin.Current);
            Vector3[] cameraPositions = new Vector3[5];

            for (int i = 0; i < 5; i++)
            {
                // Skip node bytes until actual data start
                reader.BaseStream.Seek(0xC, SeekOrigin.Current);
                cameraPositions[i] = ReadVector3(reader);
            }
            int[] additionalData = [reader.ReadInt32(), reader.ReadInt32()];
            DataBlocks.Add((cameraPositions, additionalData));
        }
    }

    public static void Write(RailFile rail, string outputPath)
    {
        // Block tags (2) + Cam positions (5)  
        SectionCount += (uint)(rail.DataBlocks.Count * 7);

        using FileStream stream = new(outputPath, FileMode.Create, FileAccess.Write);
        using BinaryWriter writer = new(stream);

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

        int nodeId = 1;

        writer.Write((uint)NodeType.RootNode);
        writer.Write(nodeId++);
        writer.Write(1);

        List<int> closerNodeIds = [];
        // Node opening statements and node data writing
        for (int i = 0; i < rail.DataBlocks.Count; i++)
        {
            writer.Write((uint)NodeType.BlockHeader);
            writer.Write(nodeId);
            closerNodeIds.Add(nodeId++);
            writer.Write(1);

            writer.Write((uint)NodeType.BlockTag);
            writer.Write(nodeId++);
            writer.Write(1);

            // Write camera positions
            for (int j = 0; j < 5; j++)
            {
                writer.Write((uint)NodeType.VectorEntry);
                writer.Write(nodeId++);
                writer.Write(1);

                WriteVector3(writer, rail.DataBlocks[i].Item1[j]);
            }
            // Write additional data
            writer.Write(rail.DataBlocks[i].Item2[0]);
            writer.Write(rail.DataBlocks[i].Item2[1]);
        }

        closerNodeIds.Reverse();

        // Node closing statements writing
        writer.Write((uint)NodeType.Separator);
        for (int i = 1; i < closerNodeIds.Count; i++)
        {
            writer.Write((uint)NodeType.RootNode);
            writer.Write(1);
            writer.Write(0);
            writer.Write((uint)NodeType.BlockHeader);
            writer.Write(closerNodeIds[i]);
            writer.Write(0);
        }
        writer.Write((uint)NodeType.RootNode);
        writer.Write(1);
        writer.Write(0);
        writer.Write((uint)NodeType.Separator);
        writer.Write((uint)NodeType.BlockHeader);
        writer.Write(closerNodeIds.First());
        writer.Write(0);

        writer.Write(closerNodeIds.Count);
        // Sometimes the second four-byte segment is not zero
        writer.Write(new byte[16]);
    }

    public static void Export(RailFile rail, string outputPath)
    {
        outputPath = Path.ChangeExtension(outputPath, ".json");

        JsonWriterOptions options = new()
        {
            Indented = true
        };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, options);

        writer.WriteStartArray();
        for (int i = 0; i < rail.DataBlocks.Count; i++)
        {
            var dataSegment = rail.DataBlocks.ElementAt(i); ;

            writer.WriteStartObject();
            writer.WriteStartArray("cameraPositions");

            for (int j = 0; j < 5; j++)
            {
                writer.WriteStartObject();
                writer.WriteNumber("x", dataSegment.Item1[j].X);
                writer.WriteNumber("y", dataSegment.Item1[j].Y);
                writer.WriteNumber("z", dataSegment.Item1[j].Z);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WritePropertyName("additionalData");
            writer.WriteRawValue(
                $"[{dataSegment.Item2[0].ToString(CultureInfo.InvariantCulture)}, " +
                $"{dataSegment.Item2[1].ToString(CultureInfo.InvariantCulture)}]"
            );
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

    public static RailFile Import(string inputPath)
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllBytes(inputPath));
        JsonElement root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new Exception("Expected top-level array");
        }

        RailFile rail = new()
        {
            DataBlocks = []
        };

        foreach (JsonElement block in root.EnumerateArray())
        {
            if (block.ValueKind != JsonValueKind.Object)
            {
                throw new Exception("Expected data block object");
            }

            if (!block.TryGetProperty("cameraPositions", out JsonElement positions) ||
                positions.ValueKind != JsonValueKind.Array || positions.GetArrayLength() != 5)
            {
                throw new Exception("cameraPositions must be an array of 5 EntryValues");
            }

            Vector3[] cameraPositions = new Vector3[5];

            for (int i = 0; i < 5; i++)
            {
                JsonElement vector = positions[i];
                if (vector.ValueKind != JsonValueKind.Object || vector.GetPropertyCount() != 3)
                {
                    throw new Exception("Position entry must be an object containing 3 childs");
                }

                float x = vector.GetProperty("x").GetSingle();
                float y = vector.GetProperty("y").GetSingle();
                float z = vector.GetProperty("z").GetSingle();

                cameraPositions[i] = new(x, y, z);
            }

            if (!block.TryGetProperty("additionalData", out JsonElement extraData) ||
                extraData.ValueKind != JsonValueKind.Array || extraData.GetArrayLength() != 2)
            {
                throw new Exception("additionalData must be an array of 2 integers");
            }

            int[] additionalData = [extraData[0].GetInt32(), extraData[1].GetInt32()];

            rail.DataBlocks.Add((cameraPositions.ToArray(), additionalData));
        }

        return rail;
    }
}
