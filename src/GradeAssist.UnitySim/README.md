# GradeAssist.UnitySim

This is a Unity simulator scaffold, not a Construction Simulator mod.

## Create project

1. Open Unity Hub.
2. Create a 3D project named `GradeAssist.UnitySim` at this folder, or copy the `Assets` folder into a fresh Unity project.
3. Add these scripts to GameObjects:
   - `MockExcavatorRig`
   - `GradeMonitorSimulator`
   - `RenderTextureMonitorBinder`

## Scene idea

```text
Scene
  MockExcavator
    Boom
      Stick
        Bucket
          CuttingEdgeReference
  CabMonitor
    ScreenMesh
  UIRoot
    MonitorCanvas
```

No game runtime or protected install is touched.
