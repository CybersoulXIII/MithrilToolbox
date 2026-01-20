using MithrilToolbox.Formats.Shared;
using MithrilToolbox.Formats.ArchivedFile;
using MithrilToolbox.Formats.Model;
using MithrilToolbox.Formats.Curve;
using MithrilToolbox.Formats.CellSheet;
using MithrilToolbox.Formats.Rail;
using MithrilToolbox.Formats.ModelConfiguration;
using MithrilToolbox.Formats.CollisionMesh;
using MithrilToolbox.Formats.Texture;
using MithrilToolbox.Formats.CharacterSet;
using CommandLine;

namespace MithrilToolbox;

public class Program
{
    public static void Main(string[] args)
    {
        Type[] types =
        [
            typeof(CompressVerbs), typeof(DecompressVerbs), typeof(UnpackDatVerbs), typeof(ExportCmsVerbs),
            typeof(ImportCmsVerbs), typeof(ExportCrvVerbs), typeof(ImportCrvVerbs), typeof(ExportCsdVerbs),
            typeof(ImportCsdVerbs), typeof(ExportCshVerbs), typeof(ImportCshVerbs), typeof(ExportMdcVerbs),
            typeof(ImportMdcVerbs), typeof(ExportMdlVerbs), typeof(ImportMdlVerbs), typeof(ExportRailVerbs),
            typeof(ImportRailVerbs), typeof(ExportTexVerbs), typeof(ImportTexVerbs)
        ];

        var p = Parser.Default.ParseArguments(args, types);

        p.WithParsed<CompressVerbs>(Compress);
        p.WithParsed<DecompressVerbs>(Decompress);
        p.WithParsed<UnpackDatVerbs>(UnpackDat);
        p.WithParsed<ExportCmsVerbs>(ExportCms);
        p.WithParsed<ImportCmsVerbs>(ImportCms);
        p.WithParsed<ExportCrvVerbs>(ExportCrv);
        p.WithParsed<ImportCrvVerbs>(ImportCrv);
        p.WithParsed<ExportCsdVerbs>(ExportCsd);
        p.WithParsed<ImportCsdVerbs>(ImportCsd);
        p.WithParsed<ExportCshVerbs>(ExportCsh);
        p.WithParsed<ImportCshVerbs>(ImportCsh);
        p.WithParsed<ExportMdcVerbs>(ExportMdc);
        p.WithParsed<ImportMdcVerbs>(ImportMdc);
        p.WithParsed<ExportMdlVerbs>(ExportMdl);
        p.WithParsed<ImportMdlVerbs>(ImportMdl);
        p.WithParsed<ExportRailVerbs>(ExportRail);
        p.WithParsed<ImportRailVerbs>(ImportRail);
        p.WithParsed<ExportTexVerbs>(ExportTex);
        p.WithParsed<ImportTexVerbs>(ImportTex);       
    }

    static void Decompress(DecompressVerbs verbs) 
    {
        if (Path.Exists(verbs.InputFile))
        {
            if (!CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The input file is already decompressed");
            }
            else
            {
                CompressionUtils.Decompress(verbs.InputFile);
                Console.WriteLine("File decompressed successfully");
            }        
        }
    }

    static void Compress(CompressVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile))
        {
            if (CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The input file is already compressed");
            }
            else
            {
                CompressionUtils.Compress(verbs.InputFile);
                Console.WriteLine("File compressed successfully");
            }
        }
    }

    static void UnpackDat(UnpackDatVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".dat")
        {
            verbs.OutputFolder ??= Path.GetFileNameWithoutExtension(verbs.InputFile) + ".unpacked";
            DatFile.Unpack(verbs.InputFile, verbs.OutputFolder);           
        }
    }

    static void ExportMdl(ExportMdlVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".mdl")
        {
            if (CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The input file needs to be decompressed");
            }
            else
            {
                MdlFile model = new(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                MdlFile.Export(model, verbs.OutputFile, verbs.GltfFormat);
                Console.WriteLine("Model exported successfully");
            }
        }
    }

    static void ImportMdl(ImportMdlVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.Exists(verbs.OriginalFile))
        {
            if (CompressionUtils.IsCompressed(verbs.OriginalFile))
            {
                Console.Error.WriteLine("The original file needs to be decompressed");
            }
            else
            {
                MdlFile oldModel = new(verbs.OriginalFile);
                MdlFile newModel = MdlFile.Import(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                string path = Path.ChangeExtension(verbs.OutputFile, ".new.mdl");
                MdlFile.Write(oldModel, newModel, path, verbs.AdditiveMode);
                Console.WriteLine("Model imported successfully");
            }
        }
    }

    static void ExportMdc(ExportMdcVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".mdc")
        {
            if (CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The input file needs to be decompressed");
            }
            else
            {
                MdcFile config = new(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                MdcFile.Export(config, verbs.OutputFile);
                Console.WriteLine("Model configuration exported successfully");
            }
        }
    }

    static void ImportMdc(ImportMdcVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.Exists(verbs.OriginalFile))
        { 
            if (CompressionUtils.IsCompressed(verbs.OriginalFile))
            {
                Console.Error.WriteLine("The original file needs to be decompressed");
            }
            else
            { 
                MdcFile oldConfig = new(verbs.OriginalFile);
                MdcFile newConfig = MdcFile.Import(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                string path = Path.ChangeExtension(verbs.OutputFile, ".new.mdc");
                MdcFile.Write(oldConfig, newConfig, path);
                Console.WriteLine("Model configuration imported successfully");
            }
        }
    }

    static void ExportCrv(ExportCrvVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".crv")
        {
            if (CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The input file needs to be decompressed");
            }
            else
            {
                CrvFile config = new(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                CrvFile.Export(config, verbs.OutputFile);
                Console.WriteLine("Curve exported successfully");
            }
        }
    }

    static void ImportCrv(ImportCrvVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".json")
        {
            CrvFile curve = CrvFile.Import(verbs.InputFile);
            verbs.OutputFile ??= verbs.InputFile;
            string path = Path.ChangeExtension(verbs.OutputFile, ".new.crv");
            CrvFile.Write(curve, path);
            Console.WriteLine("Curve imported successfully");         
        }
    }

    static void ExportCsd(ExportCsdVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".csd")
        {
            if (CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The input file needs to be decompressed");
            }
            else
            {
                CsdFile set = new(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                CsdFile.Export(set, verbs.OutputFile);
                Console.WriteLine("Character set exported successfully");
            }
        }
    }

    static void ImportCsd(ImportCsdVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.Exists(verbs.OriginalFile))
        {
            if (CompressionUtils.IsCompressed(verbs.OriginalFile))
            {
                Console.Error.WriteLine("The original file needs to be decompressed");
            }
            else
            {
                CsdFile newSet = CsdFile.Import(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                string path = Path.ChangeExtension(verbs.OutputFile, ".new.csd");
                CsdFile.Write(newSet, path);
                Console.WriteLine("Character set imported successfully");
            }
        }
    }

    static void ExportRail(ExportRailVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".rail")
        {
            if (CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The input file needs to be decompressed");
            }
            else
            {
                RailFile rail = new(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                RailFile.Export(rail, verbs.OutputFile);
                Console.WriteLine("Rail exported successfully");
            }
        }
    }

    static void ImportRail(ImportRailVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".json")
        {
            RailFile rail = RailFile.Import(verbs.InputFile);
            verbs.OutputFile ??= verbs.InputFile;
            string path = Path.ChangeExtension(verbs.OutputFile, ".new.rail");
            RailFile.Write(rail, path);
            Console.WriteLine("Rail imported successfully");
        }
    }

    static void ExportTex(ExportTexVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".tex")
        {
            if (CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The original file needs to be decompressed");
            }
            else
            {
                TexFile texture = new(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                TexFile.Export(texture, verbs.OutputFile);
                Console.WriteLine("Texture exported successfully");
            }
        }
    }

    static void ImportTex(ImportTexVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.Exists(verbs.OriginalFile))
        {
            if (CompressionUtils.IsCompressed(verbs.OriginalFile))
            {
                Console.Error.WriteLine("The original file needs to be decompressed");
            }
            else
            {
                TexFile oldTexture = new(verbs.OriginalFile);
                TexFile newTexture = TexFile.Import(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                string path = Path.ChangeExtension(verbs.OutputFile, ".new.tex");
                TexFile.Write(oldTexture, newTexture, path);
                Console.WriteLine("Texture imported successfully");
            }
        }
    }

    static void ExportCsh(ExportCshVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".csh")
        {
            if (CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The input file needs to be decompressed");
            }
            else
            {
                CshFile table = new(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                CshFile.Export(table, verbs.OutputFile);
                Console.WriteLine("Cell sheet exported successfully");
            }
        }
    }

    static void ImportCsh(ImportCshVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".csv")
        {
            CshFile curve = CshFile.Import(verbs.InputFile);
            verbs.OutputFile ??= verbs.InputFile;
            string path = Path.ChangeExtension(verbs.OutputFile, ".new.csh");
            CshFile.Write(curve, path);
            Console.WriteLine("Cell sheet imported successfully");
        }
    }

    static void ExportCms(ExportCmsVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.GetExtension(verbs.InputFile) == ".cms")
        {
            if (CompressionUtils.IsCompressed(verbs.InputFile))
            {
                Console.Error.WriteLine("The input file needs to be decompressed");
            }
            else
            {
                CmsFile mesh = new(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                CmsFile.Export(mesh, verbs.OutputFile);
                Console.WriteLine("Collision mesh exported successfully");
            }
        }
    }

    static void ImportCms(ImportCmsVerbs verbs)
    {
        if (Path.Exists(verbs.InputFile) && Path.Exists(verbs.OriginalFile))
        {
            if (CompressionUtils.IsCompressed(verbs.OriginalFile))
            {
                Console.Error.WriteLine("The original file needs to be decompressed");
            }
            else
            {
                CmsFile oldMesh = new(verbs.OriginalFile);
                CmsFile newMesh = CmsFile.Import(verbs.InputFile);
                verbs.OutputFile ??= verbs.InputFile;
                string path = Path.ChangeExtension(verbs.OutputFile, ".new.cms");
                CmsFile.Write(oldMesh, newMesh, path);
                Console.WriteLine("Collision mesh imported successfully");
            }
        }
    }
}

[Verb("decompress", HelpText = "Decompresses a file using ZLIB")]
public class DecompressVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a ZLIB-compressed file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.dec.format")]
    public string? OutputFile { get; set; }
}

[Verb("compress", HelpText = "Compresses a file using ZLIB")]
public class CompressVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be an uncompressed file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.z.format")]
    public string? OutputFile { get; set; }
}

[Verb("dat-unpack", HelpText = "Unpacks an Archived File (DAT) file")]
public class UnpackDatVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a DAT file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output folder. Defaults to <input_folderName>.unpacked")]
    public string? OutputFolder { get; set; }
}

[Verb("mdl-export", HelpText = "Converts a Model (MDL) file to GLB/GLTF")]
public class ExportMdlVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a MDL file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.glb")]
    public string? OutputFile { get; set; }

    [Option("gltf", HelpText = "Optional. Output format. Use this parameter to set the format as GLTF (defaults to GLB)")]
    public bool GltfFormat { get; set; } = false;
}

[Verb("mdl-import", HelpText = "Converts a GLB/GLTF file to Model (MDL)")]
public class ImportMdlVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a GLTF file")]
    public string InputFile { get; set; }

    [Option('r', "reference", Required = true, HelpText = "Reference file. Should be the original MDL file")]
    public string OriginalFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.new.mdl")]
    public string? OutputFile { get; set; }

    [Option("additive", HelpText = "Optional. Additive mode. Use this parameter to set the reuse the original meshes (with all their attributes) and only append new ones")]
    public bool AdditiveMode { get; set; } = false;
}

[Verb("tex-export", HelpText = "Converts a texture (TEX) file to DDS")]
public class ExportTexVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a TEX file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.dds")]
    public string? OutputFile { get; set; }
}

[Verb("tex-import", HelpText = "Converts a DDS file to texture (TEX)")]
public class ImportTexVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a DDS file")]
    public string InputFile { get; set; }

    [Option('r', "reference", Required = true, HelpText = "Reference file. Should be the original TEX file")]
    public string OriginalFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.new.tex")]
    public string? OutputFile { get; set; }
}

[Verb("cms-export", HelpText = "Converts a Collision Mesh (CMS) file to GLTF")]
public class ExportCmsVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a CMS file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.glb")]
    public string? OutputFile { get; set; }
}

[Verb("cms-import", HelpText = "Converts a GLTF file to Collision Mesh (CMS)")]
public class ImportCmsVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a GLTF file")]
    public string InputFile { get; set; }

    [Option('r', "reference", Required = true, HelpText = "Reference file. Should be the original CMS file")]
    public string OriginalFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.new.cms")]
    public string? OutputFile { get; set; }
}

[Verb("mdc-export", HelpText = "Converts a Model Configuration (MDC) file to JSON")]
public class ExportMdcVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a MDC file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.json")]
    public string? OutputFile { get; set; }
}

[Verb("mdc-import", HelpText = "Converts a JSON file to Model Configuration (MDC)")]
public class ImportMdcVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a MDC file")]
    public string InputFile { get; set; }

    [Option('r', "reference", Required = true, HelpText = "Reference file. Should be the original MDC file")]
    public string OriginalFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.new.mdc")]
    public string? OutputFile { get; set; }
}

[Verb("crv-export", HelpText = "Converts a Curve (CRV) file to JSON")]
public class ExportCrvVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a CRV file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.json")]
    public string? OutputFile { get; set; }
}

[Verb("crv-import", HelpText = "Converts a JSON file to Curve (CRV)")]
public class ImportCrvVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a JSON file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.new.crv")]
    public string? OutputFile { get; set; }
}

[Verb("csd-export", HelpText = "Converts a Character Set (CSD) file to JSON")]
public class ExportCsdVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a CSD file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.json")]
    public string? OutputFile { get; set; }
}

[Verb("csd-import", HelpText = "Converts a JSON file to Character Set (CSD)")]
public class ImportCsdVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a CSD file")]
    public string InputFile { get; set; }

    [Option('r', "reference", Required = true, HelpText = "Reference file. Should be the original CSD file")]
    public string OriginalFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.new.csd")]
    public string? OutputFile { get; set; }
}

[Verb("rail-export", HelpText = "Converts a Rail file to JSON")]
public class ExportRailVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a rail file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.json")]
    public string? OutputFile { get; set; }
}

[Verb("rail-import", HelpText = "Converts a JSON file to Rail")]
public class ImportRailVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a JSON file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.new.rail")]
    public string? OutputFile { get; set; }
}

[Verb("csh-export", HelpText = "Converts a Cell Sheet (CSH) file to CSV")]
public class ExportCshVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a CSH file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.csv")]
    public string? OutputFile { get; set; }
}

[Verb("csh-import", HelpText = "Converts a CSV file to Cell Sheet (CSH)")]
public class ImportCshVerbs
{
    [Option('i', "input", Required = true, HelpText = "Input file. Should be a CSV file")]
    public string InputFile { get; set; }

    [Option('o', "output", HelpText = "Optional. Output file. Defaults to <input_filename>.csh")]
    public string? OutputFile { get; set; }
}