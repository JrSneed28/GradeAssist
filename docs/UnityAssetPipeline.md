# Unity Asset Pipeline

## Purpose

Create a standalone simulator with a cab monitor, RenderTexture display, and mock excavator bucket reference.

## Required objects

```text
CabMonitor
  Monitor_Body
  Monitor_Bezel
  Monitor_Screen
  Button_F1
  Button_F2
  Button_F3
  Button_F4
  Button_Home
  Button_Back
  Knob_Rotary
```

## RenderTexture notes

The simulator uses `RenderTextureMonitorBinder.cs` to bind a UI camera output to a screen material. It checks common shader properties with `HasProperty`.

## AssetBundle note

AssetBundles are not needed for the external prototype. If used later, build with a Unity version compatible with the target simulator project.
