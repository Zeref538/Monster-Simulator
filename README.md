# Monster Simulator: Unity 2D

A complete Unity 6000.5.10f1 project using PocketPet-Care's portrait layout, backyard, matching action buttons and original artwork. This replaces the earlier browser version.

## Clone

```bash
git clone --depth 1 https://github.com/Zeref538/Monster-Simulator.git
cd Monster-Simulator
```

In Unity Hub, add this folder as a project and open it with 6000.5.10f1. Open `Assets/Scenes/SampleScene.unity`, select a 1080 x 1920 Fixed Resolution Game view (the camera also keeps a portrait viewport in wider windows) and press Play. The folder contains Assets, Packages and ProjectSettings. No executable is needed.

On this Windows PC, you can launch the editor from the project folder:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe' -projectPath "$PWD"
```

To update your copy, close Unity and run `git pull` inside this folder, then reopen the project. Mac editor import and builds have not been tested.

## Rules

Happiness decreases by exactly 0.01 once per second. Play adds 0.5, Study subtracts 0.5, and both spend 10 stamina. Eat adds 5 happiness and 20 stamina. Sleep restores stamina to 100. Food gains and stamina costs were not specified by the assignment, so those values are explicit choices.

Play and Study are blocked below 20 stamina. Sleep is blocked at 20 or above. Happy plays at exactly 100 happiness; Sad plays at 50 or below. Mood states override action animations. Values are clamped from 0 to 100. Integer hundredths avoid rounding errors at the mood boundaries.

The new gameplay source is `Assets/Scripts/MonsterSimulator.cs`, authorized for this program-writing assignment. The earlier supplied scripts are preserved unchanged. Artwork stays separately available in PocketPet-sprites. No helper or editor scripts are included in Assets.

## Recording

Open a demo scene from `Assets/Scenes` before pressing Play. Stop Play before switching scenes. Each scene has the same phone layout and normal rules; only starting values differ.

| Scene | Happiness | Stamina | What to record |
|---|---:|---:|---|
| 01_Decay_And_Eat | 75 | 60 | Wait for 75 to display 74 after the first one-second tick, then Eat: both meters rise. |
| 02_Happy_100 | 100 | 100 | Record before pressing Play to capture the happy pose immediately at 100. Eat can bring it back to 100 after decay. |
| 03_Sad_Threshold | 50.5 internally | 100 | Click Study once: happiness reaches 50 or below and the sad pose starts. Eat brings it above 50. |
| 04_Stamina_Boundary | 75 | 20 | Sleep refuses at 20. Play spends 10 stamina; Play and Study then refuse. Sleep restores 100. |
| 05_Sleep_Recovery | 75 | 10 | Play and Study refuse without changing meters. Sleep restores 100 and shows the centered sleeping pup in his blue bed. |

Happiness still loses exactly 0.01 per second. The whole-number meter hides fractions; the first scene makes a tick visible immediately. Happy is exactly 100 and lasts only until the next decay tick, so start recording before Play. The sad scene displays 50 initially because numbers are truncated, but internally starts at 50.5; Study crosses the actual threshold.

Forbidden actions stay clickable to show refusal, as requested. Their state changes are blocked, although the rubric asks for visibly locked buttons. Name your video `SURNAME_MONSTER.mp4`, replacing SURNAME with your surname, and upload it to Google Drive.

## Tests

With the .NET 8 SDK installed:

```bash
dotnet run --project Tests/Rules.csproj
```

The runner tests the same C# state class used by the Unity game. See docs/VERIFICATION.md for measured checks.


The phone layout has one bottom row of four matching action buttons. Happiness and Stamina show whole numbers out of 100, with larger labels for screen recordings.

The display truncates Happiness to a whole number. Its internal value still decays by exactly 0.01 per second and changes by 0.5 for Play/Study. A visible 100 therefore means exactly 100, matching the happy-state rule.

Whole-number values are centered inside the colored meters. Heart and lightning icons identify the meters without headings.

Buttons remain clickable for feedback. Forbidden Play/Study or Sleep actions show grounded refusal poses with an expressive face and speech, without changing state. This differs from visibly disabled buttons in the demonstration rubric, while retaining the stamina validation rules.

Play uses a twelve-frame tail-chasing loop. Refusal uses grounded begging poses without hopping.

High-stamina Sleep requests show six seated begging poses with the bubble "Not yet... please?". Happy/Sad threshold poses retain priority.

Meter fills and whole-number values count toward their targets over about 1.2 seconds. Actual state changes and validation happen immediately; animations use actual state. Wait for meters to settle between recording actions. Passive decay remains exactly 0.01 per second.

Actions now play short toy squeaks (Play), page turns (Study), eating crunches (Eat) and blanket rustles (Sleep). See docs/AUDIO_SOURCES.md for source links and licenses. Sad frames use corrected custom pivots so their feet align with the other animations.

The project supports mouse, keyboard, touchscreen and pen. Gamepads are excluded because an attached DualShock device flooded the input queue during recording. Restart Unity after updating input settings. The default 5 MB per-update event limit remains enabled.

The APK build omits recording counters and floating stat-change labels. Status meters still count smoothly toward their values.

## Android APK submission

Download [Monster-Simulator-fullscreen.apk](https://github.com/Zeref538/Monster-Simulator/releases/download/v2.2.0/Monster-Simulator-fullscreen.apk). It requires Android 8.0 or later and an ARM64 phone. Install it over the previous APK, check the four actions and sound, then submit that APK. Phone runtime testing of this update is still pending.

The APK is 41,479,934 bytes. Its signature and archive contents passed verification. It contains only the main gameplay scene.

The upright game fills tall portrait screens without a fixed 9:16 frame. Puppy sprites keep their proportions, the yard scales uniformly, and controls respect the phone's safe area. Numbers are inside the meters. Happiness displays 100 immediately at the exact maximum; the next passive tick lowers it to 99.99, shown as 99.

Tap the puppy for a short tickle reaction. It does not change happiness or stamina. The happy/sad state thresholds retain priority.

Use Menu for music and sound-effect toggles, Continue, New puppy and Save & close. New puppy asks for confirmation; Continue cancels that reset. Android Back opens or closes the menu.

The main game saves exact stats locally after accepted actions, every five seconds and on app pause or close. Progress and sound preferences remain on this phone across restarts. Gameplay and sound pause in the menu or background; there is no offline decay. Editor and recording scenes keep their original preset behavior.

To rebuild with Unity CLI and Unity 6000.5.10f1 with Android support installed, run this from the project folder:

```powershell
unity build . --profile Assets/Settings/AndroidAPK.asset --output-path ../builds/Monster-Simulator.apk --android-export-type apk --editor-version 6000.5.10f1 --non-interactive
```
