using System.IO.Compression;
namespace MithrilToolbox.Formats.Shared;

/// <summary>
/// ZLIB compression handling methods
/// </summary>
public class CompressionUtils
{
    /// <summary>
    /// Magic number used in the custom header of the compressed block
    /// </summary>
    private const uint MAGIC = 0x5A4C4942;

    public static void Decompress(string path)
    {
        string outputExtension = "dec" + Path.GetExtension(path);

        using FileStream input = File.OpenRead(path);
        
        string outputPath = Path.ChangeExtension(path, outputExtension);
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
        using FileStream output = File.Create(outputPath);

        byte[] header = new byte[0x80];
        input.ReadExactly(header);
        output.Write(header, 0, header.Length);

        input.Seek(0x90, SeekOrigin.Begin);

        using ZLibStream zLib = new(input, CompressionMode.Decompress);
        zLib.CopyTo(output);
    }

    public static void Compress(string path)
    {
        string outputExtension = "z" + Path.GetExtension(path);

        using FileStream input = File.OpenRead(path);

        byte[] header = new byte[0x80];
        input.ReadExactly(header);

        MemoryStream compressedData = new();
        using (var zlib = new ZLibStream(compressedData, CompressionMode.Compress, leaveOpen: true))
        {
            input.CopyTo(zlib);
        }
        compressedData.Position = 0;

        byte[] compressedDataSize = BitConverter.GetBytes((uint)(compressedData.Length));
        Array.Resize(ref compressedDataSize, 4);
        Array.Reverse(compressedDataSize);       

        byte[] uncompressedDataSize = BitConverter.GetBytes((uint)(input.Length - header.Length));
        Array.Resize(ref uncompressedDataSize, 4);
        Array.Reverse(uncompressedDataSize);

        string outputPath = Path.ChangeExtension(path, outputExtension);
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
        using FileStream output = File.Create(outputPath);

        output.Write(header, 0, header.Length);     
        output.Write(BitConverter.GetBytes(MAGIC), 0, 4);
        output.Write(uncompressedDataSize, 0, 4);
        output.Write(compressedDataSize, 0, 4);
        output.Write([0, 0, 0, 7], 0, 4);

        compressedData.CopyTo(output);
    }

    public static bool IsCompressed(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream);
        
        reader.BaseStream.Seek(0x80, SeekOrigin.Begin);
        uint magic = reader.ReadUInt32();

        if (magic == MAGIC) 
        {
            return true;
        }
        else 
        {
            return false;
        }
    }
}
