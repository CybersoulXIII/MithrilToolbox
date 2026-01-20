using System.Numerics;

namespace MithrilToolbox.Formats.Model;

public class BoneGroup
{
    public string Name = "";
    public Quaternion Rotation;
    public Vector3 Position, Scale;
    /// <summary>
    /// Leaving it as 0, because it seems that it doesn't effect animations
    /// </summary>
    public int NameOffset;
    public short ParentId, ChildId;
    /// <summary>
    /// Leaving it as 0, because it seems that it doesn't effect animations
    /// </summary>
    public short BrotherId;
    /// <summary>
    /// Seems to be always 0
    /// </summary>
    public short Reserved;
    /// <summary>
    /// Seems to be nearly always 2 (except root bone; it's 3)
    /// </summary>
    public int Unknown2;
}