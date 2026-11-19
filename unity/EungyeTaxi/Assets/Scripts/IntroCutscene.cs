using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class IntroCutscene : MonoBehaviour {
    const float OfficeSeconds = 8f;
    const float TaxiSeconds = 10f;
    const float RoadSeconds = 8f;
    public const float DurationSeconds = OfficeSeconds + TaxiSeconds + RoadSeconds;

    // The storyboard art can be replaced under Resources/UI independently of timing and captions.
    static readonly string[] Art = { "UI/intro-office", "UI/intro-taxi", "UI/intro-first-drive" };
    static readonly string[] Captions = {
        "회사가 문을 닫았다.",
        "아버지가 남긴 건, 이 차 하나.",
        "오늘부터, 내 핸들로."
    };
    static readonly Texture2D[] Frames = new Texture2D[Art.Length];
    static GUIStyle captionStyle, skipStyle;

    DriveSession session;
    Action completed;
    float startTime, previousTimeScale;
    int startFrame, endFrame = -1;
    bool previousPaused, previousAudioPause, previousCursorVisible;
    CursorLockMode previousCursorLock;

    public bool IsPlaying { get; private set; }
    public bool IsReplay { get; private set; }
    public bool BlocksInput => IsPlaying || Time.frameCount == endFrame;
    public int CurrentCut {
        get {
            float elapsed = Time.unscaledTime - startTime;
            return elapsed < OfficeSeconds ? 0 : elapsed < OfficeSeconds + TaxiSeconds ? 1 : 2;
        }
    }

    public void Play(bool replay, Action onComplete) {
        if (IsPlaying || !isActiveAndEnabled) return;

        session = GetComponent<DriveSession>();
        previousPaused = session && session.paused;
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;

        if (session) session.paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        for (int i = 0; i < Art.Length; i++) Frame(i);
        completed = onComplete;
        IsReplay = replay;
        startTime = Time.unscaledTime;
        startFrame = Time.frameCount;
        IsPlaying = true;
    }

    void Update() {
        if (!IsPlaying) return;
        bool skipPressed = Time.frameCount != startFrame &&
            (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0));
        if (skipPressed || Time.unscaledTime - startTime >= DurationSeconds) Skip();
    }

    public void Skip() {
        if (!IsPlaying) return;
        Action callback = completed;
        Restore();
        callback?.Invoke();
    }

    void OnDisable() {
        if (IsPlaying) Restore();
    }

    void Restore() {
        IsPlaying = false;
        IsReplay = false;
        completed = null;
        endFrame = Time.frameCount;
        if (session) session.paused = previousPaused;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    static Texture2D Frame(int cut) {
        if (!Frames[cut]) Frames[cut] = Resources.Load<Texture2D>(Art[cut]);
        return Frames[cut];
    }

    static void DrawFrame(Rect bounds, Texture image) {
        CasualUI.RoundedFill(bounds, CasualUI.Ink);
        if (!image) return;
        var color = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(bounds, image, ScaleMode.ScaleToFit, false);
        GUI.color = color;
    }

    public static void DrawFirstDrive(Rect bounds) => DrawFrame(bounds, Frame(2));
    public static void DrawCurrentDrive(Rect bounds, Texture cockpitFrame) => DrawFrame(bounds, cockpitFrame);

    void OnGUI() {
        if (!BlocksInput) return;
        var input = Event.current;
        if (input.type == EventType.MouseDown || input.type == EventType.MouseUp ||
            input.type == EventType.KeyDown || input.type == EventType.KeyUp ||
            input.type == EventType.ScrollWheel) {
            bool skipPressed = IsPlaying && Time.frameCount != startFrame &&
                (input.type == EventType.MouseDown && input.button == 0 ||
                 input.type == EventType.KeyDown && input.keyCode == KeyCode.Escape);
            input.Use();
            if (skipPressed) Skip();
            return;
        }
        if (!IsPlaying) return;

        var matrix = GUI.matrix;
        var color = GUI.color;
        int depth = GUI.depth;
        GUI.depth = -16000;
        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
        CasualUI.Backdrop();

        float scale = Mathf.Min(Screen.width / 1200f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(
            new Vector3((Screen.width - 1200f * scale) / 2f, (Screen.height - 720f * scale) / 2f),
            Quaternion.identity, Vector3.one * scale);
        CasualUI.Panel(new Rect(132, 31, 936, 652));

        if (captionStyle == null) {
            captionStyle = new GUIStyle(GUI.skin.label) {
                font = KoreanText.BoldFont, fontSize = 28, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = CasualUI.Ink }
            };
            skipStyle = KoreanText.Button(18);
        }

        int cut = CurrentCut;
        DrawFrame(new Rect(152, 51, 896, 504), Frame(cut));
        GUI.Label(new Rect(152, 565, 896, 49), Captions[cut], captionStyle);
        GUI.Label(new Rect(838, 626, 210, 40), "건너뛰기 · Esc", skipStyle);

        GUI.matrix = matrix;
        GUI.color = color;
        GUI.depth = depth;
    }
}
