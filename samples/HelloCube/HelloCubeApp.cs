//  YesZ - HelloCube Application
//
//  Phase 0: Opens window, renders 2D background + UI text.
//  Phase 2: Will add spinning 3D cube with Graphics3D.
//
//  Depends on: NoZ (IApplication, Graphics, UI, Color)
//  Used by:    Program.cs

using NoZ;

namespace YesZ.Samples.HelloCube;

public class HelloCubeApp : IApplication
{
    private static readonly ContainerStyle RootStyle = new()
    {
        Size = new Size2(Size.Percent(1), Size.Percent(1)),
        AlignX = Align.Center,
        AlignY = Align.Center,
    };

    private static readonly ContainerStyle BoxStyle = new()
    {
        Size = Size2.Fit,
        Color = Color.FromRgb(0x16A34A),  // green — "Yes Z"
        Padding = EdgeInsets.Symmetric(24, 48),
        Border = new BorderStyle { Radius = 8 },
    };

    private static readonly LabelStyle TitleStyle = new()
    {
        FontSize = 32,
        Color = Color.White,
        AlignX = Align.Center,
    };

    private static readonly LabelStyle SubtitleStyle = new()
    {
        FontSize = 16,
        Color = Color.FromRgba(0xFFFFFF, 0.7f),
        AlignX = Align.Center,
    };

    public void LoadAssets()
    {
    }

    public void Update()
    {
        Graphics.ClearColor = Color.FromRgb(0x0F172A);

        // Phase 2: Graphics3D.Begin(camera) + draw cube + Graphics3D.End()
    }

    public void UpdateUI()
    {
        using (UI.BeginContainer(RootStyle))
        {
            using (UI.BeginContainer(BoxStyle))
            {
                UI.Label("YesZ", TitleStyle);
                UI.Label("3D is coming", SubtitleStyle);
            }
        }
    }
}
