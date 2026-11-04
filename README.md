# Kinect Solar System

An interactive 3D solar system for a live class presentation. You stand 2–3 m in front of an
Xbox 360 Kinect and explore the planets with your hand. A mouse and keyboard do everything too,
so the presentation still works if the Kinect fails. All on-screen text is in Bulgarian.

## What you need

- Windows with **.NET Framework 4.8**
- **Kinect for Windows SDK 1.8** and **Kinect for Windows Developer Toolkit 1.8**
- An Xbox 360 Kinect with its USB and power adapter (optional, the mouse also works)
- To build it: **Visual Studio** with the ".NET desktop development" workload and the
  **.NET Framework 4.8 targeting pack** (Visual Studio Installer → Modify → Individual components)

## How to run

1. Open `Solar-System.sln` in Visual Studio.
2. Check that the top bar says **x64**. Never use "Any CPU".
3. Press **F5**.

If the app is already open, close it before building again.

## Kinect controls

Hold your open palm toward the camera.

| Gesture | What it does |
|---|---|
| Raise your hand | The hand cursor appears |
| Move your hand over a planet | Highlights it, shows its name and slows the orbits |
| **Fist on a planet** | Flies to the planet and shows its information |
| Hold your hand still on a planet for 2 seconds | Same as the fist (a ring shows the progress) |
| Push your hand forward on a planet | Same as the fist |
| Fist on empty space and move your hand | Turns the camera around the Sun |
| Fist on empty space and push forward / pull back | Zooms the whole solar system in / out |
| Fist and move your hand while zoomed in | Spins the planet |
| Hold your hand still on "Назад" or close a fist on it | Goes back to the overview |

## Mouse and keyboard

| Input | What it does |
|---|---|
| Move the mouse | Highlights a planet |
| Left click | Selects a planet |
| Left drag | Turns the camera, or spins the planet when zoomed in |
| Mouse wheel | Zooms in and out |
| `Esc` / `Backspace` | Back to the overview |
| `Space` | Pause or resume |
| `+` / `-` | Faster or slower simulation |
| `R` | Reset the camera |
| `F11` | Fullscreen on or off |
| `K` | Kinect on or off |
| `Alt+F4` | Exit |

## Changing things

- **Sizes, speeds, camera and Kinect sensitivity:** `Solar-System/Config/SimulationConfig.cs`
- **Planet text:** `Solar-System/Assets/planets.json`. Edit the copy next to the `.exe` to
  change it without rebuilding, then restart the app.
- **Problems:** `SolarSystem.log` next to the `.exe` lists missing files and Kinect errors.

Sizes and distances are not realistic, or the planets would be invisible. Orbit speeds keep
their real ratios (one Earth year = 20 seconds). Planet spins use a separate, slower scale, and
the Moon's orbit is slowed down so it doesn't look frantic.

## Credits

- Planet textures: Solar System Scope (CC BY 4.0), https://www.solarsystemscope.com/textures/
- Kinect for Windows SDK 1.8 and Developer Toolkit 1.8 by Microsoft
- HelixToolkit.Wpf for the 3D helpers
- Moon counts change as new moons are confirmed. They were checked in November 2026.
