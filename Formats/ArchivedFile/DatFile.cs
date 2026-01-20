using System.Text;

namespace MithrilToolbox.Formats.ArchivedFile;

public class DatFile
{
    public static void Unpack(string inputPath, string outputPath)
    {
        using FileStream stream = new(inputPath, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(stream);

        // Skip magic
        reader.BaseStream.Position = 4;

        uint fileCount = reader.ReadUInt32();

        (uint, int)[] fileDescriptors = new (uint, int)[fileCount];
        for (int i = 0; i < fileCount; i++)
        {
            fileDescriptors[i] = (reader.ReadUInt32(), reader.ReadInt32());
        }

        byte[][] fileData = new byte[fileCount][];
        for (int i = 0; i < fileCount; i++)
        {
            reader.BaseStream.Seek(fileDescriptors[i].Item1, SeekOrigin.Begin);
            fileData[i] = reader.ReadBytes(fileDescriptors[i].Item2);
        }

        reader.Close();
        stream.Close();

        if (!Path.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        string fileType = "";

        for (int i = 0; i < fileCount; i++)
        {
            byte[] currentFile = fileData[i];

            byte[] magic = currentFile[0..4];
            // Dirty fix for some PS4 and NX shader files
            if (!magic.SequenceEqual(new byte[]{0, 0, 0, 0}) && 
                !magic.SequenceEqual(new byte[]{0x91, 0x68, 0x86, 0x19}) &&
                !magic.SequenceEqual(new byte[]{0, 4, 0x30, 0xAE}))
            {
                Array.Reverse(magic);
                fileType = Encoding.ASCII.GetString(magic).TrimEnd('\0');
            }
            else
            {
                fileType = "unknown";
            }

            File.WriteAllBytes(Path.Combine(outputPath, $"{i}.{fileType}"), currentFile);
        }

        Console.WriteLine($"{fileCount} file(s) were exported successfully to \"{Path.GetFullPath(outputPath)}\"");
    }
}
