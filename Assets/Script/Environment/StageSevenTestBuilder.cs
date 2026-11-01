#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class StageSevenTestBuilder
{
    const string Path="Assets/Scenes/Stage/Stage5.unity";
    static readonly Vector2[] Centers={new Vector2(0,-40),new Vector2(-20,-20),new Vector2(20,-20),Vector2.zero,new Vector2(-20,20),new Vector2(20,20),new Vector2(0,40)};
    static readonly string[] Titles={"Entry","West Woods","East Woods","Central Clearing","West Camp","East Camp","Exit Guard"};
    static readonly int[,] Edges={{0,1},{0,2},{1,3},{2,3},{3,4},{3,5},{4,6},{5,6}};
    static GameObject Enemy(string name){return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefep/Enemy/"+name+".prefab");}
    public static string Build()
    {
        if(Application.isPlaying)throw new Exception("Exit Play mode first");
        if(File.Exists(Path))File.Copy(Path,@"C:\Users\fromj\Documents\Codex\2026-10-27\su\output\forest-backups\Stage7-original-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var grid=new GameObject("Grid",typeof(Grid));SceneManager.MoveGameObjectToScene(grid,scene);
        var groundGO=new GameObject("Ground",typeof(Tilemap),typeof(TilemapRenderer));groundGO.transform.SetParent(grid.transform,false);var ground=groundGO.GetComponent<Tilemap>();groundGO.GetComponent<TilemapRenderer>().sortingOrder=-10;
        var wallGO=new GameObject("Wall",typeof(Tilemap),typeof(TilemapRenderer),typeof(TilemapCollider2D));wallGO.transform.SetParent(grid.transform,false);wallGO.tag="Wall";var wall=wallGO.GetComponent<Tilemap>();
        const string folder="Assets/Script/Environment/StageSevenTest";Directory.CreateDirectory(folder);
        var boundary=AssetDatabase.LoadAssetAtPath<Tile>(folder+"/Boundary.asset");if(boundary==null){boundary=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(boundary,folder+"/Boundary.asset");}boundary.colliderType=Tile.ColliderType.Grid;EditorUtility.SetDirty(boundary);
        var dirt=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Script/Environment/Forest/ForestDirt.asset");var cells=new HashSet<Vector3Int>();
        for(int y=-50;y<=50;y++)for(int x=-31;x<=31;x++)
        {
            Vector2 point=new Vector2(x+.5f,y+.5f);bool inside=false;
            for(int i=0;i<Centers.Length;i++)if(Vector2.Distance(point,Centers[i])<(i==3?11f:8f)){inside=true;break;}
            for(int i=0;i<Edges.GetLength(0)&&!inside;i++){var a=Centers[Edges[i,0]];var delta=Centers[Edges[i,1]]-a;var closest=a+delta*Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude);if(Vector2.Distance(point,closest)<2.75f)inside=true;}
            if(inside){var p=new Vector3Int(x,y,0);cells.Add(p);ground.SetTile(p,dirt);}
        }
        var walls=new HashSet<Vector3Int>();foreach(var p in cells)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){var q=p+new Vector3Int(dx,dy,0);if(!cells.Contains(q))walls.Add(q);}foreach(var p in walls)wall.SetTile(p,boundary);
        ground.CompressBounds();wall.CompressBounds();
        var managerGO=new GameObject("Stage7Encounters",typeof(StageSevenEncounters));SceneManager.MoveGameObjectToScene(managerGO,scene);var manager=managerGO.GetComponent<StageSevenEncounters>();manager.maximumAlive=14;manager.zones=new StageSevenEncounters.Zone[7];
        string[][] kinds={
            new[]{"Rat","Rat","Rat","SpikedSlime","SpikedSlime","SpikedSlime"},
            new[]{"Rat","Rat","Skeleton","Skeleton","Skeleton","Skeleton","Devil","Devil"},
            new[]{"Bat","Bat","Bat","Bat","Skeleton","Skeleton","Devil","Devil"},
            new[]{"SpikedSlime","SpikedSlime","SpikedSlime","Skeleton","Skeleton","Skeleton","Skeleton","Devil","Devil","DevilMage"},
            new[]{"Bat","Bat","Bat","Rat","Rat","Skeleton","Skeleton","Devil"},
            new[]{"Rat","Rat","Skeleton","Skeleton","Skeleton","Devil","Devil","DevilMage"},
            new[]{"Bat","Bat","Skeleton","Skeleton","Skeleton","Devil","Devil","Devil"}
        };
        int total=0;
        int[] counts={4,6,6,8,6,6,6};
        for(int i=0;i<7;i++)
        {
            int count=counts[i];
            var zone=new StageSevenEncounters.Zone{title=Titles[i],center=Centers[i],radius=i==3?11f:8f,enemies=new GameObject[count],spawnPoints=new Vector3[count]};
            for(int n=0;n<count;n++){int index=Mathf.FloorToInt((n+.5f)*kinds[i].Length/count);zone.enemies[n]=Enemy(kinds[i][index]);float angle=(n+.5f)*Mathf.PI*2/count;float radius=3.8f+(n%2)*1.3f;zone.spawnPoints[n]=(Vector3)(Centers[i]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);total++;}
            manager.zones[i]=zone;
        }
        var portalPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefep/Portal/PortalToStage6.prefab");var exit=(GameObject)PrefabUtility.InstantiatePrefab(portalPrefab,scene);exit.name="ExitToStage6";exit.transform.position=new Vector3(0,42,0);exit.SetActive(false);manager.exitPortal=exit;
        var warning=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefep/Enemy/denger.prefab");manager.warningPrefab=warning;
        EditorSceneManager.SaveScene(scene,Path);EditorSceneManager.CloseScene(scene,true);AssetDatabase.SaveAssets();
        ForestStageBuilder.BuildStages(new[]{5});
        var settings=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);bool found=false;foreach(var s in settings)if(s.path==Path){s.enabled=true;found=true;}if(!found)settings.Add(new EditorBuildSettingsScene(Path,true));EditorBuildSettings.scenes=settings.ToArray();
        return "Stage5: floor="+cells.Count+", boundary="+walls.Count+", zones=7, enemies="+total+", main route=30, optional=12, live cap=14";
    }
    public static string AddLobbyPortal()
    {
        var scene=SceneManager.GetSceneByName("DontDestroyObject");if(!scene.IsValid()||!scene.isLoaded)throw new Exception("Open DontDestroyObject first");
        foreach(var root in scene.GetRootGameObjects())if(root.name=="PortalToStage5")UnityEngine.Object.DestroyImmediate(root);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefep/Portal/PortalToStage5.prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.name="PortalToStage5";go.transform.position=new Vector3(6.67f,2.8f,0);
        var portal=go.GetComponent<Portal>();portal.next_stage="Stage5";portal.nextstage=5;portal.x=0;portal.y=-40;portal.z=0;
        var labelGO=new GameObject("Stage5Label",typeof(TMPro.TextMeshPro));labelGO.transform.SetParent(go.transform,false);labelGO.transform.localPosition=new Vector3(0,1.4f,0);var label=labelGO.GetComponent<TMPro.TextMeshPro>();label.font=TMPro.TMP_Settings.defaultFontAsset;label.text="5";label.fontSize=3;label.alignment=TMPro.TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(4,.8f);label.color=new Color(1,.85f,.25f);label.GetComponent<MeshRenderer>().sortingOrder=30;
        EditorUtility.SetDirty(portal);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Lobby Stage7 test portal created at (12.96, 2.68)";
    }
}
#endif
