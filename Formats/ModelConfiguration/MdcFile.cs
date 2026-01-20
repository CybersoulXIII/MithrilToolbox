using System.Text;
using System.Text.Json;
using static MithrilToolbox.Formats.Shared.StringUtils;

namespace MithrilToolbox.Formats.ModelConfiguration;

/// <summary>
/// Material configuration file format
/// </summary>
public class MdcFile
{
    private readonly struct Header
    {
        public const int Magic = 0x6D646300;
        public const int Unknown0 = 0x02000000;
        public const int Unknown1 = 0x0C000000;
        public const int Unknown2 = 0x02000000;
        public const byte Padding0Size = 24;
        public const int Unknown3 = 0x01000000;
        public const byte Padding1Size = 84;
    }

    private struct TableOfContents
    {
        public uint MaterialNameBufferSize;
        public uint MaterialCount;
        public uint MaterialParameterBufferSize;
        public uint StringBufferSize;
    }

    private byte[] UnknownFloats = [];
    private byte[] MaterialParameters = [];
    private byte[] UnknownData = [];
    /// <summary>
    /// Shader and texture path + material parameter name (per-material)
    /// </summary>
    private List<string[]> Strings = [];
    private List<string> MaterialNames = [];

    private MdcFile() { }

    public MdcFile(string filePath)
    {
        Read(filePath);
    }

    private void Read(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(stream);

        // Skip header
        reader.BaseStream.Position = 0x80;

        TableOfContents toc = new()
        {
            MaterialNameBufferSize = reader.ReadUInt32(),
            MaterialCount = reader.ReadUInt32(),
            MaterialParameterBufferSize = reader.ReadUInt32(),
            StringBufferSize = reader.ReadUInt32()
        };

        UnknownFloats = reader.ReadBytes(0x20);

        for (int i = 0; i < toc.MaterialCount; i++)
        {
            string name = ReadNullTerminatedString(reader);
            MaterialNames.Add(name);
        }

        // Just in case there's padding in the buffers
        long expectedPosition = 0xB0 + toc.MaterialNameBufferSize;
        reader.BaseStream.Position = expectedPosition;

        MaterialParameters = reader.ReadBytes((int)toc.MaterialParameterBufferSize);
        
        // Get position after the parameters
        expectedPosition = reader.BaseStream.Length - toc.StringBufferSize;
        
        UnknownData = reader.ReadBytes((int)(expectedPosition-reader.BaseStream.Position));
        
        List<string> stringBlock = [];
        for (int i = 0; i < toc.MaterialCount; i++)
        {
            stringBlock.Clear();
            while (reader.BaseStream.Position + 4 < reader.BaseStream.Length)
            {
                string entry = ReadNullTerminatedString(reader);
                if (stringBlock.Contains(entry))
                {
                    reader.BaseStream.Position -= entry.Length + 1;
                    break;
                }
                else
                {
                    if (string.IsNullOrEmpty(entry))
                    {
                        break;
                    }
                    stringBlock.Add(entry);
                }             
            }

            Strings.Add([.. stringBlock]);
        }
    }

    public static void Write(MdcFile oldConfiguration, MdcFile newConfiguration, string outputPath)
    {
        TableOfContents toc = new()
        {
            MaterialCount = (uint)newConfiguration.MaterialNames.Count,           
        };

        if (toc.MaterialCount != oldConfiguration.MaterialNames.Count)
        {
            throw new NotImplementedException("Additional material handling hasn't been added yet");
        }

        toc.MaterialParameterBufferSize = (uint)oldConfiguration.MaterialParameters.Length;

        foreach (string name in newConfiguration.MaterialNames)
        {
            // Adding 1 because they're null-terminated
            toc.MaterialNameBufferSize += (uint)(name.Length + 1);
        }

        // Adding alignment if necessary
        int padding = (int)(toc.MaterialNameBufferSize % 4);
        if (padding != 0)
        {
            toc.MaterialNameBufferSize += (uint)(4 - padding);
        }

        for (int i = 0; i < toc.MaterialCount; i++)
        {
            foreach (string text in newConfiguration.Strings[i])
            {
                toc.StringBufferSize += (uint)(text.Length + 1);
            }
        }

        padding = (int)(toc.StringBufferSize % 4);
        if (padding != 0)
        {
            toc.StringBufferSize += (uint)(4 - padding);
        }

        using FileStream stream = new(outputPath, FileMode.Create, FileAccess.Write);
        using BinaryWriter writer = new(stream);

        writer.Write(Header.Magic);
        writer.Write(Header.Unknown0);
        writer.Write(Header.Unknown1);
        writer.Write(Header.Unknown2);
        writer.Write(new byte[Header.Padding0Size]);
        writer.Write(Header.Unknown3);
        writer.Write(new byte[Header.Padding1Size]);

        writer.Write(toc.MaterialNameBufferSize);
        writer.Write(toc.MaterialCount);
        writer.Write(toc.MaterialParameterBufferSize);
        writer.Write(toc.StringBufferSize);

        writer.Write(oldConfiguration.UnknownFloats);

        foreach (string name in newConfiguration.MaterialNames)
        {
            WriteNullTerminatedString(writer, name);
        }

        padding = (int)(writer.BaseStream.Position % 4);
        if (padding != 0)
        {
            padding = 4 - padding;
            writer.Write(new byte[padding]);
        }

        writer.Write(oldConfiguration.MaterialParameters);
        writer.Write(oldConfiguration.UnknownData);

        foreach (string[] stringSet in newConfiguration.Strings)
        {
            foreach (string entry in stringSet)
            {
                WriteNullTerminatedString(writer, entry);
            }
        }

        padding = (int)(writer.BaseStream.Position % 4);
        if (padding != 0)
        {
            padding = 4 - padding;
            writer.Write(new byte[padding]);
        }

        writer.Flush();
    }

    public static void Export(MdcFile configuration, string outputPath)
    {
        outputPath = Path.ChangeExtension(outputPath, ".json");

        JsonWriterOptions options = new()
        {
            Indented = true
        };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, options);

        writer.WriteStartArray();
        for (int i = 0; i < configuration.MaterialNames.Count; i++)
        {
            writer.WriteStartObject();
            writer.WriteString("materialName", configuration.MaterialNames[i]);
            writer.WriteStartArray("stringBlock");
            foreach (string entry in configuration.Strings[i])
            {
                writer.WriteStringValue(entry);
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

    public static MdcFile Import(string inputPath)
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(inputPath));
        JsonElement root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new Exception("Expected top-level element to be an array");
        }

        MdcFile configuration = new()
        {
            MaterialNames = [],
            Strings = []
        };

        foreach (JsonElement material in root.EnumerateArray())
        {
            if (material.ValueKind != JsonValueKind.Object)
            {
                throw new Exception("Expected material object");
            }

            if (!material.TryGetProperty("materialName", out JsonElement materialName) ||
                materialName.ValueKind != JsonValueKind.String)
            {
                throw new Exception("Expected materialName string");
            }

            configuration.MaterialNames.Add(materialName.GetString()!);

            if (!material.TryGetProperty("stringBlock", out JsonElement stringBlock) ||
                stringBlock.ValueKind != JsonValueKind.Array)
            {
                throw new Exception("Expected stringBlock array");
            }

            string[] entryArray = new string[stringBlock.GetArrayLength()];

            for (int i = 0; i < entryArray.Length; i++)
            {
                entryArray[i] = stringBlock[i].GetString()!;
            }

            configuration.Strings.Add(entryArray);
        }

        return configuration;
    }
}
