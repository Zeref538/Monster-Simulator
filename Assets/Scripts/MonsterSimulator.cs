using System;

public sealed class MonsterState
{
    public int HappinessHundredths { get; private set; }
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
    public MonsterState State { get; private set; }
    float clock, actionUntil;
    int actionMood;
    UnityEngine.Vector3 basePosition;
    UnityEngine.Camera view;
    int viewWidth, viewHeight;

    void Awake()
    {
        State = new MonsterState(startingHappinessHundredths, startingStamina);
        basePosition = pet.transform.localPosition;
        view = UnityEngine.Camera.main;
        FitPortrait();
        speechBubble.SetActive(true);
        speech.text = "Hi! Want to play?";
        Refresh();
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
        int index = Array.IndexOf(centeredSprites, sprite);
        float offset = index >= 0 && index < bodyCenters.Length
            ? (0.5f - bodyCenters[index]) * sprite.rect.width / sprite.pixelsPerUnit * pet.transform.localScale.x : 0;
        pet.transform.localPosition = basePosition + new UnityEngine.Vector3(offset, 0, 0);
    }
    void Refresh()
    {
        happinessBar.fillAmount = State.Happiness / 100f;
        staminaBar.fillAmount = State.Stamina / 100f;
        happinessText.text = "Happiness\n" + ((int)State.Happiness).ToString() + " / 100";
        staminaText.text = "Stamina\n" + State.Stamina + " / 100";
        playButton.interactable = studyButton.interactable = State.CanWork;
        sleepButton.interactable = State.CanSleep;
        int mood = State.Mood != 0 ? State.Mood : UnityEngine.Time.unscaledTime < actionUntil ? actionMood : 0;
        if (pet.GetInteger("Mood") != mood) pet.SetInteger("Mood", mood);
    }
    public void Act(int action)
    {
        if (!State.Act(action)) return;
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
        clock = 0; actionUntil = 0;
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
