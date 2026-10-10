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
    int actionMood;
    [UnityEngine.SerializeField] int lastStateMood = -1;
    [UnityEngine.SerializeField] UnityEngine.Vector3 basePosition, baseScale;
    [UnityEngine.SerializeField] UnityEngine.Quaternion baseRotation;
    [UnityEngine.SerializeField] UnityEngine.Camera view;
    int viewWidth, viewHeight;
    UnityEngine.Rect lastSafeArea;
    [UnityEngine.SerializeField] UnityEngine.RectTransform safeRoot;
    [UnityEngine.SerializeField] UnityEngine.GameObject settingsPanel;
    [UnityEngine.SerializeField] UnityEngine.UI.Text musicSetting, soundSetting, resetSetting, menuDescription;
    bool menuOpen, appPaused, confirmingReset, systemPaused;
    bool appFocused = true;
    bool skipResumeFrame;
    bool musicEnabled = true, soundEnabled = true;
    float tickleUntil, nextSave;
    bool SaveEnabled => !UnityEngine.Application.isEditor &&
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SampleScene";

    void Awake()
    {
        State = new MonsterState(startingHappinessHundredths, startingStamina);
        if (SaveEnabled && UnityEngine.PlayerPrefs.GetInt("pet.saved", 0) == 1)
            State = new MonsterState(UnityEngine.PlayerPrefs.GetInt("pet.happiness", 7500),
                UnityEngine.PlayerPrefs.GetInt("pet.stamina", 100));
        musicEnabled = UnityEngine.PlayerPrefs.GetInt("pet.music", 1) == 1;
        soundEnabled = UnityEngine.PlayerPrefs.GetInt("pet.sound", 1) == 1;
        if (SaveEnabled) clock = UnityEngine.Mathf.Clamp(UnityEngine.PlayerPrefs.GetFloat("pet.clock", 0), 0, 0.999f);
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
        var oldCounter = happinessText.transform.parent.Find("Recording tick counter");
        if (oldCounter != null) UnityEngine.Object.Destroy(oldCounter.gameObject);
        if (sleepBed == null) sleepBed = pet.transform.Find("SleepBed");
        for (int i = 0; i < 3; i++)
        {
            var oldChange = happinessText.transform.parent.Find("Stat change " + i);
            if (oldChange != null) UnityEngine.Object.Destroy(oldChange.gameObject);
        }
        CreateAppMenu();
        viewWidth = 0;
        FitPortrait();
        SetMenu(false);
    }
    void Start() => ApplyAudioSettings();
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
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetMenu(!menuOpen);
        if (appPaused || menuOpen) return;
        // Android can report a long first frame after returning from the background.
        if (skipResumeFrame) { skipResumeFrame = false; Refresh(); DrawMeters(); return; }
        ReadPetTap();
        clock += UnityEngine.Time.unscaledDeltaTime;
        while (clock >= 1f)
        {
            State.Tick();
            clock -= 1f;
        }
        Refresh();
        shownHappiness = UnityEngine.Mathf.MoveTowards(shownHappiness, targetHappiness,
            happinessSpeed * UnityEngine.Time.unscaledDeltaTime);
        shownStamina = UnityEngine.Mathf.MoveTowards(shownStamina, targetStamina,
            staminaSpeed * UnityEngine.Time.unscaledDeltaTime);
        DrawMeters();
        if (UnityEngine.Time.unscaledTime >= nextSave)
        {
            SaveProgress();
            nextSave = UnityEngine.Time.unscaledTime + 5f;
        }
    }
    void FitPortrait()
    {
        int width = UnityEngine.Screen.width, height = UnityEngine.Screen.height;
        var safe = UnityEngine.Screen.safeArea;
        if (view == null || height == 0 || width == 0 ||
            (width == viewWidth && height == viewHeight && safe == lastSafeArea)) return;
        viewWidth = width; viewHeight = height;
        lastSafeArea = safe;
        float ratio = (float)width / height;
        view.rect = new UnityEngine.Rect(0, 0, 1, 1);
        // Keep the puppy's width unchanged; taller phones see more of the yard.
        view.orthographicSize = 2.8125f / ratio;
        var background = UnityEngine.GameObject.Find("Background");
        var renderer = background != null ? background.GetComponent<UnityEngine.SpriteRenderer>() : null;
        if (renderer != null && renderer.sprite != null)
        {
            var size = renderer.sprite.bounds.size;
            float cover = UnityEngine.Mathf.Max(5.625f / size.x, view.orthographicSize * 2f / size.y);
            background.transform.localScale = UnityEngine.Vector3.one * cover;
        }
        if (safeRoot == null) return;
        safeRoot.anchorMin = new UnityEngine.Vector2(safe.xMin / width, safe.yMin / height);
        safeRoot.anchorMax = new UnityEngine.Vector2(safe.xMax / width, safe.yMax / height);
        safeRoot.offsetMin = safeRoot.offsetMax = UnityEngine.Vector2.zero;
        float safeWidth = 720f * safe.width / width;
        float scale = UnityEngine.Mathf.Min(1f, safeWidth / 720f);
        settingsPanel.transform.GetChild(0).localScale = UnityEngine.Vector3.one * scale;
        Place(happinessBar.transform.parent as UnityEngine.RectTransform, 0.25f, 1f, 0, -124, 496, 160, 0.62f * scale);
        Place(staminaBar.transform.parent as UnityEngine.RectTransform, 0.75f, 1f, 0, -124, 496, 160, 0.62f * scale);
        string[] actions = { "Play", "Study", "Feed", "Sleep" };
        for (int i = 0; i < actions.Length; i++)
        {
            var button = safeRoot.Find(actions[i]);
            if (button != null) Place(button as UnityEngine.RectTransform, (i + 0.5f) / 4f, 0, 0, 140, 164, 150, scale);
        }
        Place(speechBubble.transform as UnityEngine.RectTransform, 0.5f, 0.5f, 0, 305, 420, 195.277f, scale);
        basePosition.y = 0.2f + (safe.center.y / height - 0.5f) * view.orthographicSize * 2f;
    }
    void LateUpdate()
    {
        if (appPaused || menuOpen) return;
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
        float lift = 0, tilt = UnityEngine.Time.unscaledTime < tickleUntil
            ? UnityEngine.Mathf.Sin(UnityEngine.Time.unscaledTime * 18f) * 5f : 0;
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
        if (State.HappinessHundredths == 10000) shownHappiness = 100f;
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
        happinessText.text = (State.HappinessHundredths == 10000 ? 100 : (int)happinessNumber).ToString() + " / 100";
        staminaText.text = ((int)shownStamina).ToString() + " / 100";
    }
    public void Act(int action)
    {
        if (menuOpen || appPaused) return;
        tickleUntil = 0;
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
        SaveProgress();
    }
    public void ResetDemo(int preset)
    {
        int[] happiness = { 7500, 9950, 5050, 7500 };
        if (preset < 0 || preset >= happiness.Length) return;
        State = new MonsterState(happiness[preset], preset == 3 ? 20 : 100);
        clock = 0;
        actionUntil = 0; refusalUntil = 0; lastStateMood = -1;
        SnapMeters();
        foreach (var source in actionMusic) source.Stop();
        speech.text = new[] { "Hi! Want to play?", "One more game?", "Keep me company?", "A little play?" }[preset];
        Refresh();
    }
    void SaveProgress()
    {
        if (!SaveEnabled || State == null) return;
        UnityEngine.PlayerPrefs.SetInt("pet.saved", 1);
        UnityEngine.PlayerPrefs.SetInt("pet.happiness", State.HappinessHundredths);
        UnityEngine.PlayerPrefs.SetInt("pet.stamina", State.Stamina);
        UnityEngine.PlayerPrefs.SetFloat("pet.clock", clock);
        UnityEngine.PlayerPrefs.Save();
    }
    void OnApplicationPause(bool paused)
    {
        systemPaused = paused;
        skipResumeFrame = true;
        appPaused = systemPaused || !appFocused;
        if (paused) SaveProgress();
        SetPausedAudioAndAnimation();
    }
    void OnApplicationFocus(bool focused)
    {
        appFocused = focused;
        skipResumeFrame = true;
        appPaused = systemPaused || !appFocused;
        if (!focused) SaveProgress();
        SetPausedAudioAndAnimation();
    }
    void OnApplicationQuit() => SaveProgress();
    void SetPausedAudioAndAnimation()
    {
        if (pet != null) pet.speed = appPaused || menuOpen ? 0 : 1;
        UnityEngine.AudioListener.pause = appPaused || menuOpen;
    }
    void SetMenu(bool open)
    {
        menuOpen = open;
        skipResumeFrame = true;
        confirmingReset = false;
        if (settingsPanel != null) settingsPanel.SetActive(open);
        if (open) SaveProgress();
        UpdateMenuLabels();
        SetPausedAudioAndAnimation();
    }
    void ApplyAudioSettings()
    {
        foreach (var source in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>())
            source.mute = source.loop ? !musicEnabled : !soundEnabled;
        UnityEngine.PlayerPrefs.SetInt("pet.music", musicEnabled ? 1 : 0);
        UnityEngine.PlayerPrefs.SetInt("pet.sound", soundEnabled ? 1 : 0);
        UnityEngine.PlayerPrefs.Save();
        UpdateMenuLabels();
    }
    void UpdateMenuLabels()
    {
        if (musicSetting == null) return;
        musicSetting.text = "Music: " + (musicEnabled ? "On" : "Off");
        soundSetting.text = "Sound effects: " + (soundEnabled ? "On" : "Off");
        resetSetting.text = confirmingReset ? "Yes, start fresh" : "New puppy";
        menuDescription.text = confirmingReset ? "Start fresh? This replaces your saved puppy.\nPress Continue to cancel."
            : "Your puppy is saved on this phone.\nThe game rests while you are away.";
    }
    void ReadPetTap()
    {
        UnityEngine.Vector2 point;
        var touch = UnityEngine.InputSystem.Touchscreen.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) point = touch.primaryTouch.position.ReadValue();
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame) point = mouse.position.ReadValue();
        else return;
        if (UnityEngine.Time.unscaledTime < tickleUntil || view == null) return;
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null)
        {
            var pointer = new UnityEngine.EventSystems.PointerEventData(events) { position = point };
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            events.RaycastAll(pointer, hits);
            if (hits.Count > 0) return;
        }
        var world = view.ScreenToWorldPoint(new UnityEngine.Vector3(point.x, point.y, petSprite.transform.position.z - view.transform.position.z));
        var bounds = petSprite.bounds;
        if (world.x < bounds.min.x || world.x > bounds.max.x || world.y < bounds.min.y || world.y > bounds.max.y) return;
        tickleUntil = UnityEngine.Time.unscaledTime + 1.4f;
        actionUntil = tickleUntil;
        actionMood = 1;
        refusalUntil = 0;
        speech.text = "Hehe! That tickles!";
        if (actionMusic.Length > 0 && actionMusic[0] != null && actionMusic[0].clip != null)
            actionMusic[0].PlayOneShot(actionMusic[0].clip, 0.5f);
        Refresh();
    }
    UnityEngine.RectTransform AppRect(string name, UnityEngine.Transform parent)
    {
        var item = new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform));
        item.layer = 5;
        var rect = item.GetComponent<UnityEngine.RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
    void Place(UnityEngine.RectTransform rect, float x, float y, float dx, float dy, float width, float height, float scale = 1)
    {
        rect.SetParent(safeRoot, false);
        rect.anchorMin = rect.anchorMax = new UnityEngine.Vector2(x, y);
        rect.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new UnityEngine.Vector2(dx, dy);
        rect.sizeDelta = new UnityEngine.Vector2(width, height);
        rect.localScale = UnityEngine.Vector3.one * scale;
    }
    UnityEngine.UI.Text AppText(string name, UnityEngine.Transform parent, string text, float y, int fontSize, float width = 480)
    {
        var rect = AppRect(name, parent);
        rect.anchorMin = rect.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
        rect.sizeDelta = new UnityEngine.Vector2(width, 80);
        rect.anchoredPosition = new UnityEngine.Vector2(0, y);
        var label = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        label.font = speech.font;
        label.fontSize = fontSize;
        label.alignment = UnityEngine.TextAnchor.MiddleCenter;
        label.color = new UnityEngine.Color(0.08f, 0.2f, 0.17f);
        label.raycastTarget = false;
        label.text = text;
        return label;
    }
    UnityEngine.UI.Text AppButton(string name, UnityEngine.Transform parent, string text, float y, UnityEngine.Events.UnityAction clicked)
    {
        var rect = AppRect(name, parent);
        rect.anchorMin = rect.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
        rect.sizeDelta = new UnityEngine.Vector2(480, 62);
        rect.anchoredPosition = new UnityEngine.Vector2(0, y);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new UnityEngine.Color(0.19f, 0.42f, 0.35f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(clicked);
        var label = AppText("Label", rect, text, 0, 28);
        label.color = UnityEngine.Color.white;
        return label;
    }
    void CreateAppMenu()
    {
        var canvas = happinessText.GetComponentInParent<UnityEngine.Canvas>();
        canvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight = 0;
        if (safeRoot != null) return;
        safeRoot = AppRect("Phone safe controls", canvas.transform);
        string[] controls = { "Play", "Study", "Feed", "Sleep" };
        foreach (var name in controls)
        {
            var control = canvas.transform.Find(name);
            if (control != null) control.SetParent(safeRoot, false);
        }
        foreach (var pair in new[] { (happinessText, happinessBar), (staminaText, staminaBar) })
        {
            var rect = pair.Item1.rectTransform;
            rect.SetParent(pair.Item2.transform, false);
            rect.anchorMin = rect.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
            rect.anchoredPosition = UnityEngine.Vector2.zero;
            rect.sizeDelta = new UnityEngine.Vector2(300, 68);
            rect.localScale = UnityEngine.Vector3.one;
            pair.Item1.fontSize = 36;
            pair.Item1.alignment = UnityEngine.TextAnchor.MiddleCenter;
            pair.Item1.raycastTarget = false;
        }
        var title = AppText("App title", safeRoot, "Pocket Pup", 0, 26, 350);
        title.color = UnityEngine.Color.white;
        title.gameObject.AddComponent<UnityEngine.UI.Outline>().effectDistance = new UnityEngine.Vector2(1, -1);
        Place(title.rectTransform, 0.5f, 1, -45, -35, 350, 48);
        var menu = AppButton("Menu", safeRoot, "Menu", 0, () => SetMenu(true));
        Place(menu.transform.parent as UnityEngine.RectTransform, 1, 1, -68, -35, 104, 48);
        menu.rectTransform.sizeDelta = new UnityEngine.Vector2(104, 48);
        menu.fontSize = 24;
        var panel = AppRect("App settings", safeRoot);
        panel.anchorMin = UnityEngine.Vector2.zero;
        panel.anchorMax = UnityEngine.Vector2.one;
        panel.offsetMin = panel.offsetMax = UnityEngine.Vector2.zero;
        panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(0.04f, 0.12f, 0.1f, 0.94f);
        settingsPanel = panel.gameObject;
        var card = AppRect("Settings card", panel);
        card.anchorMin = card.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
        card.sizeDelta = new UnityEngine.Vector2(580, 780);
        card.gameObject.AddComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(0.83f, 0.96f, 0.89f);
        AppText("Settings title", card, "Pocket Pup", 320, 40);
        menuDescription = AppText("About", card, "", 230, 24);
        musicSetting = AppButton("Music", card, "", 125, () => { musicEnabled = !musicEnabled; ApplyAudioSettings(); });
        soundSetting = AppButton("Sounds", card, "", 45, () => { soundEnabled = !soundEnabled; ApplyAudioSettings(); });
        AppButton("Continue", card, "Continue", -45, () => SetMenu(false));
        resetSetting = AppButton("New puppy", card, "", -125, () =>
        {
            if (!confirmingReset) { confirmingReset = true; UpdateMenuLabels(); return; }
            ResetDemo(0);
            SaveProgress();
            SetMenu(false);
        });
        AppButton("Close", card, "Save & close", -215, () => { SaveProgress(); UnityEngine.Application.Quit(); });
        AppText("Version", card, "Monster Simulator 1.1\nTap your puppy for a tickle!", -315, 22);
        settingsPanel.SetActive(false);
        UnityEngine.Screen.fullScreen = true;
        UnityEngine.Application.targetFrameRate = 60;
    }
    [UnityEngine.ContextMenu("Recording presets/New pet")] void NewPet() => ResetDemo(0);
    [UnityEngine.ContextMenu("Recording presets/Almost happy")] void AlmostHappy() => ResetDemo(1);
    [UnityEngine.ContextMenu("Recording presets/A quiet mood")] void QuietMood() => ResetDemo(2);
    [UnityEngine.ContextMenu("Recording presets/Nearly tired")] void NearlyTired() => ResetDemo(3);
}
#endif
