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
        Object.DontDestroyOnLoad(CreateOverlay());
    }

    static GameObject CreateOverlay()
    {
        var root = new GameObject("FFH_MobileControls",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.matchWidthOrHeight = 0.5f;

        Add(root.transform,"W","<Keyboard>/w",new Vector2(210,280),100,false);
        Add(root.transform,"A","<Keyboard>/a",new Vector2(100,170),100,false);
        Add(root.transform,"S","<Keyboard>/s",new Vector2(210,60),100,false);
        Add(root.transform,"D","<Keyboard>/d",new Vector2(320,170),100,false);

        Add(root.transform,"L","<Mouse>/leftButton",new Vector2(-230,180),150,false);
        Add(root.transform,"R","<Mouse>/rightButton",new Vector2(-80,330),125,false);
        Add(root.transform,"ESC","<Keyboard>/escape",new Vector2(90,-70),105,true);
        Add(root.transform,"OK","<Keyboard>/enter",new Vector2(-90,-70),105,true);
        return root;
    }

    static void Add(Transform parent,string label,string control,Vector2 pos,float size,bool top)
    {
        var go = new GameObject("Mobile_"+label,
            typeof(RectTransform), typeof(Image), typeof(OnScreenButton));
        go.transform.SetParent(parent,false);
        var rt=(RectTransform)go.transform;
        rt.sizeDelta=new Vector2(size,size);
        rt.anchorMin=rt.anchorMax=top ? (pos.x < 0 ? new Vector2(1,1) : new Vector2(0,1))
                                     : (pos.x < 0 ? new Vector2(1,0) : new Vector2(0,0));
        rt.pivot=new Vector2(.5f,.5f);
        rt.anchoredPosition=pos;

        go.GetComponent<Image>().color=new Color(0,0,0,.28f);
        go.GetComponent<OnScreenButton>().controlPath=control;

        var tgo=new GameObject("Text",typeof(RectTransform),typeof(Text));
        tgo.transform.SetParent(go.transform,false);
        var tr=(RectTransform)tgo.transform;
        tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one;
        tr.offsetMin=tr.offsetMax=Vector2.zero;
        var txt=tgo.GetComponent<Text>();
        txt.text=label;
        txt.alignment=TextAnchor.MiddleCenter;
        txt.font=Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize=36;
        txt.color=new Color(1,1,1,.75f);
        txt.raycastTarget=false;
    }
}
