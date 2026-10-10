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
    [UnityEngine.HideInInspector] public UnityEngine.Sprite[] beggingSprites;
    [UnityEngine.HideInInspector] public float[] bodyCenters;
    public int startingHappinessHundredths = 7500;
    public int startingStamina = 100;
    [field: UnityEngine.SerializeField]
    public MonsterState State { get; private set; }
    [UnityEngine.SerializeField] UnityEngine.ParticleSystem foodCrumbs;
    [UnityEngine.SerializeField] UnityEngine.Transform sleepBed;
    [UnityEngine.SerializeField] float shownHappiness, shownStamina;
    [UnityEngine.SerializeField] float targetHappiness, targetStamina;
    [UnityEngine.SerializeField] float happinessSpeed, staminaSpeed;
    float clock, actionUntil, refusalUntil;
    [UnityEngine.SerializeField] UnityEngine.UI.Text tickText;
    [UnityEngine.SerializeField] int tickCount, lastTickLoss;
    int actionMood;
    [UnityEngine.SerializeField] int lastStateMood = -1;
    UnityEngine.Vector3 basePosition, baseScale;
    UnityEngine.Quaternion baseRotation;
    UnityEngine.Camera view;
    int viewWidth, viewHeight;

    void Awake()
    {
        State = new MonsterState(startingHappinessHundredths, startingStamina);
        tickCount = 0; lastTickLoss = 0;
        basePosition = pet.transform.localPosition;
        baseScale = pet.transform.localScale;
        baseRotation = pet.transform.localRotation;
        view = UnityEngine.Camera.main;
        FitPortrait();
        speechBubble.SetActive(true);
        speech.text = "Hi! Want to play?";
        SnapMeters();
        Refresh();
    }
    void OnEnable()
    {
        // Script reloads do not call Awake again on an existing component.
        if (State == null) Awake();
        CreateFoodCrumbs();
        if (sleepBed == null) sleepBed = pet.transform.Find("SleepBed");
        if (tickText == null)
        {
            tickText = UnityEngine.Object.Instantiate(happinessText, happinessText.transform.parent);
            tickText.name = "Recording tick counter";
            tickText.fontSize = 22;
            tickText.alignment = UnityEngine.TextAnchor.MiddleCenter;
            tickText.raycastTarget = false;
            var rect = tickText.rectTransform;
            rect.anchorMin = rect.anchorMax = UnityEngine.Vector2.zero;
            rect.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new UnityEngine.Vector2(360, 1080);
            rect.sizeDelta = new UnityEngine.Vector2(640, 60);
        }
    }
    void CreateFoodCrumbs()
    {
        if (foodCrumbs != null) return;
        var crumbs = new UnityEngine.GameObject("Food crumbs");
        crumbs.transform.SetParent(pet.transform, false);
        crumbs.transform.localPosition = new UnityEngine.Vector3(-0.15f, -0.35f, 0);
        foodCrumbs = crumbs.AddComponent<UnityEngine.ParticleSystem>();
        foodCrumbs.Stop(true, UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = foodCrumbs.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new UnityEngine.ParticleSystem.MinMaxCurve(0.4f, 0.65f);
        main.startSpeed = 0.12f;
        main.startSize = new UnityEngine.ParticleSystem.MinMaxCurve(0.025f, 0.045f);
        main.startColor = new UnityEngine.Color(1f, 0.72f, 0.3f, 1f);
        main.maxParticles = 16;
        main.simulationSpace = UnityEngine.ParticleSystemSimulationSpace.Local;
        var emission = foodCrumbs.emission;
        emission.rateOverTime = 7f;
        var shape = foodCrumbs.shape;
        shape.shapeType = UnityEngine.ParticleSystemShapeType.Sphere;
        shape.radius = 0.025f;
        var velocity = foodCrumbs.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = UnityEngine.ParticleSystemSimulationSpace.Local;
        velocity.x = new UnityEngine.ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        velocity.y = new UnityEngine.ParticleSystem.MinMaxCurve(-0.5f, -0.3f);
        velocity.z = new UnityEngine.ParticleSystem.MinMaxCurve(0f, 0f);
        var renderer = crumbs.GetComponent<UnityEngine.ParticleSystemRenderer>();
        renderer.sharedMaterial = petSprite.sharedMaterial;
        renderer.sortingLayerID = petSprite.sortingLayerID;
        renderer.sortingOrder = petSprite.sortingOrder + 2;
    }
    void Update()
    {
        FitPortrait();
        clock += UnityEngine.Time.unscaledDeltaTime;
        while (clock >= 1f)
        {
            int before = State.HappinessHundredths;
            State.Tick();
            lastTickLoss = before - State.HappinessHundredths;
            tickCount++;
            clock -= 1f;
        }
        Refresh();
        shownHappiness = UnityEngine.Mathf.MoveTowards(shownHappiness, targetHappiness,
            happinessSpeed * UnityEngine.Time.unscaledDeltaTime);
        shownStamina = UnityEngine.Mathf.MoveTowards(shownStamina, targetStamina,
            staminaSpeed * UnityEngine.Time.unscaledDeltaTime);
        DrawMeters();
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
        bool refusing = UnityEngine.Time.unscaledTime < refusalUntil;
        if (refusing && State.Mood == 0 && beggingSprites != null && beggingSprites.Length > 0)
        {
            float elapsed = 2.1f - (refusalUntil - UnityEngine.Time.unscaledTime);
            int frame = (int)(elapsed * 5f) % beggingSprites.Length;
            petSprite.sprite = beggingSprites[frame];
        }
        bool eating = pet.GetInteger("Mood") == 4 && !refusing;
        if (eating && !foodCrumbs.isPlaying) foodCrumbs.Play();
        if (!eating && foodCrumbs.isPlaying)
            foodCrumbs.Stop(true, UnityEngine.ParticleSystemStopBehavior.StopEmitting);
        var sprite = petSprite.sprite;
        float lift = 0, tilt = 0;
        pet.transform.localScale = baseScale;
        var turn = UnityEngine.Quaternion.Euler(0, 0, tilt);
        pet.transform.localRotation = baseRotation * turn;

        int index = Array.IndexOf(centeredSprites, sprite);
        float offset = index >= 0 && index < bodyCenters.Length
            ? (0.5f - bodyCenters[index]) * sprite.rect.width / sprite.pixelsPerUnit * pet.transform.localScale.x : 0;
        // Rotate around the feet so refusal never hops upward.
        var ground = new UnityEngine.Vector3(0, -(sprite.pivot.y - 12f) / sprite.pixelsPerUnit * baseScale.y, 0);
        var turnedGround = turn * ground;
        offset -= turnedGround.x;
        lift = ground.y - turnedGround.y;
        pet.transform.localPosition = basePosition + new UnityEngine.Vector3(offset, lift, 0);
        // The pet is centered by its body; keep the bed centered in the portrait too.
        if (sleepBed != null)
        {
            var bedPosition = sleepBed.localPosition;
            bedPosition.x = -offset / baseScale.x;
            sleepBed.localPosition = bedPosition;
        }
    }

    void Refresh()
    {
        if (tickText != null)
            tickText.text = "Time " + (tickCount / 60).ToString("00") + ":" + (tickCount % 60).ToString("00")
                + "  |  Tick " + tickCount
                + "\nLast tick: -" + (lastTickLoss / 100f).ToString("0.00") + " happiness";
        // Only the display eases. Validation and mood always use the exact state.
        if (targetHappiness != State.Happiness)
        {
            targetHappiness = State.Happiness;
            happinessSpeed = UnityEngine.Mathf.Abs(targetHappiness - shownHappiness) / 1.2f;
        }
        if (targetStamina != State.Stamina)
        {
            targetStamina = State.Stamina;
            staminaSpeed = UnityEngine.Mathf.Abs(targetStamina - shownStamina) / 1.2f;
        }
        if (lastStateMood != State.Mood)
        {
            lastStateMood = State.Mood;
            if (State.Mood == 2) speech.text = "Stay with me?";
            else if (State.Mood == 1) speech.text = "I'm so happy!";
        }
        playButton.interactable = studyButton.interactable = sleepButton.interactable = true;
        int mood = State.Mood != 0 ? State.Mood : UnityEngine.Time.unscaledTime < actionUntil ? actionMood : 0;
        if (pet.GetInteger("Mood") != mood) pet.SetInteger("Mood", mood);
    }
    void SnapMeters()
    {
        shownHappiness = targetHappiness = State.Happiness;
        shownStamina = targetStamina = State.Stamina;
        DrawMeters();
    }
    void DrawMeters()
    {
        happinessBar.fillAmount = shownHappiness / 100f;
        staminaBar.fillAmount = shownStamina / 100f;
        // Do not display 100 happiness after the exact happy state has ended.
        float happinessNumber = State.Happiness < 100f
            ? UnityEngine.Mathf.Min(shownHappiness, 99.99f) : shownHappiness;
        happinessText.text = ((int)happinessNumber).ToString() + " / 100";
        staminaText.text = ((int)shownStamina).ToString() + " / 100";
    }
    public void Act(int action)
    {
        if (!State.Act(action))
        {
            if (action == 0 || action == 1 || action == 3)
            {
                refusalUntil = UnityEngine.Time.unscaledTime + 2.1f;
                actionUntil = 0;
                speech.text = action == 3 ? "Not yet... please?" : "Too tired! I need to sleep.";
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
        clock = 0; tickCount = 0; lastTickLoss = 0;
        actionUntil = 0; refusalUntil = 0; lastStateMood = -1;
        SnapMeters();
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
