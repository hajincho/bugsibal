#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public static class StoneStageThreeBuilder
{
    const string Folder="Assets/Script/Environment/StoneBuilding";
    static Sprite masonry,pillar,sideWall;
    public static string Build()
    {
        if(Application.isPlaying)throw new Exception("Edit mode required");
        Directory.CreateDirectory(Folder);
        string gen=@"C:\Users\fromj\.codex\generated_images\01a17d8d-cd5b-77a3-a41b-b09f9e144b73\";
        File.Copy(gen+"exec-f286df5c-ee91-4970-9d2b-75340966f530.png",Folder+"/RPGFlagstone.png",true);
        File.Copy(gen+"exec-e7a59a6a-0a41-4665-a905-409bd38e6b74.png",Folder+"/ArchedWindow.png",true);
        File.Copy(@"C:\Users\fromj\Documents\Codex\2026-10-27\su\original-stage3-shadow-cells.csv",Folder+"/OutdoorShadowMask.csv",true);
        AssetDatabase.Refresh();
        var full=Import(Folder+"/RPGFlagstone.png",64);var window=Import(Folder+"/ArchedWindow.png",128);PrepareArchitecture(gen);
        var texture=full.texture;var tiles=new Tile[16];int half=texture.width/4;
        for(int i=0;i<16;i++)
        {
            string spritePath=Folder+"/RPGWideQuarter"+i+".asset";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if(sprite==null){sprite=Sprite.Create(texture,new Rect((i%4)*half,(i/4)*half,half,half),new Vector2(.5f,.5f),half,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(sprite,spritePath);}
            string tilePath=Folder+"/RPGWideFloorTile"+i+".asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,tilePath);}tile.sprite=sprite;tile.flags=TileFlags.None;tile.colliderType=Tile.ColliderType.None;EditorUtility.SetDirty(tile);tiles[i]=tile;
        }
        string path="Assets/Scenes/Stage/Stage3.unity";
        string backup=@"C:\Users\fromj\Documents\Codex\2026-10-27\su\output\forest-backups";Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"Stage3-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity"));
        var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);Tilemap ground=null,wall=null;
        foreach(var root in scene.GetRootGameObjects())foreach(var tm in root.GetComponentsInChildren<Tilemap>()){if(tm.name=="Groud")ground=tm;if(tm.name=="Wall")wall=tm;}
        foreach(var root in scene.GetRootGameObjects())if(root.name=="StoneBuildingTheme")UnityEngine.Object.DestroyImmediate(root);
        var theme=new GameObject("StoneBuildingTheme",typeof(Grid));SceneManager.MoveGameObjectToScene(theme,scene);
        // Original outdoor shadow cells were never indoor flooring.
        foreach(var line in File.ReadAllLines(Folder+"/OutdoorShadowMask.csv")){var xy=line.Split(',');if(xy.Length==2)ground.SetTile(new Vector3Int(int.Parse(xy[0]),int.Parse(xy[1]),0),null);}
        ground.CompressBounds();
        var cells=new HashSet<Vector3Int>();foreach(var p in ground.cellBounds.allPositionsWithin)if(ground.HasTile(p))cells.Add(p);
        ground.color=new Color(.85f,.86f,.84f);
        foreach(var p in cells){int index=((p.x%4+4)%4)+((p.y%4+4)%4)*4;ground.SetTile(p,tiles[index]);ground.SetTileFlags(p,TileFlags.None);float shade=.95f+Mathf.PerlinNoise((p.x+60)*.13f,(p.y+60)*.13f)*.08f;ground.SetColor(p,new Color(shade,shade,shade));}
        foreach(var root in scene.GetRootGameObjects())foreach(var tm in root.GetComponentsInChildren<Tilemap>())if(tm.GetComponent<Collider2D>()!=null)
        {
            tm.GetComponent<TilemapRenderer>().enabled=false;
        }
        int windows=0,facades=0;
        var northern=new HashSet<Vector3Int>();foreach(var p in cells)if(!cells.Contains(p+Vector3Int.up))northern.Add(p);
        foreach(var p in northern)if(!northern.Contains(p+Vector3Int.left))
        {
            int length=1;while(northern.Contains(p+Vector3Int.right*length))length++;
            if(length<4)continue; // Short isolated caps stay low instead of becoming lone tall blocks.
            var center=ground.GetCellCenterWorld(p);
            for(int x=0;x<length;x+=3)
            {
                int span=Mathf.Min(3,length-x);float start=x==0?-1.555f:x-.5f;float end=x+span==length?length+.555f:x+span-.5f;Place(theme.transform,"MasonryWallPanel",masonry,center+new Vector3((start+end)*.5f,1.9f,0),new Vector2(end-start+.02f,3.05f),-9,new Color(.75f,.78f,.8f));facades++;
            }
            for(int x=2;x<length-1;x+=6){Place(theme.transform,"LargeArchedWindow",window,center+new Vector3(x,1.95f,0),new Vector2(1.72f,2.9f),-7,new Color(.94f,.97f,1));windows++;}
            for(int x=6;x<length;x+=6)Place(theme.transform,"WallPilaster",pillar,center+new Vector3(x-.5f,1.9f,0),new Vector2(.78f,3.05f),-6,new Color(.86f,.89f,.9f));
            foreach(float endX in new[]{-.98f,length-.02f})Place(theme.transform,"CornerPilaster",pillar,center+new Vector3(endX,1.9f,0),new Vector2(1.15f,3.05f),-6,new Color(.86f,.89f,.9f));
        }
        CompleteBoundary(theme.transform,ground,wall,cells,window);
        int trimmed=TrimPassageWalls(theme.transform,ground,cells);
        FinalizeWallLayers(theme.transform);
        RestoreVanishingWall(scene);
        var outdoors=new GameObject("ExteriorForestGround",typeof(Tilemap),typeof(TilemapRenderer));outdoors.transform.SetParent(theme.transform,false);var outside=outdoors.GetComponent<Tilemap>();outdoors.GetComponent<TilemapRenderer>().sortingOrder=-13;outside.color=new Color(.3f,.37f,.3f);
        var grass=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Script/Environment/Forest/ForestGrass.asset");var water=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Script/Environment/Forest/ForestWater.asset");
        Sprite weed=null;var trees=new List<Sprite>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Script/Environment/Forest/ForestProps.png")){var sprite=asset as Sprite;if(sprite==null)continue;if(sprite.name=="Oak"||sprite.name=="Pine")trees.Add(sprite);if(sprite.name=="Shrub")weed=sprite;}
        var bounds=ground.cellBounds;int treeCount=0,wetCount=0,weedCount=0;
        for(int y=bounds.yMin-23;y<bounds.yMax+23;y++)for(int x=bounds.xMin-30;x<bounds.xMax+30;x++)
        {
            var p=new Vector3Int(x,y,0);if(cells.Contains(p))continue;
            bool close=false;for(int dy=-4;dy<=4&&!close;dy++)for(int dx=-3;dx<=3;dx++)if(cells.Contains(p+new Vector3Int(dx,dy,0))||RealWall(wall,p+new Vector3Int(dx,dy,0))){close=true;break;}
            float stream=Mathf.Abs(x-(bounds.center.x+Mathf.Sin(y*.12f)*13));bool wet=!close&&(stream<1.3f||Mathf.PerlinNoise((x+81)*.1f,(y+43)*.1f)>.76f);
            outside.SetTile(p,wet?water:grass);if(wet)wetCount++;
            if(!close&&!wet&&x%2==0&&y%3==0&&trees.Count>0)
            {
                var sprite=trees[Mathf.Abs((x*7+y*13)/3)%trees.Count];float width=3.8f+Mathf.PerlinNoise(x*.4f+40,y*.4f+40)*.7f;
                var go=new GameObject("SomberExteriorTree",typeof(SpriteRenderer));go.transform.SetParent(theme.transform,false);go.transform.position=ground.GetCellCenterWorld(p);var sr=go.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=new Color(.43f,.48f,.4f);sr.sortingOrder=-7;go.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);treeCount++;
            }
            if(!close&&!wet&&x%3==1&&y%3==1&&weed!=null)
            {
                var go=new GameObject("LargeExteriorWeeds",typeof(SpriteRenderer));go.transform.SetParent(theme.transform,false);go.transform.position=ground.GetCellCenterWorld(p);var sr=go.GetComponent<SpriteRenderer>();sr.sprite=weed;sr.color=new Color(.45f,.52f,.4f);sr.sortingOrder=-7;go.transform.localScale=Vector3.one*(4.8f/weed.bounds.size.x);weedCount++;
            }
        }
        ClearPlantOverlap(theme.transform,ground,cells);
        StabilizeFoliage(theme.transform);
        var manager=Array.Find(scene.GetRootGameObjects(),r=>r.name=="Stage3Manager");if(manager.GetComponent<StageThreeBuildingEntry>()==null)manager.AddComponent<StageThreeBuildingEntry>();
        var spawn=ground.WorldToCell(new Vector3(-23,0,0));bool safe=cells.Contains(spawn)&&!wall.HasTile(spawn);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        typeof(ForestStageBuilder).GetMethod("Capture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{scene,ground,3});
        EditorSceneManager.CloseScene(scene,true);return "Stage3 indoor floor="+cells.Count+", trimmed wall pieces="+trimmed+", northern wall faces="+facades+", windows="+windows+", exterior trees="+treeCount+", enlarged weeds="+weedCount+", water="+wetCount+", left spawn safe="+safe;
    }
    static Sprite Import(string path,int max)
    {
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=max;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static bool RealWall(Tilemap wall,Vector3Int p){var tile=wall.GetTile(p);return tile!=null&&!tile.name.Contains("Shadows");}
    static void CompleteBoundary(Transform theme,Tilemap ground,Tilemap wall,HashSet<Vector3Int> cells,Sprite window)
    {
        var bottom=new HashSet<Vector3Int>();var left=new HashSet<Vector3Int>();var right=new HashSet<Vector3Int>();
        var missing=new HashSet<Vector3Int>();
        foreach(var p in cells)
        {
            if(!cells.Contains(p+Vector3Int.down))bottom.Add(p);
            if(!cells.Contains(p+Vector3Int.left))left.Add(p);
            if(!cells.Contains(p+Vector3Int.right))right.Add(p);
            foreach(var d in new[]{Vector3Int.up,Vector3Int.down,Vector3Int.left,Vector3Int.right})if(!cells.Contains(p+d)&&!wall.HasTile(p+d))missing.Add(p+d);
        }
        foreach(var p in bottom)if(!bottom.Contains(p+Vector3Int.left))
        {
            int length=1;while(bottom.Contains(p+Vector3Int.right*length))length++;var center=ground.GetCellCenterWorld(p);
            for(int x=0;x<length;x+=3){int span=Mathf.Min(3,length-x);float start=x==0?-1.555f:x-.5f;float end=x+span==length?length+.555f:x+span-.5f;Place(theme,"SouthFacingMasonry",masonry,center+new Vector3((start+end)*.5f,-1.9f,0),new Vector2(end-start+.02f,3.05f),-8,new Color(.83f,.86f,.88f));}
            if(length>=4)
            {
                for(int x=6;x<length;x+=6)Place(theme,"SouthWallPilaster",pillar,center+new Vector3(x-.5f,-1.9f,0),new Vector2(.75f,3.05f),-6,new Color(.87f,.89f,.9f));
                foreach(float endX in new[]{-.98f,length-.02f})Place(theme,"SouthCornerPilaster",pillar,center+new Vector3(endX,-1.9f,0),new Vector2(1.15f,3.05f),-6,new Color(.87f,.89f,.9f));
            }
        }
        foreach(var p in bottom)
        {
            // Match the actual window column above, including runs split by a doorway.
            var top=p;while(cells.Contains(top+Vector3Int.up))top+=Vector3Int.up;
            var start=top;while(cells.Contains(start+Vector3Int.left)&&!cells.Contains(start+Vector3Int.left+Vector3Int.up))start+=Vector3Int.left;
            int length=1;while(cells.Contains(start+Vector3Int.right*length)&&!cells.Contains(start+Vector3Int.right*length+Vector3Int.up))length++;
            int offset=p.x-start.x;
            if(length>=4&&offset>=2&&offset<length-1&&(offset-2)%6==0)Place(theme,"SouthArchedWindow",window,ground.GetCellCenterWorld(p)+new Vector3(0,-1.95f,0),new Vector2(1.72f,2.9f),-7,new Color(.94f,.97f,1));
        }
        for(int side=0;side<2;side++)
        {
            var edge=side==0?left:right;float sign=side==0?-1:1;
            foreach(var p in edge)if(!edge.Contains(p+Vector3Int.down))
            {
                int length=1;while(edge.Contains(p+Vector3Int.up*length))length++;var center=ground.GetCellCenterWorld(p);
                var top=p+Vector3Int.up*(length-1);int northLength=1;
                for(var q=top+Vector3Int.left;cells.Contains(q)&&!cells.Contains(q+Vector3Int.up);q+=Vector3Int.left)northLength++;
                for(var q=top+Vector3Int.right;cells.Contains(q)&&!cells.Contains(q+Vector3Int.up);q+=Vector3Int.right)northLength++;
                float low=-3.425f,high=length-1+(northLength>=4?3.425f:.52f);int modules=Mathf.CeilToInt((high-low)/3f);float height=(high-low)/modules;
                for(int y=0;y<modules;y++)
                {
                    Place(theme,"VerticalStoneSide",sideWall,center+new Vector3(sign*.98f,low+(y+.5f)*height,0),new Vector2(1.15f,height+.02f),-8,new Color(.82f,.85f,.87f));
                }
            }
        }
        if(missing.Count>0)
        {
            var tilePath=Folder+"/CompletedBoundaryTile.asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(tilePath);if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,tilePath);}tile.colliderType=Tile.ColliderType.Grid;EditorUtility.SetDirty(tile);
            var go=new GameObject("CompletedBuildingPerimeter",typeof(Tilemap),typeof(TilemapCollider2D));go.transform.SetParent(theme,false);var tm=go.GetComponent<Tilemap>();foreach(var p in missing)tm.SetTile(p,tile);
        }
    }
    public static int TrimPassageWalls(Transform theme,Tilemap ground,HashSet<Vector3Int> cells,string outputName="TrimmedWalls")
    {
        // Subtract the walkable floor from decorative masonry, preserving texture scale.
        var floor=new List<Rect>();
        foreach(var p in cells)if(!cells.Contains(p+Vector3Int.left))
        {
            int length=1;while(cells.Contains(p+Vector3Int.right*length))length++;
            var center=ground.GetCellCenterWorld(p);floor.Add(new Rect(center.x-.5f,center.y-.5f,length,1));
        }
        string folder=Folder+"/"+outputName;Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        int index=0,trimmed=0;
        foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())
        {
            var bounds=sr.bounds;var original=new Rect(bounds.min.x,bounds.min.y,bounds.size.x,bounds.size.y);
            var pieces=new List<Rect>{original};bool changed=false;
            foreach(var cut in floor)
            {
                var next=new List<Rect>();
                foreach(var piece in pieces)
                {
                    float x0=Mathf.Max(piece.xMin,cut.xMin),x1=Mathf.Min(piece.xMax,cut.xMax),y0=Mathf.Max(piece.yMin,cut.yMin),y1=Mathf.Min(piece.yMax,cut.yMax);
                    if(x1-x0<.001f||y1-y0<.001f){next.Add(piece);continue;}
                    changed=true;
                    if(x0-piece.xMin>.01f)next.Add(Rect.MinMaxRect(piece.xMin,piece.yMin,x0,piece.yMax));
                    if(piece.xMax-x1>.01f)next.Add(Rect.MinMaxRect(x1,piece.yMin,piece.xMax,piece.yMax));
                    if(y0-piece.yMin>.01f)next.Add(Rect.MinMaxRect(x0,piece.yMin,x1,y0));
                    if(piece.yMax-y1>.01f)next.Add(Rect.MinMaxRect(x0,y1,x1,piece.yMax));
                }
                pieces=next;if(pieces.Count==0)break;
            }
            if(changed)
            {
                var source=sr.sprite;var rect=source.rect;int part=0;
                foreach(var piece in pieces)
                {
                    var crop=new Rect(rect.x+(piece.x-original.x)/original.width*rect.width,rect.y+(piece.y-original.y)/original.height*rect.height,piece.width/original.width*rect.width,piece.height/original.height*rect.height);
                    if(crop.width<.5f||crop.height<.5f)continue;
                    var sprite=Sprite.Create(source.texture,crop,new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);sprite.name=source.name+"_PassageTrim";
                    string path=folder+"/Wall"+index+"Part"+(part++)+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if(existing==null){AssetDatabase.CreateAsset(sprite,path);existing=sprite;}else{EditorUtility.CopySerialized(sprite,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(sprite);}
                    Place(theme,sr.name,existing,new Vector3(piece.center.x,piece.center.y,sr.transform.position.z),piece.size,sr.sortingOrder,sr.color);
                }
                UnityEngine.Object.DestroyImmediate(sr.gameObject);trimmed++;
            }
            index++;
        }
        return trimmed;
    }
    public static int RestoreVanishingWall(Scene scene)
    {
        Sprite sprite=null;
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Folder+"/StraightIStoneWall.png")){var sp=asset as Sprite;if(sp!=null&&sp.name=="StraightIWall")sprite=sp;}
        if(sprite==null)throw new Exception("Missing straight stone gate sprite");
        int restored=0;
        foreach(var root in scene.GetRootGameObjects())foreach(var door in root.GetComponentsInChildren<DoorManager>(true))
        {
            var map=door.GetComponent<Tilemap>();if(map==null)continue;
            var old=door.transform.Find("VanishingStoneGate");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            bool hasTile=false;var bounds=new Bounds();
            foreach(var p in map.cellBounds.allPositionsWithin)if(map.HasTile(p)){var cellBounds=new Bounds(map.GetCellCenterWorld(p),new Vector3(1,1,0));if(!hasTile){bounds=cellBounds;hasTile=true;}else bounds.Encapsulate(cellBounds);}
            if(!hasTile)continue;
            // Keep the existing collider and disappearance timer on their original owner.
            door.door=door.gameObject;door.enabled=true;map.GetComponent<TilemapRenderer>().enabled=false;
            Place(door.transform,"VanishingStoneGate",sprite,bounds.center,new Vector2(bounds.size.x+.1f,bounds.size.y),-5,new Color(.76f,.79f,.81f));
            EditorUtility.SetDirty(door);restored++;
        }
        return restored;
    }
    public static int FinalizeWallLayers(Transform theme)
    {
        int removed=0,index=0;
        foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())
        {
            if(sr.name=="ContinuousStoneBoundary"){UnityEngine.Object.DestroyImmediate(sr.gameObject);removed++;continue;}
            if(sr.name=="SomberExteriorTree"||sr.name=="LargeExteriorWeeds")continue;
            // Joined wall panels can overlap slightly; use a fixed unique depth at each join.
            var position=sr.transform.position;position.z=.5f+index++*.001f;sr.transform.position=position;
        }
        return removed;
    }
    static void ClearPlantOverlap(Transform theme,Tilemap ground,HashSet<Vector3Int> cells)
    {
        var plants=new List<SpriteRenderer>();var walls=new List<Bounds>();
        foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())if(sr.name=="SomberExteriorTree"||sr.name=="LargeExteriorWeeds")plants.Add(sr);else{var b=sr.bounds;b.Expand(new Vector3(.35f,.35f,0));walls.Add(b);}
        foreach(var plant in plants)
        {
            Vector3 nearest=Vector3.zero;float best=float.MaxValue;foreach(var p in cells){var point=ground.GetCellCenterWorld(p);float distance=((Vector2)(plant.bounds.center-point)).sqrMagnitude;if(distance<best){best=distance;nearest=point;}}
            var delta=plant.bounds.center-nearest;var direction=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?new Vector3(Mathf.Sign(delta.x),0,0):new Vector3(0,Mathf.Sign(delta.y),0);
            for(int i=0;i<96;i++){bool overlaps=false;foreach(var b in walls)if(Overlap(plant.bounds,b)){overlaps=true;break;}if(!overlaps)break;plant.transform.position+=direction*.3f;}
            foreach(var b in walls)if(Overlap(plant.bounds,b)){UnityEngine.Object.DestroyImmediate(plant.gameObject);break;}
        }
    }
    static void StabilizeFoliage(Transform theme)
    {
        var plants=new List<SpriteRenderer>();var trees=new List<SpriteRenderer>();
        foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())if(sr.name=="SomberExteriorTree"||sr.name=="LargeExteriorWeeds")plants.Add(sr);
        plants.Sort((a,b)=>{int y=b.bounds.min.y.CompareTo(a.bounds.min.y);return y!=0?y:a.transform.position.x.CompareTo(b.transform.position.x);});
        int depthIndex=0;
        foreach(var plant in plants)
        {
            if(plant.name=="SomberExteriorTree")
            {
                bool duplicate=false;foreach(var tree in trees)if(((Vector2)(plant.transform.position-tree.transform.position)).sqrMagnitude<1.44f){duplicate=true;break;}
                if(duplicate){UnityEngine.Object.DestroyImmediate(plant.gameObject);continue;}trees.Add(plant);
            }
            // Fixed depth breaks sorting ties while keeping foliage above the forest floor.
            plant.sortingOrder=-10;var position=plant.transform.position;position.z=8f-depthIndex++*.001f;plant.transform.position=position;
        }
    }
    static bool Overlap(Bounds a,Bounds b){return a.min.x<b.max.x&&a.max.x>b.min.x&&a.min.y<b.max.y&&a.max.y>b.min.y;}
    static void PrepareArchitecture(string gen)
    {
        string path=Folder+"/StoneArchitecture.png";File.Copy(gen+"exec-323938c1-f1d3-4a5b-b692-ee6f21c0b5f4.png",path,true);AssetDatabase.ImportAsset(path);
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));var metadata=new SpriteMetaData[2];int split=(int)(image.width*.65f);
        for(int i=0;i<2;i++)
        {
            int left=i==0?0:split,right=i==0?split:image.width,minX=right,minY=image.height,maxX=left,maxY=0;
            for(int y=0;y<image.height;y++)for(int x=left;x<right;x++)if(image.GetPixel(x,y).a>.7f){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
            metadata[i]=new SpriteMetaData{name=i==0?"RPGMasonry":"RPGPilaster",rect=new Rect(minX-2,minY-2,maxX-minX+5,maxY-minY+5),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
        }
        UnityEngine.Object.DestroyImmediate(image);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritesheet=metadata;importer.spritePixelsPerUnit=100;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=256;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)){var sp=asset as Sprite;if(sp==null)continue;if(sp.name=="RPGMasonry")masonry=sp;else if(sp.name=="RPGPilaster")pillar=sp;}
        path=Folder+"/StraightIStoneWall.png";File.Copy(gen+"exec-4a26f69c-0566-4db1-aea7-3c52c0fb1eef.png",path,true);AssetDatabase.ImportAsset(path);
        image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));metadata=new SpriteMetaData[1];
        int minIX=image.width,minIY=image.height,maxIX=0,maxIY=0;
        for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++)if(image.GetPixel(x,y).a>.7f){minIX=Mathf.Min(minIX,x);minIY=Mathf.Min(minIY,y);maxIX=Mathf.Max(maxIX,x);maxIY=Mathf.Max(maxIY,y);}
        metadata[0]=new SpriteMetaData{name="StraightIWall",rect=new Rect(minIX-2,minIY-2,maxIX-minIX+5,maxIY-minIY+5),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
        UnityEngine.Object.DestroyImmediate(image);importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritesheet=metadata;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=256;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)){var sp=asset as Sprite;if(sp!=null&&sp.name=="StraightIWall")sideWall=sp;}
    }
    static SpriteRenderer Place(Transform parent,string name,Sprite sprite,Vector3 position,Vector2 size,int order,Color color)
    {
        var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);var sr=go.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=order;return sr;
    }
}
#endif
