using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

public static class FFHMobileRuntime
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Application.isMobilePlatform) return;
        if (GameObject.Find("FFH_MobileControls") != null) return;

        var root = new GameObject("FFH_MobileControls",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
            typeof(FFHMobileControlManager));

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Object.DontDestroyOnLoad(root);
        root.GetComponent<FFHMobileControlManager>().Build();
    }
}

public sealed class FFHMobileControlManager : MonoBehaviour
{
    readonly Dictionary<string, GameObject> buttons = new Dictionary<string, GameObject>();
    float nextRefresh;

    public void Build()
    {
        Add("Up", "▲", "<Keyboard>/w", new Vector2(210, 300), 120, false);
        Add("Left", "◀", "<Keyboard>/a", new Vector2(80, 170), 120, false);
        Add("Down", "▼", "<Keyboard>/s", new Vector2(210, 40), 120, false);
        Add("Right", "▶", "<Keyboard>/d", new Vector2(340, 170), 120, false);

        Add("Shoot", "●", "<Mouse>/leftButton", new Vector2(-190, 190), 155, false);
        Add("Return", "↩", "<Mouse>/rightButton", new Vector2(-90, 340), 125, false);

        Add("Esc", "ESC", "<Keyboard>/escape", new Vector2(85, -65), 100, true);
        Add("Ok", "OK", "<Keyboard>/enter", new Vector2(-85, -65), 100, true);

        HideGameplay();
        Set("Esc", true);
        Set("Ok", true);
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.15f;
        RefreshForEnabledActions();
    }

    void RefreshForEnabledActions()
    {
        var names = new HashSet<string>();
        foreach (var action in InputSystem.ListEnabledActions())
            names.Add(action.name);

        bool artefact = names.Contains("Drag") && names.Contains("MousePosition");
        bool shoot = names.Contains("Shoot");
        bool left = names.Contains("Left");
        bool right = names.Contains("Right");
        bool up = names.Contains("Up");
        bool down = names.Contains("Down");

        HideGameplay();

        if (artefact)
        {
            // Drag and absolute pointer position are patched directly to Touchscreen.
            // Keep a visible fallback for the original right-click Return action.
            Set("Return", true);
            return;
        }

        if (shoot)
        {
            Set("Shoot", true);
            return;
        }

        if (left && right && up && down)
        {
            Set("Up", true);
            Set("Left", true);
            Set("Down", true);
            Set("Right", true);
            return;
        }

        if (left && right)
        {
            Set("Left", true);
            Set("Right", true);
            return;
        }

        if (up && down)
        {
            Set("Up", true);
            Set("Down", true);
        }
    }

    void HideGameplay()
    {
        Set("Up", false);
        Set("Left", false);
        Set("Down", false);
        Set("Right", false);
        Set("Shoot", false);
        Set("Return", false);
    }

    void Set(string name, bool state)
    {
        if (buttons.TryGetValue(name, out var go) && go.activeSelf != state)
            go.SetActive(state);
    }

    void Add(string name, string label, string control, Vector2 pos, float size, bool top)
    {
        var go = new GameObject("FFH_" + name,
            typeof(RectTransform), typeof(Image), typeof(OnScreenButton));
        go.transform.SetParent(transform, false);

        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchorMin = rt.anchorMax =
            top ? (pos.x < 0 ? new Vector2(1, 1) : new Vector2(0, 1))
                : (pos.x < 0 ? new Vector2(1, 0) : new Vector2(0, 0));
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.02f, 0.02f, 0.02f, .34f);

        go.GetComponent<OnScreenButton>().controlPath = control;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var tr = (RectTransform)textGo.transform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        var txt = textGo.GetComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = name == "Esc" || name == "Ok" ? 27 : 48;
        txt.color = new Color(1, 1, 1, .82f);
        txt.raycastTarget = false;

        buttons[name] = go;
    }
}
