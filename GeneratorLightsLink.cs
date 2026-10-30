using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UHFPS.Runtime;

/// <summary>
/// GeneratorLightsAuto v1.0
/// با روشن شدن جنراتور، همه لامپ‌های صحنه خودکار روشن می‌شوند
/// و با خاموش شدن جنراتور (یا تمام شدن سوخت) همه خاموش می‌شوند.
///
/// این اسکریپت هم‌زمان از ۳ مسیر مطمئن می‌شود لامپ‌ها کار کنند:
/// مسیر ۱: لامپ‌ها را داخل لیست PowerConsumers خود جنراتور ثبت می‌کند (مسیر داخلی کیت)
/// مسیر ۲: قفل prevState لامپ‌ها را باز می‌کند تا دستور جنراتور را نادیده نگیرند
/// مسیر ۳: مستقیم به ایونت‌های OnGeneratorStart / OnGeneratorEnd وصل می‌شود
/// </summary>
public class GeneratorLightsAuto : MonoBehaviour
{
    [Header("تنظیمات")]
    [Tooltip("جنراتور هدف — اگر خالی بماند خودش در صحنه پیدا می‌کند")]
    public PowerGenerator generator;

    [Tooltip("لامپ‌ها — اگر خالی بماند خودش همه لامپ‌های فعال صحنه را پیدا می‌کند")]
    public InteractableLight[] lamps;

    [Tooltip("وقتی جنراتور خاموش شد، لامپ‌ها هم خاموش شوند")]
    public bool turnOffWithGenerator = true;

    [Tooltip("نمایش پیام‌های راهنما در Console")]
    public bool debugLogs = true;

    private bool setupDone;

    void Start()
    {
        StartCoroutine(SetupWhenReady());
    }

    IEnumerator SetupWhenReady()
    {
        // دو فریم صبر می‌کنیم تا همه لامپ‌ها و جنراتور در صحنه آماده شوند
        yield return null;
        yield return null;
        Setup();
    }

    void Setup()
    {
        if (setupDone) return;
        setupDone = true;

        // ─── ۱) پیدا کردن جنراتور ───
        if (generator == null)
            generator = FindObjectOfType<PowerGenerator>();

        if (generator == null)
        {
            Debug.LogWarning("[GeneratorLightsAuto] هیچ جنراتوری (PowerGenerator) در صحنه پیدا نشد!");
            return;
        }

        // ─── ۲) پیدا کردن لامپ‌ها ───
        if (lamps == null || lamps.Length == 0)
            lamps = FindObjectsOfType<InteractableLight>();

        if (lamps == null || lamps.Length == 0)
        {
            Debug.LogWarning("[GeneratorLightsAuto] هیچ InteractableLight ای در صحنه پیدا نشد!");
            return;
        }

        Log($"جنراتور پیدا شد: {generator.name} | تعداد لامپ: {lamps.Length}");

        // ─── ۳) ثبت لامپ‌ها در لیست PowerConsumers جنراتور (مسیر داخلی کیت) ───
        RegisterLampsToGenerator();

        // ─── ۴) باز کردن قفل prevState لامپ‌ها ───
        // در کد اصلی کیت، لامپی که بازیکن قبلاً دستی باز نکرده باشد
        // دستور جنراتور را نادیده می‌گیرد؛ اینجا آن قفل را برای همه باز می‌کنیم.
        UnlockPrevState();

        // ─── ۵) اتصال مستقیم به ایونت‌های خود جنراتور ───
        // حتی اگر مسیرهای داخلی کیت کار نکنند، این مسیر مستقیم لامپ‌ها را کنترل می‌کند.
        generator.OnGeneratorStart.AddListener(TurnAllOn);
        if (turnOffWithGenerator)
        {
            generator.OnGeneratorEnd.AddListener(TurnAllOff);
            generator.OnOutOfFuel.AddListener(TurnAllOff);
        }

        Log("آماده شد ✅ — حالا جنراتور را روشن کن، لامپ‌ها همراهش روشن می‌شوند.");
    }

    void RegisterLampsToGenerator()
    {
        FieldInfo field = typeof(PowerGenerator).GetField("PowerConsumers",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // اگر با این اسم پیدا نشد، دنبال اولین لیست (IList) داخل جنراتور می‌گردیم
        if (field == null)
        {
            foreach (FieldInfo f in typeof(PowerGenerator).GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (typeof(IList).IsAssignableFrom(f.FieldType))
                {
                    field = f;
                    break;
                }
            }
        }

        if (field == null)
        {
            Debug.LogWarning("[GeneratorLightsAuto] لیست PowerConsumers پیدا نشد (مهم نیست — مسیر مستقیم فعال است)");
            return;
        }

        if (field.GetValue(generator) is IList list)
        {
            int added = 0;
            foreach (InteractableLight lamp in lamps)
            {
                if (lamp != null && !list.Contains(lamp))
                {
                    list.Add(lamp);
                    added++;
                }
            }
            Log($"{added} لامپ در لیست مصرف‌کننده‌های جنراتور ثبت شد");
        }
    }

    void UnlockPrevState()
    {
        FieldInfo field = typeof(InteractableLight).GetField("prevState",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // اگر با این اسم نبود، دنبال هر متغیر bool ای با نام مشابه می‌گردیم
        if (field == null)
        {
            foreach (FieldInfo f in typeof(InteractableLight).GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(bool) && f.Name.ToLower().Contains("prev"))
                {
                    field = f;
                    break;
                }
            }
        }

        if (field == null)
        {
            Debug.LogWarning("[GeneratorLightsAuto] فیلد prevState پیدا نشد (مهم نیست — مسیر مستقیم فعال است)");
            return;
        }

        foreach (InteractableLight lamp in lamps)
        {
            if (lamp != null)
                field.SetValue(lamp, true);
        }
        Log("قفل prevState همه لامپ‌ها باز شد");
    }

    void TurnAllOn()
    {
        foreach (InteractableLight lamp in lamps)
        {
            if (lamp != null)
                lamp.SetLightState(true);
        }
        Log("جنراتور روشن شد → همه لامپ‌ها روشن شدند 💡");
    }

    void TurnAllOff()
    {
        foreach (InteractableLight lamp in lamps)
        {
            if (lamp != null)
                lamp.SetLightState(false);
        }
        Log("جنراتور خاموش شد → همه لامپ‌ها خاموش شدند 🌑");
    }

    void Log(string msg)
    {
        if (debugLogs)
            Debug.Log("[GeneratorLightsAuto] " + msg);
    }

    // ─── ابزارهای تست از داخل Editor (راست کلیک روی کامپوننت) ───
    [ContextMenu("Debug: Turn All Lights ON")]
    void DebugOn()
    {
        if (lamps == null || lamps.Length == 0)
            lamps = FindObjectsOfType<InteractableLight>();
        TurnAllOn();
    }

    [ContextMenu("Debug: Turn All Lights OFF")]
    void DebugOff()
    {
        if (lamps == null || lamps.Length == 0)
            lamps = FindObjectsOfType<InteractableLight>();
        TurnAllOff();
    }

    [ContextMenu("Debug: Run Setup Now")]
    void DebugSetup()
    {
        setupDone = false;
        Setup();
    }
}