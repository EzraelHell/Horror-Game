using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    private float deltaTime = 0f;

    void Start()
{
}

    void Update()
    {
        // محاسبه زمان بین فریم‌ها با میانگین‌گیری ساده
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        // مختصات و اندازه متن
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();

        // تنظیمات ظاهری
        Rect rect = new Rect(10, 10, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 3 / 100;
        style.normal.textColor = Color.green;

        // محاسبه FPS
        float fps = 1.0f / deltaTime;
        string text = string.Format("FPS: {0:0.0}", fps);

        // نمایش متن
        GUI.Label(rect, text, style);
    }
}