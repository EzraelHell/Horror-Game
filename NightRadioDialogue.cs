// NightRadioDialogue.cs  (v1.1)
// هر شب بیسیم یه ویس/دیالوگ «متفاوت» پخش میکنه — بدون تکرار.
// با همون سیستم UHFPS Dialogue (DialogueTrigger + Dialogue Asset) کار میکنه.
//
// تغییر v1.1 — رفع ارور CS0246:
//   توی نسخه UHFPS شما کلاس DialogueAsset داخل UHFPS.Runtime نیست، پس این اسکریپت
//   دیگه به اسم/namespace اون کلاس هیچ وابستگی‌ای نداره. آرایه Night Dialogues
//   الان از نوع ScriptableObject هست و اسکریپت خودش نوع واقعی دیالوگ رو از روی
//   DialogueTrigger تشخیص میده و چک میکنه. نحوه استفاده همون قبلیه — دیالوگ‌ها رو بکش تو.
//
// نصب (فقط ۲ قدم):
//   1) برای هر شب یه Dialogue Asset جدا بساز (کپیِ همونی که داری + متن/صدای جدید)
//   2) این اسکریپت رو روی همون GameObject بیسیم بنداز که DialogueTrigger روش هست،
//      بعد توی Inspector آرایه Night Dialogues رو به ترتیب شب‌ها پر کن:
//         Element 0 = شب 1  |  Element 1 = شب 2  |  Element 2 = شب 3  |  ...
//
// چطور کار میکنه؟
//   با شروع هر شب (و موقع Start بازی)، دیالوگِ شبِ فعلی جایگزین دیالوگِ فعلیِ
//   DialogueTrigger میشه — پس سیم‌کشی دکمه بیسیم (OnStart → DialogueTrigger)
//   هیچ تغییری لازم نداره. خودش هم با NightManager همگام میشه.
//
// اختیاری (پیشنهاد میکنم پرش کنی):
//   Night Listen Times — طول دیالوگ هر شب به ثانیه؛ Min Listen Time داخل RadioTVGate
//   خودکار برای همون شب تنظیم میشه.
//
// دیباگ:
//   راست‌کلیک روی کامپوننت → Debug: Apply Current Night / Simulate Next Night
//   اگه هشدار نوع/فیلد دیدی → راست‌کلیک → Debug: Print DialogueTrigger Fields
//   و خروجی رو برام بفرست.

using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UHFPS.Runtime;

public class NightRadioDialogue : MonoBehaviour
{
    [Header("Dialogue Per Night (ترتیب: شب 1، شب 2، ...)")]
    [Tooltip("هر شب یک Dialogue Asset — اندیس 0 = شب 1 (همون اسکریپت‌آبجکت دیالوگ UHFPS رو بکش تو)")]
    public ScriptableObject[] nightDialogues;

    [Tooltip("DialogueTrigger بیسیم — خالی بذاری خودش از همین GameObject برمیداره")]
    public DialogueTrigger radioTrigger;

    [Header("Optional: Min Listen Time per Night")]
    [Tooltip("طول دیالوگ هر شب به ثانیه (اندیس 0 = شب 1) — تنظیم خودکار Min Listen Time در RadioTVGate")]
    public float[] nightListenTimes;

    [Tooltip("اسکریپت RadioTVGate صحنه — خالی بذاری خودش پیدا میکنه")]
    public RadioTVGate radioGate;

    [Header("Sync")]
    [Tooltip("خودکار با شب فعلی NightManager همگام میشه (پیشنهادی: روشن)")]
    public bool autoSyncWithNightManager = true;

    [HideInInspector] public int currentNight = 1;

    // ---------- Runtime ----------
    private FieldInfo dialogueField;      // فیلد دیالوگ داخل DialogueTrigger — یک بار پیدا میشه
    private bool reflectionFailed;
    private NightManager nightManager;

    private void Awake()
    {
        if (radioTrigger == null)
            radioTrigger = GetComponent<DialogueTrigger>();

        if (radioGate == null)
            radioGate = GetComponent<RadioTVGate>();

        if (radioGate == null)
            radioGate = FindObjectOfType<RadioTVGate>();
    }

    private void Start()
    {
        // شروع از شبی که NightManager الان توشه (بعد از reload صحنه هم درسته،
        // چون CurrentNight استاتیکه و حفظ میشه)
        if (autoSyncWithNightManager)
        {
            nightManager = FindObjectOfType<NightManager>();
            if (nightManager != null)
            {
                currentNight = NightManager.CurrentNight;
                nightManager.OnNightStarted.AddListener(HandleNightStarted);
            }
        }

        ApplyNightDialogue();
    }

    private void OnDestroy()
    {
        if (nightManager != null)
            nightManager.OnNightStarted.RemoveListener(HandleNightStarted);
    }

    // شب جدید شروع شد → دیالوگش رو بذار رو بیسیم
    private void HandleNightStarted()
    {
        if (autoSyncWithNightManager)
            currentNight = NightManager.CurrentNight;

        ApplyNightDialogue();
    }

    // ================== Public API ==================

    /// <summary>تغییر دستی شب (مثلاً از UnityEvent) و اعمال دیالوگش.</summary>
    public void SetNight(int night)
    {
        currentNight = night;
        ApplyNightDialogue();
    }

    /// <summary>بدون پارامتر: شب رو از NightManager میخونه و اعمال میکنه.</summary>
    public void SyncWithNightManager()
    {
        currentNight = NightManager.CurrentNight;
        ApplyNightDialogue();
    }

    /// <summary>
    /// دیالوگِ شبِ فعلی رو روی DialogueTrigger بیسیم میذاره.
    /// </summary>
    public void ApplyNightDialogue()
    {
        if (radioTrigger == null)
        {
            Debug.LogWarning("[NightRadioDialogue] DialogueTrigger پیدا نشد! اگه روی آبجکت دیگه‌ست، دستی وصلش کن.");
            return;
        }

        if (nightDialogues == null || nightDialogues.Length == 0)
        {
            Debug.LogWarning("[NightRadioDialogue] هیچ Dialogue Assetای واسه شب‌ها تعیین نشده (Night Dialogues خالیه)!");
            return;
        }

        if (currentNight > nightDialogues.Length)
            Debug.LogWarning($"[NightRadioDialogue] واسه شب {currentNight} دیالوگی نداری — " +
                             "از آخرین اسلات استفاده میشه. یه Dialogue Asset دیگه اضافه کن.");

        int idx = Mathf.Clamp(currentNight - 1, 0, nightDialogues.Length - 1);
        UnityEngine.Object asset = nightDialogues[idx];

        if (asset == null)
        {
            Debug.LogWarning($"[NightRadioDialogue] اسلات شب {currentNight} (Element {idx}) خالیه!");
            return;
        }

        FieldInfo field = GetDialogueField();
        if (field == null)
        {
            if (!reflectionFailed)
            {
                reflectionFailed = true;
                Debug.LogWarning("[NightRadioDialogue] فیلد دیالوگ توی DialogueTrigger پیدا نشد! " +
                                 "راست‌کلیک روی این کامپوننت → Debug: Print DialogueTrigger Fields → خروجی رو بفرست.");
            }
            return;
        }

        // چک نوع: asset باید از همون نوعی باشه که فیلد DialogueTrigger انتظار داره
        if (!field.FieldType.IsInstanceOfType(asset))
        {
            Debug.LogWarning($"[NightRadioDialogue] Element {idx} از نوع '{asset.GetType().Name}' هست، " +
                             $"ولی فیلد '{field.Name}' توی DialogueTrigger از نوع '{field.FieldType.Name}' هست! " +
                             "دیالوگ درست رو بکش توی اسلات (اگه مطمئنی درسته: Debug: Print DialogueTrigger Fields رو برام بفرست).");
            return;
        }

        field.SetValue(radioTrigger, asset);
        Debug.Log($"[NightRadioDialogue] Night {currentNight} → '{asset.name}' (field: {field.Name} : {field.FieldType.Name})");

        // اختیاری: طول دیالوگ این شب → Min Listen Time گیت
        ApplyNightListenTime();
    }

    private void ApplyNightListenTime()
    {
        if (radioGate == null || nightListenTimes == null || nightListenTimes.Length == 0)
            return;

        int idx = Mathf.Clamp(currentNight - 1, 0, nightListenTimes.Length - 1);
        float t = nightListenTimes[idx];

        if (t > 0f)
        {
            radioGate.minListenTime = t;
            Debug.Log($"[NightRadioDialogue] Night {currentNight} → Min Listen Time = {t}s");
        }
    }

    // ================== پیدا کردن فیلد دیالوگ (بدون وابستگی به اسم کلاس) ==================

    private FieldInfo GetDialogueField()
    {
        if (dialogueField != null) return dialogueField;

        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = radioTrigger.GetType();

        // 1) اسم‌های متداول — هر فیلد تک‌مقداری که UnityEngine.Object قبول کنه
        string[] names = { "Dialogue", "dialogue", "dialogueAsset", "DialogueAsset", "globalDialogue", "GlobalDialogue" };
        foreach (string n in names)
        {
            FieldInfo f = type.GetField(n, Flags);
            if (f != null && IsAssetField(f.FieldType))
            {
                dialogueField = f;
                return f;
            }
        }

        // 2) هر فیلدی که «نوع»ش اسم Dialogue داره (مثلاً DialogueAsset / Dialogue / GlobalDialogue)
        //    فقط ScriptableObject — تا référence های UI (MonoBehaviour) اشتباه گرفته نشن
        foreach (FieldInfo f in type.GetFields(Flags))
        {
            if (typeof(ScriptableObject).IsAssignableFrom(f.FieldType) &&
                f.FieldType.Name.ToLower().Contains("dialogue"))
            {
                dialogueField = f;
                return f;
            }
        }

        // 3) آخرین چاره: اولین فیلد ScriptableObject تک‌مقداری
        foreach (FieldInfo f in type.GetFields(Flags))
        {
            if (typeof(ScriptableObject).IsAssignableFrom(f.FieldType))
            {
                dialogueField = f;
                return f;
            }
        }

        return null;
    }

    // فیلد تک‌مقداری که یک asset از نوع UnityEngine.Object میگیره (آرایه/لیست/string نه)
    private static bool IsAssetField(Type t)
    {
        if (t.IsArray || typeof(IEnumerable).IsAssignableFrom(t))
            return false;

        return typeof(UnityEngine.Object).IsAssignableFrom(t);
    }

    // ================== دیباگ ==================

    [ContextMenu("Debug: Apply Current Night")]
    private void DebugApplyCurrent() => ApplyNightDialogue();

    [ContextMenu("Debug: Simulate Next Night")]
    private void DebugNextNight()
    {
        currentNight++;
        Debug.Log($"[NightRadioDialogue] شب {currentNight} (شبیه‌سازی) —");
        ApplyNightDialogue();
    }

    [ContextMenu("Debug: Print DialogueTrigger Fields")]
    private void DebugPrintFields()
    {
        if (radioTrigger == null)
        {
            Debug.Log("[NightRadioDialogue] radioTrigger خالیه.");
            return;
        }

        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Debug.Log($"[NightRadioDialogue] فیلدهای {radioTrigger.GetType().FullName}:");
        foreach (FieldInfo f in radioTrigger.GetType().GetFields(Flags))
            Debug.Log($"   - {f.FieldType.FullName} {f.Name}");
    }
}
