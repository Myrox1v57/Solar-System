# Presentation Checklist

## A few days before: set up the presentation PC

Skip this part if you present from your own laptop.

- [ ] In Visual Studio, set the top bar to **Release** and **x64**, then build.
- [ ] Copy the **whole** folder `Solar-System\bin\x64\Release` to the presentation PC, not just the `.exe`.
- [ ] Install the full **Kinect for Windows SDK 1.8**. The smaller "Runtime" is not enough:
      an Xbox 360 Kinect needs the full SDK.
- [ ] Install **.NET Framework 4.8** if it's missing. Windows 10 and 11 normally have it.
- [ ] Install the **Visual C++ 2010 Redistributable (x64)** if the hand tracking doesn't start.
      The hand tracking needs it, and the SDK may already have installed it.
- [ ] Plug in the Kinect, run `Solar-System.exe`, and do the full test below.

Visual Studio and the Developer Toolkit are **not** needed on the presentation PC.

## The day before: try it in the real room

- [ ] **Space:** 2–3 m of free floor in front of the Kinect.
- [ ] **Kinect position:** at waist to chest height, on something that won't shake, and pointing at you.
- [ ] **Light:** no direct sunlight on you or on the Kinect. Close the blinds if needed. Sunlight
      blocks the Kinect's infrared light.
- [ ] **Background:** nobody sitting close behind you, and no mirrors or shiny surfaces behind you.
- [ ] **Screen:** set the projector to **Duplicate** (`Win+P`), so the app opens on the big screen.
- [ ] **Text:** read an info panel from the back of the room.
- [ ] **Practice:** go through the demo below at least twice, standing where you will stand.

## 15 minutes before you start

- [ ] Plug the laptop into power.
- [ ] Turn off sleep mode, the screen saver and notifications (Focus / Do not disturb).
- [ ] Close other programs, especially anything that uses the Kinect.
- [ ] Plug in the Kinect's **USB** and **power adapter** before starting the app.
- [ ] Run `Solar-System.exe` directly, not from Visual Studio. It opens in fullscreen.
- [ ] Raise your hand. The corner should say **"Проследяване"**.
- [ ] Have a **mouse** connected and within reach.

## Demo order (about 2 minutes)

1. Show the overview and point out that Mercury is fast and Neptune barely moves.
2. Raise your hand and hover over a planet.
3. Make a **fist on Earth**, read one fact, and spin it with a fist.
4. Hold your hand still on **"← Назад"** for 2 seconds.
5. Make a **fist on empty space**, turn the camera, then push forward to zoom in and pull back to zoom out.
6. Open **Saturn**, then the **Sun**.

## What to tell classmates

There is no start screen, so explain the gestures before they try. You can say this:

> Застани на 2–3 метра и вдигни ръка с отворена длан към камерата.
> Посочи планета и стисни юмрук, за да я разгледаш.
> Стисни юмрук в празното и движи ръката, за да завъртиш изгледа.
> Със стиснат юмрук бутни ръката напред, за да приближиш, и я дръпни назад, за да отдалечиш.
> Ако юмрукът не сработи, задръж ръката неподвижна за 2 секунди.

## If something goes wrong

**Stay calm: the mouse does everything the hand does.** Press `K` and carry on with the mouse.

| Problem | What to do |
|---|---|
| "Няма свързан Kinect" | Check the Kinect's power adapter and USB cable. Try another USB port. The app finds it again by itself |
| "Kinect няма захранване" | The power adapter isn't plugged in. USB alone is not enough |
| "USB портът е зает" | Use another USB port, and unplug other USB devices |
| "Kinect се използва от друга програма" | Close the other program or a second copy of this app, then restart the app. If that doesn't help, check `SolarSystem.log` and install Visual C++ 2010 x64 |
| "Няма Kinect (инсталиран ли е Kinect SDK 1.8?)" | Install the full Kinect SDK 1.8 |
| Kinect is on but doesn't see you | Move back to 2–3 m, step into the light, and face the camera |
| The cursor follows the wrong person | Ask others to step out of view. Lower your hand, then raise it again |
| The fist isn't detected | Show an open palm to the camera first, or hold your hand still for 2 seconds |
| The Kinect keeps failing | Press `K` to turn it off, and use the mouse |
| Planets are plain colors | The `Assets` folder wasn't copied. Copy the whole Release folder again |
| The info panel says "няма данни" | `Assets\planets.json` is missing. Copy the whole Release folder again |
| Anything else | Look in `SolarSystem.log` next to the `.exe`, then restart the app |

## Keys to remember

| Key | What it does |
|---|---|
| `K` | Kinect on or off |
| `Esc` | Back to the overview |
| `R` | Reset the camera |
| `Space` | Pause or resume |
| `+` / `-` | Faster or slower |
| `F11` | Fullscreen on or off |
| `Alt+F4` | Exit |
