#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MidBossSkyBuilder
{
    const string Folder="Assets/Script/Environment/MidBossSky";
    const string Stone="Assets/Script/Environment/StoneBuilding/";
    static int depth;
    static Sprite LoadSprite(string path,string name)
    {
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)){var sp=asset as Sprite;if(sp!=null&&sp.name==name)return sp;}
        throw new Exception("Missing sprite: "+name);
    }
    static SpriteRenderer Place(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size,int order,Color color)
    {
        var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.position=new Vector3(position.x,position.y,.5f+depth++*.001f);
        go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
        var sr=go.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;sr.color=color;return sr;
    }
    public static string Build(string cloudSource)
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        Directory.CreateDirectory(Folder);File.Copy(cloudSource,Folder+"/SkyClouds.png",true);AssetDatabase.ImportAsset(Folder+"/SkyClouds.png");
        var pixels=new Texture2D(2,2);pixels.LoadImage(File.ReadAllBytes(Folder+"/SkyClouds.png"));var metadata=new SpriteMetaData[3];int panel=pixels.width/3;
        for(int i=0;i<3;i++)
        {
            int cropMinX=(i+1)*panel-1,cropMaxX=i*panel,cropMinY=pixels.height-1,cropMaxY=0;
            for(int y=0;y<pixels.height;y++)for(int x=i*panel;x<(i+1)*panel;x++)if(pixels.GetPixel(x,y).a>.1f){cropMinX=Mathf.Min(cropMinX,x);cropMaxX=Mathf.Max(cropMaxX,x);cropMinY=Mathf.Min(cropMinY,y);cropMaxY=Mathf.Max(cropMaxY,y);}
            metadata[i]=new SpriteMetaData{name="SkyCloud"+i,rect=new Rect(cropMinX,cropMinY,cropMaxX-cropMinX+1,cropMaxY-cropMinY+1),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
        }
        UnityEngine.Object.DestroyImmediate(pixels);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/SkyClouds.png");importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritesheet=metadata;importer.spritePixelsPerUnit=64;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=512;importer.SaveAndReimport();
        var clouds=new Sprite[3];for(int i=0;i<3;i++)clouds[i]=LoadSprite(Folder+"/SkyClouds.png","SkyCloud"+i);
        string skyPath=Folder+"/SkyBlue.png";
        if(!File.Exists(skyPath)){var tex=new Texture2D(2,2);tex.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});tex.Apply();File.WriteAllBytes(skyPath,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
        AssetDatabase.ImportAsset(skyPath);var skyImporter=(TextureImporter)AssetImporter.GetAtPath(skyPath);skyImporter.textureType=TextureImporterType.Sprite;skyImporter.spriteImportMode=SpriteImportMode.Single;skyImporter.filterMode=FilterMode.Point;skyImporter.mipmapEnabled=false;skyImporter.SaveAndReimport();var sky=AssetDatabase.LoadAssetAtPath<Sprite>(skyPath);
        var masonry=LoadSprite(Stone+"StoneArchitecture.png","RPGMasonry");var pillar=LoadSprite(Stone+"StoneArchitecture.png","RPGPilaster");var side=LoadSprite(Stone+"StraightIStoneWall.png","StraightIWall");var window=AssetDatabase.LoadAssetAtPath<Sprite>(Stone+"ArchedWindow.png");
        string path="Assets/Scenes/Stage/MidBoss.unity";File.Copy(path,@"C:\Users\fromj\Documents\Codex\2026-10-27\su\output\forest-backups\MidBoss-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
        var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);Tilemap ground=null;
        foreach(var root in scene.GetRootGameObjects())foreach(var tm in root.GetComponentsInChildren<Tilemap>()){if(tm.name=="ground")ground=tm;else if(tm.GetComponent<Collider2D>()!=null)tm.GetComponent<TilemapRenderer>().enabled=false;}
        foreach(var root in scene.GetRootGameObjects())if(root.name=="MidBossStoneSkyTheme")UnityEngine.Object.DestroyImmediate(root);
        var theme=new GameObject("MidBossStoneSkyTheme");SceneManager.MoveGameObjectToScene(theme,scene);depth=0;
        ground.CompressBounds();int count=0;foreach(var p in ground.cellBounds.allPositionsWithin)if(ground.HasTile(p))
        {
            int index=((p.x%4+4)%4)+((p.y%4+4)%4)*4;ground.SetTile(p,AssetDatabase.LoadAssetAtPath<Tile>(Stone+"RPGWideFloorTile"+index+".asset"));ground.SetTileFlags(p,TileFlags.None);float shade=.95f+Mathf.PerlinNoise((p.x+60)*.13f,(p.y+60)*.13f)*.08f;ground.SetColor(p,new Color(shade,shade,shade));count++;
        }
        ground.color=new Color(.85f,.86f,.84f);ground.GetComponent<TilemapRenderer>().sortingOrder=-12;
        var b=ground.localBounds;Vector3 center=ground.transform.TransformPoint(b.center);float minX=center.x-b.extents.x,maxX=center.x+b.extents.x,minY=center.y-b.extents.y,maxY=center.y+b.extents.y;
        Place(theme.transform,"ClearBlueSky",sky,center,new Vector2(240,220),-30,new Color(.43f,.74f,.94f));
        var cloudTransforms=new List<Transform>();var random=new System.Random(318);
        for(int y=-75;y<=80;y+=10)for(int x=-80;x<=90;x+=15)
        {
            Vector2 p=new Vector2(x+(float)random.NextDouble()*7,y+(float)random.NextDouble()*5);float width=7+(float)random.NextDouble()*7;var sp=clouds[random.Next(3)];
            var sr=Place(theme.transform,"DriftingSkyCloud",sp,p,new Vector2(width,width*sp.bounds.size.y/sp.bounds.size.x),-20,new Color(1,1,1,.82f+(float)random.NextDouble()*.15f));cloudTransforms.Add(sr.transform);
        }
        var drift=theme.AddComponent<MidBossCloudDrift>();drift.clouds=cloudTransforms.ToArray();
        int windows=0;
        for(int face=0;face<2;face++)
        {
            float sign=face==0?1:-1,edge=face==0?maxY:minY;float width=maxX-minX+2.2f;int panels=Mathf.CeilToInt(width/3f);float span=width/panels;
            for(int i=0;i<panels;i++)Place(theme.transform,"StoneWallPanel",masonry,new Vector2(minX-1.1f+(i+.5f)*span,edge+sign*1.525f),new Vector2(span+.01f,3.05f),-8,new Color(.75f,.78f,.8f));
            for(float x=minX+2.5f;x<maxX-1;x+=6){Place(theme.transform,"ArchedWindow",window,new Vector2(x,edge+sign*1.5f),new Vector2(1.72f,2.9f),-6,new Color(.94f,.97f,1));windows++;}
            for(float x=minX+6;x<maxX;x+=6)Place(theme.transform,"WallPilaster",pillar,new Vector2(x,edge+sign*1.525f),new Vector2(.78f,3.05f),-5,new Color(.86f,.89f,.9f));
            foreach(float x in new[]{minX-.55f,maxX+.55f})Place(theme.transform,"CornerPilaster",pillar,new Vector2(x,edge+sign*1.525f),new Vector2(1.1f,3.05f),-5,new Color(.86f,.89f,.9f));
        }
        float low=minY-3.05f,high=maxY+3.05f;int modules=Mathf.CeilToInt((high-low)/3);float height=(high-low)/modules;
        foreach(float x in new[]{minX-.55f,maxX+.55f})for(int i=0;i<modules;i++)Place(theme.transform,"StraightStoneSide",side,new Vector2(x,low+(i+.5f)*height),new Vector2(1.1f,height+.01f),-8,new Color(.82f,.85f,.87f));
        SealStructure(scene);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        typeof(ForestStageBuilder).GetMethod("Capture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{scene,ground,99});
        EditorSceneManager.CloseScene(scene,true);return "MidBoss floor="+count+", windows="+windows+", drifting clouds="+clouds.Length+" variants/"+cloudTransforms.Count+" instances";
    }
    public static string SealStructure(Scene scene)
    {
        Tilemap ground=null;Transform theme=null;
        foreach(var root in scene.GetRootGameObjects()){if(root.name=="MidBossStoneSkyTheme")theme=root.transform;foreach(var tm in root.GetComponentsInChildren<Tilemap>())if(tm.name=="ground")ground=tm;}
        if(theme==null||ground==null)throw new Exception("Missing MidBoss theme or floor");
        var old=theme.Find("SealedStoneFoundation");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var foundation=new GameObject("SealedStoneFoundation");foundation.transform.SetParent(theme,false);
        var sky=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/SkyBlue.png");var masonry=LoadSprite(Stone+"StoneArchitecture.png","RPGMasonry");
        var r=masonry.rect;var coreRect=new Rect(r.x+r.width*.2f,r.y+r.height*.2f,r.width*.6f,r.height*.6f);
        var generated=Sprite.Create(masonry.texture,coreRect,new Vector2(.5f,.5f),masonry.pixelsPerUnit,0,SpriteMeshType.FullRect);generated.name="SolidMasonryCore";
        string corePath=Folder+"/SolidMasonryCore.asset";var core=AssetDatabase.LoadAssetAtPath<Sprite>(corePath);
        if(core==null){AssetDatabase.CreateAsset(generated,corePath);core=generated;}else{EditorUtility.CopySerialized(generated,core);EditorUtility.SetDirty(core);UnityEngine.Object.DestroyImmediate(generated);}
        var b=ground.localBounds;var center=ground.transform.TransformPoint(b.center);float left=center.x-b.extents.x,right=center.x+b.extents.x,bottom=center.y-b.extents.y,top=center.y+b.extents.y;
        // Opaque foundation blocks sky and drifting clouds throughout the building footprint.
        Place(foundation.transform,"OpaqueBuildingFootprint",sky,center,new Vector2(right-left+2.2f,top-bottom+6.1f),-19,new Color(.29f,.32f,.35f));
        int index=0;
        foreach(float y in new[]{top+1.525f,bottom-1.525f})
        {
            int panels=Mathf.CeilToInt((right-left+2.2f)/3);float width=(right-left+2.2f)/panels;
            for(int i=0;i<panels;i++)Place(foundation.transform,"HorizontalSeamStone",core,new Vector2(left-1.1f+(i+.5f)*width,y),new Vector2(width+.035f,3.09f),-10,new Color(.72f,.75f,.77f));
        }
        float low=bottom-3.05f,high=top+3.05f;int rows=Mathf.CeilToInt((high-low)/2);float height=(high-low)/rows;
        foreach(float x in new[]{left-.55f,right+.55f})for(int i=0;i<rows;i++)
        {
            var sr=Place(foundation.transform,"VerticalSeamStone",core,new Vector2(x,low+(i+.5f)*height),new Vector2(1.14f,height+.035f),-10,new Color(.67f,.7f,.73f));
            var p=sr.transform.position;p.z=1.5f+index++*.001f;sr.transform.position=p;
        }
        AssetDatabase.SaveAssets();return "Opaque footprint and continuous masonry backing added";
    }
}
#endif
