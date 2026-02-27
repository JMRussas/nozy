//  YesZ - Graphics3D Tests
//
//  Verifies the 3D rendering interface stubs exist.
//  Phase 2 will add real tests when Graphics3D has implementation.
//
//  Depends on: YesZ.Rendering (Graphics3D), YesZ.Core (Camera3D)
//  Used by:    CI

using Xunit;

namespace YesZ.Rendering.Tests;

public class Graphics3DTests
{
    [Fact]
    public void BeginEnd_DoesNotThrow()
    {
        var camera = new Camera3D();
        Graphics3D.Begin(camera);
        Graphics3D.End();
    }
}
