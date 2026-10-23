using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UHFPS.Runtime;

public class PlayerDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue / Subtitles")]
    [TextArea(2, 5)] public string[] dialogueLines = { "" };
    public float lineDuration = 3.5f;        // مدت نمایش هر خط (ثانیه)
    public bool useTypewriter = true;        // افکت تایپ شدن متن
    public float charsPerSecond = 25f;       // سرعت تایپ
    public KeyCode skipKey = KeyCode.Space;  // کلید رد کردن کل دیالوگ

    [Header("Subtitle UI")]
    [Tooltip("متن TMP که زیرنویس توش نشون داده میشه")]
    public TMP_Text subtitleText;

    [Header("Trigger Settings")]
    public string playerTag = "Player";
    public bool playOnce = true;             // فقط بار اول اجرا بشه؟

    [Header("Player Lock Method")]
    [Tooltip("FreezeStates = روش رسمی UHFPS: برگشت به Idle + قفل استیت‌های حرکتی (پیشنهادی) | بقیه گزینه‌ها فقط برای تست/جایگزین هستن")]
    public LockMethod lockMethod = LockMethod.FreezeStates;

    public enum LockMethod
    {
        FreezeStates,               // ChangeState(Idle) + قفل استیت‌های حرکتی — تمیزترین روش (پیشنهادی)
        DisableStateMachine,        // خاموش کردن کامل PlayerStateMachine (انیمیشن Walk گیر میکنه)
        DisableCharacterController, // فقط CharacterController خاموش میشه (موقعیت قفل ولی صدای قدم میاد)
        GameManagerFreeze           // GameManager.FreezePlayer — همه چیز قفل میشه حتی ماوس (مثل Pause)
    }

    // استیت‌های حرکتی که موقع دیالوگ باید قفل بشن (ثابت‌های خود PlayerStateMachine)
    private static readonly string[] MovementStates =
    {
        PlayerStateMachine.WALK_STATE,
        PlayerStateMachine.RUN_STATE,
        PlayerStateMachine.CROUCH_STATE,
        PlayerStateMachine.JUMP_STATE,
        PlayerStateMachine.SLIDING_STATE,
        PlayerStateMachine.LADDER_STATE,
        PlayerStateMachine.ZIPLINE_STATE,
        PlayerStateMachine.PUSHING_STATE,
    };

    private bool hasPlayed;
    private bool isPlaying;
    private bool skipRequested;
    private CharacterController playerCC;

    private void Reset()
    {
        // وقتی اسکریپت روی آبجکت جدید انداخته میشه، کولایدر خودکار Trigger میشه
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (isPlaying) return;
        if (playOnce && hasPlayed) return;

        // کش کردن CharacterController پلیر برای روش جایگزین
        if (playerCC == null)
            playerCC = other.GetComponent<CharacterController>();

        hasPlayed = true;
        isPlaying = true;
        StartCoroutine(DialogueRoutine());
    }

    private IEnumerator DialogueRoutine()
    {
        // ---------- 1) قفل کردن پلیر ----------
        LockPlayer(true);

        // ---------- 2) نمایش زیرنویس‌ها ----------
        if (subtitleText != null) subtitleText.gameObject.SetActive(true);

        foreach (string line in dialogueLines)
        {
            yield return StartCoroutine(ShowLine(line));
            if (skipRequested) break;
        }

        // ---------- 3) پاک کردن زیرنویس و آزاد کردن پلیر ----------
        if (subtitleText != null)
        {
            subtitleText.text = string.Empty;
            subtitleText.gameObject.SetActive(false);
        }

        LockPlayer(false);
        isPlaying = false;
    }

    private IEnumerator ShowLine(string line)
    {
        float timer = 0f;
        float typeTime = useTypewriter ? line.Length / Mathf.Max(1f, charsPerSecond) : 0f;

        if (subtitleText != null)
            subtitleText.text = useTypewriter ? string.Empty : line;

        while (timer < lineDuration)
        {
            // رد کردن کل دیالوگ با یک کلید
            if (Input.GetKeyDown(skipKey))
            {
                skipRequested = true;
                yield break;
            }

            timer += Time.deltaTime;

            // افکت تایپ شدن
            if (subtitleText != null)
            {
                if (timer < typeTime)
                {
                    int count = Mathf.FloorToInt(timer * charsPerSecond);
                    subtitleText.text = line.Substring(0, Mathf.Min(count, line.Length));
                }
                else
                {
                    subtitleText.text = line;
                }
            }

            yield return null;
        }
    }

    // ================== قفل / آزادسازی پلیر ==================
    private void LockPlayer(bool state)
    {
        switch (lockMethod)
        {
            case LockMethod.FreezeStates:
                {
                    // روش رسمی UHFPS — فقط با API های عمومی PlayerStateMachine
                    PlayerStateMachine sm = (GameManager.Instance != null)
                        ? GameManager.Instance.PlayerPresence.StateMachine
                        : null;

                    if (sm != null)
                    {
                        if (state)
                        {
                            // 1) اول برگرد به Idle — خروج تمیز از Walk
                            //    (انیمیشن ایستاده میشه، صدای قدم قطع میشه)
                            TryChangeState(sm, PlayerStateMachine.IDLE_STATE);

                            // 2) استیت‌های حرکتی رو قفل کن تا transition جدید انجام نشه
                            SetMovementStatesEnabled(sm, false);
                        }
                        else
                        {
                            // آزادسازی: استیت‌های حرکتی دوباره فعال میشن
                            // (پلیر الان توی Idle ه — با اولین input خودش راه میفته)
                            SetMovementStatesEnabled(sm, true);
                        }
                        return;
                    }

                    Debug.LogWarning("[PlayerDialogueTrigger] StateMachine در دسترس نیست → سوییچ به روش DisableStateMachine");
                    goto case LockMethod.DisableStateMachine;
                }

            case LockMethod.DisableStateMachine:
                {
                    // خاموش کردن کامل سیستم حرکت (نکته: انیمیشن Walk ممکنه گیر کنه)
                    if (GameManager.Instance != null)
                    {
                        var sm = GameManager.Instance.PlayerPresence.StateMachine;
                        if (sm != null)
                        {
                            sm.enabled = !state;
                            return;
                        }
                        Debug.LogWarning("[PlayerDialogueTrigger] StateMachine پیدا نشد → سوییچ به روش CharacterController");
                    }
                    goto case LockMethod.DisableCharacterController;
                }

            case LockMethod.GameManagerFreeze:
                // متد رسمی UHFPS برای Pause/Inventory — همه چیز رو قفل میکنه حتی ماوس
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.FreezePlayer(state, false);
                    return;
                }
                Debug.LogWarning("[PlayerDialogueTrigger] GameManager پیدا نشد → سوییچ به روش CharacterController");
                goto case LockMethod.DisableCharacterController;

            case LockMethod.DisableCharacterController:
                if (playerCC != null)
                {
                    playerCC.enabled = !state;
                }
                else
                {
                    Debug.LogWarning("[PlayerDialogueTrigger] هیچ CharacterController ای روی پلیر پیدا نشد!");
                }
                break;
        }
    }

    // فعال/غیرفعال کردن استیت‌های حرکتی
    // (اگه استیتی توی States Asset پروژه‌ت تعریف نشده باشه، بی‌صدا رد میشه)
    private void SetMovementStatesEnabled(PlayerStateMachine sm, bool enabled)
    {
        foreach (string stateKey in MovementStates)
        {
            sm.SetStateEnabled(stateKey, enabled);
        }
    }

    // تغییر استیت با مدیریت خطا (اگه استیتی توی States Asset نبود، کرش نمیکنه)
    private void TryChangeState(PlayerStateMachine sm, string stateKey)
    {
        try
        {
            sm.ChangeState(stateKey);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayerDialogueTrigger] ChangeState('{stateKey}') شکست خورد: {e.Message}");
        }
    }
}
