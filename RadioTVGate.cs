// تلویزیون تا وقتی ویس/دیالوگ بیسیم کامل شنیده نشده، روشن نمیشه.
// حالت B = UHFPS Dialogue System (Dialogue Trigger + Dialogue Asset)

using UnityEngine;
using UnityEngine.Events;
using UHFPS.Runtime;

public class RadioTVGate : MonoBehaviour
{
    [Header("Radio Voice")]
    public AudioSource radioVoice;

    public float minListenTime = 0f;

    [Header("TV Lock (اختیاری - قفل ساکت)")]
    public Collider[] tvColliders;

    public bool blockTVColliders = false;

    [Header("Locked Feedback (وقتی TV زودتر استفاده شه)")]
    public bool useHint = true;
    [TextArea] public string lockedHintText = "I should listen to the radio first...";
    public float hintDuration = 3f;
    public UnityEvent OnTryTVLocked;

    [Header("Events")]
    public UnityEvent OnGateOpened;

    public UnityEvent OnTVUnlocked;

    public bool GateOpen { get; private set; }

    private float listenedTime;
    private bool timingByEvent;   // حالت B: تایمر از لحظه NotifyRadioStarted
    private bool warned;

    // hint از طریق Reflection صدا زده میشه تا اگه اسم متد فرق داشت، Compile Error نگیری
    private System.Reflection.MethodInfo hintMethod;
    private bool hintChecked;

    private void Start()
    {
        if (blockTVColliders)
            SetTVColliders(false);

        if (radioVoice == null)
            Debug.LogWarning("[RadioTVGate] radioVoice خالیه. اگه از Dialogue System استفاده میکنی " +
                             "و NotifyRadioStarted رو وصل کردی، این هشدار رو نادیده بگیر.");
    }

    private void Update()
    {
        if (GateOpen) return;

        // ── حالت B: تایمر از لحظه شروع دیالوگ ──
        if (timingByEvent)
        {
            listenedTime += Time.deltaTime;
            if (minListenTime > 0f && listenedTime >= minListenTime)
                OpenGate();
            return;
        }

        // ── حالت A: رصد AudioSource معمولی ──
        if (radioVoice == null) return;

        if (radioVoice.isPlaying)
        {
            listenedTime += Time.deltaTime;

            float required;
            if (minListenTime > 0f)
                required = minListenTime;
            else if (radioVoice.clip != null)
                required = radioVoice.clip.length;
            else
            {
                Warn("این AudioSource با PlayOneShot صدا پخش می‌کنه (clip نداره). " +
                     "طول ویس رو دستی توی Min Listen Time وارد کن، یا از حالت B استفاده کن.");
                return;
            }

            if (listenedTime >= required)
                OpenGate();
        }
    }

    // ================== اتصال به CustomInteractEvent ==================

    /// <summary>
    /// حالت B (Dialogue System): این متد رو به OnStart دکمه رادیو وصل کن
    /// (کنار DialogueTrigger). بعد از Min Listen Time ثانیه گیت خودکار باز میشه.
    /// </summary>
    public void NotifyRadioStarted()
    {
        if (GateOpen) return;

        timingByEvent = true;
        listenedTime = 0f;

        if (minListenTime <= 0f)
            Warn("حالت B فعاله ولی Min Listen Time صفره! طول کل دیالوگ رو وارد کن.");
    }

    /// <summary>این متد رو به OnStart دکمه تلویزیون وصل کن.</summary>
    public void TryUseTV()
    {
        if (!GateOpen)
        {
            ShowLockedFeedback();
            return;
        }

        OnTVUnlocked?.Invoke();
    }

    /// <summary>باز کردن گیت — دستی یا خودکار.</summary>
    public void OpenGate()
    {
        if (GateOpen) return;

        GateOpen = true;

        if (blockTVColliders)
            SetTVColliders(true);

        OnGateOpened?.Invoke();
        Debug.Log("[RadioTVGate] Gate opened - TV is now usable.");
    }

    [ContextMenu("Debug: Open Gate")]
    private void DebugOpenGate() => OpenGate();

    // ================== داخلی ==================

    private void ShowLockedFeedback()
    {
        OnTryTVLocked?.Invoke();

        if (!useHint) return;

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (!hintChecked)
        {
            hintChecked = true;
            hintMethod = gm.GetType().GetMethod("ShowHintMessage",
                new[] { typeof(string), typeof(float) });

            if (hintMethod == null)
                Debug.LogWarning("[RadioTVGate] GameManager.ShowHintMessage پیدا نشد. " +
                                 "به جاش از ایونت OnTryTVLocked استفاده کن.");
        }

        hintMethod?.Invoke(gm, new object[] { lockedHintText, hintDuration });
    }

    private void SetTVColliders(bool state)
    {
        foreach (var col in tvColliders)
        {
            if (col != null)
                col.enabled = state;
        }
    }

    private void Warn(string msg)
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning("[RadioTVGate] " + msg);
    }
}