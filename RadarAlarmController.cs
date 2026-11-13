// RadarAlarmController.cs  (v1.2)
// رادار: صدای خیلی ملایم در حالت عادی ↔ اژیر وقتی هیولا دیده میشه
//
// v1.2 — شکار کاملِ «اژیر از لحظه‌ی شروع»:
//   • اسکن کل صحنه موقع شروع بازی (نه فقط رادار): هر سورسی که در حال پخش کلیپ اژیر باشه یا
//     Play On Awake + کلیپ اژیر داشته باشه → خودکار خاموش + لاگ 🚨
//   • گزارش کامل صدای صحنه در Console: همه‌ی سورس‌هایی که صدا پخش می‌کنن یا Play On Awake دارن
//     با مسیر/کلیپ/ولوم نوشته میشن → عکسش بفرست تا دقیق بگم اژیر از کجاست
//   • کلید اضطراری forceSoftOnly: اژیر کلاً غیرفعال میشه — اگه با این تیک بازم اژیر شنیدی،
//     یعنی ۱۰۰٪ از یه سورس بیرونیه و گزارش بالا اسمش رو میاره
//   • راست‌کلیک → «Swap»: اگه کلیپ‌ها رو برعکس گذاشتی، با یه کلیک جابه‌جا میشن
//
// v1.1 — فیکس قبلی:
//   • Play On Awake روی هر سورسی که این اسکریپت استفاده می‌کنه (حتی سورس قدیمی خودت) خاموش میشه
//   • سورس‌های بیگانه روی رادار/فرزندهاش که Play On Awake دارن یا در حال پخشن خاموش میشن
//   • کلیپ اژیر روی سورسِ ملایم → خودش برمیداره + هشدار فارسی
//   • منوی دیباگ: راست‌کلیک → «Debug: Radar Audio Status (وضعیت صداها)»
//
// نصب:
//   1) این اسکریپت رو روی آبجکت «رادار» توی صحنه بنداز.
//   2) اگه روی همون آبجکت از قبل AudioSource گذاشتی، خودکار همون استفاده میشه
//      (ولی Play On Awake و Loop و ولومش رو خود اسکریپت کنترل می‌کنه).
//   3) فیلد Soft Clip = صدای خیلی ملایم (مثلاً پینگ سونار) — فیلد Alarm Clip = اژیر.
//      حواست باشه برعکس نندازی! (برعکس بود → راست‌کلیک → Swap)
//   4) سورس اژیر رو خودش می‌سازه (فرزند به اسم RadarAlarmSource).
//   5) نیازی به وصل کردن دستی به Night1ScareEvent نیست — خودش پیدا میشه.
//
// منطق:
//   • هیولا توی کادر CAM افتاد و رویداد شروع شد → اژیر (فید این) + صدای ملایم (فید اوت)
//   • هیولا غیب شد / جامپ‌اسکر / ریست شب → همه‌چی برمی‌گرده به صدای ملایم
//   • تعویض‌ها فیددارن (Fade Duration) تا پرش صدا نداشته باشیم

using UnityEngine;

[DisallowMultipleComponent]
public class RadarAlarmController : MonoBehaviour
{
    [Header("صدای ملایم رادار (حالت عادی)")]
    [Tooltip("خالی باشه = AudioSource خودِ این آبجکت استفاده میشه (اگه نباشه یکی ساخته میشه)")]
    public AudioSource softSource;

    [Tooltip("کلیپ صدای ملایم (مثلاً پینگ سونار) — خالی باشه = کلیپ فعلیِ سورس دست نمی‌خوره")]
    public AudioClip softClip;

    [Tooltip("ولوم صدای ملایم (چون باید خیلی آروم باشه، پیش‌فرض 0.25)")]
    [Range(0f, 1f)] public float softVolume = 0.25f;

    [Header("اژیر (وقتی هیولا دیده شد)")]
    [Tooltip("خالی باشه = خودکار یه سورس 2D به اسم RadarAlarmSource روی همین آبجکت ساخته میشه")]
    public AudioSource alarmSource;

    [Tooltip("کلیپ اژیر (لوپ‌پذیر باشه بهتره) — اژیر باید توی این فیلد بره، نه توی Soft Clip!")]
    public AudioClip alarmClip;

    [Range(0f, 1f)] public float alarmVolume = 0.85f;

    [Header("نرمی تعویض")]
    [Tooltip("سرعت فید بین ملایم ↔ اژیر (ثانیه)")]
    [Range(0f, 3f)] public float fadeDuration = 0.6f;

    [Header("عیب‌یابی خودکار (v1.1)")]
    [Tooltip("اگه روشن باشه، هر AudioSource دیگه‌ای روی رادار/فرزندهاش که Play On Awake داره یا داره صدا پخش می‌کنه، موقع شروع بازی خاموش میشه")]
    public bool silenceForeignSources = true;

    [Header("شکار اژیر سرگردان (v1.2)")]
    [Tooltip("موقع شروع بازی کل صحنه اسکن میشه: هر سورسی که کلیپ اژیر رو پخش کنه خاموش میشه + گزارش همه‌ی صداهای روشن توی Console میاد")]
    public bool sirenHuntWholeScene = true;

    [Tooltip("اگه روشن باشه اژیر هیچ‌وقت پخش نمیشه (فقط صدای ملایم) — برای تست اینکه اژیر از کجا میاد. بعد از حل مشکل تیکش رو بردار!")]
    public bool forceSoftOnly = false;

    private bool alarmOn;
    private bool initialized;
    private bool warningsShown;   // هشدارها فقط یک بار توی هر اجرا
    private bool forceSoftLogged; // لاگ یک‌باره‌ی کلید اضطراری

    // ------------------ Unity ------------------

    private void Awake()
    {
        EnsureSources();
        initialized = true;
        ApplyImmediate();   // شروع: صدای ملایم روشن، اژیر خاموش — حتی اگه Play On Awake سورسی رو زودتر روشن کرده باشه
    }

    private void OnEnable()
    {
        if (initialized) ApplyImmediate();
    }

    private void Update()
    {
        float k = (fadeDuration <= 0.001f) ? 1f : Mathf.Clamp01(Time.deltaTime / fadeDuration);

        float softTarget = alarmOn ? 0f : softVolume;
        float alarmTarget = alarmOn ? alarmVolume : 0f;

        softSource.volume = Mathf.Lerp(softSource.volume, softTarget, k);
        alarmSource.volume = Mathf.Lerp(alarmSource.volume, alarmTarget, k);

        // پخش/توقف هوشمند — سورسی که کاملاً بی‌صده متوقف میشه
        if (!softSource.isPlaying && (softTarget > 0f || softSource.volume > 0.001f))
            softSource.Play();
        if (softTarget <= 0f && softSource.volume <= 0.001f && softSource.isPlaying)
            softSource.Stop();

        if (!alarmSource.isPlaying && (alarmTarget > 0f || alarmSource.volume > 0.001f))
            alarmSource.Play();
        if (alarmTarget <= 0f && alarmSource.volume <= 0.001f && alarmSource.isPlaying)
            alarmSource.Stop();
    }

    // ------------------ API عمومی ------------------

    /// <summary>اژیر روشن/خاموش (فیددار) — Night1ScareEvent خودش موقع رویداد صداش می‌زنه.</summary>
    public void SetAlarm(bool on)
    {
        if (on && forceSoftOnly)
        {
            if (!forceSoftLogged)
            {
                forceSoftLogged = true;
                Debug.LogWarning("[Radar] 🚫 درخواست اژیر نادیده گرفته شد — چون تیک forceSoftOnly فعاله (بعد از حل مشکل بردارش)");
            }
            return;
        }

        if (alarmOn == on) return;
        alarmOn = on;
        Debug.Log($"[Radar] {(on ? "🚨 اژیر روشن شد (هیولا دیده شد!)" : "📻 اژیر قطع شد — برگشت به صدای ملایم (هیولا رفت)")}");
    }

    /// <summary>وضعیت فعلی اژیر.</summary>
    public bool IsAlarming => alarmOn;

    // ------------------ منوها ------------------

    [ContextMenu("Test: Alarm ON (تست اژیر)")]
    private void TestAlarmOn()
    {
        if (!Application.isPlaying) { Debug.Log("[Radar] این تست رو توی Play Mode اجرا کن (دکمه‌ی Play بالای یونیتی)."); return; }
        EnsureSources(); SetAlarm(true);
    }

    [ContextMenu("Test: Alarm OFF (تست برگشت به ملایم)")]
    private void TestAlarmOff()
    {
        if (!Application.isPlaying) { Debug.Log("[Radar] این تست رو توی Play Mode اجرا کن (دکمه‌ی Play بالای یونیتی)."); return; }
        EnsureSources(); SetAlarm(false);
    }

    [ContextMenu("Swap: جابه‌جایی Soft/Alarm (اگه برعکس گذاشتی)")]
    private void SwapClips()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Swap Radar Clips");
#endif
        AudioClip tmp = softClip;
        softClip = alarmClip;
        alarmClip = tmp;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[Radar] 🔄 جابه‌جا شد! Soft Clip = {(softClip != null ? softClip.name : "خالی")} | " +
                  $"Alarm Clip = {(alarmClip != null ? alarmClip.name : "خالی")} — اگه هنوز برعکسه، دوباره Swap بزن.");
    }

    [ContextMenu("Debug: Radar Audio Status (وضعیت صداها)")]
    private void DebugAudioStatus()
    {
        Debug.Log($"[Radar] ───── وضعیت فعلی: alarmOn = {alarmOn} | forceSoftOnly = {forceSoftOnly} ─────");
        LogSourceState("ملایم (Soft)", softSource != null ? softSource : GetComponent<AudioSource>());
        AudioSource alarm = alarmSource;
        if (alarm == null)
        {
            Transform existing = transform.Find("RadarAlarmSource");
            if (existing != null) alarm = existing.GetComponent<AudioSource>();
        }
        LogSourceState("اژیر (Alarm)", alarm);

        // سورس‌های بیگانه: هر صدا دیگه‌ای که از رادار میاد ولی مالِ این اسکریپت نیست
        AudioSource[] children = GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource s in children)
        {
            if (s == softSource || s == alarmSource) continue;
            LogSourceState($"بیگانه روی «{ObjectPath(s.transform)}» ⚠️", s);
        }
        Debug.Log("[Radar] ───── اگه صدای اضافه می‌شنوی، سورس «بیگانه» بالا مقصره ─────");
    }

    private void LogSourceState(string title, AudioSource src)
    {
        if (src == null)
        {
            Debug.Log($"[Radar] 🔇 {title}: سورس وجود نداره (سورس‌های خودکار فقط توی Play Mode ساخته میشن).");
            return;
        }
        string clipName = (src.clip != null) ? src.clip.name : "هیچ کلیپی نداره!";
        Debug.Log($"[Radar] 🎵 {title}: کلیپ = «{clipName}» | volume = {src.volume:F2} | isPlaying = {src.isPlaying}" +
                  $" | Play On Awake = {src.playOnAwake} | Loop = {src.loop}");
    }

    private string ObjectPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return ObjectPath(t.parent) + "/" + t.name;
    }

    // ------------------ داخلی ------------------

    private void EnsureSources()
    {
        // صدای ملایم: اول سورس خود آبجکت
        if (softSource == null)
        {
            softSource = GetComponent<AudioSource>();
            if (softSource == null)
            {
                softSource = gameObject.AddComponent<AudioSource>();
                softSource.spatialBlend = 0f;   // 2D — همه‌جا شنیده میشه
            }
        }

        // هر سورسی که این اسکریپت مدیریت می‌کنه (حتی سورس قدیمی خودت) دستِ خودشه:
        softSource.loop = true;
        softSource.playOnAwake = false;

        // سورس اژیر: فرزند جدا که با سورس اصلی قاطی نشه
        if (alarmSource == null)
        {
            Transform existing = transform.Find("RadarAlarmSource");
            if (existing != null)
                alarmSource = existing.GetComponent<AudioSource>();

            if (alarmSource == null)
            {
                GameObject go = new GameObject("RadarAlarmSource");
                go.transform.SetParent(transform, false);
                alarmSource = go.AddComponent<AudioSource>();
                alarmSource.spatialBlend = 0f;
            }
        }
        alarmSource.loop = true;
        alarmSource.playOnAwake = false;   // اژیر فقط با SetAlarm روشن میشه

        if (softClip != null && softSource.clip != softClip) softSource.clip = softClip;
        if (alarmClip != null && alarmSource.clip != alarmClip) alarmSource.clip = alarmClip;

        // ترمیم خودکار: کلیپ اژیر نباید روی سورسِ «ملایم» باشه
        if (softClip == null && alarmClip != null && softSource.clip == alarmClip)
        {
            softSource.clip = null;
            Debug.LogWarning("[Radar] ⚠️ کلیپ اژیر روی سورسِ «ملایم» بود! خودم برداشتمش. " +
                             "حالا فیلد Soft Clip رو با صدای ملایم (مثلاً پینگ سونار) پر کن و کلیپ اژیر رو بنداز توی Alarm Clip.");
        }

        // سورس‌های بیگانه‌ی روی رادار که از قبل روشنن یا Play On Awake دارن → خاموش
        if (silenceForeignSources)
            SilenceForeignSources();

        // v1.2 — اسکن کل صحنه: اژیر سرگردان + گزارش صداهای روشن
        if (sirenHuntWholeScene)
            SirenHuntWholeScene();

        WarnIfSetupWrong();
    }

    private void SilenceForeignSources()
    {
        AudioSource[] all = GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource s in all)
        {
            if (s == softSource || s == alarmSource) continue;
            if (s.playOnAwake || s.isPlaying)
            {
                Debug.LogWarning($"[Radar] 🔇 AudioSource بیگانه روی «{ObjectPath(s.transform)}» روشن بود " +
                                 "(Play On Awake یا در حال پخش) — خاموشش کردم چون باعث صدای اضافه از لحظه‌ی شروع میشه. " +
                                 "اگه این سورس رو عمداً گذاشته بودی، تیک Silence Foreign Sources رو بردار.");
                s.playOnAwake = false;
                s.Stop();
            }
        }
    }

    private void SirenHuntWholeScene()
    {
#if UNITY_2020_1_OR_NEWER
        AudioSource[] all = FindObjectsOfType<AudioSource>(true);
#else
        AudioSource[] all = FindObjectsOfType<AudioSource>();
#endif
        int reported = 0;
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("[Radar] 🔎 گزارش صدای شروع صحنه (سورس‌هایی که پخش می‌کنن یا Play On Awake دارن):");

        foreach (AudioSource s in all)
        {
            bool isOurs = (s == softSource || s == alarmSource);
            bool active = s.isPlaying || s.playOnAwake;
            if (!active) continue;
            reported++;

            string path = ObjectPath(s.transform);
            string clipName = (s.clip != null) ? s.clip.name : "بدون کلیپ";

            // اژیر سرگردان: سورسی بیرون از رادار که دقیقاً کلیپ اژیر ما رو پخش می‌کنه → خاموشش کن
            if (!isOurs && alarmClip != null && s.clip == alarmClip)
            {
                s.playOnAwake = false;
                s.Stop();
                Debug.LogWarning($"[Radar] 🚨 سورس اژیر سرگردان روی «{path}» پیدا و خاموش شد — همین باعث اژیر از لحظه‌ی شروع میشد! " +
                                 "(اگه عمداً اونجا گذاشته بودیش، تیک Siren Hunt Whole Scene رو بردار)");
                continue;
            }

            string tag = isOurs ? "✅ مالِ رادار" : "⚠️ خارج از رادار";
            report.AppendLine($"[Radar]   • «{path}» | کلیپ: {clipName} | پخش: {s.isPlaying}" +
                              $" | PlayOnAwake: {s.playOnAwake} | ولوم: {s.volume:F2} | {tag}");
        }

        if (reported == 0)
            report.AppendLine("[Radar]   (هیچ سورسی در حال پخش یا Play-On-Awake نیست)");
        Debug.Log(report.ToString());
    }

    private void WarnIfSetupWrong()
    {
        if (warningsShown || !Application.isPlaying) return;   // فقط توی Play Mode و فقط یک بار
        warningsShown = true;

        if (forceSoftOnly)
            Debug.LogWarning("[Radar] 🚫 کلید اضطراری forceSoftOnly فعاله — اژیر عمداً غیرفعاله. بعد از حل مشکل تیکش رو بردار.");

        if (softClip == null && softSource.clip == null)
            Debug.LogWarning("[Radar] ⚠️ هیچ کلیپ ملایمی ست نشده — فیلد Soft Clip خالیه! صدای ملایم رادار رو بنداز توش.");

        if (alarmClip == null && alarmSource.clip == null)
            Debug.LogWarning("[Radar] ⚠️ فیلد Alarm Clip خالیه — اژیر صدا نداره! کلیپ اژیر رو بنداز توی Alarm Clip " +
                             "(حواست باشه کلیپ‌ها برعکس نشن: ملایم → Soft Clip، اژیر → Alarm Clip).");

        if (softClip != null && alarmClip != null && softClip == alarmClip)
            Debug.LogWarning("[Radar] ⚠️ Soft Clip و Alarm Clip یکی هستن! دو تا صدای متفاوت بذار.");
    }

    private void ApplyImmediate()
    {
        softSource.volume = alarmOn ? 0f : softVolume;
        alarmSource.volume = alarmOn ? alarmVolume : 0f;

        // فقط سورسِ همون حالت اجازه‌ی پخش داره؛ اژیری که Play On Awake زودتر روشنش کرده بود همینجا کشته میشه
        if (alarmOn)
        {
            if (!alarmSource.isPlaying) alarmSource.Play();
            if (softSource.isPlaying) softSource.Stop();
        }
        else
        {
            if (alarmSource.isPlaying) alarmSource.Stop();
            if (!softSource.isPlaying && softSource.clip != null) softSource.Play();
        }
    }
}
