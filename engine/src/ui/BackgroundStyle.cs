//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

namespace NoZ;

public struct BackgroundStyle()
{
    public Color Color = Color.Transparent;
    public Color GradientColor = Color.Transparent;
    public float GradientAngle = 0;
    public Sprite? Image = null;
    public Color ImageColor = Color.White;

    public readonly bool HasGradient => !GradientColor.IsTransparent;
    public readonly bool HasImage => Image != null;
    public readonly bool IsTransparent => Color.IsTransparent && !HasGradient && !HasImage;
    public static implicit operator BackgroundStyle(Color color) => new() { Color = color };
}
