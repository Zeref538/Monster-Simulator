# Verification

The shared C# MonsterState passed 14 assertions under Unity's bundled compiler and Mono runtime. The .NET 8 runner passed the same 14 assertions. Checks cover exact passive decay, relative Play/Study changes, 100 and 50 mood boundaries, stamina validation at 20 and below, blocked actions, Sleep, Eat, clamping and invalid actions.

The scene has four MonsterSimulator.Act calls and no old fixed-value bar setters. The three earlier scripts match PocketPet-Care byte for byte. No editor helper scripts are included in Assets.

Unity 6000.5.10f1 compiled Assembly-CSharp successfully. At 405 x 720 in Play mode, all four actions were clicked successfully. Happiness decayed, Play used 10 stamina, eight Study clicks took stamina from 90 to 10 and disabled Play/Study, and Sleep restored 100 and disabled itself. Eat increased happiness, and each action changed the sprite and speech. The console showed zero errors. Happy/Sad boundary logic passed the shared tests; their full animation cycles were not separately captured. Mac import and builds are untested. No executable is being produced; the deliverable is full Unity source.

October 10 phone patch: the full updated runtime script compiled against Unity 6000.5.10f1 Core, Animation, Audio and UI assemblies. All 14 state checks passed again. Serialized scene inspection confirms a camera-bound canvas, two action rows, 40-point live status text and Preserve Aspect on the buttons. Desktop control was unavailable, so the updated layout has not had a live visual check; the Play-mode evidence above describes the previous layout.

The follow-up composition patch enlarges action art, raises the body and includes initial meter numbers in the scene. Happiness text truncates to an integer; MonsterState and threshold checks keep hundredths. All 14 logic checks were rerun. Live visual inspection remains unavailable.

Top-meter/refusal patch: 14 shared state checks passed again, including rejected actions preserving state. The runtime compiled against Unity assemblies. Buttons remain enabled and rejected actions shake briefly without restoring or spending stats. Both FoodBowl animation curves now keep it inactive, matching the bone in the eating frames. The new Play image is transparent 256 x 256, normalized to the Study artwork height. Live UI inspection remains unavailable.

Cute-refusal patch: runtime compilation and all 14 state checks passed. Refusal uses uniform scale, two little hops and alternating tilts over 1.8 seconds, with the existing expressive crying pose when no threshold mood overrides it. Play scales uniformly by 1.2 and compensates for its 12-pixel bottom padding to preserve the ground. In frame 1, visible playing height is 111 pixels versus idle 137; scaling makes it 133.2 equivalent pixels. Live animation inspection remains unavailable.

Reload fix: the Editor log shows assembly reload followed by repeated Update line 70 exceptions. MonsterState and its stored values now use Unity serialization, and OnEnable initializes a missing state before Update. The runtime compiles and 14 state checks pass. A live script-reload regression check remains unverified because desktop control is unavailable.

Grounded-refusal patch: removed the hop and scale pulse, replaced crouched Play sprite keys with the upright happy frames, and kept base scale unchanged. Ground-pivot compensation was checked at 181 angles from -8 to 8 degrees: the foot height stays constant and the position correction never moves upward. Runtime compilation and 14 logic assertions pass. Live visual animation remains unverified.

Tail-chasing patch: 12 transparent 192 x 192 frames cut from the new 4 x 3 sheet, each grounded at pixel 180. The playing clip references all 12 new sprites and returns to the first at the loop boundary. Added matching body-center entries. Refusal no longer selects the crying pose. Runtime compilation and 14 rules passed; live visual inspection is still unavailable.

Begging patch: six new begging frames and twelve corrected Play frames use transparent 192 x 192 canvases and a common ground at pixel 180. Six begging references and matched body-centering entries are wired. No transform hop or scale pulse runs during refusal. Runtime compilation and 14 rules pass. Live appearance remains unverified.

## Eating and reading details

- Runtime C# compiled against Unity 6000.5.10f1, including ParticleSystemModule.
- All 14 state-rule assertions pass. No state-rule changes in this update.
- Measured 12 study sprites: each 192 x 192 with nontransparent bottom at pixel 180. One common resize scale preserves relative frame proportions. Existing sprite GUIDs retained.
- Inspected generated sheet and extracted frame 08 for reading expressions. Updated all 12 body center anchors.
- Food effect emits seven small golden crumbs per second only while the Eat mood is active, with downward velocity; it stops emitting when the action ends or refusal begins.
- New effects have not been observed in Unity Play mode: native app-control connection is unavailable.

## Recording scenes and sleep alignment

- Five new scene files preserve the SampleScene layout and references; only starting happiness and stamina differ. Each has a unique scene GUID and a build-settings entry.
- Blue bed retains its existing sprite GUID, canvas size and aspect ratio. Twelve sleep body anchors exclude the curled tail; runtime offsets the bed independently to the portrait center.
- Scene starting values: 75/60, 100/100, 50.5/100, 75/20, 75/10. Rule checks still pass; happy lasts until the next normal one-second decay tick.
- Runtime compilation and static scene reference/value checks passed. Unity Play mode visual review remains unverified because native app control is unavailable.

## Smooth status display

- Bars and integer labels use separate displayed values that move toward the exact state, with a 1.2-second duration for each new target. Starting scene values and recording resets appear immediately.
- The complete MonsterState source is byte-equivalent after newline normalization to the previous commit. All 14 rule assertions pass and runtime code compiles against Unity 6000.5.10f1.
- State validation and mood continue using actual values, regardless of meter animation. Repeated actions retarget from the currently displayed values.
- Live meter motion remains unverified because native app control is unavailable.

## Sad pivot correction and action sound effects

- Diagnosis: six original Sad textures use alignment 7 (bottom-center), unlike the centered smooth frames. Their opaque feet sit 3 pixels above the origin; normal frames sit 84 pixels below it. Before/after asset checks show all six fail the normal baseline before and match it after custom-pivot correction. Artwork is unchanged, and body anchors are measured from the upper body rather than the tail.
- All six scenes raise the bubble from y=905 to 945 without stretching it. Sad and Happy state entry now updates speech to fit the mood.
- Four sourced CC0 effects replace existing action WAV contents while retaining GUIDs. Measured durations: Play 1.15 seconds, Study 2.01, Eat 1.83, Sleep 1.26. All are mono 44100 Hz PCM16, non-silent, with peaks below 0.66. All six scenes retain references to these clips. See AUDIO_SOURCES.md for licenses and source files.
- Runtime compilation and all 14 rule assertions pass. Live Sad positioning and in-game audio listening remain unverified because native app control is unavailable.

## Input event flood

- User screenshot reports 5,242,952 bytes processed from DualShock4GamepadHID in one update, exceeding the default 5,242,880-byte limit. The underlying device/driver cause is not established.
- Read the installed Input System 1.20.0 source: InputSettings.supportedDevices filters discovered device layouts; InputManager removes already discovered unsupported devices when settings change. Editor user settings can override this filter through Add Devices Not Supported by Project (false by default).
- Added MonsterInputSettings with Keyboard, Mouse, Touchscreen and Pen and linked it through the package's com.unity.input.settings build-settings key. Existing input actions and the default event budget are retained.
- Asset GUID, script GUID, layout list and build-settings link checked against package source. Game logic unchanged. Resolution of the actual device flood needs a Unity restart and live observation.

## Recording tick counter

- Counter increments in the same while loop that calls MonsterState.Tick. Time is derived from completed one-second ticks. Last loss is measured from before/after HappinessHundredths, so a clamped tick at zero reports 0.00 rather than falsely claiming a loss.
- Counter is created in the existing runtime component for all six scenes, using the existing numeric-label font and styling. Main meter numbers remain whole numbers. Fresh runs and recording preset resets zero the counter.
- Runtime compiled against installed Unity assemblies, including TextRenderingModule for TextAnchor. All 14 rule assertions pass. Live label position and count progression remain unverified.

## Floating stat changes only

- Removed the time/tick display and last-tick line. Existing runtime counter objects are removed when the component enables, including after a script reload.
- Passive ticks show measured happiness change in a separate label from action changes. Accepted actions measure both state values before/after validation, including capped gains. Rejected actions return before displaying stat-change feedback.
- Three reused text labels appear for 0.65 seconds, fade in the final 0.2 seconds and clear on demo reset. Main status meters retain whole numbers. No new scripts or assets are required.
- Runtime compilation and 14 rule assertions pass. Live popup positions and fading remain unverified.

## Android APK submission, October 10, 2026

- Removed recording counters and floating changes. The four actions and smooth whole-number meters remain. Only SampleScene is enabled in the build profile.
- Unity 6000.5.10f1 completed the IL2CPP ARM64 release APK build. Output size: 41,252,025 bytes.
- Android aapt reports com.zeref538.monstersimulator, minimum SDK 26, target SDK 36, portrait orientation and arm64-v8a.
- Android apksigner verifies the APK Signature Scheme v2 signature with one signer. ZIP CRC verification passed for all 398 entries. AndroidManifest.xml, classes.dex, libunity.so and libil2cpp.so are present.
- SHA256: DC9730414C7BC277C19ACFE528CDA44250767E517E16AD70AAC59AF3347874EB.
- No Android device or emulator was connected. Installation, touch input, audio and rendering on a phone remain unverified; John will test the APK on his phone.

## Upright portrait and launcher icon update

- The previous APK manifest specified Android reversePortrait (9). Unity's serialized defaultScreenOrientation was 1, which is UIOrientation.PortraitUpsideDown, not upright portrait. Changed it to 0 and disabled upside-down and landscape autorotation options. No gameplay scripts changed.
- The rebuilt APK manifest specifies portrait (1), version code 2, the same application identifier and signing key. It can update the earlier APK.
- Added the puppy launcher icon at all Android legacy and round icon sizes. Visually checked the generated 192-pixel launcher resource. Original artwork is also stored in PocketPet-sprites/app/puppy-icon.png.
- APK size: 41,466,026 bytes. Signature v2 verification passed; all 405 archive entries passed CRC checks. SHA256: 6639B136C25848FCE9B9E7B8CF1A3EDDF7E10C5C090CF2AC4E4E117C28C6B63C.
- Actual phone orientation and launcher appearance still need John's phone check.

## Full-screen phone app update

- Removed the fixed 9:16 camera viewport. Camera rect is now the full screen; world width stays 5.625 units, with additional vertical room on tall phones. Background scales uniformly to cover the view, and UI uses a width-based canvas and Screen.safeArea. Whole-number labels are reparented to the colored meters.
- Calculated geometry for four portrait sizes with explicit top/bottom safe-area insets: 1080x1920 (72/48), 1080x2408 (96/48), 1080x2340 (96/48), 720x1600 (64/32). Meter and button widths fit the safe area. Bubble-to-meter clearance is positive in all four cases. These calculations are not phone screenshots.
- Added main-scene local saves, separate music/effect preferences, a pause menu, reset confirmation, Android Back menu handling and Save & close. The first resumed frame does not advance the tick clock. Demo scenes and editor runs do not load saved stats.
- Tap detection rejects UI hits before checking puppy bounds. A tap triggers a short grounded wiggle and happy animation for neutral mood, without calling MonsterState.Act or changing stats. Happy/sad mood thresholds retain priority.
- The slow meter could miss the maximum before the next decay tick. Exact 100 now snaps the happiness display to 100 immediately. The next tick still subtracts exactly one hundredth.
- Unity 6000.5.10f1 completed the ARM64 release build. All 14 C# rule assertions pass. APK version code 3, version name 1.1, upright portrait. Signature v2 verifies and matches the preceding APK's signing certificate, enabling an in-place update.
- All 405 archive entries passed CRC checks; native Unity and IL2CPP libraries are present. Size: 41,479,934 bytes. SHA256: 34DF7CA90E7B1651712BE1F5E078AFADD5D996B3AB50387E7E0B635B0CEC69C2.
- No Android device is connected. Actual fullscreen appearance, touch reactions, settings and persistence still need a phone runtime check.
