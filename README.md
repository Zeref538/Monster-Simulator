# Monster Simulator: Unity 2D

A complete Unity 6000.5.10f1 project using PocketPet-Care's portrait layout, backyard, matching action buttons and original artwork. This replaces the earlier browser version.

## Clone

```bash
git clone --depth 1 https://github.com/Zeref538/Monster-Simulator.git
cd Monster-Simulator
```

In Unity Hub, add this folder as a project and open it with 6000.5.10f1. Open `Assets/Scenes/SampleScene.unity`, select a 9:16 Game view and press Play. The folder contains Assets, Packages and ProjectSettings. No executable is needed.

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

John records the video. During Play mode, select Canvas and open the Monster Simulator component's three-dot menu. Under Recording presets, use Almost happy, A quiet mood or Nearly tired. These change initial state only; action validation and tick decay remain active.

Show passive decay, then Eat from Almost happy to reach 100. Happy lasts until the next decay tick. Study from A quiet mood reaches 50 or below. Nearly tired starts at 20: Sleep is locked. Study takes stamina below 20, locking Play/Study and unlocking Sleep. Sleep restores 100 and locks itself again.

Name the recording `MARTINEZ_MONSTER.mp4` and upload it to Google Drive. The presets are labelled recording aids, not automatic gameplay.

## Tests

With the .NET 8 SDK installed:

```bash
dotnet run --project Tests/Rules.csproj
```

The runner tests the same C# state class used by the Unity game. See docs/VERIFICATION.md for measured checks.

