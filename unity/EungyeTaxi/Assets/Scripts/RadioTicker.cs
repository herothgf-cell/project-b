using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class RadioTicker : MonoBehaviour {
    DriveSession session;
    PassengerBehaviorUI behavior;
    PassengerActionUI action;
    DriveHUD driveHud;
    RadioSchedule schedule;
    RadioScheduleSnapshot awaitingRestore;
    string campaign;
    int day;
    GUIStyle stationStyle, subtitleStyle;
    string fittedLine, previousLine;
    float previousWidth;
    int previousSize;

    public void Initialize(DriveSession owner) {
        session = owner;
        behavior = owner.GetComponent<PassengerBehaviorUI>();
        action = owner.GetComponent<PassengerActionUI>();
        driveHud = owner.GetComponent<DriveHUD>();
    }

    public string VisibleText { get { if(session&&session.business&&session.business.EveningDrive)return EveningDriveRules.RadioLine(session.business.EveningDriveSeconds); return schedule == null ? "" : ContentTuning.TextOrCatalog(schedule.VisibleKey); } }
    public RadioCueKind? VisibleKind { get { return schedule == null ? (RadioCueKind?)null : schedule.VisibleKind; } }
    public double VisibleRemainingSeconds { get { return VisibleKind.HasValue ? schedule.Remaining : 0; } }

    public RadioScheduleSnapshot Capture() {
        if (!session || !session.business || session.business.ledger == null) return null;
        int currentDay = session.business.ledger.Day;
        if (awaitingRestore != null && awaitingRestore.day == currentDay) return awaitingRestore;
        if (schedule == null || day != currentDay || !session.law || campaign != session.law.ids.campaign) return new RadioScheduleSnapshot {day=currentDay};
        return schedule.Capture();
    }

    public void Restore(RadioScheduleSnapshot saved) {
        schedule = null;
        awaitingRestore = saved;
    }

    void LateUpdate() {
        if (!session || !session.StartupReady || session.IsSimulationBot || !session.business) return;
        var business = session.business;
        if (!business.SupportsCareer || !business.enabledCampaign || business.IsRestoring || business.ResumeBlocked) return;
        var ledger = business.ledger;
        string identity = session.law.ids.campaign;
        if (schedule == null || day != ledger.Day || campaign != identity) {
            schedule = null;
            if (!ledger.Operating || business.preparing) return;
            var condition = business.DailyConditionsEnabled ? business.DayCondition : DailyConditionKind.Weekday;
            var saved = awaitingRestore;
            awaitingRestore = null;
            schedule = new RadioSchedule(identity, ledger.Day, condition, ledger.Elapsed, ledger.Settings.daySeconds,
                ContentTuning.Int("radio.traffic.secondPercent", 50), saved);
            campaign = identity;
            day = ledger.Day;
        }
        var student=ReunionRide;
        if(student!=null&&CanDisplay(business)&&ElapsedSinceBoard(student)>=5&&!student.studentRadioOff&&
           (!DrivingSettings.Current.radioSubtitles||Input.GetKeyDown(KeyCode.R)))QuietStudentRide();
        bool enabled = DrivingSettings.Current.radioSubtitles && !(student!=null&&student.studentRadioOff);
        double elapsed = ledger.Elapsed, duration = ledger.Settings.daySeconds;
        bool closing = business.ShowingSummary;
        bool show = enabled && schedule.NeedsPresentation(elapsed, duration, closing) && CanDisplay(business);
        schedule.Tick(elapsed, duration, closing, show, enabled, Time.unscaledDeltaTime);
    }

    bool CanDisplay(BusinessProgress business) {
        if (!business.SupportsCareer || session.paused || session.SettingsOpen || session.FullMapOpen || session.ChoiceOpen ||
            session.PracticeMenuOpen || session.PracticeBusy || business.IntroInputBlocked || business.preparing ||
            business.restartPrompt || business.LogbookOpen || business.GoalComparison || business.RoomOpen ||
            business.ResumeBlocked || session.services.open || session.law.HistoryOpen ||
            session.accident && session.accident.Locked)
            return false;
        if (business.ShowingSummary) return true;
        return Time.timeScale > 0 && !(driveHud && driveHud.HelpOpen) && !PassengerDialogueVisible();
    }

    bool PassengerDialogueVisible() {
        var jobs = session.jobs;
        var requests = session.requests;
        if (session.business.StoryQuestionVisible || jobs.SituationPending && jobs.SituationPresentationAvailable ||
            !string.IsNullOrEmpty(jobs.VisibleCareDialogue) || action && action.State.Active)
            return true;
        if (behavior && (behavior.Showing && jobs.BehaviorChoicesOpen || !string.IsNullOrEmpty(behavior.CardMessage)))
            return true;
        return jobs.trip.State == TripState.OnTrip && !jobs.trip.ArrivalLocked && requests.HasVisiblePanel ||
            !string.IsNullOrEmpty(requests.CardReply);
    }

    public static Rect Bounds(float width, float height, bool settlement) {
        float scale = UiReadability.HudScale(width, height);
        float panelWidth = Mathf.Max(0, Mathf.Min(620 * scale, width - 32 * scale));
        float bottom = settlement ? 44 : width / scale < 1600 ? 285 : 190;
        return new Rect((width - panelWidth) / 2, height - bottom * scale, panelWidth, 38 * scale);
    }

    AudioClip studentDoor;
    public void PlayStudentDoor(){
        if(!studentDoor){
            const int rate=22050;
            var samples=new float[rate/3];
            float noise=0;
            var random=new System.Random(19);
            for(int i=0;i<samples.Length;i++){
                float t=(float)i/rate;
                noise=Mathf.Lerp(noise,(float)random.NextDouble()*2-1,.22f);
                float latch=t<.035f?0:Mathf.Exp(-95*(t-.035f));
                samples[i]=.5f*Mathf.Exp(-27*t)*Mathf.Sin(2*Mathf.PI*84*t)+noise*(.32f*Mathf.Exp(-42*t)+.6f*latch);
            }
            studentDoor=AudioClip.Create("뒷문 닫힘",samples.Length,1,rate,false);
            studentDoor.SetData(samples,0);
        }
        var source=GetComponent<AudioSource>();
        if(!source){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;}
        source.PlayOneShot(studentDoor,.65f);
    }
    void OnDestroy(){if(studentDoor)Destroy(studentDoor);}
    JobOffer ReunionRide {get{
        if(!session||!session.jobs||session.jobs.trip.State!=TripState.OnTrip||session.jobs.trip.ArrivalLocked)return null;
        var offer=session.jobs.current;return offer!=null&&offer.reunionKind!=0?offer:null;
    }}
    double ElapsedSinceBoard(JobOffer offer){return session.business.ledger.Elapsed-offer.reunionBoardTime;}
    public bool QuietStudentRide(){
        var offer=ReunionRide;if(offer==null||offer.studentRadioOff)return false;
        offer.studentRadioOff=true;offer.quietThanksAt=session.business.ledger.Elapsed;session.business.SaveCheckpoint();return true;
    }
    public string StudentLine {get{
        var offer=ReunionRide;if(offer==null)return "";
        double t=ElapsedSinceBoard(offer);
        if(t<.7)return "";
        if(t<5)return offer.reunionKind==1?"기사님! 오늘은 안 뛰었어요. 오실 줄 알았거든요.":"어, 그날 기사님이시죠? 또 뵙네요.";
        if(t<10)return "오늘 처음 혼자 실습을 끝냈어요. 가는 동안 좀 조용히 가도 돼요?";
        if(offer.studentRadioOff)return session.business.ledger.Elapsed<System.Math.Max(offer.reunionBoardTime+10,offer.quietThanksAt)+4?"고맙습니다.":"";
        return t<22?"[R] 은계 FM 끄기":"";
    }}
    void OnGUI() {
        string student=StudentLine;
        bool speaking=student.Length>0;
        string line = speaking?student:VisibleText;
        if (Event.current.type != EventType.Repaint || line.Length == 0 || (!speaking&&(!DrivingSettings.Current.radioSubtitles||ReunionRide!=null&&ReunionRide.studentRadioOff)) ||
            !CanDisplay(session.business)) return;

        float scale = CockpitUI.Scale;
        Rect bounds = Bounds(Screen.width, Screen.height, session.business.ShowingSummary);
        var matrix = GUI.matrix;
        var color = GUI.color;
        var content = GUI.contentColor;
        int depth = GUI.depth;
        GUI.depth = -175;
        GUI.matrix = Matrix4x4.TRS(new Vector3(bounds.x, bounds.y), Quaternion.identity, Vector3.one * scale);
        GUI.color = GUI.contentColor = Color.white;

        if (stationStyle == null) {
            stationStyle = new GUIStyle(GUI.skin.label) {
                font=KoreanText.BoldFont, alignment=TextAnchor.MiddleCenter, wordWrap=false, normal={textColor=Color.white}
            };
            subtitleStyle = new GUIStyle(GUI.skin.label) {
                font=KoreanText.Font, alignment=TextAnchor.MiddleLeft, wordWrap=false, normal={textColor=CasualUI.Ink}
            };
        }
        stationStyle.fontSize = Mathf.Min(22, UiReadability.FontFor(12, scale));
        subtitleStyle.fontSize = UiReadability.FontFor(14, scale);
        var panel = new Rect(0, 0, bounds.width / scale, bounds.height / scale);
        float textWidth = panel.width - 114;
        if (previousLine != line || previousWidth != textWidth || previousSize != subtitleStyle.fontSize) {
            fittedLine = UiReadability.Ellipsis(line, subtitleStyle, textWidth);
            previousLine = line;
            previousWidth = textWidth;
            previousSize = subtitleStyle.fontSize;
        }
        CasualUI.Panel(panel);
        var badge = new Rect(6, 5, 90, 28);
        CasualUI.RoundedFill(badge, CasualUI.Amber);
        if(speaking){
            CasualUI.Portrait(new Rect(7,5,28,28),PassengerPresentation.Persona(PassengerDialogueUI.StudentIdentity(session.business.Logbook)).portrait);
            badge.x+=26;badge.width-=26;
        }
        GUI.Label(badge, speaking?"학생":ContentTuning.Text("radio.station", "은계 FM"), stationStyle);
        GUI.Label(new Rect(106, 0, textWidth, panel.height), fittedLine, subtitleStyle);

        GUI.depth = depth;
        GUI.matrix = matrix;
        GUI.color = color;
        GUI.contentColor = content;
    }
}
