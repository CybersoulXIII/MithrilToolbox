using System.Text;
using System.Text.Json;
using static MithrilToolbox.Formats.Shared.StringUtils;

namespace MithrilToolbox.Formats.CharacterSet;

/// <summary>
/// Stage configuration file format
/// </summary>
public class CsdFile
{
    private readonly struct Header
    {
        public const int Magic = 0x63736400;
        public const int Unknown0 = 0x02000000;
        public const int Unknown1 = 0x04000000;
        public const int Unknown2 = 0x02000000;
        public const byte Padding0Size = 24;
        public const int Unknown3 = 0x01000000;
        public const byte Padding1Size = 84;
    }

    private readonly struct TableOfContents
    {
        public const uint DataStartSignal = 0x0000006E;
        public const uint SubTypeMagic = 0x78EE6CEF;
    }

    private enum NodeType : uint
    {
        RootNode = 0xBF88CB99,
        SectionNode = 0xE8FFC96C,
        ChildNode = 0x22FE430F,
        DescriptorNode = 0x424EEC15,
        DataNode = 0x56FC5DA0,
        Separator = 0xFFFFFFFF,
        Padding = 0x00000000
    }

    private struct EntryData
    {
        public uint Type;
        public uint Subtype;
        public string Value;
    }

    private struct DataBlock()
    {
        public string? Name;
        public uint Type;
        public List<EntryData> Entries = [];
    }

    private struct SectionBlock()
    {
        public string? Name;
        public List<DataBlock> DataBlocks = [];
    }

    private List<SectionBlock> FileSections = [];

    private CsdFile() { }

    public CsdFile(string filePath)
    {
        Read(filePath);
    }

    private void Read(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(stream);

        // Skip until first section node
        reader.BaseStream.Seek(0xA4, SeekOrigin.Begin);

        NodeType currentNode = NodeType.RootNode;
        uint entryCount = 0;
        List<int> closerNodeIds = [];
        SectionBlock section = new();

        while (reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
        {
            currentNode = (NodeType)reader.ReadUInt32();

            switch (currentNode)
            {
                case NodeType.SectionNode:
                    section = new();
                    closerNodeIds.Clear();
                    break;
                case NodeType.ChildNode:
                    closerNodeIds.Add(reader.ReadInt32());
                    break;

                case NodeType.DescriptorNode:
                    reader.BaseStream.Seek(0x8, SeekOrigin.Current);
                    DataBlock block = new()
                    {
                        Type = reader.ReadUInt32()
                    };
                    entryCount = reader.ReadUInt32();
                    (block.Name, block.Entries) = ReadEntryValues(reader, entryCount);
                    section.DataBlocks.Add(block);
                    break;

                case NodeType.Separator:
                    if (closerNodeIds.Count > 0)
                    {
                        string? name = ReadClosers(reader, closerNodeIds);
                        if (name != null)
                        {
                            section.Name = name;
                        }
                        closerNodeIds.Clear();
                        if (section.DataBlocks.Count > 0 && !FileSections.Contains(section))
                        {
                            FileSections.Add(section);
                        }
                    }
                    break;
            }
        }
    }

    public static void Write(CsdFile newSet, string outputPath)
    {
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
        
        // Placeholder node count
        writer.Write(0);

        int nodeId = 1;
        
        writer.Write((uint)NodeType.RootNode);
        writer.Write(nodeId++);
        writer.Write(1);

        int rootNode = 1;
        int nodeCount = 0;
        string? name = null;
        byte[] nameByteData = [];
        List<int> sectionCloserIds = [];
        List<(int,int)> childCloserIds = [];

        for (int sectionIndex = 0; sectionIndex < newSet.FileSections.Count; sectionIndex++)
        {
            SectionBlock currentSection = newSet.FileSections[sectionIndex];

            writer.Write((uint)NodeType.ChildNode);
            writer.Write(nodeId);
            sectionCloserIds.Add(nodeId++);
            writer.Write(1);

            writer.Write((uint)NodeType.SectionNode);
            writer.Write(nodeId++);
            writer.Write(1);

            writer.Write((uint)NodeType.RootNode);
            writer.Write(nodeId);
            rootNode = nodeId++;
            writer.Write(1);

            for (int blockIndex = 0; blockIndex < currentSection.DataBlocks.Count; blockIndex++)
            {
                writer.Write((uint)NodeType.ChildNode);
                writer.Write(nodeId);
                childCloserIds.Add((rootNode, nodeId++));
                writer.Write(1);

                writer.Write((uint)NodeType.DescriptorNode);
                writer.Write(nodeId++);
                writer.Write(1);
                writer.Write(currentSection.DataBlocks[blockIndex].Type);

                var dataEntries = currentSection.DataBlocks[blockIndex].Entries;
                writer.Write(dataEntries.Count);

                foreach (EntryData entry in dataEntries)
                {
                    writer.Write((uint)NodeType.DataNode);
                    writer.Write(nodeId++);
                    if (sectionIndex == newSet.FileSections.Count - 1 &&
                        blockIndex == currentSection.DataBlocks.Count -1)
                    {
                        nodeCount = nodeId;
                    }                    
                    writer.Write(1);
                    writer.Write(entry.Type);
                    writer.Write(entry.Subtype);
                    writer.Write(entry.Value.Length);
                    writer.Write(EncodeJsonString(entry.Value));
                }

                name = currentSection.DataBlocks[blockIndex].Name;
                if (name != null)
                {
                    nameByteData = EncodeJsonString(name);
                    writer.Write(nameByteData.Length);
                    writer.Write(nameByteData);
                } 
                else
                {
                    writer.Write((uint)NodeType.Padding);
                }
            }

            childCloserIds.Reverse();

            writer.Write((uint)NodeType.Separator);
            for (int i = 1; i < childCloserIds.Count; i++)
            {
                writer.Write((uint)NodeType.RootNode);
                writer.Write(childCloserIds[i].Item1);
                writer.Write(0);
                writer.Write((uint)NodeType.ChildNode);
                writer.Write(childCloserIds[i].Item2);
                writer.Write(0);
            }
            writer.Write((uint)NodeType.RootNode);
            writer.Write(childCloserIds.First().Item1);
            writer.Write(0);
            writer.Write((uint)NodeType.Separator);
            writer.Write((uint)NodeType.ChildNode);
            writer.Write(childCloserIds.First().Item2);
            writer.Write(0);

            

            writer.Write(childCloserIds.Count);
            name = newSet.FileSections[sectionIndex].Name;
            if (name != null)
            {
                nameByteData = EncodeJsonString(name);
                writer.Write(nameByteData.Length);
                writer.Write(nameByteData);
            }

            rootNode = 1;
            childCloserIds = [];
        }

        sectionCloserIds.Reverse();

        writer.Write((uint)NodeType.Separator);
        for (int i = 1; i < sectionCloserIds.Count; i++)
        {
            writer.Write((uint)NodeType.RootNode);
            writer.Write(1);
            writer.Write(0);
            writer.Write((uint)NodeType.ChildNode);
            writer.Write(sectionCloserIds[i]);
            writer.Write(0);
        }
        writer.Write((uint)NodeType.RootNode);
        writer.Write(1);
        writer.Write(0);
        writer.Write((uint)NodeType.Separator);
        writer.Write((uint)NodeType.ChildNode);
        writer.Write(sectionCloserIds.First());
        writer.Write(0);

        writer.Write(sectionCloserIds.Count);

        writer.Flush();
        
        writer.BaseStream.Seek(0x88, SeekOrigin.Begin);        
        writer.Write(nodeCount);

        writer.Flush();
    }

    public static void Export(CsdFile set, string outputPath)
    {
        outputPath = Path.ChangeExtension(outputPath, ".json");

        JsonWriterOptions options = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = true
        };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, options);

        writer.WriteStartArray();
        foreach (var section in set.FileSections)
        {
            writer.WriteStartObject();
            writer.WriteString("sectionName", section.Name);
            writer.WriteStartArray("sectionBlocks");

            for (int i = 0; i < section.DataBlocks.Count; i++)
            {
                var currentBlock = section.DataBlocks[i];

                writer.WriteStartObject();
                writer.WriteString("blockName", currentBlock.Name);
                writer.WriteNumber("blockType", currentBlock.Type);
                writer.WriteStartArray("dataEntries");

                foreach (var entry in currentBlock.Entries)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber($"entryType", entry.Type);
                    writer.WriteNumber($"entrySubtype", entry.Subtype);
                    writer.WriteString($"entryValue", entry.Value);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
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

    public static CsdFile Import(string inputPath)
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(inputPath));
        JsonElement root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new Exception("Expected top-level element to be an array");
        }

        CsdFile set = new()
        {
            FileSections = []
        };

        foreach (JsonElement segment in root.EnumerateArray())
        {
            if (segment.ValueKind != JsonValueKind.Object)
            {
                throw new Exception("Expected section object");
            }

            CsdFile.SectionBlock section = new();
            if (!segment.TryGetProperty("sectionName", out JsonElement sectionName))
            {
                throw new Exception("Expected sectionName object");
            }

            section.Name = sectionName.GetString();

            if (!segment.TryGetProperty("sectionBlocks", out JsonElement sectionBlocks) ||
                    sectionBlocks.ValueKind != JsonValueKind.Array)
            {
                throw new Exception("Expected sectionBlocks array");
            }

            JsonElement blocks = segment.GetProperty("sectionBlocks");

            foreach (JsonElement block in blocks.EnumerateArray())
            {
                if (block.ValueKind != JsonValueKind.Object)
                {
                    throw new Exception("Expected block object");
                }

                CsdFile.DataBlock dataBlock = new();

                if (!block.TryGetProperty("blockName", out JsonElement blockName))
                {
                    throw new Exception("Expected blockName object");
                }

                dataBlock.Name = blockName.GetString();

                if (!block.TryGetProperty("blockType", out JsonElement blockType) ||
                    blockType.ValueKind != JsonValueKind.Number)
                {
                    throw new Exception("Expected blockType numeric object");
                }

                dataBlock.Type = blockType.GetUInt32();

                if (!block.TryGetProperty("dataEntries", out JsonElement entryValues) ||
                    entryValues.ValueKind != JsonValueKind.Array)
                {
                    throw new Exception("Expected dataEntries array");
                }

                int entryCount = entryValues.GetArrayLength();

                for (int i = 0; i < entryCount; i++)
                {
                    JsonElement entry = entryValues[i];
                    CsdFile.EntryData dataEntry = new();

                    if (entry.ValueKind != JsonValueKind.Object)
                    {
                        throw new Exception("Expected entry object");
                    }

                    if (!entry.TryGetProperty("entryType", out JsonElement entryType) ||
                    entryType.ValueKind != JsonValueKind.Number)
                    {
                        throw new Exception("Expected entryType numeric object");
                    }

                    dataEntry.Type = entryType.GetUInt32();

                    if (!entry.TryGetProperty("entrySubtype", out JsonElement entrySubtype) ||
                    entrySubtype.ValueKind != JsonValueKind.Number)
                    {
                        throw new Exception("Expected entrySubtype numeric object");
                    }

                    dataEntry.Subtype = entrySubtype.GetUInt32();

                    if (!entry.TryGetProperty("entryValue", out JsonElement entryValue) ||
                    entryValue.ValueKind != JsonValueKind.String)
                    {
                        throw new Exception("Expected entrySubtype numeric object");
                    }

                    dataEntry.Value = entryValue.GetString()!;

                    dataBlock.Entries.Add(dataEntry);
                }

                section.DataBlocks.Add(dataBlock);
            }

            set.FileSections.Add(section);
        }

        return set;
    }

    private static string? ReadClosers(BinaryReader reader, List<int> closerNodeIds)
    {
        string? sectionName = null;

        for (int i = 0; i < closerNodeIds.Count; i++)
        {
            if (closerNodeIds.Count == 1 || i == closerNodeIds.Count - 1)
            {
                reader.BaseStream.Seek(0x20, SeekOrigin.Current);
            }
            else
            {
                reader.BaseStream.Seek(0x18, SeekOrigin.Current);
            }
        }

        if (reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
        {
            uint nextValue = reader.ReadUInt32();

            if (IsValidNodeType(nextValue))
            {
                reader.BaseStream.Position -= 4;
            }
            else
            {
                byte[] rawData = reader.ReadBytes((int)nextValue);
                sectionName = EncodeBinaryString(rawData);
            }
        }

        return sectionName;
    }

    private static (string?, List<EntryData>) ReadEntryValues(BinaryReader reader, uint entryCount)
    {
        List<EntryData> entries = [];
        string? name = null;
        byte[] rawData;

        for (int i = 0; i < entryCount; i++)
        {
            NodeType currentNode = (NodeType)reader.ReadUInt32();            
            
            if (currentNode == NodeType.DataNode)
            {               
                reader.BaseStream.Seek(0x8, SeekOrigin.Current);

                EntryData entry = new()
                {
                    Type = reader.ReadUInt32(),
                    Subtype = reader.ReadUInt32()
                };

                int dataLength = reader.ReadInt32();
                rawData = reader.ReadBytes(dataLength);
                entry.Value = Encoding.UTF8.GetString(rawData);

                entries.Add(entry);
            }       
        }

        uint nextValue = reader.ReadUInt32();
        if (IsValidNodeType(nextValue))
        {
            reader.BaseStream.Position -= 4;
        }
        else 
        {
            rawData = reader.ReadBytes((int)nextValue);
            name = EncodeBinaryString(rawData);
        }

        return (name, entries);
    }   

    private static bool IsValidNodeType(uint value)
    {
        return Enum.IsDefined(typeof(NodeType), value);
    }
}
