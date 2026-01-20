using System.Numerics;

namespace MithrilToolbox.Formats.Shared;

/// <summary>
/// Numeric parsing related helpers
/// </summary>
public class NumericUtils
{
    public struct Matrix3x4
    {
        public Vector4 m0, m1, m2;

        public static Matrix3x4 Identity => new()
        {
            m0 = new(1, 0, 0, 0),
            m1 = new(0, 1, 0, 0),
            m2 = new(0, 0, 1, 0)
        };

        public Matrix3x4(float m00, float m01, float m02, float m03,
                         float m10, float m11, float m12, float m13,
                         float m20, float m21, float m22, float m23)
        {
            m0 = new(m00, m01, m02, m03);
            m1 = new(m10, m11, m12, m13);
            m2 = new(m20, m21, m22, m23);
        }
    }  

    public static Matrix3x4 ReadMatrix3x4(BinaryReader reader)
    {
        float[] m = new float[12];

        for (int i = 0; i < 12; i++)
        {
            m[i] = reader.ReadSingle();
        }

        return new Matrix3x4(
                m[0], m[1], m[2], m[3],
                m[4], m[5], m[6], m[7],
                m[8], m[9], m[10], m[11]
        );
    }

    public static Matrix4x4 ReadMatrix4x4(BinaryReader reader)
    {
        float[] m = new float[16];

        for (int i = 0; i < 16; i++)
        {
            m[i] = reader.ReadSingle();
        }

        return new Matrix4x4(
                m[0], m[1], m[2], m[3],
                m[4], m[5], m[6], m[7],
                m[8], m[9], m[10], m[11],
                m[12], m[13], m[14], m[15]
        );
    }

    public static Quaternion ReadQuaternion(BinaryReader reader)
    {
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        float w = reader.ReadSingle();

        return new Quaternion(x, y, z, w);
    }

    public static Vector4 ReadVector4(BinaryReader reader)
    {
        float x = reader.ReadSingle(); 
        float y = reader.ReadSingle(); 
        float z = reader.ReadSingle(); 
        float w = reader.ReadSingle();

        return new Vector4(x, y, z, w);
    }

    /// <summary>
    /// Currently returns a Vector3 
    /// (skipping the W element as per the original MaxScript importer)
    /// </summary>
    public static Vector3 ReadVector4Half(BinaryReader reader)
    {
        float x = (float)reader.ReadHalf();
        float y = (float)reader.ReadHalf();
        float z = (float)reader.ReadHalf();
        reader.ReadHalf();

        return new Vector3(x, y, z);
    }

    public static Vector3 ReadVector3(BinaryReader reader)
    {
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();

        return new Vector3(x, y, z);
    }

    public static Vector2 ReadVector2(BinaryReader reader)
    {
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();

        return new Vector2(x, y);
    }

    /// <summary>
    /// Currently returns a Vector2 
    /// (skipping the Z element as per the original MaxScript importer)
    /// </summary>
    public static Vector2 ReadVector3Half(BinaryReader reader)
    {
        float x = (float)reader.ReadHalf();
        float y = (float)reader.ReadHalf();
        reader.ReadHalf();

        return new Vector2(x, y);
    }

    public static void WriteVector4(BinaryWriter w, Vector4 v)
    {
        w.Write(v.X);
        w.Write(v.Y);
        w.Write(v.Z);
        w.Write(v.W);
    }

    public static void WriteVector3(BinaryWriter w, Vector3 v)
    {
        w.Write(v.X);
        w.Write(v.Y);
        w.Write(v.Z);
    }

    public static void WriteVector2(BinaryWriter w, Vector2 v)
    {
        w.Write(v.X);
        w.Write(v.Y);
    }

    public static void WriteMatrix4x4(BinaryWriter w, Matrix4x4 m)
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                w.Write(m[i,j]);
            }
        }
    }

    public static void WriteMatrix3x4(BinaryWriter w, Matrix3x4 m)
    {
        WriteVector4(w, m.m0);
        WriteVector4(w, m.m1);
        WriteVector4(w, m.m2);
    }
}
