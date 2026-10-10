# Unity approach

Reuse the existing portrait scene and serialized assets. One runtime script contains the pure state class and MonoBehaviour UI controller. The old fixed-value button events are replaced with validated relative actions.

Unity replaces the browser implementation at John's request. No editor builder script is included.

The main scene stores exact happiness hundredths, stamina and partial tick time in local PlayerPrefs. Save after accepted actions, every five seconds, when opening the menu and when Android pauses or closes the app. Demo scenes and editor runs do not load or overwrite main-game progress. Pause gameplay and audio while the menu is open or the app is away; do not apply offline decay.

Reuse the existing runtime component for safe-area layout, settings and touch reactions. Keep professor scripts unchanged. Show the exact maximum of 100 immediately, then resume whole-number display when the next 0.01 decay tick occurs.
