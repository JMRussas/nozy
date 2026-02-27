//  YesZ - HelloCube Sample
//
//  Minimal YesZ application. Opens a NoZ window with a colored
//  background and 2D UI text. 3D cube rendering will be added
//  in Phase 2 when Graphics3D is functional.
//
//  Depends on: YesZ.Desktop (DesktopBootstrap), NoZ (Graphics, UI)
//  Used by:    Developer testing, demo

using YesZ.Desktop;
using YesZ.Samples.HelloCube;

DesktopBootstrap.Run(new HelloCubeApp(), "YesZ - Hello Cube");
