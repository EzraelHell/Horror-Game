// Night2BreathEvent.cs  (v1.2)
// جدید در v1.2 — رفع «F رو نگه میدارم ولی نوار پر نمیشه»:
//   • تشخیص نشستن ۳ لایه شد:
//       (۱) استیت Crouch از PlayerStateMachine با Reflection — دقیق‌ترین روش
//       (۲) ارتفاع CharacterController — کروچ UHFPS ارتفاع کنترلر رو کم میکنه
//       (۳) ارتفاع دوربین (روش قدیمی v1.0)
//     هر لایه‌ای true بشه «نشسته» حساب میشه — قبلاً فقط ارتفاع دوربین چک می‌شد و
//     اگه کروچ، دوربین رو کمتر از ۰.۴ متر می‌آورد پایین، هیچ‌وقت نشسته حساب نمیشدی!
//   • تایمر Engage Time Limit وقتی پلیر هنوز توی نمای دوربین (CCTV) هست متوقف می‌مونه
//     (چون رویداد همون لحظه‌ی چک کردن آخرین دوربین شروع میشه و پلیر داخل نماست)
//   • اگه کلید رو نگه داری ولی نشسته نباشی، بعد ۲ ثانیه راهنمای نشستن نشون داده میشه
//   • ContextMenu جدید: Debug: Crouch Info — اعداد دقیق رو برای دیباگ چاپ می‌کنه
//   • فیکس کامپایل (CS0246): BindingFlags توی خط ۶۰۹ با اسم کامل System.Reflection.BindingFlags
//     نوشته شد — باگ این بود که بقیه‌ی انواع reflection با اسم کامل بودن ولی این یکی از قلم افتاده بود
// جدید در v1.1 — رفع باگ «شب ۱ رو تموم می‌کنم، می‌رم شب ۲، یهو رادار می‌زنه و برمی‌گردم شب ۱»:
//   • شمارش دوربین‌ها حالا فقط توی «شبِ خودِ رویداد» انجام میشه — چک‌های دوربینِ شب ۱
//     دیگه برای شب ۲ حساب نمیشن! (قبلاً با ریست نرم، ۱۰ دوربینِ چک‌شده‌ی شب ۱ توی حافظه
//     می‌موند و همین که شب ۲ شروع میشد، رویداد فکر می‌کرد همه چک شده → رادار همون لحظه
//     بوق می‌زد → جامپ‌اسکر → برگشت به شب ۱)
//   • هر وقت شماره شب عوض شه (رفتن به شب بعد / برگشت به شب ۱ بعد از باخت) رویداد
//     کاملاً ریست میشه: دوربین‌های چک‌شده صفر، «فقط یک بار در شب» از نو، فاز برمی‌گرده Idle.
//     (قبلاً بعد از باخت با ریست نرم، فاز توی Jumpscaring گیر می‌کرد و دفعه‌ی بعد
//     که به شب ۲ می‌رسیدی، رویداد دیگه هیچ‌وقت فعال نمی‌شد — باگ دوم هم رفع شد)
// رویداد شب ۲ — «حبس نفس»:
//   1) پلیر باید ۱۰ تا دوربین رو چک کنه (هر دوربین حداقل یک بار دیده بشه)
//   2) ناگهان رادار شروع می‌کنه به بوق زدن (RadarAlarmController صحنه روشن میشه)
//   3) پلیر باید روی زمین بشینه (کروچ) — نوار «حبس نفس» ظاهر میشه
//   4) تا وقتی کلید نگه داشته شده، نوار پر میشه (انگار نفس حبس شده)
//   5) اگه کلید رها شه (یا بلند بشه) قبل از پر شدن نوار → جامپ‌اسکر → برگشت به شب ۱
//   6) اگه نوار پر شه → هیولا رد میشه، رادار آروم میشه، رویداد تموم
//
// نصب:
//   1) یه GameObject خالی توی صحنه بساز (مثلاً "Night2Event") و این اسکریپت رو بنداز.
//   2) فیلد CCTV رو بکش (همون CCTV_CameraSystem صحنه).
//   3) فیلد Monster رو با مدل هیولا پر کن (ترجیحاً غیرفعال توی صحنه باشه — موقع باخت
//      خودش فعال و جلوی صورت پلیر تلپورت میشه).
//   4) رادار لازم نیست وصل کنی — خودش پیدا میشه (همون RadarAlarmController شب ۱).
//   5) پیش‌فرض‌ها: کلید نگه‌داشتن = F، مدت حبس نفس = ۶ ثانیه، مهلت نشستن = ۱۵ ثانیه.
//
// نکته‌ها:
//   • «نشستن» با ارتفاع دوربین پلیر تشخیص داده میشه (کروچ = دوربین میاد پایین).
//     اگه درست تشخیص نداد، ارتفاع ایستاده دوربین رو دستی توی Stand Camera Height بذار.
//   • اگه پلیر هیچ کاری نکنه، بعد از Engage Time Limit ثانیه جامپ‌اسکر میشه (فرار ممکن نیست!)
//   • بعد از باخت: NightManager.CurrentNight = 1 میشه و شب ۱ از نو شروع میشه.
//   • وسط رویداد، دکمه ثبت گزارش (NightManager) خودکار قفله.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;

public class Night2BreathEvent : MonoBehaviour
{
    private enum Phase { Idle, Alert, Holding, Jumpscaring, Done }

    [Header("ارجاع‌ها")]
    [Tooltip("سیستم دوربین‌های صحنه (CCTV_CameraSystem)")]
    public CCTV_CameraSystem cctv;

    [Tooltip("مدل هیولا — موقع باخت جلوی صورت پلیر تلپورت میشه (خالی = جامپ‌اسکر بدون تلپورت)")]
    public GameObject monster;

    [Tooltip("انیماتور هیولا (برای تریگر جیغ — اختیاری)")]
    public Animator monsterAnimator;

    [Tooltip("اسم تریگر جیغ توی انیماتور هیولا")]
    public string screamTrigger = "";

    [Tooltip("منبع صدای جامپ‌اسکر (خالی باشه یکی روی همین آبجکت ساخته میشه)")]
    public AudioSource eventAudio;

    [Tooltip("کلیپ صدای جامپ‌اسکر/جیغ")]
    public AudioClip screamClip;

    [Header("شمارش دوربین‌ها (شرط شروع)")]
    [Tooltip("رویداد بعد از چک شدن چند دوربین (دیدن حداقل یک بار) شروع شه؟")]
    public int camerasToTrigger = 10;

    [Tooltip("شماره شب این رویداد")]
    public int nightNumber = 2;

    [Tooltip("فقط یک بار در هر شب؟")]
    public bool oncePerNight = true;

    [Header("رادار")]
    [Tooltip("خالی باشه خودکار توی صحنه پیدا میشه — شروع رویداد = بوق، پایان = برگشت به ملایم")]
    public RadarAlarmController radar;

    [Header("مکانیک حبس نفس")]
    [Tooltip("کلیدی که باید نگه داشته شه")]
    public KeyCode holdKey = KeyCode.F;

    [Tooltip("چند ثانیه طول بکشه نوار پر شه؟")]
    public float fillDuration = 6f;

    [Tooltip("چند ثانیه مهلت داری بشینی و کلید رو بگیری؟ (۰ = بی‌نهایت) — بعدش جامپ‌اسکر")]
    public float engageTimeLimit = 15f;

    [Tooltip("بلند شدن وسط حبس نفس هم مثل رها کردن کلیده؟")]
    public bool standingCountsAsFail = true;

    [Tooltip("تایمر مهلت (Engage) وقتی پلیر هنوز توی نمای دوربینه متوقف بمونه؟")]
    public bool pauseTimerInCameraView = true;

    [Header("راهنمای نشستن")]
    [Tooltip("اگه کلید رو نگه داری ولی نشسته نباشی، راهنما نشون داده شه؟")]
    public bool showCrouchHint = true;

    [Tooltip("چند ثانیه بعد از نگه داشتن کلید بدون نشستن، راهنما نشون داده شه؟")]
    public float crouchHintDelay = 2f;

    [TextArea] public string crouchHintText = "I need to crouch down...";

    [Header("تشخیص نشستن")]
    [Tooltip("دوربین چقدر بیاد پایین‌تر از حالت ایستاده تا «نشسته» حساب شه؟ (متر)")]
    public float crouchHeightThreshold = 0.4f;

    [Tooltip("ارتفاع ایستاده‌ی دوربین پلیر (متر، جهانی) — ۰ = خودکار موقع شروع ثبت میشه")]
    public float standingCameraHeight = 0f;

    [Header("جامپ‌اسکر باخت")]
    [Tooltip("فاصله هیولا از صورت پلیر موقع تلپورت (متر)")]
    public float jumpscareDistance = 0.9f;

    [Tooltip("چند ثانیه جامپ‌اسکر بمونه بعدش شب ۱ شروع شه؟")]
    public float jumpscareDuration = 1.8f;

    [Tooltip("چند ثانیه نگاه اجباری به هیولا باشه")]
    public float forceLookDuration = 1.4f;

    [Tooltip("سرعت چرخش اجباری دوربین به سمت هیولا")]
    public float lookSnapSpeed = 12f;

    [Tooltip("لایه‌های کف برای پیدا کردن زمین زیر نقطه تلپورت هیولا")]
    public LayerMask groundMask = ~0;

    [Tooltip("ارتفاع سر هیولا برای نگاه کردن (متر بالای pivot)")]
    public float monsterLookHeight = 1.5f;

    [Header("نوار حبس نفس (UI خودکار)")]
    [Tooltip("متن راهنما موقع انتظار — {key} جاش اسم کلید میشه")]
    public string labelCrouchText = "CROUCH DOWN & HOLD {key} !";

    [Tooltip("متن راهنما وسط حبس نفس")]
    public string labelHoldText = "HOLD YOUR BREATH...";

    [Tooltip("فونت TMP برای متن (اگه فارسی نوشتی، فونت فارسی مثل Vazirmatn بده)")]
    public TMP_FontAsset barFont;

    [Tooltip("عرض نوار نسبت به عرض صفحه (۰ تا ۱)")]
    [Range(0.1f, 0.9f)] public float barWidthFraction = 0.38f;

    [Tooltip("ارتفاع نوار (پیکسل)")]
    public float barHeightPx = 26f;

    [Tooltip("ارتفاع نوار از پایین صفحه (۰ = پایین، ۱ = بالا)")]
    [Range(0f, 1f)] public float barYAnchor = 0.35f;

    public Color barBackgroundColor = new Color(0f, 0f, 0f, 0.7f);
    public Color barFillColor = new Color(0.9f, 0.2f, 0.15f, 1f);
    public Color labelColor = Color.white;

    [Header("ایونت‌ها (اختیاری)")]
    public UnityEvent OnEventStarted;
    public UnityEvent OnSurvived;
    public UnityEvent OnFailed;

    // ------------------ Runtime ------------------
    private Phase phase = Phase.Idle;
    private int lastKnownNight = 1;   // v1.1: برای تشخیص «عوض شدن شب»
    private bool usedThisNight;
    private readonly HashSet<int> checkedCams = new HashSet<int>();
    private float phaseTime;
    private float holdProgress;

    private Camera playerCam;
    private float standingY;
    private bool standingCaptured;
    private RadarAlarmController cachedRadar;

    // v1.2: تشخیص نشستن چندلایه
    private CharacterController playerCC;
    private float standingCCHeight;
    private bool ccCaptured;
    private PlayerStateMachine cachedSM;
    private bool stateProbeFailed;
    private string lastStateName;
    private float fHeldWithoutCrouch;
    private bool crouchHintShown;
    private System.Reflection.MethodInfo hintMethod;
    private bool hintChecked;

    // UI
    private CanvasGroup barGroup;
    private RectTransform fillRect;
    private TMP_Text label;
    private bool uiBuilt;

    // ------------------ Unity ------------------

    private void Awake()
    {
        if (eventAudio == null)
        {
            eventAudio = gameObject.AddComponent<AudioSource>();
            eventAudio.playOnAwake = false;
            eventAudio.spatialBlend = 0f;
        }

        BuildUI();
    }

    private void Start()
    {
        // v1.1: شب فعلی رو به عنوان مبنای تشخیص عوض شدن شب ثبت کن
        lastKnownNight = NightManager.CurrentNight;

        // دوربین پلیر رو همون اول ثبت کن (قبل از هر رفتن به نمای CCTV)
        Camera cam = GetPlayerCamera();
        if (cam != null)
        {
            playerCam = cam;
            standingY = (standingCameraHeight > 0.01f) ? standingCameraHeight : cam.transform.position.y;
            standingCaptured = true;
            Debug.Log($"[Night2] ارتفاع ایستاده‌ی دوربین ثبت شد: {standingY:0.00} (کلید حبس نفس: {holdKey})");
        }
        else
        {
            Debug.LogWarning("[Night2] دوربین پلیر پیدا نشد — تشخیص نشستن غیرفعاله! " +
                             "دوربین پلیر باید Tag = MainCamera داشته باشه.");
        }

        // v1.2: ارتفاع ایستاده‌ی CharacterController پلیر رو هم ثبت کن (لایه ۲ تشخیص نشستن)
        CharacterController cc = GetPlayerCC();
        if (cc != null)
        {
            playerCC = cc;
            standingCCHeight = cc.height;
            ccCaptured = true;
            Debug.Log($"[Night2] ارتفاع ایستاده‌ی CharacterController ثبت شد: {standingCCHeight:0.00}");
        }

        // اگه تعداد دوربین‌های صحنه کمتر از حد خواسته بود، خودکار کم کن
        if (cctv != null && cctv.Cameras != null && cctv.Cameras.Length > 0 && camerasToTrigger > cctv.Cameras.Length)
        {
            Debug.LogWarning($"[Night2] Cameras To Trigger ({camerasToTrigger}) بیشتر از تعداد دوربین‌های صحنه " +
                             $"({cctv.Cameras.Length})ه — خودکار کم شد.");
            camerasToTrigger = cctv.Cameras.Length;
        }

        // وسط رویداد، ثبت گزارش قفل شه
        NightManager.AddExternalBlock(IsEventBlocking);
    }

    private void OnDestroy()
    {
        NightManager.RemoveExternalBlock(IsEventBlocking);
    }

    private void Update()
    {
        // v1.1: اگه شماره شب عوض شد (شب بعد / برگشت به شب ۱) — همه‌چیز از نو
        if (NightManager.CurrentNight != lastKnownNight)
        {
            lastKnownNight = NightManager.CurrentNight;
            ResetForNewNight();
        }

        switch (phase)
        {
            case Phase.Idle:
                CountCameras();
                TryTrigger();
                break;

            case Phase.Alert:
                // v1.2: تایمر مهلت وقتی پلیر هنوز توی نمای دوربین (CCTV) هست متوقف می‌مونه
                bool inCameraView = (pauseTimerInCameraView && cctv != null && cctv.IsCameraView);
                if (!inCameraView)
                    phaseTime += Time.deltaTime;
                if (engageTimeLimit > 0f && phaseTime >= engageTimeLimit)
                {
                    Fail("هیولا پیدات کرد — دیر نشستی و کلید رو نگه نداشتی!");
                    return;
                }
                // v1.2: اگه کلید گرفته ولی نشسته نیست — بعد از crouchHintDelay راهنما بده
                if (Input.GetKey(holdKey) && !IsCrouched())
                {
                    fHeldWithoutCrouch += Time.deltaTime;
                    if (showCrouchHint && !crouchHintShown && fHeldWithoutCrouch >= crouchHintDelay)
                    {
                        crouchHintShown = true;
                        ShowHint(crouchHintText);
                        Debug.Log("[Night2] پلیر کلید رو نگه داشته ولی نشسته نیست — راهنمای نشستن نشون داده شد.");
                    }
                }
                else
                {
                    fHeldWithoutCrouch = 0f;
                }

                if (Input.GetKey(holdKey) && IsCrouched())
                    StartHolding();
                break;

            case Phase.Holding:
                bool held = Input.GetKey(holdKey);
                bool crouched = IsCrouched();

                if (!held)
                {
                    Fail("نفس رو رها کردی!");
                    return;
                }
                if (standingCountsAsFail && !crouched)
                {
                    Fail("بلند شدی!");
                    return;
                }

                holdProgress = Mathf.Clamp01(holdProgress + Time.deltaTime / Mathf.Max(0.1f, fillDuration));
                UpdateBar();

                if (holdProgress >= 1f)
                    Succeed();
                break;
        }
    }

    // ------------------ فازها ------------------

    // v1.1: با عوض شدن شب، رویداد کاملاً از نو آماده میشه
    private void ResetForNewNight()
    {
        checkedCams.Clear();
        usedThisNight = false;
        holdProgress = 0f;
        phaseTime = 0f;
        fHeldWithoutCrouch = 0f;
        crouchHintShown = false;

        // اگه وسط رویداد بودیم (بوق روشن / نوار باز)، تمیز تمیز ببندش
        if (phase == Phase.Alert || phase == Phase.Holding)
        {
            RadarAlarmController r = EnsureRadar();
            if (r != null) r.SetAlarm(false);
            HideBar();
            Debug.Log("[Night2] شب عوض شد — رویداد حبس نفس لغو و ریست شد.");
        }

        // حتی اگه فاز Jumpscaring گیر کرده بود هم برگرده به Idle
        // (تا دفعه‌ی بعدی که به شب ۲ می‌رسیم، رویداد دوباره کار کنه)
        phase = Phase.Idle;
        Debug.Log($"[Night2] 🔄 ریست برای شروع شب {lastKnownNight} — شمارش دوربین‌ها از صفر.");
    }

    private void CountCameras()
    {
        // v1.1: فقط دوربین‌های «همین شب» شمرده میشن — چک‌های شب قبل حساب نمیشن!
        if (NightManager.CurrentNight != nightNumber) return;
        if (cctv == null || !cctv.IsCameraView) return;

        int idx = cctv.CurrentCameraIndex;
        if (idx < 0) return;

        if (checkedCams.Add(idx))
            Debug.Log($"[Night2] دوربین {idx + 1} چک شد — {checkedCams.Count}/{camerasToTrigger}");
    }

    private void TryTrigger()
    {
        if (oncePerNight && usedThisNight) return;
        if (NightManager.CurrentNight != nightNumber) return;
        if (cctv == null) return;

        if (checkedCams.Count >= camerasToTrigger)
            StartAlert();
    }

    private void StartAlert()
    {
        usedThisNight = true;
        phase = Phase.Alert;
        phaseTime = 0f;
        holdProgress = 0f;

        // رادار: بوق!
        RadarAlarmController r = EnsureRadar();
        if (r != null) r.SetAlarm(true);
        else Debug.LogWarning("[Night2] رادار توی صحنه پیدا نشد — بوق پخش نمیشه (RadarAlarmController نداری).");

        ShowBar();
        SetLabel(string.IsNullOrEmpty(labelCrouchText) ? "" : labelCrouchText.Replace("{key}", holdKey.ToString()));

        Debug.Log($"[Night2] 🚨 رادار شروع به بوق زدن کرد! پلیر باید بشینه و کلید {holdKey} رو نگه داره " +
                  $"(مهلت: {(engageTimeLimit > 0f ? engageTimeLimit + "s" : "بی‌نهایت")})");
        OnEventStarted?.Invoke();
    }

    private void StartHolding()
    {
        phase = Phase.Holding;
        phaseTime = 0f;
        holdProgress = 0f;
        fHeldWithoutCrouch = 0f;
        crouchHintShown = false;
        UpdateBar();
        SetLabel(labelHoldText);
        Debug.Log($"[Night2] 😮 حبس نفس شروع شد — تا آخر نگهش دار ({fillDuration:0.#}s)!");
    }

    private void Succeed()
    {
        phase = Phase.Done;
        holdProgress = 1f;
        UpdateBar();

        // رادار: برگشت به صدای ملایم
        RadarAlarmController r = EnsureRadar();
        if (r != null) r.SetAlarm(false);

        HideBar();
        Debug.Log("[Night2] 😮‍💨 نوار پر شد — نفس حبس موند و هیولا رد شد! رادار برگشت روی صدای ملایم.");

        if (!oncePerNight)
            checkedCams.Clear();   // برای تریگر بعدی باید دوباره همه دوربین‌ها چک شن

        OnSurvived?.Invoke();
    }

    private void Fail(string reason)
    {
        if (phase == Phase.Jumpscaring) return;
        phase = Phase.Jumpscaring;

        Debug.Log($"[Night2] 💀 باخت: {reason} جامپ‌اسکر و برگشت به شب ۱...");
        HideBar();
        OnFailed?.Invoke();
        StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
    {
        // پلیر فریز شه
        GameManager gm = GameManager.Instance;
        if (gm != null)
            gm.FreezePlayer(true);

        // اگه داخل نمای دوربینه، اول بیا بیرون
        if (cctv != null)
        {
            cctv.SetControlLocked(false);
            if (cctv.IsCameraView)
            {
                cctv.SwitchBack();
                yield return new WaitForSeconds(0.4f);
            }
        }

        // هیولا جلوی صورت پلیر
        if (monster != null)
            TeleportMonsterInFront();

        if (monsterAnimator != null && !string.IsNullOrEmpty(screamTrigger))
            monsterAnimator.SetTrigger(screamTrigger);

        if (eventAudio != null && screamClip != null)
            eventAudio.PlayOneShot(screamClip);

        // نگاه اجباری به هیولا
        if (monster != null)
            StartCoroutine(ForceLookRoutine());

        yield return new WaitForSecondsRealtime(jumpscareDuration);

        // رادار قطع + برگشت به شب ۱
        RadarAlarmController r = EnsureRadar();
        if (r != null) r.SetAlarm(false);

        NightManager.CurrentNight = 1;

        NightManager nm = FindObjectOfType<NightManager>();
        if (nm != null)
        {
            nm.RestartCurrentNight();
        }
        else
        {
            Debug.LogWarning("[Night2] NightManager توی صحنه نیست — فقط صحنه از نو لود میشه.");
            int idx = SceneManager.GetActiveScene().buildIndex;
            if (idx >= 0) SceneManager.LoadScene(idx);
        }
    }

    private IEnumerator ForceLookRoutine()
    {
        Camera cam = GetPlayerCamera();
        if (cam == null) yield break;

        Vector3 target = monster.transform.position + Vector3.up * monsterLookHeight;
        float t = 0f;

        while (t < forceLookDuration && monster != null)
        {
            t += Time.unscaledDeltaTime;
            Vector3 dir = target - cam.transform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetRot,
                    Mathf.Clamp01(Time.unscaledDeltaTime * lookSnapSpeed));
            }
            yield return null;
        }
    }

    private void TeleportMonsterInFront()
    {
        Camera cam = GetPlayerCamera();
        if (cam == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;

        Vector3 pos = origin + fwd * jumpscareDistance;

        // کف واقعی زیر نقطه اسپاون (مثل شب ۱ — که هیولا توی سقف/زیر کف نره)
        float manY = monster.transform.position.y;
        if (Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 30f, groundMask, QueryTriggerInteraction.Ignore))
            manY = hit.point.y;
        pos.y = manY;

        monster.transform.position = pos;

        Vector3 dir = origin - pos;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            monster.transform.rotation = Quaternion.LookRotation(dir);

        if (!monster.activeSelf) monster.SetActive(true);

        Debug.Log($"[Night2] هیولا جلوی صورت پلیر تلپورت شد (فاصله {jumpscareDistance}م).");
    }

    // ------------------ کمکی ------------------

    private bool IsCrouched()
    {
        // ── v1.2 لایه ۱: استیت Crouch از PlayerStateMachine (UHFPS) — دقیق‌ترین روش ──
        string state = GetCurrentStateName();
        if (!string.IsNullOrEmpty(state) && state.ToLower().Contains("crouch"))
            return true;

        // ── v1.2 لایه ۲: ارتفاع CharacterController — کروچ ارتفاع کنترلر رو کم می‌کنه ──
        CharacterController cc = GetPlayerCC();
        if (cc != null && ccCaptured && standingCCHeight > 0.1f &&
            cc.height <= standingCCHeight - 0.25f)
            return true;

        // ── لایه ۳: ارتفاع دوربین (روش قدیمی v1.0) ──
        Camera cam = GetPlayerCamera();
        if (cam == null) return false;

        if (!standingCaptured)
        {
            standingY = (standingCameraHeight > 0.01f) ? standingCameraHeight : cam.transform.position.y;
            standingCaptured = true;
        }

        return cam.transform.position.y <= standingY - crouchHeightThreshold;
    }

    // v1.2: CharacterController پلیر (برای لایه ۲)
    private CharacterController GetPlayerCC()
    {
        if (playerCC != null) return playerCC;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.PlayerPresence != null && gm.PlayerPresence.StateMachine != null)
            playerCC = gm.PlayerPresence.StateMachine.GetComponent<CharacterController>();

        if (playerCC == null)
        {
            Camera cam = GetPlayerCamera();
            if (cam != null)
                playerCC = cam.GetComponentInParent<CharacterController>();
        }
        return playerCC;
    }

    // v1.2: خوندن استیت فعلی PlayerStateMachine با Reflection (اگه پیدا نشد، لایه‌های بعدی کار می‌کنن)
    private string GetCurrentStateName()
    {
        if (stateProbeFailed) return lastStateName;

        if (cachedSM == null)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.PlayerPresence != null)
                cachedSM = gm.PlayerPresence.StateMachine;
            if (cachedSM == null) return null;
        }

        try
        {
            const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            System.Type type = cachedSM.GetType();

            string[] names = { "CurrentState", "CurrentStateKey", "currentState", "State", "state" };

            foreach (string n in names)
            {
                System.Reflection.PropertyInfo prop = type.GetProperty(n, Flags);
                if (prop != null && prop.CanRead)
                {
                    object v = prop.GetValue(cachedSM, null);
                    if (v is string s && !string.IsNullOrEmpty(s)) { lastStateName = s; return lastStateName; }
                    if (v != null && v.GetType().IsEnum) { lastStateName = v.ToString(); return lastStateName; }
                }

                System.Reflection.FieldInfo fld = type.GetField(n, Flags);
                if (fld != null)
                {
                    object v = fld.GetValue(cachedSM);
                    if (v is string s2 && !string.IsNullOrEmpty(s2)) { lastStateName = s2; return lastStateName; }
                    if (v != null && v.GetType().IsEnum) { lastStateName = v.ToString(); return lastStateName; }
                }
            }

            stateProbeFailed = true;   // هیچ‌کدوم نبود — دیگه دنبالش نگرد
        }
        catch
        {
            stateProbeFailed = true;
        }

        return null;
    }

    // v1.2: نمایش راهنما از طریق GameManager.ShowHintMessage (اگه موجود باشه — مثل NightManager)
    private void ShowHint(string message)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (!hintChecked)
        {
            hintChecked = true;
            hintMethod = gm.GetType().GetMethod("ShowHintMessage",
                new[] { typeof(string), typeof(float) });

            if (hintMethod == null)
                Debug.LogWarning("[Night2] GameManager.ShowHintMessage پیدا نشد — راهنمای متنی نشون داده نمیشه.");
        }

        hintMethod?.Invoke(gm, new object[] { message, 3f });
    }

    [ContextMenu("Debug: Crouch Info (اعداد نشستن)")]
    private void DebugCrouchInfo()
    {
        Camera cam = GetPlayerCamera();
        CharacterController cc = GetPlayerCC();
        float camY = (cam != null) ? cam.transform.position.y : -1f;

        Debug.Log("[Night2] CROUCH DEBUG —\n" +
                  $"  camY = {camY:0.000} | standingY = {standingY:0.000} | drop = {(standingY - camY):0.000} (threshold {crouchHeightThreshold:0.00})\n" +
                  $"  cc.height = {((cc != null) ? cc.height : -1f):0.000} | standingCCHeight = {standingCCHeight:0.000} (need ≤ {(standingCCHeight - 0.25f):0.000})\n" +
                  $"  state = '{lastStateName}' | IsCrouched = {(Application.isPlaying ? IsCrouched().ToString() : "؟ (Play Mode)")}\n" +
                  $"  phase = {phase} | holdKey = {holdKey}");
    }

    private Camera GetPlayerCamera()
    {
        if (playerCam != null) return playerCam;

        playerCam = Camera.main;
        if (playerCam == null)
        {
            Camera[] all = FindObjectsOfType<Camera>();
            foreach (Camera c in all)
            {
                if (c.CompareTag("MainCamera")) { playerCam = c; break; }
            }
        }
        return playerCam;
    }

    private RadarAlarmController EnsureRadar()
    {
        if (cachedRadar == null)
        {
            cachedRadar = radar;
            if (cachedRadar == null)
                cachedRadar = FindObjectOfType<RadarAlarmController>();
        }
        return cachedRadar;
    }

    private bool IsEventBlocking()
    {
        return phase == Phase.Alert || phase == Phase.Holding || phase == Phase.Jumpscaring;
    }

    // ------------------ UI ------------------

    private void BuildUI()
    {
        if (uiBuilt) return;
        uiBuilt = true;

        var canvasGO = new GameObject("Night2BreathCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20000;   // زیر فید سیاه NightManager (32767)

        barGroup = canvasGO.AddComponent<CanvasGroup>();
        barGroup.alpha = 0f;
        barGroup.blocksRaycasts = false;
        barGroup.interactable = false;

        // پس‌زمینه نوار (با anchor — مستقل از رزولوشن)
        var bgGO = new GameObject("BarBackground");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bg = bgGO.AddComponent<UnityEngine.UI.Image>();
        bg.color = barBackgroundColor;
        RectTransform bgRt = bg.rectTransform;
        bgRt.anchorMin = new Vector2(0.5f - barWidthFraction * 0.5f, barYAnchor);
        bgRt.anchorMax = new Vector2(0.5f + barWidthFraction * 0.5f, barYAnchor);
        bgRt.offsetMin = new Vector2(0f, -barHeightPx * 0.5f);
        bgRt.offsetMax = new Vector2(0f, barHeightPx * 0.5f);

        // پرکننده (عرضش با progress کنترل میشه)
        var fillGO = new GameObject("BarFill");
        fillGO.transform.SetParent(bgGO.transform, false);
        var fill = fillGO.AddComponent<UnityEngine.UI.Image>();
        fill.color = barFillColor;
        fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // متن راهنما
        var labelGO = new GameObject("BreathLabel");
        labelGO.transform.SetParent(canvasGO.transform, false);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        if (barFont != null) tmp.font = barFont;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = labelColor;
        RectTransform lRt = tmp.rectTransform;
        lRt.anchorMin = new Vector2(0.5f, barYAnchor);
        lRt.anchorMax = new Vector2(0.5f, barYAnchor);
        lRt.pivot = new Vector2(0.5f, 0f);
        lRt.sizeDelta = new Vector2(800f, 44f);
        lRt.anchoredPosition = new Vector2(0f, 10f);
        label = tmp;
        labelGO.SetActive(false);
    }

    private void UpdateBar()
    {
        if (fillRect != null)
            fillRect.anchorMax = new Vector2(holdProgress, 1f);
    }

    private void SetLabel(string text)
    {
        if (label == null) return;
        if (string.IsNullOrEmpty(text))
        {
            label.gameObject.SetActive(false);
            return;
        }
        label.text = text;
        label.gameObject.SetActive(true);
    }

    private void ShowBar() { if (barGroup != null) barGroup.alpha = 1f; }

    private void HideBar()
    {
        if (barGroup != null) barGroup.alpha = 0f;
        if (label != null) label.gameObject.SetActive(false);
    }

    // ------------------ تست و دیباگ ------------------

    [ContextMenu("Debug: Status (وضعیت رویداد)")]
    private void DebugStatus()
    {
        RadarAlarmController r = EnsureRadar();
        Debug.Log($"[Night2] phase = {phase} | دوربین‌های چک‌شده: {checkedCams.Count}/{camerasToTrigger}" +
                  $" | کلید = {holdKey} | نشسته = {(Application.isPlaying ? IsCrouched().ToString() : "؟ (Play Mode)")}" +
                  $" | رادار = {(r != null ? "پیدا شد ✅" : "پیدا نشد ❌")}" +
                  $" | Monster = {(monster != null ? monster.name : "خالی")}" +
                  $" | Night فعلی = {NightManager.CurrentNight}");
    }

    [ContextMenu("Test: Start Alert Now (شروع دستی بوق)")]
    private void TestStartAlert()
    {
        if (!Application.isPlaying) { Debug.Log("[Night2] این تست رو توی Play Mode اجرا کن."); return; }
        phase = Phase.Idle;
        usedThisNight = false;
        StartAlert();
    }

    [ContextMenu("Test: Fail Jumpscare (تست باخت)")]
    private void TestFail()
    {
        if (!Application.isPlaying) { Debug.Log("[Night2] این تست رو توی Play Mode اجرا کن."); return; }
        Fail("تست دستی");
    }
}
