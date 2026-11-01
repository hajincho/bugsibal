#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class SkyStoneStagesBuilder
{
    const string Stone="Assets/Script/Environment/StoneBuilding/";
    const string Sky="Assets/Script/Environment/MidBossSky/";
    static int depth;
    static Sprite SpriteAt(string path,string name=null)
    {
        foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path)){var sp=a as Sprite;if(sp!=null&&(name==null||sp.name==name))return sp;}
        throw new Exception("Missing sprite "+path+" "+name);
    }
    static SpriteRenderer Place(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size,int order,Color color)
    {
        var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.position=new Vector3(position.x,position.y,.5f+depth++*.0005f);
        go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);var sr=go.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;sr.color=color;return sr;
    }
    public static string Build(int stage)
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        if(stage<4||stage>6)throw new ArgumentOutOfRangeException("stage");
        string path="Assets/Scenes/Stage/Stage"+stage+".unity";File.Copy(path,@"C:\Users\fromj\Documents\Codex\2026-10-27\su\output\forest-backups\Stage"+stage+"-before-sky-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
        var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);Tilemap ground=null;var walls=new HashSet<Vector3Int>();var maps=new List<Tilemap>();
        // Remove the previous theme before looking for the actual playable floor.
        foreach(var root in scene.GetRootGameObjects())
            if(root.name=="ForestTheme")UnityEngine.Object.DestroyImmediate(root);
        foreach(var root in scene.GetRootGameObjects())if(root.name!="SkyStoneStageTheme")foreach(var tm in root.GetComponentsInChildren<Tilemap>())
        {
            if(tm.GetComponent<Collider2D>()!=null){maps.Add(tm);foreach(var p in tm.cellBounds.allPositionsWithin)if(tm.HasTile(p))walls.Add(p);}
            else if(tm.name.ToLower().Contains("grou")||tm.name.ToLower().Contains("grao"))ground=tm;
        }
        if(ground==null)throw new Exception("No ground in stage "+stage);
        foreach(var root in scene.GetRootGameObjects())if(root.name=="SkyStoneStageTheme")UnityEngine.Object.DestroyImmediate(root);
        var theme=new GameObject("SkyStoneStageTheme",typeof(Grid));SceneManager.MoveGameObjectToScene(theme,scene);depth=0;
        var walkable=new HashSet<Vector3Int>();var footprint=new HashSet<Vector3Int>(walls);var floorTiles=new Tile[16];
        for(int i=0;i<16;i++)floorTiles[i]=AssetDatabase.LoadAssetAtPath<Tile>(Stone+"RPGWideFloorTile"+i+".asset");
        ground.CompressBounds();int floorCount=0;foreach(var p in ground.cellBounds.allPositionsWithin)if(ground.HasTile(p))
        {
            footprint.Add(p);if(!walls.Contains(p))walkable.Add(p);floorCount++;
            ground.SetTile(p,floorTiles[((p.x%4+4)%4)+((p.y%4+4)%4)*4]);ground.SetTileFlags(p,TileFlags.None);
            float shade=.95f+Mathf.PerlinNoise((p.x+60)*.13f,(p.y+60)*.13f)*.08f;ground.SetColor(p,new Color(shade,shade,shade));
        }
        ground.color=new Color(.85f,.86f,.84f);ground.GetComponent<TilemapRenderer>().sortingOrder=-12;
        foreach(var map in maps)map.GetComponent<TilemapRenderer>().enabled=false;
        var masonry=SpriteAt(Stone+"StoneArchitecture.png","RPGMasonry");var pillar=SpriteAt(Stone+"StoneArchitecture.png","RPGPilaster");var side=SpriteAt(Stone+"StraightIStoneWall.png","StraightIWall");var window=SpriteAt(Stone+"ArchedWindow.png");var white=SpriteAt(Sky+"SkyBlue.png");var core=AssetDatabase.LoadAssetAtPath<Sprite>(Sky+"SolidMasonryCore.asset");
        string opaquePath=Sky+"OpaqueFloorBacking.asset",wallPath=Sky+"SolidWallBacking.asset";
        var opaque=AssetDatabase.LoadAssetAtPath<Tile>(opaquePath);if(opaque==null){opaque=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(opaque,opaquePath);}opaque.sprite=white;opaque.transform=Matrix4x4.Scale(Vector3.one/white.bounds.size.x);opaque.color=new Color(.29f,.32f,.35f);opaque.colliderType=Tile.ColliderType.None;EditorUtility.SetDirty(opaque);
        var wallTile=AssetDatabase.LoadAssetAtPath<Tile>(wallPath);if(wallTile==null){wallTile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(wallTile,wallPath);}wallTile.sprite=core;wallTile.transform=Matrix4x4.Scale(new Vector3(1.015f/core.bounds.size.x,1.015f/core.bounds.size.y,1));wallTile.color=new Color(.70f,.73f,.75f);wallTile.colliderType=Tile.ColliderType.None;EditorUtility.SetDirty(wallTile);
        var under=new GameObject("OpaqueStructureFootprint",typeof(Tilemap),typeof(TilemapRenderer));under.transform.SetParent(theme.transform,false);under.GetComponent<TilemapRenderer>().sortingOrder=-19;foreach(var p in footprint)under.GetComponent<Tilemap>().SetTile(p,opaque);
        var baseWalls=new GameObject("ContinuousStoneWallCore",typeof(Tilemap),typeof(TilemapRenderer));baseWalls.transform.SetParent(theme.transform,false);baseWalls.GetComponent<TilemapRenderer>().sortingOrder=-10;foreach(var p in walls)baseWalls.GetComponent<Tilemap>().SetTile(p,wallTile);
        int windows=0,facades=0;
        for(int face=0;face<2;face++)
        {
            int sign=face==0?1:-1;var edge=new HashSet<Vector3Int>();foreach(var p in walls)if(walkable.Contains(p+Vector3Int.down*sign))
            {
                bool oppositeFace=false;
                if(face==1)for(int y=0;y<=3;y++){var q=p+Vector3Int.down*y;if(!walls.Contains(q))break;if(walkable.Contains(q+Vector3Int.down)){oppositeFace=true;break;}}
                if(!oppositeFace)edge.Add(p);
            }
            foreach(var start in edge)if(!edge.Contains(start+Vector3Int.left))
            {
                int length=1;while(edge.Contains(start+Vector3Int.right*length))length++;
                if(length<4)continue;
                float height=3.05f;
                for(int x=0;x<length;x++)for(int y=1;y<=3;y++)if(walkable.Contains(start+Vector3Int.right*x+Vector3Int.up*(sign*y)))height=Mathf.Min(height,y);
                var center=ground.GetCellCenterWorld(start);float edgeY=center.y-sign*.5f;
                int panels=Mathf.CeilToInt(length/3f);float span=(float)length/panels;
                for(int i=0;i<panels;i++){Place(theme.transform,"StoneWallPanel",masonry,new Vector2(center.x-.5f+(i+.5f)*span,edgeY+sign*height*.5f),new Vector2(span+.015f,height),-8,new Color(.75f,.78f,.8f));facades++;}
                if(height>=1.7f)
                {
                    float h=Mathf.Min(2.9f,height-.12f),w=h*1.72f/2.9f;
                    for(int x=2;x<length-1;x+=6){Place(theme.transform,"ArchedWindow",window,new Vector2(center.x+x,edgeY+sign*height*.5f),new Vector2(w,h),-6,new Color(.94f,.97f,1));windows++;}
                }
                for(int x=0;x<length;x+=6)Place(theme.transform,"StonePilaster",pillar,new Vector2(center.x+x-.1f,edgeY+sign*height*.5f),new Vector2(.70f,height),-5,new Color(.86f,.89f,.9f));
                Place(theme.transform,"EndPilaster",pillar,new Vector2(center.x+length-.85f,edgeY+sign*height*.5f),new Vector2(.70f,height),-5,new Color(.86f,.89f,.9f));
            }
        }
        var vertical=new HashSet<Vector3Int>();foreach(var p in walls)if(walkable.Contains(p+Vector3Int.left)||walkable.Contains(p+Vector3Int.right))vertical.Add(p);
        foreach(var start in vertical)if(!vertical.Contains(start+Vector3Int.down))
        {
            int length=1;while(vertical.Contains(start+Vector3Int.up*length))length++;var center=ground.GetCellCenterWorld(start);int count=Mathf.CeilToInt(length/3f);float h=(float)length/count;
            for(int i=0;i<count;i++)Place(theme.transform,"StraightStoneSide",side,new Vector2(center.x,center.y-.5f+(i+.5f)*h),new Vector2(1.015f,h+.015f),-9,new Color(.82f,.85f,.87f));
        }
        StoneStageThreeBuilder.TrimPassageWalls(theme.transform,ground,walkable,"Stage"+stage+"SkyTrim");
        StoneStageThreeBuilder.FinalizeWallLayers(theme.transform);StoneStageThreeBuilder.RestoreVanishingWall(scene);
        // Solid backing at every facade prevents the moving sky showing through transparent edges.
        foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())
        {
            var b=sr.bounds;Place(theme.transform,"OpaqueMasonryBacking",white,b.center,new Vector2(b.size.x,b.size.y),-18,new Color(.34f,.37f,.40f));
        }
        var bounds=ground.localBounds;var worldCenter=ground.transform.TransformPoint(bounds.center);
        Place(theme.transform,"ClearBlueSky",white,worldCenter,new Vector2(bounds.size.x+160,bounds.size.y+160),-30,new Color(.43f,.74f,.94f));
        var clouds=new Sprite[3];for(int i=0;i<3;i++)clouds[i]=SpriteAt(Sky+"SkyClouds.png","SkyCloud"+i);
        var list=new List<Transform>();var random=new System.Random(318+stage);float left=worldCenter.x-bounds.extents.x-45,right=worldCenter.x+bounds.extents.x+45;
        for(float y=worldCenter.y-bounds.extents.y-40;y<worldCenter.y+bounds.extents.y+40;y+=10)for(float x=left;x<right;x+=15)
        {
            var sp=clouds[random.Next(3)];float width=7+(float)random.NextDouble()*7;var sr=Place(theme.transform,"DriftingSkyCloud",sp,new Vector2(x+(float)random.NextDouble()*7,y+(float)random.NextDouble()*5),new Vector2(width,width*sp.bounds.size.y/sp.bounds.size.x),-20,new Color(1,1,1,.82f+(float)random.NextDouble()*.15f));list.Add(sr.transform);
        }
        var drift=theme.AddComponent<MidBossCloudDrift>();drift.left=left-15;drift.right=right+15;drift.clouds=list.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        typeof(ForestStageBuilder).GetMethod("Capture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{scene,ground,stage});
        EditorSceneManager.CloseScene(scene,true);return "Stage"+stage+": floor="+floorCount+", original wall cells="+walls.Count+", facades="+facades+", windows="+windows+", clouds="+list.Count;
    }
}
#endif
