using System;

[Serializable]
public sealed class MonsterState
{
#if UNITY_5_3_OR_NEWER
    [field: UnityEngine.SerializeField]
#endif
    public int HappinessHundredths { get; private set; }
#if UNITY_5_3_OR_NEWER
    [field: UnityEngine.SerializeField]
#endif
    public int Stamina { get; private set; }
    public float Happiness => HappinessHundredths / 100f;
    public bool CanWork => Stamina >= 20;
    public bool CanSleep => Stamina < 20;
    public int Mood => HappinessHundredths == 10000 ? 1 : HappinessHundredths <= 5000 ? 2 : 0;
    public MonsterState(int happinessHundredths = 7500, int stamina = 100)
    {
        HappinessHundredths = Math.Max(0, Math.Min(10000, happinessHundredths));
        Stamina = Math.Max(0, Math.Min(100, stamina));
    }
    public void Tick() => HappinessHundredths = Math.Max(0, HappinessHundredths - 1);
    public bool Act(int action)
    {
        if (action < 0 || action > 3) return false;
        if ((action == 0 || action == 1) && !CanWork) return false;
        if (action == 3 && !CanSleep) return false;
        if (action == 0) { HappinessHundredths += 50; Stamina -= 10; }
        if (action == 1) { HappinessHundredths -= 50; Stamina -= 10; }
        if (action == 2) { HappinessHundredths += 500; Stamina += 20; }
        if (action == 3) Stamina = 100;
        HappinessHundredths = Math.Max(0, Math.Min(10000, HappinessHundredths));
        Stamina = Math.Max(0, Math.Min(100, Stamina));
        return true;
    }
}

#if UNITY_5_3_OR_NEWER
public class MonsterSimulator : UnityEngine.MonoBehaviour
{
    public UnityEngine.Animator pet;
    public UnityEngine.SpriteRenderer petSprite;
    public UnityEngine.UI.Image happinessBar, staminaBar;
    public UnityEngine.UI.Text happinessText, staminaText, speech;
    public UnityEngine.UI.Button playButton, studyButton, sleepButton;
    public UnityEngine.GameObject speechBubble;
    [UnityEngine.HideInInspector] public UnityEngine.AudioSource[] actionMusic;
    [UnityEngine.HideInInspector] public UnityEngine.Sprite[] centeredSprites;
    [UnityEngine.HideInInspector] public float[] bodyCenters;
    public int startingHappinessHundredths = 7500;
    public int startingStamina = 100;
    [field: UnityEngine.SerializeField]
    public MonsterState State { get; private set; }
    float clock, actionUntil, refusalUntil;
    int actionMood;
    UnityEngine.Vector3 basePosition, baseScale;
    UnityEngine.Quaternion baseRotation;
    UnityEngine.Camera view;
    int viewWidth, viewHeight;

    void Awake()
    {
        State = new MonsterState(startingHappinessHundredths, startingStamina);
        basePosition = pet.transform.localPosition;
        baseScale = pet.transform.localScale;
        baseRotation = pet.transform.localRotation;
        view = UnityEngine.Camera.main;
        FitPortrait();
        speechBubble.SetActive(true);
        speech.text = "Hi! Want to play?";
        Refresh();
    }
    void OnEnable()
    {
        // Script reloads do not call Awake again on an existing component.
        if (State == null) Awake();
    }
    void Update()
    {
        FitPortrait();
        clock += UnityEngine.Time.unscaledDeltaTime;
        while (clock >= 1f) { State.Tick(); clock -= 1f; }
        Refresh();
    }
    void FitPortrait()
    {
        int width = UnityEngine.Screen.width, height = UnityEngine.Screen.height;
        if (view == null || height == 0 || (width == viewWidth && height == viewHeight)) return;
        viewWidth = width; viewHeight = height;
        float ratio = (float)width / height;
        const float portrait = 9f / 16f;
        float w = ratio > portrait ? portrait / ratio : 1f;
        float h = ratio < portrait ? ratio / portrait : 1f;
        view.rect = new UnityEngine.Rect((1f - w) / 2f, (1f - h) / 2f, w, h);
    }
    void LateUpdate()
    {
        var sprite = petSprite.sprite;
        bool refusing = UnityEngine.Time.unscaledTime < refusalUntil;
        float size = pet.GetInteger("Mood") == 5 ? 1.2f : 1f;
        float lift = 0, tilt = 0;
        if (refusing)
        {
            float elapsed = 1.8f - (refusalUntil - UnityEngine.Time.unscaledTime);
            float progress = UnityEngine.Mathf.Clamp01(elapsed / 1.8f);
            float bounce = UnityEngine.Mathf.Abs(UnityEngine.Mathf.Sin(progress * UnityEngine.Mathf.PI * 2f));
            lift = bounce * 0.17f * (1f - progress);
            tilt = UnityEngine.Mathf.Sin(progress * UnityEngine.Mathf.PI * 4f) * 10f * (1f - progress);
            size *= 1f + bounce * 0.035f;
        }
        pet.transform.localScale = baseScale * size;
        pet.transform.localRotation = baseRotation * UnityEngine.Quaternion.Euler(0, 0, tilt);
        int index = Array.IndexOf(centeredSprites, sprite);
        float offset = index >= 0 && index < bodyCenters.Length
            ? (0.5f - bodyCenters[index]) * sprite.rect.width / sprite.pixelsPerUnit * pet.transform.localScale.x : 0;
        // Keep the playing pup's feet on the same ground when increasing its size.
        if (pet.GetInteger("Mood") == 5)
            lift += (sprite.pivot.y - 12f) / sprite.pixelsPerUnit * baseScale.y * (size - 1f);
        pet.transform.localPosition = basePosition + new UnityEngine.Vector3(offset, lift, 0);
    }

    void Refresh()
    {
        happinessBar.fillAmount = State.Happiness / 100f;
        staminaBar.fillAmount = State.Stamina / 100f;
        happinessText.text = ((int)State.Happiness).ToString() + " / 100";
        staminaText.text = State.Stamina + " / 100";
        playButton.interactable = studyButton.interactable = sleepButton.interactable = true;
        int mood = State.Mood != 0 ? State.Mood : UnityEngine.Time.unscaledTime < refusalUntil ? 3 : UnityEngine.Time.unscaledTime < actionUntil ? actionMood : 0;
        if (pet.GetInteger("Mood") != mood) pet.SetInteger("Mood", mood);
    }
    public void Act(int action)
    {
        if (!State.Act(action))
        {
            if (action == 0 || action == 1 || action == 3)
            {
                refusalUntil = UnityEngine.Time.unscaledTime + 1.8f;
                actionUntil = 0;
                speech.text = action == 3 ? "But... one more game?" : "Too tired! I need to sleep.";
                foreach (var source in actionMusic) source.Stop();
                Refresh();
            }
            return;
        }
        refusalUntil = 0;
        actionMood = new[] { 5, 6, 4, 7 }[action];
        actionUntil = UnityEngine.Time.unscaledTime + 3f;
        speech.text = new[] { "Let's play!", "Time to learn.", "Yum! Thank you.", "Rested and ready!" }[action];
        foreach (var source in actionMusic) source.Stop();
        if (action < actionMusic.Length) actionMusic[action].Play();
        Refresh();
    }
    public void ResetDemo(int preset)
    {
        int[] happiness = { 7500, 9950, 5050, 7500 };
        if (preset < 0 || preset >= happiness.Length) return;
        State = new MonsterState(happiness[preset], preset == 3 ? 20 : 100);
        clock = 0; actionUntil = 0; refusalUntil = 0;
        foreach (var source in actionMusic) source.Stop();
        speech.text = new[] { "Hi! Want to play?", "One more game?", "Keep me company?", "A little play?" }[preset];
        Refresh();
    }
    [UnityEngine.ContextMenu("Recording presets/New pet")] void NewPet() => ResetDemo(0);
    [UnityEngine.ContextMenu("Recording presets/Almost happy")] void AlmostHappy() => ResetDemo(1);
    [UnityEngine.ContextMenu("Recording presets/A quiet mood")] void QuietMood() => ResetDemo(2);
    [UnityEngine.ContextMenu("Recording presets/Nearly tired")] void NearlyTired() => ResetDemo(3);
}
#endif
