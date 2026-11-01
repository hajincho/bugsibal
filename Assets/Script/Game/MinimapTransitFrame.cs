using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MinimapTransitFrame : MonoBehaviour
{
    public List<string> stages=new List<string>();
    public List<Image> lamps=new List<Image>(),halos=new List<Image>(),tracks=new List<Image>();
    public List<TMP_Text> labels=new List<TMP_Text>();
    static readonly Color Pending=new Color(.23f,.3f,.29f),Passed=new Color(.27f,.91f,.43f),Current=new Color(1f,.81f,.12f),Boss=new Color(.95f,.23f,.21f);
    void OnEnable(){SceneManager.sceneLoaded+=Loaded;Refresh(SceneManager.GetActiveScene().name);}
    void OnDisable(){SceneManager.sceneLoaded-=Loaded;}
    void Loaded(Scene scene,LoadSceneMode mode){if(mode==LoadSceneMode.Single)Refresh(scene.name);}
    public void Refresh(string scene)
    {
        int index=stages.IndexOf(scene);
        for(int i=0;i<stages.Count;i++)
        {
            bool boss=stages[i].Contains("Boss"),active=i==index,passed=index>=0&&i<index;
            Color color=boss?Boss:active?Current:passed?Passed:Pending;
            lamps[i].color=color;halos[i].color=active?Current:passed?Passed:new Color(.12f,.17f,.16f);
            labels[i].color=active?Current:boss?Boss:passed?Passed:new Color(.48f,.54f,.5f);
            if(i<tracks.Count)tracks[i].color=index>i?Passed:new Color(.19f,.25f,.22f);
        }
    }
    public void Build(RawImage map,TMP_FontAsset font)
    {
        stages.Clear();lamps.Clear();halos.Clear();labels.Clear();tracks.Clear();
        foreach(string scene in RouteScenes())
        {
            if(Regex.IsMatch(scene,@"^Stage\d+$")||scene.Contains("Boss"))stages.Add(scene);
        }
        var outer=GetComponent<RectTransform>();outer.anchorMin=outer.anchorMax=Vector2.one;outer.pivot=Vector2.one;outer.sizeDelta=new Vector2(344,344);outer.anchoredPosition=new Vector2(-20,-20);
        var wood=new Color(.15f,.125f,.09f);var gold=new Color(.48f,.4f,.23f);
        Box("FrameBody",transform,Vector2.zero,new Vector2(344,344),wood);
        Box("Inset",transform,Vector2.zero,new Vector2(328,328),new Color(.025f,.036f,.031f));
        Border("OuterBevel",transform,new Vector2(340,340),3,gold);
        Border("InnerBevel",transform,new Vector2(314,314),1,new Color(.31f,.29f,.2f));
        foreach(float x in new[]{-163f,163f})foreach(float y in new[]{-163f,163f})Box("FrameStud",transform,new Vector2(x,y),new Vector2(4,4),new Color(.65f,.56f,.34f));
        map.transform.SetParent(transform,false);var rt=map.rectTransform;rt.anchorMin=rt.anchorMax=new Vector2(.5f,1);rt.pivot=new Vector2(.5f,1);rt.localScale=Vector3.one;rt.sizeDelta=new Vector2(300,300);rt.anchoredPosition=new Vector2(0,-22);map.raycastTarget=false;
        var mapEdge=Box("MapRecess",transform,Vector2.zero,new Vector2(306,306),new Color(.4f,.36f,.24f));mapEdge.transform.SetSiblingIndex(map.transform.GetSiblingIndex());
        var boardObject=new GameObject("TransitRoute",typeof(RectTransform));boardObject.transform.SetParent(transform,false);
        var board=boardObject.GetComponent<RectTransform>();board.sizeDelta=new Vector2(330,55);board.anchoredPosition=new Vector2(0,-201);
        var points=new List<Vector2>();
        for(int i=0;i<stages.Count;i++)
        {
            float x=Mathf.Lerp(-146,146,i/(float)Mathf.Max(1,stages.Count-1));points.Add(new Vector2(x,0));
        }
        for(int i=0;i<points.Count-1;i++)
        {
            var delta=points[i+1]-points[i];var track=Box("RouteTrack"+i,board,(points[i]+points[i+1])*.5f,new Vector2(delta.magnitude,3),Pending);
            track.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);tracks.Add(track);
        }
        for(int i=0;i<points.Count;i++)
        {
            halos.Add(Box("StationRim"+i,board,points[i],new Vector2(17,17),Pending));
            Box("StationInset"+i,board,points[i],new Vector2(13,13),new Color(.025f,.042f,.034f));
            lamps.Add(Box("StationLED"+i,board,points[i],new Vector2(9,9),Pending));
            var go=new GameObject("StationLabel"+i,typeof(RectTransform));go.transform.SetParent(board,false);
            var label=go.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=16;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            label.text=stages[i].Contains("Boss")?(stages[i].Contains("Final")?"F":"B"):stages[i].Replace("Stage","");
            label.rectTransform.sizeDelta=new Vector2(36,23);label.rectTransform.anchoredPosition=points[i]+Vector2.down*20;labels.Add(label);
        }
        Refresh(SceneManager.GetActiveScene().name);
    }
    static IEnumerable<string> RouteScenes()
    {
#if UNITY_EDITOR
        foreach(var scene in UnityEditor.EditorBuildSettings.scenes)if(scene.enabled)yield return Path.GetFileNameWithoutExtension(scene.path);
#else
        for(int i=0;i<SceneManager.sceneCountInBuildSettings;i++)yield return Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
#endif
    }
    static Image Box(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
        var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;image.rectTransform.sizeDelta=size;image.rectTransform.anchoredPosition=pos;return image;
    }
    static void Border(string name,Transform parent,Vector2 size,float width,Color color)
    {
        Box(name+"Top",parent,new Vector2(0,size.y/2),new Vector2(size.x,width),color);Box(name+"Bottom",parent,new Vector2(0,-size.y/2),new Vector2(size.x,width),color);
        Box(name+"Left",parent,new Vector2(-size.x/2,0),new Vector2(width,size.y),color);Box(name+"Right",parent,new Vector2(size.x/2,0),new Vector2(width,size.y),color);
    }
}
