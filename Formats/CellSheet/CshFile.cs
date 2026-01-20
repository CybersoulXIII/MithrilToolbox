using System.Globalization;
using static System.Buffers.Binary.BinaryPrimitives;
using static MithrilToolbox.Formats.Shared.StringUtils;

namespace MithrilToolbox.Formats.CellSheet;

/// <summary>
/// Data table file format
/// </summary>
public class CshFile
{
    private struct Header
    {
        public const int Magic = 0x63736800;
        public const int Unknown0 = 0x02000000;
        public const int Unknown1 = 0x0D000000;
        public const int Unknown2 = 0x02000000;
        public const byte Padding0Size = 24;
        public const int Unknown3 = 0x01000000;
        public const byte Padding1Size = 84;
    }

    private enum DataType : byte
    {
        String = 0x00,
        Data = 0x20, // Offset+Length
        Integer = 0x40,
        Float = 0x80,
        Null = 0xA0,
        Empty = 0xC0
    }

    private struct CellMetadata
    {
        public uint DataOffset;
        public DataType Type;
        public ushort SizeMultiplier;
    }

    /// <summary>
    /// Can be 0 or 1
    /// </summary>
    private static uint FileType;
    private static uint RowCount, ColumnCount;

    private CellMetadata[,] CellDescriptors;
    private string[,] CellData;

    private CshFile() { }

    public CshFile(string filePath)
    {
        Read(filePath);
    }

    private void Read(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(stream);

        // Skip file header (0x80) and FileType (0x4)
        reader.BaseStream.Seek(0x84, SeekOrigin.Begin);

        ColumnCount = ReverseEndianness(reader.ReadUInt32());
        RowCount = ReverseEndianness(reader.ReadUInt32());

        CellDescriptors = new CellMetadata[RowCount, ColumnCount];

        for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                CellMetadata metadata = new()
                {
                    DataOffset = ReverseEndianness(reader.ReadUInt32()),
                    Type = (DataType)reader.ReadByte()
                };
                _ = reader.ReadByte();
                metadata.SizeMultiplier = ReverseEndianness(reader.ReadUInt16());

                CellDescriptors[rowIndex, columnIndex] = metadata;
            }
        }

        CellData = new string[RowCount, ColumnCount];

        for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                var cell = CellDescriptors[rowIndex, columnIndex];
                if (cell.DataOffset != 0)
                {                  
                    reader.BaseStream.Seek(cell.DataOffset + 0x80, SeekOrigin.Begin);                    
                }

                string cellValue = "";
                switch (cell.Type)
                {
                    case DataType.String:
                        cellValue = ReadNullTerminatedString(reader);
                        break;
                    case DataType.Data:
                        cellValue += ReverseEndianness(reader.ReadInt16());
                        cellValue += " / ";
                        cellValue += ReverseEndianness(reader.ReadInt16());
                        break;
                    case DataType.Integer:
                        cellValue += ReverseEndianness(reader.ReadInt32());
                        break;
                    case DataType.Float:
                        byte[] rawValue = reader.ReadBytes(4);
                        float floatValue = BitConverter.ToSingle([.. rawValue.Reverse()], 0);
                        cellValue += floatValue;
                        break;
                    case DataType.Null:
                        cellValue = "null";
                        break;
                    case DataType.Empty:
                        break;
                }
                CellData[rowIndex, columnIndex] = cellValue;
            }
        }
    }

    public static void Export(CshFile table, string outputPath)
    {
        outputPath = Path.ChangeExtension(outputPath, ".csv");

        using FileStream stream = new(outputPath, FileMode.Create, FileAccess.Write);
        using StreamWriter writer = new(stream);

        // Write table data
        for (int rowIndex = 0; rowIndex < table.CellData.GetLength(0); rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < table.CellData.GetLength(1); columnIndex++)
            {              
                writer.Write(table.CellData[rowIndex, columnIndex]);

                if (columnIndex < table.CellData.GetLength(1)-1)
                {
                    writer.Write("|");
                }
            }
            writer.WriteLine();
        }
    }

    public static void Write(CshFile table, string outputPath)
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

        writer.Write(ReverseEndianness(FileType));
        writer.Write(ReverseEndianness(ColumnCount));
        writer.Write(ReverseEndianness(RowCount));

        foreach (CellMetadata descriptor in table.CellDescriptors)
        {
            writer.Write(ReverseEndianness(descriptor.DataOffset));
            writer.Write((byte)descriptor.Type);
            writer.Write((byte)0);
            writer.Write(ReverseEndianness(descriptor.SizeMultiplier));
        }

        for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                var cell = table.CellDescriptors[rowIndex, columnIndex];
                
                string cellValue = table.CellData[rowIndex, columnIndex];
                if (cellValue == "null")
                {
                    continue;
                }
                
                switch (cell.Type)
                {
                    case DataType.String:
                        WriteNullTerminatedString(writer, cellValue);
                        int cellSize = cell.SizeMultiplier * 4;
                        byte[] valueArray = System.Text.Encoding.UTF8.GetBytes(cellValue);
                        //writer.Write(valueArray);
                        int padding = cellSize - (valueArray.Length + 1);
                        //int padding = cellSize - valueArray.Length;
                        if (padding > 0)
                        {
                            writer.Write(new byte[padding]);
                        }
                        break;
                    case DataType.Data:
                        string[] rawValue = cellValue.Split(" / ");
                        writer.Write(ReverseEndianness(ushort.Parse(rawValue[0])));
                        writer.Write(ReverseEndianness(ushort.Parse(rawValue[1])));
                        break;
                    case DataType.Integer:
                        writer.Write(ReverseEndianness(int.Parse(cellValue)));
                        break;
                    case DataType.Float:
                        float value = ReverseEndianness(int.Parse(cellValue));
                        writer.Write(value);
                        break;
                }
            }
        }

        
    }

    public static CshFile Import(string inputPath)
    {
        IEnumerable<string> rows = File.ReadLines(inputPath);        

        string[] firstRow = rows.First().Split("|");

        RowCount = (uint)rows.Count();
        ColumnCount = (uint)firstRow.Length;

        CshFile table = new()
        {
            CellDescriptors = new CellMetadata[RowCount, ColumnCount],
            CellData = new string[RowCount, ColumnCount]
        };
       
        uint metadataBlockSize = RowCount * ColumnCount * 8;
        // Table of contents size + Metadata size
        uint localDataOffset = 0xC + metadataBlockSize;

        for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
        {
            string[] rowCells = rows.ElementAt(rowIndex).Split("|");
            
            for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
            {
                string cellValue = rowCells[columnIndex];
                DataType type = ParseType(cellValue);

                CellMetadata descriptor = new()
                {
                    DataOffset = localDataOffset,
                    Type = type,
                    SizeMultiplier = 1
                };
                
                if (cellValue == "null")
                {                 
                    type = DataType.Null;
                    descriptor.Type = type;
                }

                switch (type)
                {
                    case DataType.String:
                        // Using this (instead of string.Length) to match the original format better
                        byte[] valueArray = System.Text.Encoding.UTF8.GetBytes(cellValue);
                        int padding = (valueArray.Length + 1) % 4;
                        if (padding != 0)
                        {
                            padding = 4 - padding;
                        }
                        int size = (valueArray.Length + 1) + padding;
                        descriptor.SizeMultiplier = (ushort)(size / 4);
                        localDataOffset += (uint)size;
                        break;
                    case DataType.Data:
                        descriptor.SizeMultiplier = 2;
                        localDataOffset += 8; 
                        break;
                    case DataType.Float:
                    case DataType.Integer:
                        localDataOffset += 4;
                        break;
                    case DataType.Null:
                    case DataType.Empty:
                        descriptor.DataOffset = 0;
                        descriptor.SizeMultiplier = 0;
                        break;
                }

                table.CellDescriptors[rowIndex, columnIndex] = descriptor;
                table.CellData[rowIndex, columnIndex] = cellValue;
            }
        }

        return table; 
    }

    private static DataType ParseType(string cellValue)
    {
        if (string.IsNullOrWhiteSpace(cellValue))
        {
            return DataType.Empty;
        }

        if (string.Equals(cellValue, "null", StringComparison.OrdinalIgnoreCase))
        {
            return DataType.Null;
        }

        if (int.TryParse(cellValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return DataType.Integer;
        }

        if (float.TryParse(cellValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return DataType.Float;
        }

        if (TryParseData(cellValue))
        {
            return DataType.Data;
        }

        return DataType.String;
    }

    private static bool TryParseData(string rawValue)
    {
        string[] values = rawValue.Split(" / ");

        if (values.Length != 2)
        {
            return false;
        }

        return short.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _) &&
               short.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }
}
