#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class ForestStageBuilder
{
    const string Folder="Assets/Script/Environment/Forest";
    static readonly string[] Names={"Oak","Pine","Boulders","Log","Shrub","Rock"};
    static Sprite[] props;
    static Sprite[] crowns;
    static Sprite[] stoneSprites;
    static Tile dirt,grass,sand,water;
    public static string Build()
    {return BuildStages(new[]{1,2});}
    public static string BuildStages(int[] stages)
    {
        if(Application.isPlaying)throw new Exception("Exit Play mode first.");
        Prepare();var report=new System.Text.StringBuilder();
        var previous=SceneManager.GetActiveScene();
        foreach(int stage in stages)
        {
            string path="Assets/Scenes/Stage/Stage"+stage+".unity";
            string backup=@"C:\Users\fromj\Documents\Codex\2026-10-27\su\output\forest-backups";
            Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"Stage"+stage+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity"));
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects())if(root.name=="SkyStoneStageTheme")UnityEngine.Object.DestroyImmediate(root);
            Tilemap ground=null,wall=null;
            foreach(var root in scene.GetRootGameObjects())foreach(var map in root.GetComponentsInChildren<Tilemap>(true))
            {if(map.name=="Ground"||map.name=="Graound")ground=map;if(map.name=="Wall")wall=map;}
            if(ground==null||wall==null)throw new Exception("Missing stage tilemaps.");
            foreach(var root in scene.GetRootGameObjects())if(root.name=="ForestTheme")UnityEngine.Object.DestroyImmediate(root);
            var theme=new GameObject("ForestTheme",typeof(Grid));SceneManager.MoveGameObjectToScene(theme,scene);
            var cells=new HashSet<Vector3Int>();foreach(var p in ground.cellBounds.allPositionsWithin)if(ground.HasTile(p))cells.Add(p);
            ground.color=new Color(.66f,.72f,.73f);
            ground.GetComponent<TilemapRenderer>().sortingOrder=-10;
            foreach(var p in cells)
            {
                float n=Mathf.PerlinNoise((p.x+stage*41)*.11f,(p.y+32)*.13f);
                float pathNoise=Mathf.PerlinNoise((p.x+100)*.14f,(p.y+stage*15)*.15f);
                ground.SetTile(p,n>.66f?grass:pathNoise>.55f?sand:dirt);
                ground.SetTileFlags(p,TileFlags.None);
                float variation=.92f+Mathf.PerlinNoise(p.x*.4f+23,p.y*.4f+55)*.12f;
                var selected=ground.GetTile<Tile>(p);
                var tone=selected==grass?grass.color:Color.Lerp(dirt.color,sand.color,Mathf.SmoothStep(0,1,(pathNoise-.32f)/.4f));
                ground.SetColor(p,tone*new Color(variation,variation,variation,1));
            }
            // Keep the original wall tiles and their exact collider shapes; only replace their rendering.
            wall.GetComponent<TilemapRenderer>().enabled=false;
            int rocks=0;var covered=new HashSet<Vector3Int>();
            foreach(var p in wall.cellBounds.allPositionsWithin)if(wall.HasTile(p)&&!covered.Contains(p))
            {
                bool vertical=!wall.HasTile(p+Vector3Int.right)&&!wall.HasTile(p+Vector3Int.left);
                var along=vertical?Vector3Int.up:Vector3Int.right;
                bool doubleCell=wall.HasTile(p+along)&&!covered.Contains(p+along)&&Hash(p.x,p.y,stage)%3!=0;
                int kind=doubleCell?(vertical?new[]{0,2,5}[Hash(p.x,p.y,stage)%3]:new[]{1,3,4}[Hash(p.x,p.y,stage)%3]):Hash(p.x,p.y,stage)%6;
                covered.Add(p);if(doubleCell)covered.Add(p+along);
                var go=new GameObject("StoneWall_"+(kind+1)+"_"+(doubleCell?"2Cells":"1Cell"));go.transform.SetParent(theme.transform,false);
                float stagger=((Hash(p.x,p.y,83)%100)/100f-.5f)*.22f;
                go.transform.position=wall.GetCellCenterWorld(p)+(Vector3)(doubleCell?along:Vector3Int.zero)*.5f+new Vector3(vertical?stagger:0,.2f+(vertical?0:stagger),rocks*.001f);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=stoneSprites[kind];sr.sortingOrder=-8;sr.color=new Color(.73f,.76f,.69f);
                float span=doubleCell?2.5f:1.65f;
                float axis=vertical?sr.sprite.bounds.size.y:sr.sprite.bounds.size.x;
                go.transform.localScale=Vector3.one*(span/axis);
                rocks++;
            }
            var outsideGO=new GameObject("ForestOutside",typeof(Tilemap),typeof(TilemapRenderer));
            outsideGO.transform.SetParent(ground.transform.parent,false);
            var outside=outsideGO.GetComponent<Tilemap>();outsideGO.GetComponent<TilemapRenderer>().sortingOrder=-12;
            outside.color=new Color(.29f,.36f,.27f);
            var bounds=ground.cellBounds;int trees=0,waters=0;
            for(int y=bounds.yMin-28;y<bounds.yMax+28;y++)for(int x=bounds.xMin-36;x<bounds.xMax+36;x++)
            {
                var p=new Vector3Int(x,y,0);if(cells.Contains(p))continue;
                bool near=false;for(int dy=-2;dy<=2&&!near;dy++)for(int dx=-2;dx<=2;dx++)if(cells.Contains(p+new Vector3Int(dx,dy,0))){near=true;break;}
                float stream=Mathf.Abs(x-(bounds.center.x+Mathf.Sin(y*.19f+stage)*6+stage*4));
                float pond=Mathf.PerlinNoise((x+stage*53)*.12f,(y+74)*.12f);
                bool wet=!wall.HasTile(p)&&!near&&(stream<1.15f||pond>.73f);
                outside.SetTile(p,wet?water:grass);if(wet)waters++;
                if(wall.HasTile(p))continue;
                if(!wet&&x%2==0&&y%2==0&&!near)
                {
                    bool safe=true;for(int dy=-4;dy<=1&&safe;dy++)for(int dx=-2;dx<=2;dx++)if(cells.Contains(p+new Vector3Int(dx,dy,0))){safe=false;break;}
                    if(safe)
                    {
                        float jitter=(Hash(x,y,31)%100)/100f;
                        var position=ground.GetCellCenterWorld(p)+new Vector3(jitter*.7f-.35f,(Hash(y,x,12)%100)/150f,0);
                        int type=Hash(x,y,stage)%5==0?4:(Hash(x,y,stage)/17)%2;
                        Prop(theme.transform,type,position,type==4?1.6f:3.1f+jitter*.7f,-7,new Color(.45f,.49f,.40f));trees++;
                    }
                }
            }
            // Small low border plants break up the silhouette without hiding playable ground.
            foreach(var p in wall.cellBounds.allPositionsWithin)if(wall.HasTile(p)&&Hash(p.x,p.y,45)%7==0&&
                (!cells.Contains(p+Vector3Int.up)||!cells.Contains(p+Vector3Int.down)||!cells.Contains(p+Vector3Int.left)||!cells.Contains(p+Vector3Int.right)))
                Prop(theme.transform,4,wall.GetCellCenterWorld(p)+Vector3.down*.18f,.85f,-7,new Color(.65f,.68f,.52f));
            outsideGO.transform.SetParent(theme.transform,true);
            // Two purely decorative logs outside the walkable corners, never part of the wall.
            Prop(theme.transform,3,ground.GetCellCenterWorld(new Vector3Int(bounds.xMin-3,bounds.yMin-2,0)),2.7f,-8,new Color(.63f,.65f,.54f)).name="CornerLog_A";
            Prop(theme.transform,3,ground.GetCellCenterWorld(new Vector3Int(bounds.xMax+2,bounds.yMax+2,0)),2.4f,-8,new Color(.63f,.65f,.54f)).name="CornerLog_B";
            // Northern trees sit just behind the wall, with low branches in the 6.5-unit player view.
            foreach(var p in cells)if(!cells.Contains(p+Vector3Int.up)&&wall.HasTile(p+Vector3Int.up)&&Hash(p.x,0,stage)%3==0)
            {
                var pos=ground.GetCellCenterWorld(p)+new Vector3(.15f,2.5f,0);
                int kind=(Hash(p.x,p.y,53)/17)%2;
                var canopy=Prop(theme.transform,kind,pos,3.8f,-7,new Color(.43f,.48f,.39f));
            }
            SeparateFoliage(theme.transform,ground,cells);
            ClearPlayableFoliage(theme.transform,ground,wall);
            StabilizeFoliage(theme.transform);
            AddSparseOutsideLogs(scene,stage);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Capture(scene,ground,stage);
            report.AppendLine("Stage"+stage+": ground="+cells.Count+", boundary props="+rocks+", outside trees/shrubs="+trees+", water tiles="+waters);
            EditorSceneManager.CloseScene(scene,true);
        }
        if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
        AssetDatabase.SaveAssets();return report.ToString();
    }
    static int Hash(int x,int y,int seed){unchecked{return (int)((uint)(x*73856093^y*19349663^seed*83492791)&0x7fffffff);}}
    static void SeparateFoliage(Transform theme,Tilemap ground,HashSet<Vector3Int> cells)
    {
        var barriers=new List<Bounds>();var foliage=new List<SpriteRenderer>();
        foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())
        {
            if(sr.name.StartsWith("StoneWall_")||sr.name.StartsWith("CornerLog_")){var b=sr.bounds;b.Expand(new Vector3(.6f,.6f,2));barriers.Add(b);}
            else foliage.Add(sr);
        }
        foreach(var sr in foliage)
        {
            Vector3 closest=Vector3.zero;float best=float.MaxValue;
            foreach(var cell in cells){var center=ground.GetCellCenterWorld(cell);float d=((Vector2)(sr.bounds.center-center)).sqrMagnitude;if(d<best){best=d;closest=center;}}
            Vector3 direction=sr.bounds.center-closest;direction.z=0;
            if(Mathf.Abs(direction.x)>Mathf.Abs(direction.y))direction=new Vector3(Mathf.Sign(direction.x),0,0);else direction=new Vector3(0,Mathf.Sign(direction.y),0);
            for(int i=0;i<128;i++)
            {
                bool overlap=false;foreach(var b in barriers)if(OverlapXY(sr.bounds,b)){overlap=true;break;}
                if(!overlap)break;sr.transform.position+=direction*.25f;
            }
            // Narrow concave corners have no outward slot: omit only that decorative plant.
            foreach(var b in barriers)if(OverlapXY(sr.bounds,b)){UnityEngine.Object.DestroyImmediate(sr.gameObject);break;}
        }
    }
    static bool OverlapXY(Bounds a,Bounds b){return a.min.x<b.max.x&&a.max.x>b.min.x&&a.min.y<b.max.y&&a.max.y>b.min.y;}
    public static int ClearPlayableFoliage(Transform theme,Tilemap ground,Tilemap wall)
    {
        int removed=0;
        foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())
        {
            if(sr.name.StartsWith("StoneWall_")||sr.name.StartsWith("CornerLog_"))continue;
            var min=ground.WorldToCell(sr.bounds.min+new Vector3(.01f,.01f,0));
            var max=ground.WorldToCell(sr.bounds.max-new Vector3(.01f,.01f,0));bool overlaps=false;
            for(int y=min.y;y<=max.y&&!overlaps;y++)for(int x=min.x;x<=max.x;x++)
            {var p=new Vector3Int(x,y,0);if(ground.HasTile(p)&&!wall.HasTile(p)){overlaps=true;break;}}
            if(overlaps){UnityEngine.Object.DestroyImmediate(sr.gameObject);removed++;}
        }
        return removed;
    }
    static void StabilizeFoliage(Transform theme)
    {
        var plants=new List<SpriteRenderer>();var trees=new List<SpriteRenderer>();
        foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())if(!sr.name.StartsWith("StoneWall_")&&!sr.name.StartsWith("CornerLog_"))plants.Add(sr);
        plants.Sort((a,b)=>{int y=b.bounds.min.y.CompareTo(a.bounds.min.y);return y!=0?y:a.transform.position.x.CompareTo(b.transform.position.x);});
        int depth=0;
        foreach(var plant in plants)
        {
            if(plant.name=="Oak"||plant.name=="Pine")
            {
                bool duplicate=false;foreach(var tree in trees)if(((Vector2)(plant.transform.position-tree.transform.position)).sqrMagnitude<1.44f){duplicate=true;break;}
                if(duplicate){UnityEngine.Object.DestroyImmediate(plant.gameObject);continue;}trees.Add(plant);
            }
            var p=plant.transform.position;p.z=5f-depth++*.001f;plant.transform.position=p;
        }
    }
    public static int AddSparseOutsideLogs(Scene scene,int stage)
    {
        Transform theme=null;Tilemap ground=null,wall=null,outside=null;
        foreach(var root in scene.GetRootGameObjects())
        {
            if(root.name=="ForestTheme")theme=root.transform;
            foreach(var map in root.GetComponentsInChildren<Tilemap>())
            {if(map.name=="Ground"||map.name=="Graound")ground=map;if(map.name=="Wall")wall=map;if(map.name=="ForestOutside")outside=map;}
        }
        if(theme==null||ground==null||wall==null||outside==null)throw new Exception("Missing forest scene components");
        foreach(var tr in theme.GetComponentsInChildren<Transform>())if(tr.name=="SparseOutsideLog"||tr.name.StartsWith("CornerLog_"))UnityEngine.Object.DestroyImmediate(tr.gameObject);
        Sprite log=null;foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Folder+"/ForestProps.png")){var sp=asset as Sprite;if(sp!=null&&sp.name=="Log")log=sp;}
        if(log==null)throw new Exception("Missing log sprite");
        var barriers=new List<Bounds>();var foliage=new List<Bounds>();
        foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())
        {
            if(sr.name.StartsWith("StoneWall_")||sr.name.StartsWith("CornerLog_"))barriers.Add(sr.bounds);
            else foliage.Add(sr.bounds);
        }
        var bounds=ground.cellBounds;var candidates=new List<Vector3Int>();
        for(int y=bounds.yMin-14;y<bounds.yMax+14;y+=2)for(int x=bounds.xMin-14;x<bounds.xMax+14;x+=2)
        {
            var p=new Vector3Int(x,y,0);var tile=outside.GetTile(p);if(tile==null||tile.name.Contains("Water"))continue;
            bool clear=true,near=false;
            if(ground.HasTile(p)||wall.HasTile(p))clear=false;
            if(!clear)continue;
            for(int dy=-10;dy<=10&&!near;dy++)for(int dx=-10;dx<=10;dx++)if(ground.HasTile(p+new Vector3Int(dx,dy,0))){near=true;break;}
            if(near)candidates.Add(p);
        }
        candidates.Sort((a,b)=>Hash(a.x,a.y,stage+501).CompareTo(Hash(b.x,b.y,stage+501)));
        int target=Mathf.Clamp((bounds.size.x+bounds.size.y)/14,6,16);var placed=new List<Vector2>();
        foreach(var p in candidates)
        {
            var position=ground.GetCellCenterWorld(p);float width=2.3f+(Hash(p.x,p.y,64)%80)/100f;
            var size=log.bounds.size*(width/log.bounds.size.x);var center=position+(Vector3)(log.bounds.center*(width/log.bounds.size.x));var area=new Bounds(center,size);
            bool safe=true;foreach(var barrier in barriers)if(OverlapXY(area,barrier)){safe=false;break;}
            if(!safe)continue;
            var padded=area;padded.Expand(new Vector3(.2f,.2f,0));
            foreach(var plant in foliage)if(OverlapXY(padded,plant)){safe=false;break;}
            if(!safe)continue;
            var min=ground.WorldToCell(area.min);var max=ground.WorldToCell(area.max);
            for(int y=min.y;y<=max.y&&safe;y++)for(int x=min.x;x<=max.x;x++){var q=new Vector3Int(x,y,0);if(ground.HasTile(q)||wall.HasTile(q)){safe=false;break;}}
            if(!safe)continue;
            foreach(var other in placed)if(((Vector2)position-other).sqrMagnitude<196f){safe=false;break;}
            if(!safe)continue;
            var go=new GameObject("SparseOutsideLog",typeof(SpriteRenderer));go.transform.SetParent(theme,false);go.transform.position=new Vector3(position.x,position.y,1f+placed.Count*.001f);
            go.transform.localScale=Vector3.one*(width/log.bounds.size.x);var sr=go.GetComponent<SpriteRenderer>();sr.sprite=log;sr.sortingOrder=-6;sr.color=new Color(.63f,.65f,.54f);sr.flipX=(Hash(p.x,p.y,stage)%2)==0;
            placed.Add(position);if(placed.Count>=target)break;
        }
        return placed.Count;
    }
    public static string RepairForestDecor(Scene scene,int stage)
    {
        Transform theme=null;Tilemap ground=null,wall=null;foreach(var root in scene.GetRootGameObjects()){if(root.name=="ForestTheme")theme=root.transform;foreach(var tm in root.GetComponentsInChildren<Tilemap>()){if(tm.name=="Ground"||tm.name=="Graound")ground=tm;if(tm.name=="Wall")wall=tm;}}
        if(theme==null)return "No forest theme";
        Sprite oak=null,pine=null;foreach(var a in AssetDatabase.LoadAllAssetsAtPath(Folder+"/ForestProps.png")){var sp=a as Sprite;if(sp!=null&&sp.name=="Oak")oak=sp;if(sp!=null&&sp.name=="Pine")pine=sp;}
        int repaired=0;foreach(var sr in theme.GetComponentsInChildren<SpriteRenderer>())if(sr.sprite!=null&&sr.sprite.name.EndsWith("Crown")){float width=sr.bounds.size.x;sr.sprite=sr.sprite.name.StartsWith("Oak")?oak:pine;sr.transform.localScale=Vector3.one*(width/sr.sprite.bounds.size.x);repaired++;}
        var cells=new HashSet<Vector3Int>();foreach(var p in ground.cellBounds.allPositionsWithin)if(ground.HasTile(p))cells.Add(p);
        SeparateFoliage(theme,ground,cells);ClearPlayableFoliage(theme,ground,wall);StabilizeFoliage(theme);
        int logs=AddSparseOutsideLogs(scene,stage);return "Full-tree replacements="+repaired+", isolated logs="+logs;
    }
    static SpriteRenderer Prop(Transform parent,int index,Vector3 position,float width,int order,Color color)
    {
        var go=new GameObject(Names[index]);go.transform.SetParent(parent,false);go.transform.position=position;
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=props[index];sr.color=color;sr.sortingOrder=order;
        go.transform.localScale=Vector3.one*(width*(index==4?3f:1f)/Mathf.Max(.01f,sr.sprite.bounds.size.x));return sr;
    }
    static void Prepare()
    {
        Directory.CreateDirectory(Folder);props=new Sprite[6];
        PrepareStones();
        string source=@"C:\Users\fromj\.codex\generated_images\01a17d8d-cd5b-77a3-a41b-b09f9e144b73\exec-060e3090-6712-413d-b1bb-6c8ffc1717a6.png";
        File.Copy(source,Folder+"/ForestProps.png",true);AssetDatabase.ImportAsset(Folder+"/ForestProps.png");
        var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/ForestProps.png");importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=512;
        var rects=new[]{new Rect(0,400,550,624),new Rect(550,400,475,624),new Rect(1030,407,506,330),new Rect(5,50,530,320),new Rect(550,45,465,325),new Rect(1040,45,496,320)};
        var metadata=new SpriteMetaData[6];for(int i=0;i<6;i++)metadata[i]=new SpriteMetaData{name=Names[i],rect=rects[i],alignment=(int)SpriteAlignment.BottomCenter,pivot=new Vector2(.5f,0)};
        importer.spritesheet=metadata;importer.spritePixelsPerUnit=100;importer.SaveAndReimport();
        File.Copy(@"C:\Users\fromj\.codex\generated_images\01a17d8d-cd5b-77a3-a41b-b09f9e144b73\exec-7fcde920-0312-4b12-81ff-734e3f13d09e.png",Folder+"/InactiveStoneGate.png",true);
        AssetDatabase.ImportAsset(Folder+"/InactiveStoneGate.png");
        var gateImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"/InactiveStoneGate.png");gateImporter.textureType=TextureImporterType.Sprite;gateImporter.spriteImportMode=SpriteImportMode.Single;
        var gateSettings=new TextureImporterSettings();gateImporter.ReadTextureSettings(gateSettings);gateSettings.spriteAlignment=(int)SpriteAlignment.Custom;gateSettings.spritePivot=new Vector2(.5f,.1f);gateImporter.SetTextureSettings(gateSettings);
        gateImporter.filterMode=FilterMode.Point;gateImporter.mipmapEnabled=false;gateImporter.alphaIsTransparency=true;gateImporter.maxTextureSize=128;gateImporter.SaveAndReimport();
        foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(Folder+"/ForestProps.png")){var sprite=obj as Sprite;if(sprite!=null){int index=Array.IndexOf(Names,sprite.name);if(index>=0)props[index]=sprite;}}
        crowns=new Sprite[2];for(int i=0;i<2;i++)
        {
            string crownPath=Folder+"/"+Names[i]+"Crown.asset";crowns[i]=AssetDatabase.LoadAssetAtPath<Sprite>(crownPath);
            if(crowns[i]==null){var r=props[i].rect;r.y+=r.height*.32f;r.height*=.68f;crowns[i]=Sprite.Create(props[i].texture,r,new Vector2(.5f,0),100);crowns[i].name=Names[i]+"Crown";AssetDatabase.CreateAsset(crowns[i],crownPath);}
        }
        var sourceSprites=AssetDatabase.LoadAllAssetsAtPath("Assets/Flatform/Tileset/Ground/Tiles.png");Sprite ds=null,gs=null,ws=null;
        foreach(var obj in sourceSprites){var sp=obj as Sprite;if(sp==null)continue;if(sp.name=="Dirt5")ds=sp;if(sp.name=="Grass5")gs=sp;if(sp.name=="LakeGrass5a")ws=sp;}
        dirt=MakeTile("ForestDirt",ds,new Color(1.02f,.94f,.81f));grass=MakeTile("ForestGrass",gs,new Color(.65f,.72f,.58f));
        sand=MakeTile("ForestSand",ds,new Color(1.12f,1.02f,.85f));water=MakeTile("ForestWater",ws,new Color(.48f,.64f,.68f));
        if(ds==null||gs==null||ws==null)throw new Exception("Missing base ground sprites");
    }
    static Tile MakeTile(string name,Sprite sprite,Color color)
    {
        string path=Folder+"/"+name+".asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
        if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}
        tile.sprite=sprite;tile.color=color;tile.flags=TileFlags.None;tile.colliderType=Tile.ColliderType.None;EditorUtility.SetDirty(tile);return tile;
    }
    static void PrepareStones()
    {
        string path=Folder+"/ForestWallStones.png";
        File.Copy(@"C:\Users\fromj\.codex\generated_images\01a17d8d-cd5b-77a3-a41b-b09f9e144b73\exec-33126397-2f5f-4eb0-bf47-4bc6a06b76ac.png",path,true);AssetDatabase.ImportAsset(path);
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));
        var metadata=new SpriteMetaData[6];
        for(int i=0;i<6;i++)
        {
            int left=i%3*512,bottom=i<3?512:0,minX=left+511,minY=bottom+511,maxX=left,maxY=bottom;
            for(int y=bottom;y<bottom+512;y++)for(int x=left;x<left+512;x++)if(texture.GetPixel(x,y).a>.65f){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
            minX=Mathf.Max(left,minX-12);minY=Mathf.Max(bottom,minY-12);maxX=Mathf.Min(left+511,maxX+12);maxY=Mathf.Min(bottom+511,maxY+12);
            metadata[i]=new SpriteMetaData{name="ForestStone"+(i+1),rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
        }
        UnityEngine.Object.DestroyImmediate(texture);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritesheet=metadata;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        stoneSprites=new Sprite[6];foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)){var sprite=asset as Sprite;if(sprite!=null)for(int i=0;i<6;i++)if(sprite.name=="ForestStone"+(i+1))stoneSprites[i]=sprite;}
    }
    static void Capture(Scene scene,Tilemap ground,int stage)
    {
        var layers=new Dictionary<GameObject,int>();
        foreach(var root in scene.GetRootGameObjects())foreach(var r in root.GetComponentsInChildren<Renderer>(true)){if(!layers.ContainsKey(r.gameObject))layers.Add(r.gameObject,r.gameObject.layer);r.gameObject.layer=31;}
        var go=new GameObject("ForestPreviewCamera");SceneManager.MoveGameObjectToScene(go,scene);var cam=go.AddComponent<Camera>();
        cam.cullingMask=1<<31;
        var bounds=ground.localBounds;var center=ground.transform.TransformPoint(bounds.center);
        cam.transform.position=new Vector3(center.x,center.y,-30);cam.orthographic=true;cam.orthographicSize=Mathf.Max(bounds.size.y*.5f+7,(bounds.size.x+14)/3.2f);
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.035f,.045f,.035f);
        var rt=new RenderTexture(1280,800,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(1280,800,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
        Directory.CreateDirectory(@"C:\Users\fromj\Documents\Codex\2026-10-27\su\output");File.WriteAllBytes(@"C:\Users\fromj\Documents\Codex\2026-10-27\su\output\forest-stage"+stage+".png",image.EncodeToPNG());
        RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);
        foreach(var entry in layers)entry.Key.layer=entry.Value;
    }
}
#endif
