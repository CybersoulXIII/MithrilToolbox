// Based on Random Talking Bush's MaxScript MDL importer
// TODO: Clean-up

using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System.Numerics;
using System.Text.Json.Nodes;
using static MithrilToolbox.Formats.Model.VertexAttribute;
using static MithrilToolbox.Formats.Shared.NumericUtils;
using static MithrilToolbox.Formats.Shared.StringUtils;
using static MithrilToolbox.Formats.Shared.GltfUtils;

namespace MithrilToolbox.Formats.Model;

/// <summary>
/// 3D model file format
/// </summary>
public class MdlFile
{
    private readonly struct Header
    {
        public const int Magic = 0x6D646C00;
        public const int Unknown0 = 0x02000000;
        public const int Unknown1 = 0x23000000;
        public const int Unknown2 = 0x02000000;
        public const byte Padding0Size = 24;
        public const int Unknown3 = 0x01000000;
        public const byte Padding1Size = 84;
    }

    // Should be made a struct when all the fields are identified
    private List<uint> TableOfContents = [];

    /// <summary>
    /// Helper variable used to ease the handling of mesh data
    /// </summary>
    private List<MeshCollection> Collections = [];
    /// <summary>
    /// Global node buffer
    /// </summary>
    private List<ushort> Nodes = [];
    private List<BoneGroup> BoneGroups = [];
    /// <summary>
    /// Matrix3x3 packed/represented as Matrix4x4
    /// </summary>
    private List<Matrix4x4> LocalMatrices = [];
    /// <summary>
    /// Matrix3x3 packed/represented as Matrix4x4
    /// </summary>
    private List<Matrix4x4> WorldMatrices = [];
    private List<string> BoneNames = [];
    private List<PolyGroup> PolyGroups = [];
    private List<VisGroup> VisGroups = [];
    private static Dictionary<int, int> NodeToBone = [];

    private MdlFile() {}

    public MdlFile(string filePath)
    {
        Read(filePath);
    }

    private void Read(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);

        using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(stream);

        // Skip header
        reader.BaseStream.Position = 0x80;

        uint stringBufferSize = reader.ReadUInt32();
        uint nodeBufferSize = reader.ReadUInt32();
        uint jointCount = reader.ReadUInt32();
        uint unknown1Count = reader.ReadUInt32();
        uint blank = reader.ReadUInt32();
        uint unknown2Count = reader.ReadUInt32();
        uint visGroupCount = reader.ReadUInt32();
        uint meshCount = reader.ReadUInt32();
        uint vertexBufferSize = reader.ReadUInt32();
        uint indexBufferSize = reader.ReadUInt32();
        uint vertexStrideBufferSize = reader.ReadUInt32();

        TableOfContents.AddRange(stringBufferSize, nodeBufferSize, jointCount, unknown1Count, blank, 
            unknown2Count, visGroupCount, meshCount, vertexBufferSize, indexBufferSize, vertexStrideBufferSize);

        for (int i = 0; i < jointCount; i++)
        {
            string name = ReadNullTerminatedString(reader);
            BoneNames.Add(name);
        }

        long nodeBufferStart = stringBufferSize + 0xAC;
        reader.BaseStream.Seek(nodeBufferStart, SeekOrigin.Begin);

        for (int i = 0; i < (nodeBufferSize / 2); i++)
        {
            ushort nodeId = reader.ReadUInt16(); 
            Nodes.Add(nodeId); 
        }

        long boneBufferStart = nodeBufferStart + nodeBufferSize;
        reader.BaseStream.Seek(boneBufferStart, SeekOrigin.Begin);

        for (int i = 0; i < jointCount; i++)
        {
            BoneGroup bone = new()
            {
                Name = BoneNames[i],
                Rotation = ReadQuaternion(reader),
                Position = ReadVector3(reader),
                Scale = ReadVector3(reader),
                NameOffset = reader.ReadInt32(),
                ParentId = reader.ReadInt16(),
                ChildId = reader.ReadInt16(),
                BrotherId = reader.ReadInt16(),
                Reserved = reader.ReadInt16(),
                Unknown2 = reader.ReadInt32()
            };
            reader.BaseStream.Seek(0x8, SeekOrigin.Current);            
            BoneGroups.Add(bone);
        }

        for (int i = 0; i < jointCount; i++)
        {
            Matrix4x4 transform = ReadMatrix4x4(reader);
            LocalMatrices.Add(transform);
        }

        for (int i = 0; i < jointCount; i++)
        {
            Matrix4x4 transform = ReadMatrix4x4(reader);
            WorldMatrices.Add(transform);
        }

        for (int i = 0; i < visGroupCount; i++)
        {
            VisGroup vis = new();
            vis.VisNum = reader.ReadInt32();
            vis.LodNum = reader.ReadInt16();
            
            reader.BaseStream.Seek(0x2, SeekOrigin.Current); // May be padding or part of LodNum
                        
            vis.VertexCount = reader.ReadInt32();
            vis.FaceStart = reader.ReadInt32();
            vis.FaceCount = reader.ReadInt32();
            vis.VertexStart = reader.ReadInt32();

            reader.BaseStream.Seek(0x8, SeekOrigin.Current); // May be padding
            
            vis.BoundingMax = ReadVector4(reader);           
            vis.BoundingTransform = ReadMatrix3x4(reader);
            vis.BoundingMin = ReadVector4(reader);

            VisGroups.Add(vis);
        }

        for (int i = 0; i < meshCount; i++)
        {
            PolyGroup poly = new()
            {
                VisGroupStart = reader.ReadInt32(),
                VisGroupCount = reader.ReadInt32(),
                VertexStart = reader.ReadInt32(),
                VertexSize = reader.ReadInt32(),
                VertexStrideSize = reader.ReadInt32(),
                IndexStart = reader.ReadInt32(),
                IndexSize = reader.ReadInt32(),
                Unknown1 = reader.ReadInt32(),
                Unknown2 = reader.ReadInt32(),
                NodeIdStart = reader.ReadInt32(),
                NodeIdCount = reader.ReadInt32(),
                MaterialId = reader.ReadInt32(),
                MeshType = reader.ReadInt32()
            };
            PolyGroups.Add(poly);
        }

        long vertexBufferStart = reader.BaseStream.Position;
        long indexBufferStart = vertexBufferStart + vertexBufferSize;
        long vertexStrideBufferStart = indexBufferStart + indexBufferSize;

        reader.BaseStream.Seek(vertexStrideBufferStart, SeekOrigin.Begin);

        for (int i = 0; i < meshCount; i++)
        {
            MeshCollection col = new()
            {
                Name = $"{fileName}_g{i}",
                MaterialId = PolyGroups[i].MaterialId
            };

            short endCheck = 0;
            do
            {
                endCheck = reader.ReadInt16();
                VertexAttribute attributeSet = new()
                {
                    EndCheck = endCheck
                };

                if (endCheck != -1)
                {
                    attributeSet.StrideStart = reader.ReadInt16();
                    attributeSet.VertexAmount = reader.ReadByte();
                    attributeSet.VertexFormat = (AttributeFormat)reader.ReadByte();
                    attributeSet.VertexType = (AttributeType)reader.ReadByte();
                    attributeSet.Unknown = reader.ReadByte();
                }
                else
                {
                    reader.BaseStream.Seek(6, SeekOrigin.Current); // Padding
                }

                col.Attributes.Add(attributeSet);
            } while (endCheck != -1);

            Collections.Add(col);
        }

        for (int i = 0; i < meshCount; i++)
        {
            var group = PolyGroups[i];
            reader.BaseStream.Seek(vertexBufferStart + group.VertexStart, SeekOrigin.Begin);
            
            int groupVertexCount = group.VertexSize / group.VertexStrideSize;
            var attributeSet = Collections[i].Attributes;

            // Populate the per-group node array
            List<ushort> nodeArray = [];
            for (int j = 0; j < group.NodeIdCount; j++)
            {
                nodeArray.Add(Nodes[j + group.NodeIdStart]);
            }

            // Populate the per-group vertex array
            List<Vertex> groupVertices = [];
            for (int vertexIndex = 0; vertexIndex < groupVertexCount; vertexIndex++)
            {
                Vertex vertex = Vertex.ReadVertex(reader, nodeArray, attributeSet);

                groupVertices.Add(vertex);
            }

            reader.BaseStream.Seek(indexBufferStart + group.IndexStart, SeekOrigin.Begin);

            // Populate the per-group index array
            List<ushort> groupIndices = [];
            for (int j = 0; j < group.IndexSize/2; j++)
            {
                ushort index = reader.ReadUInt16();
                groupIndices.Add(index);
            }

            // Create the visible meshes
            for (int j = 0; j < group.VisGroupCount; j++)
            {
                var visGroup = VisGroups[group.VisGroupStart + j];

                Mesh visMesh = new()
                {
                    Name = $"{Collections[i].Name}_m{j}",
                    VisibilityId = visGroup.VisNum
                };

                visMesh.Vertices = groupVertices.GetRange(visGroup.VertexStart, visGroup.VertexCount);
                List<ushort> visIndices = groupIndices.GetRange(visGroup.FaceStart, visGroup.FaceCount);
                
                for (int k = 0; k < visIndices.Count; k += 3)
                {
                    visMesh.Faces.Add(
                    [
                        (ushort)(visIndices[k] - visGroup.VertexStart),
                        (ushort)(visIndices[k + 1] - visGroup.VertexStart),
                        (ushort)(visIndices[k + 2] - visGroup.VertexStart)        
                    ]);
                }

                Collections[i].VisibleMeshes.Add(visMesh);
            }
        }
    }

    public static void Write(MdlFile oldModel, MdlFile newModel, string path, bool useAdditiveMode)
    {
        List<long> addresses = [];

        // Some elements will be written as is, others will be updated
        newModel.TableOfContents = oldModel.TableOfContents;

        if (useAdditiveMode)
        {
            for (int i = 0; i < newModel.Collections.Count; i++)
            {
                var newGroup = newModel.Collections[i];
                var oldGroup = oldModel.Collections[i];
                for (int j = 0; j < newGroup.VisibleMeshes.Count; j++)
                {
                    if (j < oldGroup.VisibleMeshes.Count)
                    {
                        newGroup.VisibleMeshes[j] = oldGroup.VisibleMeshes[j];
                    }
                }
            }
        }

        newModel.TableOfContents[7] = (uint)newModel.Collections.Count;
        
        for (int i = 0; i < newModel.Collections.Count; i++)
        {
            newModel.PolyGroups.Add(new()
            {
                VisGroupCount = 0,
                VisGroupStart = 0,
                IndexSize = 0,
                IndexStart = 0,
                VertexSize = 0,
                VertexStart = 0,
                VertexStrideSize = 0,
                Unknown1 = 1,
                Unknown2 = 0,
                MeshType = newModel.BoneGroups.Count != 0 ? 1 : 0,
                NodeIdCount = 0,
                NodeIdStart = 0,
                MaterialId = newModel.Collections[i].MaterialId
            });               
        }

        int visOffset = 0;
        for (int i = 0; i < newModel.PolyGroups.Count; i++)
        {
            var group = newModel.Collections[i];
            var poly = newModel.PolyGroups[i];

            // Global arrays for this group (as export expects)
            int groupVertexCursor = 0;
            int groupIndexCursor = 0;

            poly.VisGroupStart = visOffset;
            poly.VisGroupCount = group.VisibleMeshes.Count;

            foreach (var visMesh in group.VisibleMeshes)
            {
                // Compute bounding box
                Vector3 min = new(float.MaxValue);
                Vector3 max = new(float.MinValue);

                foreach (var v in visMesh.Vertices)
                {
                    min = Vector3.Min(min, v.Position);
                    max = Vector3.Max(max, v.Position);
                }

                Vector3 boundingMin = (min + max) / 2;
                Vector3 boundingMax = (max - min) / 2;

                int vertexStart = groupVertexCursor;
                int vertexCount = visMesh.Vertices.Count;

                int faceStart = groupIndexCursor;
                int faceCount = visMesh.Faces.Count * 3;

                // Add the VisGroup
                newModel.VisGroups.Add(new VisGroup()
                {
                    VisNum = visMesh.VisibilityId,
                    LodNum = -1,
                    VertexStart = vertexStart,
                    VertexCount = vertexCount,
                    FaceStart = faceStart,
                    FaceCount = faceCount,
                    BoundingMin = new(boundingMin.X, boundingMin.Y, boundingMin.Z, 0),
                    BoundingMax = new(boundingMax.X, boundingMax.Y, boundingMax.Z, 0),
                    BoundingTransform = Matrix3x4.Identity
                });

                // Advance group-level cursors
                groupVertexCursor += vertexCount;
                groupIndexCursor += faceCount;

                visOffset++;
            }
        }

        newModel.TableOfContents[6] = (uint)newModel.VisGroups.Count;

        if (newModel.BoneGroups.Count > 0)
        {
            // Set the TOC's string buffer size
            foreach (string name in newModel.BoneNames)
            {
                newModel.TableOfContents[0] += (uint)name.ToCharArray().Length + 1;
            }
            newModel.TableOfContents[0] /= 2;
            if (newModel.TableOfContents[0] % 2 != 0)
            {
                newModel.TableOfContents[0] += 1;
            }

            // Populates the bone index buffer;           
            int currentNode = 0;
            for (int i = 0; i < newModel.Collections.Count; i++)
            {
                var collection = newModel.Collections[i];
                List<ushort> perGroupBoneIndices = [];
                Dictionary<ushort, ushort> remap = [];
                foreach (var mesh in collection.VisibleMeshes)
                {
                    foreach (var vertex in mesh.Vertices)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            ushort bone = vertex.BoneIndices[j];
                            if (!remap.ContainsKey(bone))
                            {
                                ushort newLocalIndex = (ushort)perGroupBoneIndices.Count;
                                perGroupBoneIndices.Add(bone);
                                remap.Add(bone, newLocalIndex);
                            }
                        }
                    }
                }

                newModel.PolyGroups[i].NodeIdStart = currentNode;
                newModel.PolyGroups[i].NodeIdCount = perGroupBoneIndices.Count;

                // Remaps the attributes to use relative indices
                foreach (var mesh in collection.VisibleMeshes)
                {                  
                    foreach (var vertex in mesh.Vertices)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            vertex.BoneIndices[j] = remap[vertex.BoneIndices[j]];
                        }
                    }
                }

                newModel.Nodes.AddRange(perGroupBoneIndices);

                currentNode += perGroupBoneIndices.Count;
            }

            // Set the TOC's bone index buffer size
            newModel.TableOfContents[1] = (uint)newModel.Nodes.Count * 2;
            if (newModel.TableOfContents[1] % 2 != 0)
            {
                newModel.TableOfContents[1] += 1;
            }
        }

        // All vertices should have the same attributes (checking the 1st one should be enough)
        foreach (var collection in newModel.Collections)
        {            
            var vertex = collection.VisibleMeshes[0].Vertices[0];
            collection.Attributes = BuildAttributes(vertex);
        }        

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write);
        using BinaryWriter writer = new(stream);

        writer.Write(Header.Magic);
        writer.Write(Header.Unknown0);
        writer.Write(Header.Unknown1);
        writer.Write(Header.Unknown2);
        writer.Write(new byte[Header.Padding0Size]);
        writer.Write(Header.Unknown3);
        writer.Write(new byte[Header.Padding1Size]);

        foreach (uint element in newModel.TableOfContents)
        {
            writer.Write(element);
        }

        if (newModel.TableOfContents[0] > 0)
        {
            foreach (string joint in newModel.BoneNames)
            {
                WriteNullTerminatedString(writer, joint);
            }

            while ((writer.BaseStream.Position - 0xAC) < newModel.TableOfContents[0])
            {
                writer.Write((byte)0);
            }
        }

        int currentFileSize = (int)writer.BaseStream.Position;
        if (newModel.TableOfContents[1] > 0)
        {
            foreach (ushort node in newModel.Nodes)
            {
                writer.Write(node);
            }
            while ((writer.BaseStream.Position - currentFileSize) < newModel.TableOfContents[1])
            {
                writer.Write((byte)0);
            }
        }

        if (newModel.TableOfContents[2] > 0)
        {
            foreach (var bone in newModel.BoneGroups)
            {
                WriteVector4(writer, bone.Rotation.AsVector4());
                WriteVector3(writer, bone.Position);
                WriteVector3(writer, bone.Scale);
                writer.Write(bone.NameOffset);
                writer.Write(bone.ParentId);
                writer.Write(bone.ChildId);
                writer.Write(bone.BrotherId);
                writer.Write(bone.Reserved);
                writer.Write(bone.Unknown2);
                writer.Write([0, 0, 0, 0, 0, 0, 0, 0]);
            }

            foreach (var matrix in newModel.LocalMatrices)
            {
                WriteMatrix4x4(writer, matrix);
            }

            foreach (var matrix in newModel.WorldMatrices)
            {
                WriteMatrix4x4(writer, matrix);
            }
        }

        foreach (var vis in newModel.VisGroups)
        {
            writer.Write(vis.VisNum);
            writer.Write(vis.LodNum);
            writer.Write([0, 0]);           
            writer.Write(vis.VertexCount);
            writer.Write(vis.FaceStart);
            writer.Write(vis.FaceCount);
            writer.Write(vis.VertexStart);
            writer.Write([0, 0, 0, 0, 0, 0, 0, 0]);
            WriteVector4(writer, vis.BoundingMax);
            WriteMatrix3x4(writer, vis.BoundingTransform);
            WriteVector4(writer, vis.BoundingMin);
        }

        foreach (var group in newModel.PolyGroups)
        {
            writer.Write(group.VisGroupStart);
            writer.Write(group.VisGroupCount);
            addresses.Add(writer.BaseStream.Position);
            writer.Write(group.VertexStart);
            writer.Write(group.VertexSize);
            writer.Write(group.VertexStrideSize);
            writer.Write(group.IndexStart);
            writer.Write(group.IndexSize);
            writer.Write(group.Unknown1);
            writer.Write(group.Unknown2);
            writer.Write(group.NodeIdStart);
            writer.Write(group.NodeIdCount);
            writer.Write(group.MaterialId);
            writer.Write(group.MeshType);
        }

        uint[] tocValues = [0,0,0];
        int currentMeshStart = 0, StrideStart = 0;
        int sectionStart = (int)writer.BaseStream.Position;

        for (int i = 0; i < newModel.PolyGroups.Count; i++)
        {
            var group = newModel.Collections[i];
            currentMeshStart = (int)writer.BaseStream.Position;
            newModel.PolyGroups[i].VertexStart = currentMeshStart - sectionStart;

            bool strideSet = false;

            foreach (var mesh in group.VisibleMeshes)
            {
                foreach (var vertex in mesh.Vertices)
                {
                    if (!strideSet)
                    {
                        StrideStart = (int)writer.BaseStream.Position;
                    }
                     
                    Vertex.WriteVertex(writer, vertex, group.Attributes);

                    if (!strideSet)
                    {
                        newModel.PolyGroups[i].VertexStrideSize = (int)writer.BaseStream.Position - StrideStart;
                        strideSet = true;
                    }
                }
            }

            newModel.PolyGroups[i].VertexSize = (int)writer.BaseStream.Position - currentMeshStart;
            tocValues[0] += (uint)newModel.PolyGroups[i].VertexSize; 
        }

        sectionStart = (int)writer.BaseStream.Position;
        for (int i = 0; i < newModel.PolyGroups.Count; i++)
        {
            MeshCollection group = newModel.Collections[i];
            int relativeVis = newModel.PolyGroups[i].VisGroupStart;

            currentMeshStart = (int)writer.BaseStream.Position;
            newModel.PolyGroups[i].IndexStart = currentMeshStart - sectionStart;
            
            for (int j = 0; j < group.VisibleMeshes.Count; j++)
            {               
                int vertexOffset = newModel.VisGroups[relativeVis + j].VertexStart;
                Mesh mesh = group.VisibleMeshes[j];

                foreach (ushort[] face in mesh.Faces)
                {
                    writer.Write((ushort)(face[0] + vertexOffset));
                    writer.Write((ushort)(face[1] + vertexOffset));
                    writer.Write((ushort)(face[2] + vertexOffset));
                }
            }

            newModel.PolyGroups[i].IndexSize = (int)writer.BaseStream.Position - currentMeshStart;
            tocValues[1] += (uint)newModel.PolyGroups[i].IndexSize;
        }

        for (int i = 0; i < newModel.PolyGroups.Count; i++)
        {
            var group = newModel.Collections[i];
            var attributes = group.Attributes;

            currentMeshStart = (int)writer.BaseStream.Position;
            foreach (var attribute in attributes)
            {
                writer.Write(attribute.EndCheck);
                writer.Write(attribute.StrideStart);
                writer.Write(attribute.VertexAmount);
                writer.Write((byte)attribute.VertexFormat);
                writer.Write((byte)attribute.VertexType);
                writer.Write(attribute.Unknown);
            }
            writer.Write([0xFF, 0xFF]);
            writer.Write([0, 0, 0, 0, 0, 0]);

            tocValues[2] += (uint)(writer.BaseStream.Position - currentMeshStart); 
        }

        writer.Flush();

        for (int i = 0; i < addresses.Count; i++)
        {
            var group = newModel.PolyGroups[i];            
            writer.BaseStream.Position = addresses[i];
            writer.Write(group.VertexStart);
            writer.Write(group.VertexSize);
            writer.Write(group.VertexStrideSize);
            writer.Write(group.IndexStart);
            writer.Write(group.IndexSize);
        }

        writer.Flush();

        writer.BaseStream.Seek(0xA0, SeekOrigin.Begin);
        writer.Write(tocValues[0]);
        writer.Write(tocValues[1]);
        writer.Write(tocValues[2]);

        writer.Flush();
    }

    public static MdlFile Import(string inputPath)
    {
        ReadSettings settings = new()
        {
            Validation = ValidationMode.TryFix
        };

        var modelRoot = ModelRoot.Load(inputPath, settings);
        MdlFile model = new();

        foreach (var node in modelRoot.LogicalScenes[0].VisualChildren)
        {
            var boneLookup = new Dictionary<int, BoneGroup>();
            // Bone processing pass
            ProcessNodes(model, node, null, Matrix4x4.Identity);
            if (boneLookup.Count > 0)
            {
                // Bone order correction and storage
                var orderedBones = boneLookup.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();
                model.BoneGroups.AddRange(orderedBones);
                foreach (var bone in orderedBones)
                {
                    model.BoneNames.Add(bone.Name);
                }
            }

            // Mesh processing pass
            ProcessMeshGroups(node, model);
        }

        return model;
    }

    // Partially adapted from the IO.NET library
    public static void Export(MdlFile model, string outputPath, bool GltfFormat)
    {
        var modelRoot = ModelRoot.CreateModel();
        var scene = modelRoot.UseScene("Scene");

        int boneCount = model.BoneGroups?.Count ?? 0;
        Skin? skin = null;
        Node? armatureRoot = null;

        if (boneCount > 0)
        {
            armatureRoot = scene.CreateNode("Armature");
            var boneNodes = new Node[boneCount];

            var children = new List<int>[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                children[i] = [];
            }

            var rootBoneIndices = new List<int>();
            for (int i = 0; i < boneCount; i++)
            {
                int p = model.BoneGroups[i].ParentId;
                if (p >= 0 && p < boneCount)
                {
                    children[p].Add(i);
                }
                else
                {
                    rootBoneIndices.Add(i);
                }
            }

            Node CreateBoneNode(int boneIndex, Node parent)
            {
                BoneGroup b = model.BoneGroups[boneIndex];
                Node node = parent.CreateNode(b.Name ?? $"bone_{boneIndex}");

                Matrix4x4 local = Matrix4x4.CreateScale(b.Scale)
                            * Matrix4x4.CreateFromQuaternion(b.Rotation)
                            * Matrix4x4.CreateTranslation(b.Position);

                node.LocalMatrix = local;

                boneNodes[boneIndex] = node;

                foreach (int child in children[boneIndex])
                {
                    CreateBoneNode(child, node);
                }

                return node;
            }

            foreach (int rootIdx in rootBoneIndices)
            {
                CreateBoneNode(rootIdx, armatureRoot);
            }

            skin = modelRoot.CreateSkin("Armature");
            skin.BindJoints(boneNodes);
            skin.Skeleton = boneNodes[0];
        }

        foreach (MeshCollection group in model.Collections)
        {
            string name = group.MaterialId.ToString();
            if (!modelRoot.LogicalMaterials.Any(m => m.Name == name))
            {
                Material mat = modelRoot.CreateMaterial(name).WithDefault();
            }
        }

        foreach (MeshCollection group in model.Collections)
        {
            Node groupNode;
            if (armatureRoot != null)
            {
                groupNode = armatureRoot.CreateNode(group.Name ?? "Collection");
            }
            else
            {
                groupNode = scene.CreateNode(group.Name ?? "Collection");
            }

            foreach (var mesh in group.VisibleMeshes)
            {
                if (mesh.Vertices.Count == 0) continue;

                Node node = groupNode.CreateNode(mesh.Name ?? "Mesh");
                var gltfMesh = modelRoot.CreateMesh(mesh.Name ?? "Mesh");
                node.Mesh = gltfMesh;

                MeshPrimitive prim = gltfMesh.CreatePrimitive();
                prim.Material = modelRoot.LogicalMaterials.Single(m => m.Name == group.MaterialId.ToString());

                List<Vector3> positions = [.. mesh.Vertices.Select(v => v.Position)];
                SetVertexData(modelRoot, prim, "POSITION", positions);

                if (mesh.Vertices.Any(v => v.Normals != Vector3.Zero))
                {
                    SetVertexData(modelRoot, prim, "NORMAL", mesh.Vertices.Select(v => v.Normals).ToList());
                }
                if (mesh.Vertices.Any(v => v.Tangents0 != Vector3.Zero))
                {
                    SetVertexData(modelRoot, prim, "TANGENT", mesh.Vertices.Select(v => new Vector4(v.Tangents0, 0f)).ToList());
                }
                if (mesh.Vertices.Any(v => v.UV0 != Vector2.Zero))
                {
                    SetVertexData(modelRoot, prim, "TEXCOORD_0", mesh.Vertices.Select(v => v.UV0).ToList());
                }
                if (mesh.Vertices.Any(v => v.UV1 != null))
                {
                    SetVertexData(modelRoot, prim, "TEXCOORD_1", mesh.Vertices.Select(v => v.UV1 ?? Vector2.Zero).ToList());
                }
                if (mesh.Vertices.Any(v => v.UV2 != null))
                {
                    SetVertexData(modelRoot, prim, "TEXCOORD_2", mesh.Vertices.Select(v => v.UV2 ?? Vector2.Zero).ToList());
                }
                if (mesh.Vertices.Any(v => v.UV3 != null))
                {
                    SetVertexData(modelRoot, prim, "TEXCOORD_3", mesh.Vertices.Select(v => v.UV3 ?? Vector2.Zero).ToList());
                }
                if (mesh.Vertices.Any(v => v.Color0 != null && v.Color0.Length > 0))
                {
                    SetVertexData(modelRoot, prim, "COLOR_0", mesh.Vertices.Select(v =>
                        new Vector4(v.Color0[0], v.Color0[1], v.Color0[2], v.Color0[3])
                    ).ToList());
                }
                if (mesh.Vertices.Any(v => v.Color1 != null && v.Color1.Length > 0))
                {
                    SetVertexData(modelRoot, prim, "COLOR_1", mesh.Vertices.Select(v =>
                        new Vector4(v.Color1[0], v.Color1[1], v.Color1[2], v.Color1[3])
                    ).ToList());
                }
                if (mesh.Vertices.Any(v => v.Weights != null))
                {
                    List<Vector4> jointVecs = [];
                    List<Vector4> weightVecs = [];

                    foreach (var v in mesh.Vertices)
                    {
                        Vector4 w = v.Weights ?? Vector4.UnitX;

                        // Unskinned vertex fallback
                        float sum = w.X + w.Y + w.Z + w.W;
                        if (sum < 0.00001f)
                        {

                            w = Vector4.UnitX;
                        }

                        // Joint indices fallback
                        ushort[] ji = v.BoneIndices ?? [0, 0, 0, 0];

                        jointVecs.Add(new Vector4(ji[0], ji[1], ji[2], ji[3]));
                        weightVecs.Add(w);
                    }

                    SetVertexDataBoneIndices(modelRoot, prim, "JOINTS_0", jointVecs);
                    SetVertexData(modelRoot, prim, "WEIGHTS_0", weightVecs);

                    if (skin != null && jointVecs.Count > 0)
                    {
                        node.Skin = skin;
                    }
                }

                List<int> indices = [];
                foreach (ushort[] face in mesh.Faces)
                {
                    indices.Add(face[0]);
                    indices.Add(face[1]);
                    indices.Add(face[2]);
                }
                SetIndexData(modelRoot, prim, indices);

                JsonObject extras = [];

                if (mesh.Vertices.Any(v => v.Tangents1 != null))
                {
                    var arr = new JsonArray(
                        [.. mesh.Vertices.Select(v =>
                        {
                            var t = v.Tangents1 ?? Vector3.Zero;
                            return new JsonArray(t.X, t.Y, t.Z);
                        })]
                    );

                    extras["TANGENT_1"] = arr;
                }

                extras["VISNUM"] = mesh.VisibilityId;
                gltfMesh.Extras = extras;
            }
        }

        string formattedPath;
        if (GltfFormat)
        {
            formattedPath = Path.ChangeExtension(outputPath, ".gltf");
            if (File.Exists(formattedPath))
            {
                File.Delete(formattedPath);
            }
            modelRoot.SaveGLTF(formattedPath, new WriteSettings
            {
                Validation = ValidationMode.TryFix,
                JsonIndented = true
            });
        }
        else
        {
            formattedPath = Path.ChangeExtension(outputPath, ".glb");
            if (File.Exists(formattedPath))
            {
                File.Delete(formattedPath);
            }
            modelRoot.SaveGLB(formattedPath, new WriteSettings
            {
                Validation = ValidationMode.TryFix,
                JsonIndented = true
            });
        }
    }

    private static void ProcessMeshGroups(Node node, MdlFile model)
    {
        if (node.Mesh == null && node.VisualChildren.Any()
            && !node.IsSkinSkeleton && !node.IsSkinJoint)
        {
            MeshCollection group = new()
            {
                Name = node.Name
            };

            foreach (var child in node.VisualChildren)
            {
                if (child.Mesh != null)
                {
                    ProcessMeshes(child, group);
                }
                else if (child.VisualChildren.Any())
                {
                    ProcessMeshGroups(child, model);
                }
            }

            // Skip Armature or Scene Collection root node
            if (node.Name != "Armature" && node.Name != "Scene Collection")
            {
                model.Collections.Add(group);
            }
        }
    }

    /// <summary>
    /// Mesh data conversion method
    /// </summary>
    private static void ProcessMeshes(Node node, MeshCollection group)
    {
        Mesh mesh = new() 
        {
            Name = node.Name
        };

        List<Vector3> tangent1 = [];
        if (node.Mesh.Extras is JsonObject mExtras)
        {
            if (mExtras.TryGetPropertyValue("VISNUM", out JsonNode? vNode)
                && vNode is JsonValue vData)
            {
                mesh.VisibilityId = (int)vData;
            }

            if (mExtras.TryGetPropertyValue("TANGENT_1", out JsonNode? t1Node)
                && t1Node is JsonArray t1Array)
            {
                foreach (var arr in t1Array.OfType<JsonArray>())
                {
                    float x = (float)(arr[0]?.GetValue<double>() ?? 0);
                    float y = (float)(arr[1]?.GetValue<double>() ?? 0);
                    float z = (float)(arr[2]?.GetValue<double>() ?? 0);
                    tangent1.Add(new(x, y, z));
                }
            }
        }

        foreach (var prim in node.Mesh.Primitives)
        {
            if (prim.Material != null)
            {
                group.MaterialId = Int32.Parse(prim.Material.Name);
            }

            foreach (var (A, B, C) in prim.GetTriangleIndices())
            {
                mesh.Faces.Add([(ushort)A, (ushort)B, (ushort)C]);
            }

            var position = prim.GetVertexAccessor("POSITION").AsVector3Array();
            var normal = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var tangent = prim.GetVertexAccessor("TANGENT")?.AsVector4Array();
            var uv0 = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            var uv1 = prim.GetVertexAccessor("TEXCOORD_1")?.AsVector2Array();
            var uv2 = prim.GetVertexAccessor("TEXCOORD_2")?.AsVector2Array();
            var uv3 = prim.GetVertexAccessor("TEXCOORD_3")?.AsVector2Array();
            var color0 = prim.GetVertexAccessor("COLOR_0")?.AsColorArray();
            var color1 = prim.GetVertexAccessor("COLOR_1")?.AsColorArray();
            var boneId = prim.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
            var weight = prim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

            int tangent1Offset = 0;
            Vertex[] vertices = new Vertex[position.Count];

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vertex
                {
                    Position = position[i]
                };
                if (normal?.Count > 0)
                {
                    vertices[i].Normals = normal[i];
                }
                if (tangent?.Count > 0)
                {
                    vertices[i].Tangents0 = new Vector3(tangent[i].X, tangent[i].Y, tangent[i].Z);
                }
                if (normal?.Count > 0 && tangent?.Count > 0)
                {
                    vertices[i].Binormals0 = Vector3.Cross(vertices[i].Normals, vertices[i].Tangents0);
                }
                if (uv0?.Count > 0)
                {
                    vertices[i].UV0 = uv0[i];
                }
                if (uv1?.Count > 0)
                {
                    vertices[i].UV1 = uv1[i];
                }
                if (uv2?.Count > 0)
                {
                    vertices[i].UV2 = uv2[i];
                }
                if (uv3?.Count > 0)
                {
                    vertices[i].UV3 = uv3[i];
                }
                if (color0?.Count > 0)
                {
                    vertices[i].Color0 = [(byte)(color0[i].X * 255), (byte)(color0[i].Y * 255), (byte)(color0[i].Z * 255), (byte)(color0[i].W * 255)];
                }
                if (color1?.Count > 0)
                {
                    vertices[i].Color1 = [(byte)(color1[i].X * 255), (byte)(color1[i].Y * 255), (byte)(color1[i].Z * 255), (byte)(color1[i].W * 255)];
                }
                if (boneId?.Count > 0)
                {
                    vertices[i].BoneIndices = [(ushort)boneId[i].X, (ushort)boneId[i].Y, (ushort)boneId[i].Z, (ushort)boneId[i].W];
                }
                if (weight?.Count > 0)
                {
                    vertices[i].Weights = weight[i];
                }
                if (tangent1?.Count > 0)
                {
                    if (i < tangent1.Count)
                    {
                        vertices[i].Tangents1 = tangent1[i];
                    }
                    else
                    {
                        vertices[i].Tangents1 = Vector3.UnitZ;
                    }
                }
                if (normal?.Count > 0 && tangent1?.Count > 0)
                {
                    vertices[i].Binormals1 = Vector3.Cross(vertices[i].Normals, vertices[i].Tangents1 ?? Vector3.UnitZ);
                }
                tangent1Offset += vertices.Length;
            }
            mesh.Vertices.AddRange(vertices);
        }
        group.VisibleMeshes.Add(mesh);
    }

    /// <summary>
    /// Bone data conversion method
    /// </summary>
    private static void ProcessNodes(MdlFile model, Node node, BoneGroup? boneParent, Matrix4x4 parentMatrix)
    {
        var worldTransform = node.LocalMatrix * parentMatrix;

        BoneGroup? bone = null;
        int? boneIndex = null;

        // Create bone only if needed
        if (node.IsSkinSkeleton || node.IsSkinJoint || boneParent != null)
        {
            Matrix4x4.Decompose(node.LocalMatrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation);
            bone = new()
            {
                Name = node.Name,
                Scale = scale,
                Rotation = rotation,
                Position = translation,
                NameOffset = 0,
                ParentId = -1,
                ChildId = -1,
                BrotherId = -1,
                Reserved = 0,
                Unknown2 = node.Name == "jointroot" ? 3 : 2
            };

            boneIndex = model.BoneGroups.Count;

            model.BoneNames.Add(bone.Name);
            model.BoneGroups.Add(bone);

            // Fix matrices to match MDL expected format
            var localMatrix = Matrix4x4.Identity;
            localMatrix.M44 = 0;
            model.LocalMatrices.Add(localMatrix);

            var worldMatrix = node.WorldMatrix;
            Matrix4x4.Invert(worldMatrix, out worldMatrix);
            for (int row = 0; row < 4; row++)
            {
                worldMatrix[row, 3] = 0;
            }
            model.WorldMatrices.Add(worldMatrix);

            // Store GLTF node index as bone index
            NodeToBone[node.LogicalIndex] = boneIndex.Value;

            // Assign parent node
            if (node.VisualParent != null && NodeToBone.TryGetValue(node.VisualParent.LogicalIndex, out int parentIdx))
            {
                bone.ParentId = (short)parentIdx;
            }
        }

        var children = node.VisualChildren ?? [];
        int? firstChildBoneIdx = null;

        for (int i = 0; i < children.Count(); i++)
        {
            var current = children.ElementAt(i);

            ProcessNodes(model, current, bone, worldTransform);

            // Remember the first child bone
            if (firstChildBoneIdx == null && NodeToBone.TryGetValue(current.LogicalIndex, out int firstChild))
            {
                firstChildBoneIdx = firstChild;
            }
        }

        // Assign ChildId now that all children have been processed
        if (boneIndex.HasValue && firstChildBoneIdx.HasValue)
        {
            model.BoneGroups[boneIndex.Value].ChildId = (short)firstChildBoneIdx.Value;
        }
    }
}