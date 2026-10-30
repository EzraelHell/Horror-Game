// NightManager.cs  (v1.2)
// جدید در v1.2 — باخت = ریست کامل:
//   اگه Reload Scene تیک بخوره، باخت (جامپ‌اسکر) هم مثل شب بعدی کل صحنه رو از نو لود می‌کنه
//   = درها، چراغ‌ها، جنراتور (وضعیت + سوخت)، بیسیم، مرد ترسناک... همه دقیقاً برمی‌گردن به حالت اول.
//   (اگه صحنه توی Build Settings نباشه، خودکار به ریست نرم برمی‌گرده و توی Console هشدار میده.)
// سیستم شب‌ها: هر شب پلیر باید بیسیم و دوربین‌ها رو چک کنه → بعد دکمه ثبت گزارش رو بزنه
// → صفحه سیاه میشه → میریم شب بعدی (تا شب آخر).
//
// نصب:
//   1) یه GameObject خالی (مثلاً "NightManager") بساز و این اسکریپت رو بنداز.
//   2) radioGate رو بکش روی همون RadioTVGate موجود صحنه.
//   3) دکمه ثبت گزارش (CustomInteractEvent → OnStart) → NightManager.TrySubmitReport
//   4) ایونت OnTVUnlocked (روی RadioTVGate) → NightManager.MarkTVUse
//      (ایونت سوئیچ دوربین‌ها هم می‌تونی به MarkTVUse وصل کنی تا شمارش دقیق‌تر شه)
//   5) تمام — بعد از ثبت گزارش: صفحه سیاه → اسم شب بعد → شب جدید.
//
// نکته: شماره شب توی NightManager.CurrentNight (استاتیک) نگه داشته میشه
//       و از اسکریپت‌های دیگه هم قابل خوندنه.

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UHFPS.Runtime;

public class NightManager : MonoBehaviour
{
    // شماره شب فعلی — استاتیک تا بعد از reload صحنه هم حفظ بشه
    public static int CurrentNight = 1;

    // v1.1: قفل خارجی گزارش — اسکریپت‌های سناریو (مثل Night1ScareEvent) وسط رویداد
    // می‌تونن گزارش رو موقتاً ببندن. true برگردونه = گزارش فعلاً غیرفعال.
    public static System.Func<bool> ExternalBlockCheck;

#if UNITY_EDITOR
    // جلوگیری از باگ مقدار استاتیک بین دو بار Play در ادیتور
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        CurrentNight = 1;
        ExternalBlockCheck = null;   // v1.1
    }
#endif

    [Header("Nights")]
    [Tooltip("بازی چند شب داره؟")]
    public int totalNights = 5;

    [Header("Requirements (شرط ثبت گزارش)")]
    [Tooltip("بیسیم باید کامل شنیده شده باشه (از RadioTVGate خونده میشه)")]
    public bool requireRadio = true;

    [Tooltip("اسکریپت RadioTVGate موجود توی صحنه")]
    public RadioTVGate radioGate;

    [Tooltip("حداقل چند بار باید TV استفاده شده باشه (روشن کردن = ۱ بار)")]
    public int requiredTVUseCount = 1;

    [Header("Report Blocked Feedback")]
    public bool useHint = true;
    [TextArea] public string notReadyHint = "I should check everything first...";
    public float hintDuration = 3f;
    [Tooltip("بازخورد دلخواه (صدا و...) وقتی گزارش زودتر زده شه")]
    public UnityEvent OnReportBlocked;

    [Header("Transition")]
    [Tooltip("مدت سیاه شدن صفحه")]
    public float fadeOutTime = 1.5f;

    [Tooltip("مدت روشن شدن صفحه در شب جدید")]
    public float fadeInTime = 1.5f;

    [Tooltip("چند ثانیه اسم شب روی صفحه سیاه بمونه")]
    public float titleHoldTime = 2.5f;

    [Tooltip("نمایش اسم شب موقع شروع هر شب")]
    public bool showTitleOnStart = true;

    [Tooltip("قالب اسم شب — {0} جاش شماره شب میشه (اگه فونت فارسی دادی میتونی بنویسی: شب {0})")]
    public string nightTitleFormat = "Night {0}";

    [Tooltip("اختیاری: فونت فارسی برای متن اسم شب (مثلاً Vazirmatn)")]
    public TMP_FontAsset titleFont;

    [Header("New Night Reset")]
    [Tooltip("true = شروع هر شب (باخت یا شب بعد) کل صحنه از نو لود میشه — همه‌چیز دقیقاً برمی‌گرده به حالت اول | false = ریست نرم")]
    public bool reloadScene = false;

    [Tooltip("فقط در ریست نرم: پلیر اول شب بره اینجا (خالی = سرجاش می‌مونه)")]
    public Transform playerSpawn;

    [Tooltip("اول هر شب (بعد از fade-in) اجرا میشه — مثلاً خاموش کردن TV")]
    public UnityEvent OnNightStarted;

    [Header("End of Game (بعد از آخرین شب)")]
    [Tooltip("اگه اسم صحنه بذاری، بعد از شب آخر اون صحنه لود میشه (باید توی Build Settings باشه)")]
    public string endSceneName = "";

    [Tooltip("متن پایانی روی صفحه سیاه (خالی = هیچی نشون نده)")]
    public string endTitleText = "The End";

    // ---------- Runtime ----------
    private CanvasGroup fadeCanvas;
    private TMP_Text titleText;
    private int tvUseCount;
    private bool transitioning;

    // hint مثل RadioTVGate با Reflection پیدا میشه تا اگه متد نبود، Compile Error نده
    private System.Reflection.MethodInfo hintMethod;
    private bool hintChecked;

    private void Awake()
    {
        BuildFadeCanvas();
    }

    private void Start()
    {
        if (requireRadio && radioGate == null)
            Debug.LogWarning("[NightManager] requireRadio فعاله ولی radioGate وصل نشده — چک بیسیم نادیده گرفته میشه.");

        StartCoroutine(StartNightRoutine());
    }

    // ================== اتصال به CustomInteractEvent ==================

    /// <summary>این متد رو به OnStart دکمه ثبت گزارش وصل کن.</summary>
    public void TrySubmitReport()
    {
        if (transitioning) return;

        if (!IsNightComplete())
        {
            // 🔍 دیباگ: توی Console نشون میده دقیقاً کدوم شرط کامل نشده
            bool radioOK = !requireRadio || radioGate == null || radioGate.GateOpen;
            Debug.Log($"[NightManager] RadioOK: {radioOK} | TV Count: {tvUseCount}/{requiredTVUseCount}");

            OnReportBlocked?.Invoke();
            ShowHint(notReadyHint);
            return;
        }

        StartCoroutine(EndNightRoutine());
    }

    /// <summary>
    /// هر بار TV روشن/سوئیچ شد صدا زده شه:
    /// OnTVUnlocked (روی RadioTVGate) یا ایونت سوئیچ دوربین‌ها → NightManager.MarkTVUse
    /// </summary>
    public void MarkTVUse() => tvUseCount++;

    /// <summary>آیا همه شروط شب انجام شده؟</summary>
    public bool IsNightComplete()
    {
        // v1.1: قفل خارجی (مثلاً وسط رویداد ترسناک شب 1)
        if (ExternalBlockCheck != null && ExternalBlockCheck())
            return false;

        if (requireRadio && radioGate != null && !radioGate.GateOpen)
            return false;

        if (tvUseCount < requiredTVUseCount)
            return false;

        return true;
    }

    /// <summary>الان وسط ترنزیشن شب هستیم؟</summary>
    public bool IsTransitioning => transitioning;

    // ================== v1.1: باخت — تکرار همین شب ==================

    /// <summary>
    /// ریست کامل همین شب (باخت — مثلاً بعد از جامپ‌اسکر):
    /// همه چیز مثل شروع شب ریست میشه ولی شماره شب همون می‌مونه.
    /// </summary>
    public void RestartCurrentNight()
    {
        if (transitioning) return;
        StartCoroutine(RestartNightRoutine());
    }

    private IEnumerator RestartNightRoutine()
    {
        transitioning = true;
        LockPlayer(true);

        yield return Fade(fadeCanvas.alpha, 1f, fadeOutTime);

        // v1.2: باخت = ریست کامل — کل صحنه از نو لود میشه
        // (شماره شب استاتیک حفظ میشه؛ NightManager جدید ادامه رو مدیریت می‌کنه)
        if (reloadScene && ReloadCurrentScene())
            yield break;

        // ── ریست نرم مثل شروع شب جدید — بدون تغییر شماره شب ──
        yield return SoftResetRoutine();
    }

    /// <summary>ریست نرم مشترک (باخت / شب بعد) — بدون لود صحنه.</summary>
    private IEnumerator SoftResetRoutine()
    {
        tvUseCount = 0;
        if (radioGate != null)
            radioGate.ResetGate();

        if (playerSpawn != null)
            TeleportPlayer(playerSpawn);

        ShowTitle(string.Format(nightTitleFormat, CurrentNight));
        yield return new WaitForSecondsRealtime(titleHoldTime);
        HideTitle();

        yield return Fade(1f, 0f, fadeInTime);

        LockPlayer(false);
        transitioning = false;

        OnNightStarted?.Invoke();
    }

    /// <summary>لود مجدد صحنه فعلی — false یعنی صحنه توی Build Settings نیست.</summary>
    private bool ReloadCurrentScene()
    {
        int idx = SceneManager.GetActiveScene().buildIndex;
        if (idx < 0)
        {
            Debug.LogWarning("[NightManager] صحنه توی Build Settings اضافه نشده! " +
                             "(File → Build Settings → Add Open Scenes) — فعلاً ریست نرم انجام میشه.");
            return false;
        }
        SceneManager.LoadScene(idx);
        return true;
    }

    // ================== جریان شب ==================

    private IEnumerator StartNightRoutine()
    {
        if (fadeCanvas != null)
            fadeCanvas.alpha = 1f;   // شروع: سیاه

        if (showTitleOnStart && titleText != null)
        {
            ShowTitle(string.Format(nightTitleFormat, CurrentNight));
            yield return new WaitForSecondsRealtime(titleHoldTime);
            HideTitle();
        }

        yield return Fade(1f, 0f, fadeInTime);

        // اطمینان از آزاد بودن پلیر (مخصوصاً بعد از reload صحنه)
        GameManager gm = GameManager.Instance;
        if (gm != null)
            gm.FreezePlayer(false);

        OnNightStarted?.Invoke();
    }

    private IEnumerator EndNightRoutine()
    {
        transitioning = true;
        LockPlayer(true);

        // سیاه شدن صفحه
        yield return Fade(fadeCanvas.alpha, 1f, fadeOutTime);

        // ── شب آخر؟ ──
        if (CurrentNight >= totalNights)
        {
            if (!string.IsNullOrEmpty(endTitleText))
                ShowTitle(endTitleText);

            GameManager gm = GameManager.Instance;
            if (gm != null)
                gm.FreezePlayer(true, true);   // قفل کامل + نشان دادن ماوس

            if (!string.IsNullOrEmpty(endSceneName))
            {
                yield return new WaitForSecondsRealtime(2f);
                SceneManager.LoadScene(endSceneName);
            }
            yield break;   // وگرنه صفحه سیاه و متن پایانی می‌مونه
        }

        CurrentNight++;   // استاتیک — بعد از لود صحنه هم حفظ میشه

        yield return new WaitForSecondsRealtime(0.5f);

        if (reloadScene && ReloadCurrentScene())
        {
            // کوروتین با لود صحنه تموم میشه؛ NightManager جدید ادامه رو مدیریت می‌کنه
            yield break;
        }

        // ── ریست نرم (بدون لود صحنه / یا صحنه توی Build Settings نیست) ──
        yield return SoftResetRoutine();
    }

    // ================== کمکی ==================

    private void TeleportPlayer(Transform spawn)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.PlayerPresence == null || gm.PlayerPresence.StateMachine == null)
            return;

        Transform player = gm.PlayerPresence.StateMachine.transform;
        CharacterController cc = gm.PlayerPresence.StateMachine.GetComponent<CharacterController>();

        // برای جابه‌جایی پلیر باید موقتاً CharacterController خاموش شه
        if (cc != null) cc.enabled = false;

        player.position = spawn.position;
        player.rotation = Quaternion.Euler(0f, spawn.eulerAngles.y, 0f);

        if (cc != null) cc.enabled = true;
    }

    private void LockPlayer(bool state)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
            gm.FreezePlayer(state);
    }

    private void ShowHint(string message)
    {
        if (!useHint) return;

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (!hintChecked)
        {
            hintChecked = true;
            hintMethod = gm.GetType().GetMethod("ShowHintMessage",
                new[] { typeof(string), typeof(float) });

            if (hintMethod == null)
                Debug.LogWarning("[NightManager] GameManager.ShowHintMessage پیدا نشد — " +
                                 "به جاش از ایونت OnReportBlocked استفاده کن.");
        }

        hintMethod?.Invoke(gm, new object[] { message, hintDuration });
    }

    private void ShowTitle(string text)
    {
        if (titleText == null || string.IsNullOrEmpty(text)) return;
        titleText.text = text;
        titleText.gameObject.SetActive(true);
    }

    private void HideTitle()
    {
        if (titleText != null)
            titleText.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        fadeCanvas.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }

        fadeCanvas.alpha = to;
    }

    private void BuildFadeCanvas()
    {
        // کانواس تمام‌صفحه سیاه که بالاتر از همه UI ها رندر میشه
        var canvasGO = new GameObject("NightFadeCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        fadeCanvas = canvasGO.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 1f;
        fadeCanvas.blocksRaycasts = false;
        fadeCanvas.interactable = false;

        var blackGO = new GameObject("Black");
        blackGO.transform.SetParent(canvasGO.transform, false);
        var image = blackGO.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        Stretch(image.rectTransform);

        // متن اسم شب
        var titleGO = new GameObject("NightTitle");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var tmp = titleGO.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 56;
        tmp.color = Color.white;
        if (titleFont != null)
            tmp.font = titleFont;
        Stretch(tmp.rectTransform);
        titleGO.SetActive(false);
        titleText = tmp;
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
