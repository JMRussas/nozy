//  YesZ - 3D Transform
//
//  Position + rotation + scale in 3D space.
//  Computes a local-to-world Matrix4x4.
//
//  Depends on: System.Numerics
//  Used by:    YesZ.Rendering, game code

using System.Numerics;

namespace YesZ;

public struct Transform3D
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public static Transform3D Identity => new()
    {
        Position = Vector3.Zero,
        Rotation = Quaternion.Identity,
        Scale = Vector3.One,
    };

    public readonly Matrix4x4 LocalMatrix =>
        Matrix4x4.CreateScale(Scale)
        * Matrix4x4.CreateFromQuaternion(Rotation)
        * Matrix4x4.CreateTranslation(Position);
}
