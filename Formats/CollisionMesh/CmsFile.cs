using BulletSharp;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static MithrilToolbox.Formats.Shared.NumericUtils;
using static MithrilToolbox.Formats.Shared.GltfUtils;

namespace MithrilToolbox.Formats.CollisionMesh;

/// <summary>
/// Collision mesh file format
/// </summary>
public class CmsFile
{
    private readonly struct Header
    {
        public const int Magic = 0x636D7300;
        public const int Unknown0 = 0x02000000;
        public const int Unknown1 = 0x02000000;
        public const int Unknown2 = 0x02000000;
        public const byte Padding0Size = 24;
        public const int Unknown3 = 0x01000000;
        public const byte Padding1Size = 84;
    }

    private struct TableOfContents
    {
        public uint VertexBufferOffset;
        public uint IndexBufferOffset;
        public uint EmptyBlockOffset0;
        public uint EmptyBlockOffset1;
        public uint BvhOffset;
        public uint BoundingMinOffset;
        public uint BoundingMaxOffset;
        public uint VertexCount;
        public uint IndexCount;
        public uint BvhSize;
        public uint MaterialIndexCountOffset;
        public uint MaterialCount;
        public uint MaterialNameOffset;
        public uint MaterialNameLengthOffset;
    }

    private List<Vector3> Vertices = [];
    private List<int> Indices = [];
    private List<byte> Bvh = [];
    private List<byte> EmptyBlock0 = [];
    private List<byte> EmptyBlock1 = [];
    private Vector4 BoundingMin;
    private Vector4 BoundingMax;
    private List<int> MaterialIndexCounts = [];
    private List<int> MaterialNameLengths = [];
    private List<string> MaterialNames = [];

    private CmsFile() {}

    public CmsFile(string filePath)
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
            VertexBufferOffset = reader.ReadUInt32(),
            IndexBufferOffset = reader.ReadUInt32(),
            EmptyBlockOffset0 = reader.ReadUInt32(),
            EmptyBlockOffset1 = reader.ReadUInt32(),
            BvhOffset = reader.ReadUInt32(),
            BoundingMinOffset = reader.ReadUInt32(),
            BoundingMaxOffset = reader.ReadUInt32(),           
            VertexCount = reader.ReadUInt32(),
            IndexCount = reader.ReadUInt32(),
            BvhSize = reader.ReadUInt32(),
            MaterialIndexCountOffset = reader.ReadUInt32(),
            MaterialCount = reader.ReadUInt32(),
            MaterialNameOffset = reader.ReadUInt32(),
            MaterialNameLengthOffset = reader.ReadUInt32(),
        };

        for (int i = 0; i < toc.VertexCount; i++)
        {
            Vector3 vector = ReadVector3(reader);
            reader.ReadInt32(); // Skip zeroed W
            Vertices.Add(vector);
        }

        for (int i = 0; i < toc.IndexCount; i++)
        {
            int index = reader.ReadInt32();
            Indices.Add(index);          
        }

        var blockSize = (toc.EmptyBlockOffset1 + 0x80) - (toc.EmptyBlockOffset0 + 0x80);
        EmptyBlock0.AddRange(reader.ReadBytes((int)blockSize));

        blockSize = (toc.BvhOffset + 0x80) - (toc.EmptyBlockOffset1 + 0x80);
        EmptyBlock1.AddRange(reader.ReadBytes((int)blockSize));

        Bvh.AddRange(reader.ReadBytes((int)toc.BvhSize));
        
        BoundingMin = ReadVector4(reader);
        BoundingMax = ReadVector4(reader);

        for (int i = 0; i < toc.MaterialCount; i++)
        {
            MaterialIndexCounts.Add(reader.ReadInt32());
        }

        reader.BaseStream.Seek(toc.MaterialNameLengthOffset + 0x80, SeekOrigin.Begin);

        for (int i = 0; i < toc.MaterialCount; i++)
        {
            MaterialNameLengths.Add(reader.ReadInt32());
        }

        reader.BaseStream.Seek(toc.MaterialNameOffset + 0x80, SeekOrigin.Begin);

        for (int i = 0; i < toc.MaterialCount; i++)
        {
            char[] text = reader.ReadChars(MaterialNameLengths[i]);
            MaterialNames.Add(new(text));
        }
    }

    public static void Write(CmsFile oldMesh, CmsFile newMesh, string path)
    {
        newMesh.Bvh.AddRange(BuildAndSerializeBvh(newMesh.Vertices, newMesh.Indices));
 
        TableOfContents toc = new()
        {
            VertexCount = (uint)newMesh.Vertices.Count,
            IndexCount = (uint)newMesh.Indices.Count,
            MaterialCount = (uint)newMesh.MaterialNames.Count,
            BvhSize = (uint)newMesh.Bvh.Count,
            VertexBufferOffset = 0x38 // Relative offset, just after TOC
        };

        toc.IndexBufferOffset = (uint)(newMesh.Vertices.Count * 16) + toc.VertexBufferOffset;
        toc.EmptyBlockOffset0 = (uint)(newMesh.Indices.Count * 4) + toc.IndexBufferOffset;
        toc.EmptyBlockOffset1 = (uint)oldMesh.EmptyBlock0.Count + toc.EmptyBlockOffset0;
        toc.BvhOffset = (uint)oldMesh.EmptyBlock1.Count + toc.EmptyBlockOffset1;
        toc.BoundingMinOffset = toc.BvhSize + toc.BvhOffset;
        toc.BoundingMaxOffset = 16 + toc.BoundingMinOffset;
        toc.MaterialIndexCountOffset = 16 + toc.BoundingMaxOffset;
        toc.MaterialNameOffset = (4 * toc.MaterialCount) + toc.MaterialIndexCountOffset;
        toc.MaterialNameLengthOffset = toc.MaterialNameOffset;
        foreach (int value in oldMesh.MaterialNameLengths)
        {
            toc.MaterialNameLengthOffset += (uint)value;
        }

        Vector3 min = new(float.MaxValue);
        Vector3 max = new(float.MinValue);

        foreach (var vertex in newMesh.Vertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }

        newMesh.BoundingMin = new(min, 0);
        newMesh.BoundingMax = new(max, 0);

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write);
        using BinaryWriter writer = new(stream);

        writer.Write(Header.Magic);
        writer.Write(Header.Unknown0);
        writer.Write(Header.Unknown1);
        writer.Write(Header.Unknown2);
        writer.Write(new byte[Header.Padding0Size]);
        writer.Write(Header.Unknown3);
        writer.Write(new byte[Header.Padding1Size]);

        writer.Write(toc.VertexBufferOffset);
        writer.Write(toc.IndexBufferOffset);
        writer.Write(toc.EmptyBlockOffset0);
        writer.Write(toc.EmptyBlockOffset1);
        writer.Write(toc.BvhOffset);
        writer.Write(toc.BoundingMinOffset);
        writer.Write(toc.BoundingMaxOffset);
        writer.Write(toc.VertexCount);
        writer.Write(toc.IndexCount);
        writer.Write(toc.BvhSize);
        writer.Write(toc.MaterialIndexCountOffset);
        writer.Write(toc.MaterialCount);
        writer.Write(toc.MaterialNameOffset);
        writer.Write(toc.MaterialNameLengthOffset);

        foreach (Vector3 vertex in newMesh.Vertices)
        {
            WriteVector4(writer, new(vertex, 0));
        }

        foreach (int index in newMesh.Indices)
        {
            writer.Write(index);
        }

        writer.Write(oldMesh.EmptyBlock0.ToArray());
        writer.Write(oldMesh.EmptyBlock1.ToArray());

        writer.Write(newMesh.Bvh.ToArray());

        WriteVector4(writer, newMesh.BoundingMin);
        WriteVector4(writer, newMesh.BoundingMax);

        foreach (int value in newMesh.MaterialIndexCounts)
        {
            writer.Write(value);
        }

        foreach (string entry in newMesh.MaterialNames)
        {
            writer.Write(Encoding.ASCII.GetBytes(entry));
        }

        foreach (int entry in newMesh.MaterialNameLengths)
        {
            writer.Write(entry);
        }

        int padding = (int)(writer.BaseStream.Position % 4);
        if (padding != 0)
        {
            padding = 4 - padding;
            writer.Write(new byte[padding]);
        }

        writer.Flush();
    }

    public static void Export(CmsFile mesh, string outputPath)
    {
        string name = Path.GetFileNameWithoutExtension(outputPath);
        ModelRoot modelRoot = ModelRoot.CreateModel();
        Scene scene = modelRoot.UseScene("Scene");

        var node = scene.CreateNode(name);
        var gltfMesh = modelRoot.CreateMesh(name);
        node.Mesh = gltfMesh;

        int indexOffset = 0;
        for (int i = 0; i < mesh.MaterialNames.Count; i++)
        {
            int materialIndexCount = mesh.MaterialIndexCounts[i];

            MeshPrimitive prim = gltfMesh.CreatePrimitive();
            prim.Material = modelRoot.CreateMaterial(mesh.MaterialNames[i]).WithDefault();

            var indexSubset = mesh.Indices.GetRange(indexOffset, materialIndexCount);

            SetVertexData(modelRoot, prim, "POSITION", mesh.Vertices);
            SetIndexData(modelRoot, prim, indexSubset);

            indexOffset += materialIndexCount;
        }

        outputPath = Path.ChangeExtension(outputPath, ".glb");
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }
        modelRoot.SaveGLB(outputPath, new WriteSettings
        {
            Validation = ValidationMode.Strict,
            JsonIndented = true
        });
    }

    public static CmsFile Import(string inputPath)
    {
        ReadSettings settings = new()
        {
            Validation = ValidationMode.Strict
        };

        var modelRoot = ModelRoot.Load(inputPath, settings);
        CmsFile mesh = new();

        Node? node = modelRoot.LogicalScenes[0].VisualChildren.FirstOrDefault();

        if (node != null && node.Mesh != null)
        {
            List<Vector3> globalVertices = [];
            Dictionary<Vector3, int> vertexToIndex = [];

            foreach (var prim in node.Mesh.Primitives)
            {
                // Store material info
                mesh.MaterialNames.Add(prim.Material.Name);
                mesh.MaterialNameLengths.Add(prim.Material.Name.Length);

                var positions = prim.GetVertexAccessor("POSITION").AsVector3Array();

                List<int> indexSubset = [];

                // Remap each triangle's indices
                foreach (var (A, B, C) in prim.GetTriangleIndices())
                {
                    int a = AddOrGetGlobalIndex(positions[A]);
                    int b = AddOrGetGlobalIndex(positions[B]);
                    int c = AddOrGetGlobalIndex(positions[C]);

                    indexSubset.Add(a);
                    indexSubset.Add(b);
                    indexSubset.Add(c);
                }

                mesh.MaterialIndexCounts.Add(indexSubset.Count);
                mesh.Indices.AddRange(indexSubset);
            }

            mesh.Vertices.AddRange(globalVertices);

            int AddOrGetGlobalIndex(Vector3 v)
            {
                if (!vertexToIndex.TryGetValue(v, out int idx))
                {
                    idx = globalVertices.Count;
                    if (!globalVertices.Contains(v))
                    {
                        globalVertices.Add(v);
                    }
                    vertexToIndex[v] = idx;
                }
                return idx;
            }
        }

        return mesh;
    }

    /// <summary>
    /// Build an optimized (quantized) BVH for a triangle mesh and return a serialized byte[] buffer
    /// containing the BVH (header + node array)
    /// </summary>
    /// <param name="vertices">Shared vertex buffer</param>
    /// <param name="indices">Shader index buffer</param>
    /// <returns>byte[] with Bullet-serialized BVH</returns>
    private static byte[] BuildAndSerializeBvh(IList<Vector3> vertices, List<int> indices)
    {
        var bVerts = vertices.Select(v => new BulletSharp.Math.Vector3(v.X, v.Y, v.Z)).ToArray();

        var indexedMesh = new IndexedMesh
        {
            NumTriangles = indices.Count / 3,
            NumVertices = bVerts.Length,
            TriangleIndexBase = IntPtr.Zero,
            VertexBase = IntPtr.Zero,
            VertexStride = Marshal.SizeOf<BulletSharp.Math.Vector3>(),
            TriangleIndexStride = sizeof(int) * 3,
            IndexType = PhyScalarType.Int32,
            VertexType = PhyScalarType.Single
        };

        // Pin arrays so Bullet can read them without GC moving
        GCHandle vertsHandle = default;
        GCHandle indsHandle = default;
        try
        {
            vertsHandle = GCHandle.Alloc(bVerts, GCHandleType.Pinned);
            var bIndexArray = indices.ToArray();
            indsHandle = GCHandle.Alloc(bIndexArray, GCHandleType.Pinned);

            indexedMesh.VertexBase = vertsHandle.AddrOfPinnedObject();
            indexedMesh.TriangleIndexBase = indsHandle.AddrOfPinnedObject();

            using var triIndexVertexArray = new TriangleIndexVertexArray();
            triIndexVertexArray.AddIndexedMesh(indexedMesh, indexedMesh.IndexType);

            // Create a TriangleMeshShape using quantized AABB compression
            using var bvhShape = new BvhTriangleMeshShape(triIndexVertexArray, true, true);
            bvhShape.BuildOptimizedBvh();

            var optimizedBvh = bvhShape.OptimizedBvh;
            int bvhSize = (int)optimizedBvh.CalculateSerializeBufferSize();

            IntPtr buffer = Marshal.AllocHGlobal(bvhSize);
            try
            {
                optimizedBvh.Serialize(buffer, (uint)bvhSize, false);

                byte[] managed = new byte[bvhSize];
                Marshal.Copy(buffer, managed, 0, bvhSize);

                return managed;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            if (vertsHandle.IsAllocated) vertsHandle.Free();
            if (indsHandle.IsAllocated) indsHandle.Free();
        }
    }
}