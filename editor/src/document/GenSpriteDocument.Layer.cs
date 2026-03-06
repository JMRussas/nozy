//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

namespace NoZ.Editor;

public class GenSpriteLayer : IDisposable
{
    public string Name = "";
    public readonly Shape Shape = new();
    public GenerationConfig Generation = new();
    public int Index;

    public void Dispose()
    {
        Shape.Dispose();
    }

    public GenSpriteLayer Clone()
    {
        var clone = new GenSpriteLayer
        {
            Name = Name,
            Generation = Generation.Clone(),
            Index = Index,
        };
        clone.Shape.CopyFrom(Shape);
        return clone;
    }
}
