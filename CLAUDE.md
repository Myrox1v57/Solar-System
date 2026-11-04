# Kinect Solar System — Project Guide for Claude Code

## What this project is
A school project: an interactive **3D solar system simulation** controlled with an
**Xbox 360 Kinect (Kinect v1)**. It will be presented live in front of a class on a
big screen. A presenter (or classmate) stands 2–3 m in front of the Kinect and uses
hand gestures to explore the planets.

**Success = it runs reliably during a live presentation, looks impressive on a big
screen, and a classmate can understand the controls in under 30 seconds.**
Reliability beats features. Never add features not listed here without asking.

## Who you are working with
- A student. Explain what you changed in short, simple English, no jargon dumps.
- **You cannot see the screen or the Kinect.** The student tests everything.
  After every phase (and after any risky change), stop and ask the student to run
  the app (F5 in Visual Studio) and report what happened. Give them a short,
  concrete checklist of what to test.
- Never start the next phase until the student confirms the current one works.

## Tech stack (fixed — do not change)
| Item | Choice |
|---|---|
| Language | C# |
| UI / 3D | **WPF** (`Viewport3D`) |
| Framework | **.NET Framework 4.8** (NOT .NET 6/8 — Kinect SDK 1.8 requires .NET Framework) |
| Platform target | **x64** (never "Any CPU") |
| Kinect | **Kinect for Windows SDK 1.8** (`Microsoft.Kinect.dll`) |
| Kinect gestures | **Kinect Developer Toolkit 1.8**: `Microsoft.Kinect.Toolkit`, `Microsoft.Kinect.Toolkit.Controls`, `Microsoft.Kinect.Toolkit.Interaction` + native `KinectInteraction180_64.dll` |
| 3D helpers | **HelixToolkit.Wpf** NuGet package (a version that supports .NET Framework 4.8) — for spheres, lines, textures. Allowed, recommended. |
| IDE | Visual Studio Community (".NET desktop development" workload) |
| Source control | Git + GitHub |

Notes:
- `Microsoft.Kinect.dll` is referenced from
  `C:\Program Files\Microsoft SDKs\Kinect\v1.8\Assemblies\`.
- The Developer Toolkit installs under `C:\Program Files\Microsoft SDKs\Kinect\Developer Toolkit v1.8.0\`.
  Some toolkit libraries ship as compiled DLLs, others only as source projects inside
  the **"Controls Basics-WPF"** sample (installable from the Developer Toolkit Browser).
  Find what exists on this machine before wiring references; if needed, copy the
  toolkit source projects into the solution. Make sure `KinectInteraction180_64.dll`
  is copied to the output folder (Copy to Output Directory).
- Building from the terminal: use **MSBuild.exe from the Visual Studio install**
  (locate it with `vswhere.exe`), not `dotnet build` — old-style .NET Framework WPF
  projects may not build with the dotnet CLI. Always build after changes and fix all
  errors before asking the student to test.

## Visual design
- **Black space background** with a star/Milky Way texture on a huge inverted sphere (skybox).
- **Sun** in the center, self-lit (emissive material), with a point light at its position
  so planets are lit from the Sun's side and dark on the other side. Add a weak ambient
  light so the dark side isn't 100% black.
- **8 planets**: Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, Neptune.
  Plus **Earth's Moon** orbiting Earth. No Pluto.
- **Saturn's ring**: flat textured ring using the ring texture with transparency (alpha).
  WPF 3D transparency needs care: add transparent objects last in the scene.
- **Thin orbit lines** (faint grey/blue circles) for every planet.
- Each planet **spins on its own axis** and **orbits the Sun**.
- **Scale is NOT realistic** (real scale would make planets invisible):
  - Distances compressed so all orbits fit on screen.
  - Sizes exaggerated but in the correct order (Jupiter biggest planet, Mercury smallest).
    The Sun can be smaller than real scale so it doesn't swallow the inner planets.
  - Orbit speeds keep the **correct relative ratios** (Mercury fastest, Neptune slowest).
    Default: Earth completes an orbit in ~20 seconds. Neptune will barely move — that's
    correct and a good talking point.
- Put all scale/speed numbers in one config class so they're easy to tweak.

### Textures
Free textures from **Solar System Scope** (solarsystemscope.com/textures, CC BY 4.0),
2K versions, stored in `Assets/Textures/` (set as Content, Copy if newer):
`2k_sun.jpg, 2k_mercury.jpg, 2k_venus_surface.jpg, 2k_earth_daymap.jpg, 2k_moon.jpg,
2k_mars.jpg, 2k_jupiter.jpg, 2k_saturn.jpg, 2k_saturn_ring_alpha.png, 2k_uranus.jpg,
2k_neptune.jpg, 2k_stars_milky_way.jpg`
If a file is missing, show the planet in a solid fallback color instead of crashing,
and log which file was missing.
Show the credit "Planet textures: Solar System Scope (CC BY 4.0)" in a small line on screen
and in the README. The license requires it, so don't remove it.

## Planet information
Stored in `Assets/planets.json` (one entry per body, including Sun and Moon) — not
hard-coded — so text can be edited without recompiling.

Fields per body: name, type (e.g. "Rocky planet", "Gas giant", "Ice giant", "Star",
"Moon"), diameter (km), average distance from the Sun (million km), length of day,
length of year, number of moons, average temperature, 2 short fun facts.

- **Language: ALL on-screen text is Bulgarian** — planet info, labels, buttons, Kinect status
  and the start screen. Texts written in English in this document (e.g. "Back",
  "Kinect connected") are descriptions: translate them in the app. The log stays in English.
- Facts must be **accurate** — use well-established values (NASA-level facts) and
  keep numbers rounded and readable (e.g. "≈ 12 742 km"). Flag anything you are unsure of.
- Text must be **big and readable from the back of a classroom**: short lines, large font.

## Two modes (screens)

### 1. Overview mode (start)
- Camera looks at the whole solar system from slightly above (about 25–35° tilt).
- Planets orbit continuously.
- **Hover** over a planet → it gets a highlight (glow / brighter outline) and its
  **name label** appears above it.
- **Select** a planet (see controls) → switch to Focus mode.
- **Drag** (grip on empty space and move hand, or mouse drag) → **orbit the camera** around the Sun
  (left/right = rotate around, up/down = change tilt, tilt clamped between ~5° and ~85°).
  Camera movement must be smoothed (no jitter).

### 2. Focus mode (one planet)
- Camera **flies smoothly** (~1.5 s, ease-in-out animation) to the selected planet,
  which fills roughly the left 60% of the screen.
- Simulation time **slows down strongly** (orbits nearly stop) so the planet stays in frame.
- **Info panel** slides in on the right side with the planet's data from `planets.json`.
- **Drag / swipe** → **spin the planet** left/right (with a bit of momentum, then it
  continues its normal slow rotation).
- A big **"Back" button** (top-left, Kinect-friendly, at least 200×120 px) → camera flies
  back to Overview, info panel slides out, simulation speed returns to normal.
- Selecting the Sun works the same way.

## Controls

### Kinect (main input)
Use the Developer Toolkit's **KinectRegion + interaction stream** (hand pointer):
- Hand pointer appears as the standard Kinect hand cursor.
- **Hover** = hand cursor over a planet. Do 3D hit testing on the `Viewport3D` using the
  hand pointer's position (`HandPointer.GetPosition(viewport)`). Make the hit area
  generous (small planets like Mercury must still be easy to hit — use an invisible
  larger hit sphere around each planet).
- **Select** = **grip** (close fist) with the cursor on a planet — this is the main gesture
  and flies the camera to that planet. **Press** (push hand toward the screen) **or** keeping
  the hand still over the planet for **2 seconds** (dwell) also work — show a circular
  progress ring around the cursor while dwelling. Dwell is the reliable fallback, because
  the Kinect can miss a fist.
- **Drag** = **grip** on empty space, move hand, **release** (open hand) → orbit the camera.
  In Focus mode, a grip anywhere except the Back button spins the planet.
- Only the **primary user's primary hand** controls the app (the raised hand, so the right
  hand works and left-handed classmates can use theirs). Ignore other people walking by.
- **Status indicator** (small, bottom corner): "Kinect connected", "No Kinect",
  "Step in front of the camera", "Tracking". Also show a big friendly prompt when no
  user is tracked: "Stand in front of the camera and raise your hand".

### Mouse + keyboard (backup — REQUIRED)
Exactly the same features with the mouse, so the app can be developed and tested
without a Kinect and can still be presented if the Kinect fails:
- Move = hover, Left click = select, Left drag = orbit camera / spin planet,
  Mouse wheel = zoom in/out in Overview (clamped).
- `Esc` / `Backspace` = Back to Overview.
- `F11` = toggle fullscreen (start in fullscreen in Release builds).
- `Space` = pause/resume orbits. `+` / `-` = simulation speed. `R` = reset camera.
- `K` = toggle Kinect input on/off. `Alt+F4` exits.

Both inputs must go through **one shared interface** (e.g. `IPointerInput` with events
Hover/Move, Select, DragStart, DragDelta, DragEnd) so the rest of the app doesn't know or
care which input is used.

## Start screen
None. The student removed it: the app opens directly on the Overview, and the presenter
explains the gestures.

## Architecture (keep files small and focused)
```
KinectSolarSystem/
  App.xaml / MainWindow.xaml(.cs)   – window, layout, wires everything together
  Config/SimulationConfig.cs         – all scale, speed, camera, timing numbers
  Model/CelestialBody.cs             – data for one body (from planets.json + config)
  Model/PlanetDataLoader.cs          – loads planets.json
  Simulation/SimulationClock.cs      – simulation time, speed, pause, slow-motion
  Scene/SceneBuilder.cs              – creates spheres, materials, ring, orbits, skybox, lights
  Scene/BodyVisual.cs                – one planet's 3D visual (+ highlight, label, hit sphere)
  Camera/CameraController.cs         – overview orbit camera, fly-to animations, smoothing
  Input/IPointerInput.cs             – shared input interface
  Input/MouseInput.cs                – mouse/keyboard implementation
  Input/KinectInput.cs               – Kinect implementation (sensor, KinectRegion, grip/press, dwell)
  Interaction/InteractionController.cs – hover/select/drag state machine, Overview↔Focus modes
  UI/InfoPanel.xaml(.cs)             – info panel
  UI/StatusIndicator.xaml(.cs)       – Kinect status
  Assets/Textures/*.jpg|png, Assets/planets.json
```
- The render/update loop uses `CompositionTarget.Rendering` with real elapsed time
  (frame-rate independent).
- The Kinect must never crash the app: handle sensor not connected, unplugged during
  use, and plugged in later (`KinectSensor.KinectSensors.StatusChanged`). If the Kinect
  is missing, everything still works with the mouse.
- Stop the sensor cleanly when the app closes.

## Build phases
Do these **in order**. At the end of each phase: build with no errors → give the
student a short test checklist → wait for confirmation → **git commit** with a clear
message → next phase.

**Phase 0 — Project check & setup**
Inspect the existing project (WPF App, .NET Framework 4.8, x64, Microsoft.Kinect reference).
Add a proper `.gitignore` if missing, HelixToolkit.Wpf, the Developer Toolkit references
and native DLL, `Assets/` folders, and the folder structure above. App still shows
"Kinect connected!" / "No Kinect found".

**Phase 1 — Static scene (mouse only)**
Skybox, Sun with light, 8 planets + Moon with textures, Saturn's ring, orbit lines,
fixed overview camera. Missing-texture fallback works.

**Phase 2 — Motion**
Orbits and self-rotation with correct relative speeds, SimulationClock with
pause/speed keys, Moon orbits Earth.

**Phase 3 — Mouse interaction & modes**
IPointerInput + MouseInput, hover highlight + name label, overview camera drag + zoom,
select → fly-to Focus mode, info panel from planets.json, spin planet, Back button and
keys, fullscreen toggle.

**Phase 4 — Kinect input**
KinectInput with KinectRegion hand pointer, hover via hit testing, grip on a planet to select
(press + dwell with progress ring as fallbacks), grip on empty space to drag, primary user only,
status indicator, unplug/replug handling.
Add sensitivity/smoothing settings to the config. The student will test standing 2–3 m away.

**Phase 5 — Polish**
Smooth transitions everywhere, hover sound/click sound optional (ask
first), readable text sizes on a projector, performance check (steady frame rate),
README with how to run, controls, and credits.

**Phase 6 — Presentation readiness**
Release build that starts fullscreen, a `PRESENTATION_CHECKLIST.md` (install Kinect SDK
on the presentation PC, test lighting/space, backup mouse controls, what to do if the
Kinect isn't detected).

## Rules
- Stay on .NET Framework 4.8 / x64. Don't "upgrade" anything.
- Don't add features beyond this document without asking.
- Don't commit `bin/`, `obj/`, `.vs/`.
- Prefer simple, readable code with short comments over clever code — the student may
  need to explain it to the teacher.
- If something in this document turns out to be impossible or a bad idea, say so and
  propose an alternative before changing course.
