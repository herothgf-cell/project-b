using System;
using UnityEngine;

// First-career opening. Four short cuts, letterboxed full screen:
//   0 black card with the date, 1 the rented room, 2 the father's taxi in the home garage
//   (rendered live from the scene), 3 the driver's seat of that taxi (rendered live).
// Click advances one cut, Esc skips the whole sequence.
[DefaultExecutionOrder(-1000)]
public sealed class IntroCutscene : MonoBehaviour {
    static readonly float[] CutSeconds = { 6f, 6f, 7f, 6f };
    public static float DurationSeconds {
        get { float total = 0; foreach (var s in CutSeconds) total += s; return total; }
    }
    public const int CutCount = 4;
    const float Fade = .7f, LettersPerSecond = 16f;

    static readonly string[] Captions = {
        "다니던 회사가 문을 닫았다.",
        "모아 둔 돈으로는 몇 달 못 버틴다.",
        "아버지가 30년 몰던 택시. 남은 건 이 차 하나다.",
        "일단, 오늘 하루만 나가 보자."
    };
    static readonly string[] Overlines = { "3월 첫째 주", "기사 숙소 · 원룸", "기사 숙소 · 1층 차고", "" };
    static Texture2D room;
    static GUIStyle captionStyle, overlineStyle, hintStyle, billTitle, billBody;

    DriveSession session;
    Action completed;
    float cutStart, previousTimeScale;
    int cut, startFrame, endFrame = -1;
    bool previousPaused, previousAudioPause, previousCursorVisible;
    CursorLockMode previousCursorLock;
    RenderTexture shot;

    public bool IsPlaying { get; private set; }
    public bool IsReplay { get; private set; }
    public bool BlocksInput => IsPlaying || Time.frameCount == endFrame;
    public int CurrentCut => cut;
    public float CutElapsed => IsPlaying ? Time.unscaledTime - cutStart : 0f;

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

        if (!room) room = Resources.Load<Texture2D>("personal-room-base");
        completed = onComplete;
        IsReplay = replay;
        cut = 0;
        cutStart = Time.unscaledTime;
        startFrame = Time.frameCount;
        IsPlaying = true;
    }

    void Update() {
        if (!IsPlaying) return;
        bool fresh = Time.frameCount == startFrame;
        if (!fresh && Input.GetKeyDown(KeyCode.Escape)) { Skip(); return; }
        if (!fresh && Input.GetMouseButtonDown(0)) { Advance(); return; }
        if (Time.unscaledTime - cutStart >= CutSeconds[cut]) Advance();
    }

    void LateUpdate() {
        if (!IsPlaying || cut < 2 || !session || !session.drivingCamera) return;
        if (!shot) shot = new RenderTexture(1280, 720, 24) { name = "Intro live shot" };
        float t = Mathf.Clamp01((Time.unscaledTime - cutStart) / CutSeconds[cut]);
        if (!session.drivingCamera.RenderIntroShot(shot, cut == 3, t)) ReleaseShot();
    }

    void Advance() {
        if (cut + 1 >= CutCount) { Skip(); return; }
        cut++;
        cutStart = Time.unscaledTime;
    }

    public void Skip() {
        if (!IsPlaying) return;
        Action callback = completed;
        Restore();
        callback?.Invoke();
    }

    void OnDisable() {
        if (IsPlaying) Restore();
        ReleaseShot();
    }

    void ReleaseShot() {
        if (!shot) return;
        shot.Release();
        Destroy(shot);
        shot = null;
    }

    void Restore() {
        IsPlaying = false;
        IsReplay = false;
        completed = null;
        endFrame = Time.frameCount;
        ReleaseShot();
        if (session) session.paused = previousPaused;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    // Goal comparison: the first-day cockpit photo saved when the first intro ended.
    public static void DrawFirstDrive(Rect bounds, Texture firstDay) {
        DrawFrame(bounds, firstDay);
        if (firstDay) return;
        Styles();
        GUI.Label(bounds, "첫날 운전석 기록이 없는 경력입니다", hintStyle);
    }
    public static void DrawCurrentDrive(Rect bounds, Texture cockpitFrame) => DrawFrame(bounds, cockpitFrame);

    static void DrawFrame(Rect bounds, Texture image) {
        CasualUI.RoundedFill(bounds, CasualUI.Ink);
        if (!image) return;
        var color = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTexture(bounds, image, ScaleMode.ScaleAndCrop, false);
        GUI.color = color;
    }

    static void Styles() {
        if (captionStyle != null) return;
        captionStyle = new GUIStyle(GUI.skin.label) {
            font = KoreanText.BoldFont, fontSize = 30, alignment = TextAnchor.MiddleCenter, wordWrap = true,
            normal = { textColor = Color.white }
        };
        overlineStyle = new GUIStyle(captionStyle) { font = KoreanText.Font, fontSize = 18, normal = { textColor = new Color(1, 1, 1, .7f) } };
        hintStyle = new GUIStyle(captionStyle) { font = KoreanText.Font, fontSize = 15, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(1, 1, 1, .55f) } };
        billTitle = new GUIStyle(GUI.skin.label) { font = KoreanText.BoldFont, fontSize = 20, normal = { textColor = CasualUI.Ink } };
        billBody = new GUIStyle(billTitle) { font = KoreanText.Font, fontSize = 16, normal = { textColor = CasualUI.Muted } };
    }

    static void Fill(Rect r, Color c) {
        var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
    }

    void OnGUI() {
        if (!BlocksInput) return;
        var input = Event.current;
        if (input.type == EventType.MouseDown || input.type == EventType.MouseUp ||
            input.type == EventType.KeyDown || input.type == EventType.KeyUp ||
            input.type == EventType.ScrollWheel) {
            input.Use();
            return;
        }
        if (!IsPlaying) return;

        var matrix = GUI.matrix;
        var color = GUI.color;
        int depth = GUI.depth;
        GUI.depth = -16000;
        GUI.matrix = Matrix4x4.identity;
        Styles();

        float w = Screen.width, h = Screen.height;
        float t = Time.unscaledTime - cutStart, length = CutSeconds[cut];
        Fill(new Rect(0, 0, w, h), Color.black);

        // 2.39:1 picture area between letterbox bars.
        float pictureH = Mathf.Min(h, w / 2.39f), top = (h - pictureH) / 2f;
        var picture = new Rect(0, top, w, pictureH);
        if (cut == 1 && room) {
            GUI.color = new Color(.55f, .55f, .62f);
            GUI.DrawTexture(picture, room, ScaleMode.ScaleAndCrop, false);
            GUI.color = Color.white;
            DrawBill(picture, t);
        } else if (cut >= 2 && shot) {
            GUI.color = Color.white;
            GUI.DrawTexture(picture, shot, ScaleMode.ScaleAndCrop, false);
        }

        float scale = Mathf.Max(.6f, h / 720f);
        if (Overlines[cut].Length > 0) {
            overlineStyle.fontSize = Mathf.RoundToInt(18 * scale);
            GUI.Label(new Rect(0, top + 22 * scale, w, 30 * scale), Overlines[cut], overlineStyle);
        }

        // Fade in from black, fade out to black.
        float alpha = Mathf.Max(1 - t / Fade, (t - (length - Fade)) / Fade);
        if (alpha > 0) Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, Mathf.Clamp01(alpha)));

        // Typewriter caption in the lower bar.
        string text = Captions[cut];
        int letters = Mathf.Clamp(Mathf.FloorToInt((t - Fade * .5f) * LettersPerSecond), 0, text.Length);
        captionStyle.fontSize = Mathf.RoundToInt(30 * scale);
        // Captions live in the lower letterbox bar so they never cover the picture.
        float barTop = top + pictureH, barHeight = Mathf.Max(h - barTop, 72 * scale);
        GUI.Label(new Rect(w * .1f, Mathf.Min(barTop, h - 72 * scale) + (barHeight - 64 * scale) * .35f, w * .8f, 64 * scale), text.Substring(0, letters), captionStyle);

        hintStyle.fontSize = Mathf.RoundToInt(15 * scale);
        GUI.Label(new Rect(0, h - 34 * scale, w - 24 * scale, 28 * scale),
            (cut + 1) + " / " + CutCount + "   클릭 다음 · Esc 건너뛰기", hintStyle);

        GUI.matrix = matrix;
        GUI.color = color;
        GUI.depth = depth;
    }

    // A rent notice slipped under the door: concrete pressure without numbers that clash with the economy.
    static void DrawBill(Rect picture, float t) {
        float scale = picture.height / 535f;
        float rise = Mathf.SmoothStep(30, 0, Mathf.Clamp01(t / 1.2f)) * scale;
        var paper = new Rect(picture.center.x - 170 * scale, picture.y + picture.height * .36f + rise, 340 * scale, 150 * scale);
        CasualUI.RoundedFill(new Rect(paper.x, paper.y + 5 * scale, paper.width, paper.height), new Color(0, 0, 0, .35f));
        CasualUI.RoundedFill(paper, CasualUI.Warm);
        billTitle.fontSize = Mathf.RoundToInt(20 * scale);
        billBody.fontSize = Mathf.RoundToInt(16 * scale);
        GUI.Label(new Rect(paper.x + 22 * scale, paper.y + 16 * scale, paper.width, 30 * scale), "월세 안내", billTitle);
        GUI.Label(new Rect(paper.x + 22 * scale, paper.y + 54 * scale, paper.width - 40 * scale, 80 * scale),
            "기사 숙소 원룸 · 매일 영업 마감 때 일할 정산\n밀리지 않게 부탁드립니다. — 관리인", billBody);
    }
}
