// Night3RadioCheckLock.cs  (v3.0)
// رویداد شب ۳ — «بیسیم رو چک کن» + «جنراتور رو خاموش کن» (مثل شب ۱):
//
//   فاز ۱ — تریگر دوربین ۶ (مثل v2.1):
//     رسیدن به دوربین ۶ توی CCTV → صدای ناگهانی + متن CHECK THE RADIO! + قفل سوئیچ
//     بین دوربین‌ها (نه جلو نه عقب؛ خروج از نما با EXAMINE آزاد می‌مونه)
//
//   فاز ۲ — گوش دادن کامل به پیام بیسیم:
//     پلیر میره بیسیم رو چک می‌کنه (NotifyRadioChecked از OnStart تعامل — سیم‌کشی
//     v2.1 سر جاشه) → پیام پخش میشه (Radio Message Clip؛ اگه خالی باشه به اندازه‌ی
//     Message Duration ثانیه صبر می‌کنه تا دیالوگ UHFPS خود بیسیم تموم شه)
//     → تا پیام «کامل» نشده، سوئیچ دوربین هنوز قفله
//
//   فاز ۳ — ددلاین جنراتور (مثل شب ۱ — PlayerTurnsOff):
//     پیام که کامل شد → سوئیچ دوربین آزاد میشه + شمارش معکوس Generator Deadline شروع میشه.
//     پلیر باید قبل از صفر شدن شمارش، جنراتور رو خاموش کنه (همون ایونت‌های شب ۱:
//     OnGeneratorEnd / OnOutOfFuel). اگه جنراتور از قبل خاموش باشه → نجات فوری.
//
//   فاز ۴ — جامپ‌اسکر و باخت (مثل شب ۱):
//     ددلاین سر اومد → اگه وسط نمای CCTVـه اول میارش بیرون → صدای جیغ + افکت‌های
//     JumpscareTrigger کیت (اگه وصل کرده باشی) + ایونت OnJumpscare → پلیر فریز →
//     RestartCurrentNight — شب ۳ از نو شروع میشه (باخت)
//
// پیش‌نیازها (مثل قبل):
//   • CCTV_CameraSystem.cs باید نسخه v2.2 باشه (SetSwitchLocked + SwitchToCamera داره)
//     — وگرنه این اسکریپت کامپایل نمیشه.
//   • PowerGenerator توی صحنه (خودش پیدا می‌کنه؛ یا فیلد Generator رو دستی وصل کن)
//
// سیم‌کشی — نسبت به v2.1 هیچ چیز عوض نشده:
//   • تعامل بیسیم (CustomInteractEvent) → OnStart → این کامپوننت → NotifyRadioChecked()
//     (همون اسلاتی که برای v2.1 وصل کردی همون می‌مونه)
//   • فقط فیلدهای جدید بخش‌های «پیام بیسیم» و «جنراتور/جامپ‌اسکر» رو توی Inspector پر کن
//
// نکته‌ها:
//   • چک کردن بیسیم «قبل از» دوربین ۶ (روتین معمول اول شب) حساب نمی‌شه — بعد از
//     تریگر باید دوباره چک کنه و کامل گوش بده (تغییر v2.1).
//   • اگه پلیر جنراتور رو قبل از شروع ددلاین خاموش کرده باشه، لحظه‌ی تموم شدن پیام
//     نجات حساب میشه (همون قاعده‌ی شب ۱: جنراتور خاموش = تاریکی = امن).
//   • شماره دوربین از سمت کیت صفر-بیسه: «CAM 6 تو = Index 5». اورلی دیباگ Idx زنده
//     نشون میده — اگه فرق کرد Trigger Camera Number رو تنظیم کن.
//   • عوض شدن شماره شب (باخت/شب بعد) = ریست کامل رویداد.
//   • منوهای راست‌کلیک: Debug: Current Camera / Force Trigger / Finish Listen Now /
//     Force Jumpscare / Generator State / Force Unlock

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;

public class Night3RadioCheckLock : MonoBehaviour
{
    private enum SeqPhase { Idle, Locked, Listening, Deadline, Jumpscaring, Done }

    [Header("ارجاع‌ها")]
    [Tooltip("سیستم دوربین‌های صحنه (خالی = خودش پیدا می‌کنه)")]
    public CCTV_CameraSystem cctv;

    [Tooltip("جنراتور — خالی بذار خودش تو صحنه پیدا کنه (مثل شب ۱)")]
    public PowerGenerator generator;

    [Header("شب و دوربین تریگر")]
    [Tooltip("این رویداد فقط توی این شب فعال میشه")]
    public int nightNumber = 3;

    [Tooltip("شماره دوربینی که با رسیدن بهش رویداد فعال میشه (شمارش خودت: ۶ = ششمین دوربین = Index 5)")]
    public int triggerCameraNumber = 6;

    [Header("فاز ۱ — صدا و متن تریگر")]
    [Tooltip("صدای ناگهانی موقع تریگر؛ اگه خالی بمونه بوق fallback خودکار پخش میشه")]
    public AudioClip triggerSound;

    [Range(0f, 1f), Tooltip("بلندی صدا")]
    public float triggerVolume = 1f;

    [Tooltip("متنی که موقع تریگر ظاهر میشه")]
    public string hintText = "CHECK THE RADIO!";

    [Tooltip("مدت نمایش متن تریگر وقتی قفلی در کار نیست (ثانیه)")]
    public float hintDuration = 4f;

    [Tooltip("تا وقتی پیام بیسیم کامل نشده، متن مدام روی صفحه بمونه")]
    public bool persistentHintWhileLocked = true;

    [Tooltip("هر چند ثانیه یادآوری تکرار شه وقتی هنوز پیام کامل نشده (۰ = خاموش)")]
    public float remindEvery = 0f;

    [Header("فاز ۲ — پیام بیسیم (بعد از تعامل)")]
    [Tooltip("صدای پیام شب ۳ که بعد از چک کردن بیسیم پخش میشه. خالی = فقط به اندازه‌ی Message Duration صبر می‌کنه (طول دیالوگ خود بیسیم رو بذار)")]
    public AudioClip radioMessageClip;

    [Tooltip("اگه Radio Message Clip خالیه: چند ثانیه گوش دادن حساب شه (طول دیالوگ/ویس شب ۳)")]
    public float messageDuration = 8f;

    [Tooltip("اختیاری: AudioSource بیسیم (مثل radioVoice توی RadioTVGate) — پیام از روی این پخش میشه؛ خالی = سورس 2D خود اسکریپت")]
    public AudioSource radioVoice;

    [Tooltip("وقتی پیام کامل شنیده شد اجرا میشه (صدا/افکت دلخواه)")]
    public UnityEvent OnListenFinished;

    [Header("فاز ۳ — جنراتور (مثل شب ۱)")]
    [Tooltip("مراقبت جنراتور فعال باشه — خاموش کردنش قبل از ددلاین = نجات")]
    public bool useGeneratorCheck = true;

    [Tooltip("ثانیه‌های شمارش معکوس بعد از تموم شدن پیام — مثل Lights Off Time Limit شب ۱")]
    public float generatorDeadline = 30f;

    [Tooltip("تا وقتی ددلاین شمارش میشه، متن هشدار روی صفحه بمونه")]
    public bool showDeadlineHint = true;

    [TextArea, Tooltip("متنی که وسط ددلاین جنراتور نشون داده میشه")]
    public string deadlineHintText = "TURN OFF THE GENERATOR!";

    [Tooltip("صدای اختیاری وقتی جنراتور سر وقت خاموش شد (آرامش/نجات)")]
    public AudioClip savedClip;

    [Tooltip("نجات موفق — جنراتور خاموش شد و رویداد تموم شد")]
    public UnityEvent OnGeneratorSaved;

    [Header("فاز ۴ — جامپ‌اسکر (مثل شب ۱)")]
    [Tooltip("اختیاری: کامپوننت JumpscareTrigger خود کیت UHFPS (همونی که شب ۱ افکتش رو می‌گیره) — وینیت/تنتاکل/لرزش فعال میشه و قبل از ریست شب بسته میشه")]
    public JumpscareTrigger kitJumpscare;

    [Tooltip("صدای جیغ لحظه جامپ‌اسکر (اختیاری)")]
    public AudioClip screamClip;

    [Tooltip("چند ثانیه جامپ‌اسکر بمونه بعدش شب ریست شه")]
    public float jumpscareDuration = 1.8f;

    [Tooltip("افکت‌های دلخواه (فلش، لرزش و...) — ریست شب خودکار انجام میشه")]
    public UnityEvent OnJumpscare;

    [Header("قفل دوربین")]
    [Tooltip("تا پیام بیسیم کامل نشده، سوئیچ بین دوربین‌ها قفل میشه (خروج از نما آزاد می‌مونه)")]
    public bool lockCameraSwitching = true;

    [Header("دیباگ (بعد از درست شدن خاموش کن)")]
    [Tooltip("نمایش وضعیت زنده گوشه‌ی صفحه: شب / Index / فاز / شمارش‌ها")]
    public bool debugOverlay = true;

    [Tooltip("لاگ کردن هر سوئیچ دوربین توی Console")]
    public bool debugLogSwitches = true;

    // ---------- وضعیت داخلی ----------
    private SeqPhase phase = SeqPhase.Idle;
    private int lastKnownNight;
    private int lockIndex;              // دوربینی که موقع تریگر روش گیر کرده
    private float listenElapsed;        // زمان گذشته‌ی گوش دادن
    private float listenTarget;         // طول پیام (کلیپ یا Message Duration)
    private float deadlineTimer;        // شمارش ددلاین جنراتور
    private bool deadlineWarned;        // پیش‌شماره ۵ ثانیه‌ای
    private bool genRunning;            // وضعیت جنراتور — با ایونت‌های خودش sync میشه (الگوی شب ۱)
    private float remindTimer;
    private float showTextUntil;
    private NightManager nightManager;
    private AudioSource audio2d;
    private AudioClip fallbackClip;
    private int lastSeenIndex = int.MinValue;

    // ---------- استایل‌های IMGUI ----------
    private GUIStyle hintStyle;
    private GUIStyle shadowStyle;
    private GUIStyle debugStyle;
    private bool stylesBuilt;

    // ------------------------------------------------------------------

    private void Start()
    {
        lastKnownNight = NightManager.CurrentNight;
        Debug.Log("[Night3] v3.0 فعال شد — شب فعلی: " + lastKnownNight + " | تریگر روی دوربین " + triggerCameraNumber);

        if (cctv == null)
        {
            cctv = FindObjectOfType<CCTV_CameraSystem>();
            if (cctv != null) Debug.Log("[Night3] CCTV_CameraSystem پیدا شد: " + cctv.name);
        }
        if (cctv == null)
            Debug.LogWarning("[Night3] CCTV_CameraSystem توی صحنه پیدا نشد! فیلد CCTV رو دستی وصل کن.");

        if (triggerCameraNumber < 1)
        {
            triggerCameraNumber = 1;
            Debug.LogWarning("[Night3] Trigger Camera Number نمی‌تونه صفر یا منفی باشه — شد ۱.");
        }

        audio2d = GetComponent<AudioSource>();
        if (audio2d == null) audio2d = gameObject.AddComponent<AudioSource>();
        audio2d.playOnAwake = false;
        audio2d.spatialBlend = 0f;   // صدا 2D — مهم نیست پلیر کجاست، «یهو» میاد

        nightManager = FindObjectOfType<NightManager>();
        if (nightManager == null)
            Debug.LogWarning("[Night3] NightManager پیدا نشد — ریست شب بعد از جامپ‌اسکر انجام نمیشه!");

        // اتصال به جنراتور — دقیقاً الگوی شب ۱ (Night1ScareEvent)
        if (useGeneratorCheck)
        {
            if (generator == null)
                generator = FindObjectOfType<PowerGenerator>();

            if (generator != null)
            {
                generator.OnGeneratorStart.AddListener(HandleGeneratorOn);
                generator.OnGeneratorEnd.AddListener(HandleGeneratorOff);
                generator.OnOutOfFuel.AddListener(HandleGeneratorOff);
                genRunning = true;   // فرض اولیه: اول شب جنراتور روشنه — ایونت‌ها تصحیحش می‌کنن
                Debug.Log("[Night3] جنراتور وصل شد: " + generator.name + " (فرض اولیه: روشن)");
            }
            else
            {
                Debug.LogWarning("[Night3] PowerGenerator توی صحنه پیدا نشد! فیلد Generator رو دستی وصل کن — " +
                                 "وگرنه ددلاین هیچ‌وقت با خاموش کردن جنراتور ذخیره نمیشه و همیشه جامپ‌اسکر می‌خوره.");
            }
        }
    }

    private void OnEnable()
    {
        // اگه وسط قفل، اسکریپت خاموش/روشن شد — قفل رو دوباره بزار
        if (IsSwitchLockedPhase() && cctv != null) cctv.SetSwitchLocked(true);
    }

    private void OnDisable()
    {
        // اگه وسط قفل اسکریپت خاموش شد، قفل کیت برداشته شه که بازی گیر نکنه
        if (IsSwitchLockedPhase() && cctv != null) cctv.SetSwitchLocked(false);
    }

    private void OnDestroy()
    {
        if (generator != null)
        {
            generator.OnGeneratorStart.RemoveListener(HandleGeneratorOn);
            generator.OnGeneratorEnd.RemoveListener(HandleGeneratorOff);
            generator.OnOutOfFuel.RemoveListener(HandleGeneratorOff);
        }

        if (IsSwitchLockedPhase() && cctv != null) cctv.SetSwitchLocked(false);
    }

    // فازهایی که سوئیچ دوربین باید قفل بمونه
    private bool IsSwitchLockedPhase()
    {
        return lockCameraSwitching && (phase == SeqPhase.Locked || phase == SeqPhase.Listening);
    }

    // ------------------------------------------------------------------

    private void Update()
    {
        // اگه شماره شب عوض شد — ریست کامل (همون الگوی Night2BreathEvent)
        if (NightManager.CurrentNight != lastKnownNight)
        {
            lastKnownNight = NightManager.CurrentNight;
            ResetForNewNight();
        }

        if (lastKnownNight != nightNumber || cctv == null) return;

        int idx = cctv.CurrentCameraIndex;

        // لاگ هر سوئیچ — برای کالیبره شدن شماره‌ها
        if (idx != lastSeenIndex)
        {
            if (lastSeenIndex != int.MinValue && debugLogSwitches)
                Debug.Log("[Night3] سوئیچ دوربین → Index=" + idx + " (CAM " + (idx + 1) + ")");
            lastSeenIndex = idx;
        }

        switch (phase)
        {
            case SeqPhase.Idle:
                // تریگر: رسیدن به دوربین مقصد (CurrentCameraIndex صفر-بیسه: CAM 6 = Index 5)
                if (cctv.IsCameraView && idx == triggerCameraNumber - 1)
                    TriggerNow();
                break;

            case SeqPhase.Locked:
            case SeqPhase.Listening:
                // ضمانت پین — اگه به هر دلیل شماره عوض شد، فوری برمی‌گرده
                if (IsSwitchLockedPhase() && idx != lockIndex)
                    cctv.SwitchToCamera(lockIndex);

                // یادآوری اختیاری تا وقتی پیام کامل نشده
                if (remindEvery > 0f)
                {
                    remindTimer += Time.deltaTime;
                    if (remindTimer >= remindEvery)
                    {
                        remindTimer = 0f;
                        showTextUntil = Time.unscaledTime + Mathf.Max(1f, hintDuration);
                    }
                }

                // گوش دادن — تا پیام کامل نشده فاز عوض نمیشه
                if (phase == SeqPhase.Listening)
                {
                    listenElapsed += Time.deltaTime;
                    if (listenElapsed >= listenTarget)
                        FinishListening();
                }
                break;

            case SeqPhase.Deadline:
                // نجات: جنراتور خاموش شد (ایونت یا از قبل خاموش بود)
                if (useGeneratorCheck && !genRunning)
                {
                    GeneratorSaved();
                    break;
                }

                deadlineTimer += Time.deltaTime;

                // پیش‌شماره — ۵ ثانیه مونده به جامپ‌اسکر (مثل شب ۱)
                if (!deadlineWarned && generatorDeadline > 5f && deadlineTimer >= generatorDeadline - 5f)
                {
                    deadlineWarned = true;
                    Debug.Log("[Night3] ⚠ ۵ ثانیه به جامپ‌اسکر مونده — جنراتور رو خاموش کن!");
                }

                if (deadlineTimer >= generatorDeadline)
                    DoJumpscare();
                break;
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        // ── اورلی دیباگ: وضعیت زنده — جواب سوال «الان کجای روندیم؟» ──
        if (debugOverlay)
        {
            string s = "Night3 v3.0 | Night=" + lastKnownNight
                     + " | CCTV=" + (cctv != null ? "OK" : "NOT FOUND!");
            if (cctv != null)
                s += " | Idx=" + cctv.CurrentCameraIndex + " (CAM " + (cctv.CurrentCameraIndex + 1) + ")"
                   + " | View=" + cctv.IsCameraView;
            s += " | Trig@CAM" + triggerCameraNumber + " | " + PhaseLabel();
            GUI.Label(new Rect(10, 8, 1000, 24), s, debugStyle);
        }

        // ── متن اصلی — بسته به فاز ──
        string mainText = null;
        if (phase == SeqPhase.Locked && persistentHintWhileLocked)
            mainText = hintText;
        else if (phase == SeqPhase.Listening)
            mainText = hintText;
        else if (phase == SeqPhase.Deadline && showDeadlineHint)
            mainText = deadlineHintText;
        else if (showTextUntil > 0f && Time.unscaledTime < showTextUntil)
            mainText = hintText;

        if (mainText == null) return;

        Rect r = new Rect(0, Screen.height * 0.10f, Screen.width, Screen.height * 0.12f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), mainText, shadowStyle);
        GUI.Label(r, mainText, hintStyle);
    }

    private string PhaseLabel()
    {
        switch (phase)
        {
            case SeqPhase.Idle: return "idle";
            case SeqPhase.Locked: return "LOCKED (تا بیسیم)";
            case SeqPhase.Listening: return "LISTEN " + listenElapsed.ToString("0.0") + "/" + listenTarget.ToString("0.0") + "s";
            case SeqPhase.Deadline: return "GEN-" + Mathf.Max(0f, generatorDeadline - deadlineTimer).ToString("0.0") + "s";
            case SeqPhase.Jumpscaring: return "JUMPSCARE";
            default: return "saved ✓";
        }
    }

    private void EnsureStyles()
    {
        if (stylesBuilt) return;
        stylesBuilt = true;

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.alignment = TextAnchor.UpperCenter;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.fontSize = Mathf.Max(26, Screen.height / 16);
        hintStyle.normal.textColor = new Color(1f, 0.28f, 0.22f);

        shadowStyle = new GUIStyle(hintStyle);
        shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);

        debugStyle = new GUIStyle(GUI.skin.label);
        debugStyle.fontSize = 13;
        debugStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
    }

    // ------------------------------------------------------------------
    // فاز ۱ — تریگر

    private void TriggerNow()
    {
        phase = SeqPhase.Locked;
        remindTimer = 0f;
        Debug.Log("[Night3] ✔ تریگر خورد — رسیدن به دوربین " + triggerCameraNumber + " + قفل سوئیچ دوربین");

        // صدا — اگه کلیپ ندی، بوق fallback تا حداقل معلوم شه تریگر خورده
        if (audio2d != null)
        {
            AudioClip clip = triggerSound;
            if (clip == null)
            {
                if (fallbackClip == null) fallbackClip = CreateFallbackSting();
                clip = fallbackClip;
                Debug.LogWarning("[Night3] Trigger Sound خالی بود — بوق fallback پخش شد (برای حس واقعی یه کلیپ خودت بذار).");
            }
            audio2d.PlayOneShot(clip, triggerVolume);
        }

        // متن
        showTextUntil = Time.unscaledTime + Mathf.Max(1f, hintDuration);

        // قفل — فقط سوئیچ بسته میشه؛ خروج از نما (EXAMINE) آزاد می‌مونه
        if (lockCameraSwitching)
        {
            lockIndex = cctv.CurrentCameraIndex;
            cctv.SetSwitchLocked(true);
            Debug.Log("[Night3] سوئیچ دوربین قفل شد روی Index=" + lockIndex + " (CAM " + (lockIndex + 1) + ") — تا گوش دادن کامل به بیسیم.");
        }
    }

    // ------------------------------------------------------------------
    // فاز ۲ — گوش دادن کامل به پیام بیسیم

    // این متد رو از OnStart تعامل بیسیم صدا بزن (UnityEvent → + → Night3RadioCheckLock.NotifyRadioChecked)
    public void NotifyRadioChecked()
    {
        // فقط توی «شبِ خودِ رویداد» حساب میشه — بیسیم شب‌های دیگه کاری با این روند نداره
        if (NightManager.CurrentNight != nightNumber) return;

        if (phase == SeqPhase.Idle)
        {
            Debug.Log("[Night3] بیسیم چک شد — ولی رویداد دوربین ۶ هنوز نخورده؛ بعد از تریگر باید دوباره چک کنه.");
            return;
        }

        // وسط گوش دادن / وسط ددلاین / بعد از تموم شدن — بی‌اثر
        if (phase != SeqPhase.Locked) return;

        // طول پیام: کلیپ داری → طول کلیپ | نداری → Message Duration (طول دیالوگ بیسیم)
        listenTarget = (radioMessageClip != null)
            ? Mathf.Max(0.1f, radioMessageClip.length)
            : Mathf.Max(0f, messageDuration);

        if (listenTarget <= 0f)
            Debug.LogWarning("[Night3] نه Radio Message Clip داری نه Message Duration > 0 — گوش دادن آنی شد! " +
                             "یکی از این دو تا رو توی Inspector تنظیم کن.");

        // پخش پیام — ترجیحاً از سورس خود بیسیم (radioVoice)، وگرنه سورس 2D خود اسکریپت
        if (radioMessageClip != null)
        {
            AudioSource src = (radioVoice != null) ? radioVoice : audio2d;
            if (src != null) src.PlayOneShot(radioMessageClip, triggerVolume);
        }

        listenElapsed = 0f;
        phase = SeqPhase.Listening;
        Debug.Log("[Night3] 📻 پیام بیسیم شروع شد — طول " + listenTarget.ToString("0.0") + "s. تا تموم شدنش سوئیچ دوربین قفله.");
    }

    private void FinishListening()
    {
        Debug.Log("[Night3] ✔ پیام بیسیم کامل شنیده شد — سوئیچ دوربین آزاد شد و ددلاین جنراتور شروع شد ("
                  + generatorDeadline.ToString("0") + "s).");

        if (cctv != null && lockCameraSwitching)
            cctv.SetSwitchLocked(false);

        phase = SeqPhase.Deadline;
        deadlineTimer = 0f;
        deadlineWarned = false;

        OnListenFinished?.Invoke();

        // جنراتور از قبل خاموشه؟ همون قاعده شب ۱: خاموش = امن → نجات فوری
        if (useGeneratorCheck && !genRunning)
        {
            Debug.Log("[Night3] جنراتور از قبل خاموش بود — نجات فوری.");
            GeneratorSaved();
        }
        else if (!useGeneratorCheck)
        {
            // مراقبت جنراتور غیرفعاله — چیزی برای ددلاین نیست، روند همین‌جا موفق تموم میشه
            Debug.Log("[Night3] Use Generator Check خاموشه — روند بدون ددلاین تموم شد.");
            phase = SeqPhase.Done;
        }
    }

    // ------------------------------------------------------------------
    // فاز ۳ — ددلاین جنراتور (همون ایونت‌های Night1ScareEvent)

    private void HandleGeneratorOn() => genRunning = true;

    private void HandleGeneratorOff()
    {
        genRunning = false;
        if (phase == SeqPhase.Deadline)
            GeneratorSaved();
    }

    private void GeneratorSaved()
    {
        if (phase != SeqPhase.Deadline) return;

        phase = SeqPhase.Done;
        Debug.Log("[Night3] ✅ جنراتور خاموش شد — نجات پیدا کرد! رویداد شب ۳ با موفقیت تموم شد.");

        if (audio2d != null && savedClip != null)
            audio2d.PlayOneShot(savedClip, triggerVolume);

        OnGeneratorSaved?.Invoke();
    }

    // ------------------------------------------------------------------
    // فاز ۴ — جامپ‌اسکر و باخت (الگوی Night1ScareEvent — بدون مدل مرد)

    private void DoJumpscare()
    {
        if (phase == SeqPhase.Jumpscaring) return;
        phase = SeqPhase.Jumpscaring;

        // اگه وسط نمای دوربینه، اول بیار بیرون (مثل شب ۱)
        if (cctv != null)
        {
            cctv.SetSwitchLocked(false);
            if (cctv.IsCameraView)
                cctv.SwitchBack();
        }

        if (audio2d != null && screamClip != null)
            audio2d.PlayOneShot(screamClip);

        // افکت‌های جامپ‌اسکر خود کیت (وینیت/تنتاکل/لرزش دوربین) — اگه وصل شده باشه
        if (kitJumpscare != null)
        {
            try
            {
                kitJumpscare.TriggerJumpscare();
                Debug.Log("[Night3] افکت‌های JumpscareTrigger خود کیت فعال شد.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Night3] JumpscareTrigger کیت خطا داد و افکت‌های کیت رد شد — " +
                               "ولی جامپ‌اسکر خودمون و ریست شب ادامه پیدا میکنه. جزئیات: " + ex.Message);
            }
        }

        Debug.Log("[Night3] 💀 جنراتور سر وقت خاموش نشد — جامپ‌اسکر! باخت و ریست شب...");
        OnJumpscare?.Invoke();
        StartCoroutine(JumpscareRoutine());
    }

    private IEnumerator JumpscareRoutine()
    {
        GameManager gm = GameManager.Instance;

        // مطمئن شو پلیر کاملاً فریزه (SwitchBack ممکنه با تأخیر آزادش کنه) — مثل شب ۱
        yield return new WaitForSeconds(0.1f);
        if (gm != null) gm.FreezePlayer(true);

        yield return new WaitForSeconds(0.4f);
        if (gm != null) gm.FreezePlayer(true);

        yield return new WaitForSeconds(Mathf.Max(0.1f, jumpscareDuration - 0.5f));

        // اگه ترنزیشن دیگه‌ای وسط کار بود، صبرش کن
        float wait = 0f;
        while (nightManager != null && nightManager.IsTransitioning && wait < 5f)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        // افکت‌های کیت رو قبل از ریست ببند
        if (kitJumpscare != null)
        {
            try { kitJumpscare.TriggerJumpscareEnded(); }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Night3] بستن افکت کیت خطا داد: " + ex.Message);
            }
        }

        if (nightManager != null)
            nightManager.RestartCurrentNight();
        else
            Debug.LogWarning("[Night3] NightManager پیدا نشد — ریست شب انجام نشد!");
    }

    // ------------------------------------------------------------------

    private void ResetForNewNight()
    {
        if (cctv != null && lockCameraSwitching) cctv.SetSwitchLocked(false);

        phase = SeqPhase.Idle;
        lockIndex = 0;
        listenElapsed = 0f;
        listenTarget = 0f;
        deadlineTimer = 0f;
        deadlineWarned = false;
        remindTimer = 0f;
        showTextUntil = 0f;
        lastSeenIndex = int.MinValue;
        Debug.Log("[Night3] ریست برای شب " + lastKnownNight + " — کل روند (تریگر/گوش دادن/ددلاین) از نو.");
    }

    // بوق fallback — یه sweep نزولی «الارم» که حداقل پلیر بفهمه چیزی خورده
    private AudioClip CreateFallbackSting()
    {
        int freq = 44100;
        float dur = 0.9f;
        int samples = (int)(freq * dur);
        AudioClip clip = AudioClip.Create("Night3FallbackSting", samples, 1, freq, false);

        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / freq;
            float f = 1100f - 500f * (t / dur);          // sweep نزولی
            float env = Mathf.Clamp01(1f - t / dur);     // محو شدن
            env *= env;
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.7f;
        }
        clip.SetData(data, 0);
        return clip;
    }

    // ------------------------------------------------------------------
    // منوهای دیباگ (راست‌کلیک روی کامپوننت توی Inspector)

    [ContextMenu("Debug: Current Camera")]
    private void DebugCurrentCamera()
    {
        if (cctv == null) { Debug.LogWarning("[Night3] CCTV وصل نیست."); return; }
        Debug.Log("[Night3] Index=" + cctv.CurrentCameraIndex + " = CAM " + (cctv.CurrentCameraIndex + 1)
                + " | IsCameraView=" + cctv.IsCameraView
                + " | شب فعلی=" + NightManager.CurrentNight);
    }

    [ContextMenu("Debug: Force Trigger")]
    private void DebugForceTrigger()
    {
        if (cctv == null) { Debug.LogWarning("[Night3] CCTV وصل نیست."); return; }
        if (phase != SeqPhase.Idle) { Debug.Log("[Night3] رویداد این شب قبلاً تریگر شده (فاز: " + phase + ")."); return; }
        if (NightManager.CurrentNight != nightNumber)
            Debug.LogWarning("[Night3] الان شب " + NightManager.CurrentNight + " هستی نه " + nightNumber + " — ولی برای تست، تریگر اجرا میشه.");
        TriggerNow();
    }

    [ContextMenu("Debug: Finish Listen Now")]
    private void DebugFinishListen()
    {
        if (phase != SeqPhase.Listening)
        {
            Debug.LogWarning("[Night3] الان توی فاز گوش دادن نیستی (فاز: " + phase + "). " +
                             "اول با تعامل بیسیم یا Debug: Force Trigger واردش شو.");
            return;
        }
        Debug.Log("[Night3] (دیباگ) گوش دادن فوری تموم شد.");
        FinishListening();
    }

    [ContextMenu("Debug: Force Jumpscare")]
    private void DebugForceJumpscare()
    {
        if (phase == SeqPhase.Jumpscaring) return;
        Debug.LogWarning("[Night3] (دیباگ) جامپ‌اسکر دستی — باخت و ریست شب اجرا میشه.");
        DoJumpscare();
    }

    [ContextMenu("Debug: Generator State")]
    private void DebugGeneratorState()
    {
        Debug.Log("[Night3] جنراتور: " + (generator != null ? generator.name : "NULL!")
                + " | genRunning=" + genRunning
                + " | فاز=" + phase
                + " | از ددلاین " + Mathf.Max(0f, generatorDeadline - deadlineTimer).ToString("0.0") + "s مونده");
    }

    [ContextMenu("Debug: Force Unlock")]
    private void DebugForceUnlock()
    {
        if (cctv != null && lockCameraSwitching) cctv.SetSwitchLocked(false);
        phase = SeqPhase.Done;
        Debug.Log("[Night3] (دیباگ) همه‌چیز دستی آزاد/تموم شد — فاز: Done.");
    }
}
