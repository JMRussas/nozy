//  YesZ - 3D Camera
//
//  Perspective camera with view and projection matrices.
//  Matrices are computed fresh on each property access (no internal caching).
//
//  Depends on: System.Numerics
//  Used by:    YesZ.Rendering, game code

using System.Numerics;

namespace YesZ;

public class Camera3D
{
    public Vector3 Position { get; set; } = new(0, 0, 5);
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public float FieldOfView { get; set; } = 60.0f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 1000.0f;
    public float AspectRatio { get; set; } = 16.0f / 9.0f;

    public Matrix4x4 ViewMatrix
    {
        get
        {
            var forward = Vector3.Transform(-Vector3.UnitZ, Rotation);
            var up = Vector3.Transform(Vector3.UnitY, Rotation);
            return Matrix4x4.CreateLookAt(Position, Position + forward, up);
        }
    }

    public Matrix4x4 ProjectionMatrix =>
        Matrix4x4.CreatePerspectiveFieldOfView(
            FieldOfView * MathF.PI / 180.0f,
            AspectRatio,
            NearPlane,
            FarPlane
        );

    public Matrix4x4 ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;
}
