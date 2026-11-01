#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class HellStageBuilder
{
    const string Folder = "Assets/Script/Environment/Hell";
    const string Output = @"C:\Users\fromj\Documents\Codex\2026-10-27\su\output\hell";
    static readonly string[] OldThemes = { "SkyStoneStageTheme", "ForestTheme", "HellStageTheme" };
    static Tile[] floorTiles, wallTiles;
    static Sprite deadTree, lavaPool, burningLog, campfire, wallSprite;
    static Texture2D floorTexture;
    static int order;

    static bool IsTheme(string name) { return Array.IndexOf(OldThemes, name) >= 0; }
    static Sprite Import(string name, int size, float ppu)
    {
        string path = Folder + "/" + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.spritePivot = new Vector2(.5f, .5f);
        importer.maxTextureSize = size;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = name == "HellWall" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Tile[] TileSet(string prefix, Sprite full)
    {
        var tiles = new Tile[16];
        int width = full.texture.width / 4, height = full.texture.height / 4;
        for (int i = 0; i < tiles.Length; i++)
        {
            string spritePath = Folder + "/" + prefix + "Quarter" + i + ".asset";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                sprite = Sprite.Create(full.texture, new Rect(i % 4 * width, i / 4 * height, width, height), new Vector2(.5f, .5f), width, 0, SpriteMeshType.FullRect);
                sprite.name = prefix + "Quarter" + i;
                AssetDatabase.CreateAsset(sprite, spritePath);
            }
            string path = Folder + "/" + prefix + "Tile" + i + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
            tile.sprite = sprite; tile.colliderType = Tile.ColliderType.None; tile.flags = TileFlags.None;
            EditorUtility.SetDirty(tile); tiles[i] = tile;
        }
        return tiles;
    }
    static void Prepare()
    {
        var floor = Import("HellFloor", 256, 64);
        floorTexture = floor.texture;
        floorTiles = TileSet("HellFloor", floor);
        wallSprite = Import("HellWall", 256, 64);
        wallTiles = TileSet("HellWall", wallSprite);
        deadTree = Import("DeadTree", 256, 32);
        lavaPool = Import("LavaPool", 256, 32);
        burningLog = Import("BurningLog", 256, 48);
        campfire = Import("Campfire", 128, 32);
        AssetDatabase.SaveAssets();
    }
    static int Index(Vector3Int p) { return (p.x % 4 + 4) % 4 + ((p.y % 4 + 4) % 4) * 4; }
    static bool Inside(Vector2 p, List<Vector2> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = polygon[i]; var b = polygon[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside;
    }
    static Tile MaskedFloor(Vector3Int cell, Vector3 center, List<Vector2> polygon)
    {
        string directory = Folder + "/FinalBossEdges";
        Directory.CreateDirectory(directory);
        string path = directory + "/Floor_" + cell.x + "_" + cell.y + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "ClippedHellFloor"; texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
        int quarter = floorTexture.width / 4;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            var point = new Vector2(center.x - .5f + (x + .5f) / size, center.y - .5f + (y + .5f) / size);
            var color = (Color32)floorTexture.GetPixel((cell.x % 4 + 4) % 4 * quarter + x * quarter / size, (cell.y % 4 + 4) % 4 * quarter + y * quarter / size);
            if (!Inside(point, polygon)) color.a = 0;
            pixels[y * size + x] = color;
        }
        texture.SetPixels32(pixels); texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect);
        sprite.name = "ClippedHellFloor";
        tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprite; tile.colliderType = Tile.ColliderType.None; tile.flags = TileFlags.None;
        AssetDatabase.CreateAsset(tile, path); AssetDatabase.AddObjectToAsset(texture, tile); AssetDatabase.AddObjectToAsset(sprite, tile);
        EditorUtility.SetDirty(tile); return tile;
    }
    static Tilemap Map(Transform parent, string name, int sorting)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false); go.GetComponent<TilemapRenderer>().sortingOrder = sorting;
        return go.GetComponent<Tilemap>();
    }
    static SpriteRenderer Prop(Transform parent, string name, Sprite sprite, Vector2 center, float width, int sorting, bool mirror = false)
    {
        var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, .4f + order++ * .0001f);
        float scale = width / sprite.bounds.size.x;
        go.transform.localScale = new Vector3(scale, scale, 1);
        var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.flipX = mirror; renderer.sortingOrder = sorting;
        return renderer;
    }
    static bool Overlap(Rect a, Rect b) { return a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin; }
    static bool Clear(Rect rect, Tilemap ground, HashSet<Vector3Int> occupied, List<Rect> props, float clearance = 1.2f)
    {
        var min = ground.WorldToCell(new Vector3(rect.xMin - clearance, rect.yMin - clearance));
        var max = ground.WorldToCell(new Vector3(rect.xMax + clearance, rect.yMax + clearance));
        for (int y = min.y; y <= max.y; y++) for (int x = min.x; x <= max.x; x++)
            if (occupied.Contains(new Vector3Int(x, y, 0))) return false;
        var padded = Rect.MinMaxRect(rect.xMin - .5f, rect.yMin - .5f, rect.xMax + .5f, rect.yMax + .5f);
        foreach (var prop in props) if (Overlap(padded, prop)) return false;
        return true;
    }
    static List<SpriteRenderer> NearPerimeter(Transform parent, Tilemap ground, HashSet<Vector3Int> occupied,
        List<Rect> placed, Sprite sprite, string name, float width, int target, Vector2 anchor, int seed)
    {
        float height = width * sprite.bounds.size.y / sprite.bounds.size.x;
        bool tree = name == "BareDeadTree";
        var noProps = new List<Rect>();
        Func<Rect, Rect> footprint = rect => tree
            ? new Rect(rect.x + rect.width * .28f, rect.y, rect.width * .44f, rect.height * .55f) : rect;
        Func<Rect, bool> clear = rect => Clear(rect, ground, occupied, noProps, .12f) &&
            Clear(footprint(rect), ground, occupied, placed, .12f);
        var candidates = new List<Vector2>();
        var directions = new[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        foreach (var cell in occupied)
        {
            Vector2 normal = Vector2.zero;
            foreach (var direction in directions) if (!occupied.Contains(cell + direction)) normal += (Vector2)(Vector3)direction;
            if (normal.sqrMagnitude < .1f) continue;
            normal.Normalize();
            float radius = Mathf.Abs(normal.x) * width * .5f + Mathf.Abs(normal.y) * height * .5f;
            for (int ring = 0; ring < 2; ring++)
            {
                var point = (Vector2)ground.GetCellCenterWorld(cell) + normal * (radius + .9f + ring * 1.2f);
                var rect = new Rect(point.x - width * .5f, point.y - height * .5f, width, height);
                if (clear(rect)) candidates.Add(point);
            }
        }
        // Start near the entry, then spread the rest around the whole playable boundary.
        var random = new System.Random(seed);
        for (int i = candidates.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var p = candidates[i]; candidates[i] = candidates[j]; candidates[j] = p; }
        var selected = new List<Vector2>();
        var renderers = new List<SpriteRenderer>();
        for (int i = 0; i < target; i++)
        {
            int best = -1; float bestScore = float.NegativeInfinity;
            for (int j = 0; j < candidates.Count; j++)
            {
                var point = candidates[j];
                var fromEntry = point - anchor;
                float score = selected.Count == 0 ? -(fromEntry.x * fromEntry.x + fromEntry.y * fromEntry.y * 4f) : float.MaxValue;
                foreach (var chosen in selected) score = Mathf.Min(score, (point - chosen).sqrMagnitude);
                if (score <= bestScore) continue;
                var rect = new Rect(point.x - width * .5f, point.y - height * .5f, width, height);
                if (!clear(rect)) continue;
                best = j; bestScore = score;
            }
            if (best < 0) break;
            var center = candidates[best]; candidates.RemoveAt(best);
            selected.Add(center); placed.Add(footprint(new Rect(center.x - width * .5f, center.y - height * .5f, width, height)));
            renderers.Add(Prop(parent, name, sprite, center, width, -7, random.Next(2) == 0));
        }
        return renderers;
    }
    static List<Vector2> SlopePolygon(Scene scene)
    {
        var polygon = new List<Vector2>();
        foreach (var root in scene.GetRootGameObjects()) if (root.name == "SlopeWall")
            foreach (var edge in root.GetComponentsInChildren<EdgeCollider2D>())
                foreach (var p in edge.points) polygon.Add(edge.transform.TransformPoint(p + edge.offset));
        return polygon;
    }
    static List<Vector2> PerimeterAnchors(Tilemap ground, HashSet<Vector3Int> cells, int count, Vector2 entry)
    {
        var candidates = new List<Vector2>();
        foreach (var cell in cells)
            if (!cells.Contains(cell + Vector3Int.up) || !cells.Contains(cell + Vector3Int.down) ||
                !cells.Contains(cell + Vector3Int.left) || !cells.Contains(cell + Vector3Int.right))
                candidates.Add(ground.GetCellCenterWorld(cell));
        var selected = new List<Vector2>();
        for (int n = 0; n < count && candidates.Count > 0; n++)
        {
            int best = 0; float bestScore = float.NegativeInfinity;
            for (int i = 0; i < candidates.Count; i++)
            {
                float score = selected.Count == 0 ? -(candidates[i] - entry).sqrMagnitude : float.MaxValue;
                foreach (var p in selected) score = Mathf.Min(score, (candidates[i] - p).sqrMagnitude);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            selected.Add(candidates[best]); candidates.RemoveAt(best);
        }
        return selected;
    }
    static void SlopeRampart(Transform parent, Vector2 a, Vector2 b, int index)
    {
        // Short textured blocks keep the pixel scale consistent on sloped boundaries.
        var delta = b - a;
        var normal = new Vector2(-delta.y, delta.x).normalized;
        var direction = delta.normalized;
        int count = Mathf.CeilToInt(delta.magnitude / .9f);
        float length = delta.magnitude / count;
        for (int i = 0; i < count; i++)
        {
            var center = a + direction * ((i + .5f) * length) + normal * .49f;
            var sprite = wallTiles[(i + index * 5) % wallTiles.Length].sprite;
            var renderer = Prop(parent, "SlopedVolcanicRampart", sprite, center, length + .02f, -4);
            renderer.transform.localScale = new Vector3((length + .02f) / sprite.bounds.size.x, .95f / sprite.bounds.size.y, 1);
            renderer.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            renderer.color = new Color(.93f, .86f, .86f);
        }
    }
    public static string LayoutSignature(Scene scene)
    {
        var lines = new List<string>();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (IsTheme(root.name)) continue;
            foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
                foreach (var p in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(p)) lines.Add("cell:" + map.name + ":" + p + ":" + map.GetCellCenterWorld(p));
            foreach (var collider in root.GetComponentsInChildren<Collider2D>(true))
            {
                lines.Add("collider:" + collider.name + ":" + collider.GetType().Name + ":" + collider.transform.position + ":" + collider.offset + ":" + collider.isTrigger);
                var edge = collider as EdgeCollider2D;
                if (edge != null) foreach (var p in edge.points) lines.Add("edge:" + collider.name + ":" + p);
            }
            var manager = root.GetComponent<StageManager>();
            if (manager != null)
            {
                var groups = new[] { manager.enemies_phase1, manager.enemies_phase2, manager.enemies_phase3, manager.enemies_phase4 };
                var points = new[] { manager.phase1_location, manager.phase2_location, manager.phase3_location, manager.phase4_location };
                for (int i = 0; i < groups.Length; i++) for (int j = 0; j < groups[i].Count; j++) lines.Add("wave:" + i + ":" + AssetDatabase.GetAssetPath(groups[i][j]) + ":" + points[i][j].position);
            }
            if (root.name.StartsWith("힐") || root.name == "spawnpoint" || root.name == "FinalBoss") lines.Add("object:" + root.name + ":" + root.transform.position);
        }
        lines.Sort(StringComparer.Ordinal); return string.Join("\n", lines.ToArray());
    }
    public static string Build()
    {
        if (Application.isPlaying) throw new Exception("Exit Play mode before changing environments.");
        Directory.CreateDirectory(Output); Prepare();
        return BuildScene("Stage7", 707) + "\n" + BuildScene("FinalBoss", 999);
    }
    static string BuildScene(string sceneName, int seed)
    {
        string path = "Assets/Scenes/Stage/" + sceneName + ".unity";
        File.Copy(path, Path.Combine(Output, sceneName + "-before-hell-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity"));
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            string before = LayoutSignature(scene);
            File.WriteAllText(Path.Combine(Output, sceneName + "-layout-before.txt"), before);
            foreach (var root in scene.GetRootGameObjects()) if (IsTheme(root.name)) UnityEngine.Object.DestroyImmediate(root);
            Tilemap ground = null;
            var originalWalls = new List<Tilemap>();
            foreach (var root in scene.GetRootGameObjects()) foreach (var map in root.GetComponentsInChildren<Tilemap>())
                if (map.GetComponent<TilemapCollider2D>() != null) originalWalls.Add(map);
                else if (map.name.ToLower().Contains("grou") || map.name.ToLower().Contains("grao")) ground = map;
            if (ground == null) throw new Exception("No playable ground in " + sceneName);
            ground.CompressBounds();
            var originalCells = new HashSet<Vector3Int>();
            foreach (var p in ground.cellBounds.allPositionsWithin) if (ground.HasTile(p)) originalCells.Add(p);
            var occupied = new HashSet<Vector3Int>(originalCells);
            foreach (var map in originalWalls) foreach (var p in map.cellBounds.allPositionsWithin)
                if (map.HasTile(p)) occupied.Add(ground.WorldToCell(map.GetCellCenterWorld(p)));
            var polygon = SlopePolygon(scene);
            var theme = new GameObject("HellStageTheme", typeof(Grid)); SceneManager.MoveGameObjectToScene(theme, scene); order = 0;
            theme.transform.position = ground.transform.position;
            theme.transform.rotation = ground.transform.rotation;
            theme.transform.localScale = ground.transform.lossyScale;
            ground.color = new Color(.85f, .82f, .82f); ground.GetComponent<TilemapRenderer>().sortingOrder = -12;
            int clipped = 0;
            foreach (var p in originalCells)
            {
                var center = ground.GetCellCenterWorld(p); Tile tile = floorTiles[Index(p)];
                if (polygon.Count >= 4)
                {
                    bool allInside = true;
                    foreach (float x in new[] { -.49f, .49f }) foreach (float y in new[] { -.49f, .49f })
                        if (!Inside((Vector2)center + new Vector2(x, y), polygon)) allInside = false;
                    if (!allInside) { tile = MaskedFloor(p, center, polygon); clipped++; }
                }
                ground.SetTile(p, tile); ground.SetTileFlags(p, TileFlags.None); ground.SetTransformMatrix(p, Matrix4x4.identity);
                float shade = .94f + Mathf.PerlinNoise((p.x + 81) * .11f, (p.y + seed) * .11f) * .1f;
                ground.SetColor(p, new Color(shade, shade, shade, 1));
            }
            var exterior = Map(theme.transform, "AshenExterior", -30);
            var bounds = ground.cellBounds;
            for (int y = bounds.yMin - 42; y <= bounds.yMax + 42; y++) for (int x = bounds.xMin - 46; x <= bounds.xMax + 46; x++)
            {
                var p = new Vector3Int(x, y, 0);
                if (originalCells.Contains(p) && polygon.Count == 0) continue;
                exterior.SetTile(p, floorTiles[Index(p)]);
            }
            exterior.color = new Color(.29f, .24f, .25f, 1);
            var wallMap = Map(theme.transform, "VolcanicStoneBoundary", -5);
            foreach (var map in originalWalls)
            {
                map.GetComponent<TilemapRenderer>().enabled = false;
                foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p))
                {
                    var q = wallMap.WorldToCell(map.GetCellCenterWorld(p));
                    wallMap.SetTile(q, wallTiles[Index(q)]);
                }
            }
            wallMap.color = new Color(.92f, .84f, .84f);
            if (polygon.Count >= 4)
            {
                wallMap.gameObject.SetActive(false);
                for (int i = 0; i < polygon.Count - 1; i++)
                    SlopeRampart(theme.transform, polygon[i], polygon[i + 1], i);
            }
            var placed = new List<Rect>(); var fires = new List<SpriteRenderer>(); var pools = new List<SpriteRenderer>();
            var random = new System.Random(seed);
            var minWorld = ground.CellToWorld(bounds.min);
            var maxWorld = ground.CellToWorld(bounds.max);
            int trees = 0, logs = 0, fireCount = 0;
            var entry = sceneName == "Stage7" ? new Vector2(35.2f, -8.2f) : Vector2.zero;
            // Share the visible shore between all four prop types, one small group at a time.
            // This prevents whichever type is placed last being pushed into the distant background.
            int nearFireTarget = sceneName == "Stage7" ? 21 : 13;
            int nearLogTarget = sceneName == "Stage7" ? 15 : 13;
            int nearPoolTarget = sceneName == "Stage7" ? 12 : 6;
            int nearTreeTarget = sceneName == "Stage7" ? 90 : 40;
            var anchors = PerimeterAnchors(ground, originalCells, sceneName == "Stage7" ? 32 : 16, entry);
            for (int group = 0; group < nearTreeTarget; group++)
            {
                var anchor = anchors[group % anchors.Count];
                for (int slot = 0; slot < 4; slot++)
                {
                    bool fire = slot == 0, log = slot == 1, pool = slot == 2;
                    int current = fire ? fireCount : log ? logs : pool ? pools.Count : trees;
                    int target = fire ? nearFireTarget : log ? nearLogTarget : pool ? nearPoolTarget : nearTreeTarget;
                    if (current >= target) continue;
                    var sprite = fire ? campfire : log ? burningLog : pool ? lavaPool : deadTree;
                    string propName = fire ? "ExteriorCampfire" : log ? "BurningCharcoalLog" : pool ? "ExteriorLavaPool" : "BareDeadTree";
                    float width = fire ? 5f : log ? 3.4f : pool ? 7.5f : 7f;
                    var additions = NearPerimeter(theme.transform, ground, originalCells, placed, sprite, propName,
                        width, 1, anchor, seed + group * 7 + slot);
                    foreach (var renderer in additions)
                    {
                        if (pool) { renderer.sortingOrder = -20; pools.Add(renderer); }
                        else if (fire || log) { fires.Add(renderer); if (fire) fireCount++; else logs++; }
                        else { renderer.color = new Color(.9f, .82f, .83f); trees++; }
                    }
                }
            }
            // Reserve room for enlarged fires before filling the remaining space with trees.
            for (int passIndex = 0; passIndex < 3; passIndex++)
            {
                int pass = passIndex == 1 ? 2 : passIndex == 2 ? 1 : 0;
                float step = pass == 0 ? 13f : pass == 1 ? 3.5f : 8f;
                float paddingX = pass == 1 ? 38f : 24f;
                float paddingY = pass == 1 ? 34f : 22f;
                for (float y = minWorld.y - paddingY; y <= maxWorld.y + paddingY; y += step)
                    for (float x = minWorld.x - paddingX; x <= maxWorld.x + paddingX; x += step)
                    {
                        if (random.NextDouble() > (pass == 0 ? .44 : pass == 1 ? .98 : .58)) continue;
                        var point = new Vector2(x + (float)random.NextDouble() * 3, y + (float)random.NextDouble() * 3);
                        bool log = pass == 2 && random.Next(2) == 0;
                        if (pass == 2)
                        {
                            int targetLogs = sceneName == "Stage7" ? 15 : 13;
                            int targetFires = sceneName == "Stage7" ? 21 : 13;
                            if (log && logs >= targetLogs) log = false;
                            if (!log && fireCount >= targetFires) log = true;
                            if (log && logs >= targetLogs) continue;
                        }
                        var sprite = pass == 0 ? lavaPool : pass == 1 ? deadTree : log ? burningLog : campfire;
                        float width = pass == 0 ? 6.5f + (float)random.NextDouble() * 3 : pass == 1 ? (4.7f + (float)random.NextDouble() * 1.7f) * 1.25f : log ? 3.4f : 5f;
                        float height = width * sprite.bounds.size.y / sprite.bounds.size.x;
                        var rect = new Rect(point.x - width * .5f, point.y - height * .5f, width, height);
                        if (!Clear(rect, ground, occupied, placed)) continue;
                        var name = pass == 0 ? "ExteriorLavaPool" : pass == 1 ? "BareDeadTree" : log ? "BurningCharcoalLog" : "ExteriorCampfire";
                        var renderer = Prop(theme.transform, name, sprite, point, width, pass == 0 ? -20 : -7, random.Next(2) == 0);
                        placed.Add(rect);
                        if (pass == 0) pools.Add(renderer);
                        else if (pass == 1) { renderer.color = new Color(.9f, .82f, .83f); trees++; }
                        else { fires.Add(renderer); if (log) logs++; else fireCount++; }
                    }
            }
            // Thin only lava after layout, so every other decoration keeps its position.
            for (int i = pools.Count - 1; i >= 0; i--)
                if (i % 2 == 1) { UnityEngine.Object.DestroyImmediate(pools[i].gameObject); pools.RemoveAt(i); }
            var animation = theme.AddComponent<HellEnvironmentAnimator>(); animation.fires = fires.ToArray(); animation.lavaPools = pools.ToArray();
            if (LayoutSignature(scene) != before) throw new Exception("Gameplay layout changed unexpectedly in " + sceneName);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Capture(scene, ground, sceneName, ground.transform.TransformPoint(ground.localBounds.center), Mathf.Max(ground.localBounds.size.y * .5f + 10, (ground.localBounds.size.x + 20) / 3.2f));
            Capture(scene, ground, sceneName + "-detail", sceneName == "Stage7" ? new Vector3(35.2f, -8.2f) : new Vector3(3.85f, 17f), 13f);
            Capture(scene, ground, sceneName + "-player-view", sceneName == "Stage7" ? new Vector3(35.2f, -8.2f) : new Vector3(3.85f, 23f), 6.5f);
            return sceneName + ": floor=" + originalCells.Count + ", clipped diagonal cells=" + clipped + ", trees=" + trees + ", lava pools=" + pools.Count + ", burning logs=" + logs + ", campfires=" + fireCount + ", gameplay layout preserved";
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
    static void Capture(Scene scene, Tilemap ground, string name, Vector3 center, float size)
    {
        var layers = new Dictionary<GameObject, int>();
        foreach (var root in scene.GetRootGameObjects()) foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        { if (!layers.ContainsKey(renderer.gameObject)) layers.Add(renderer.gameObject, renderer.gameObject.layer); renderer.gameObject.layer = 31; }
        var go = new GameObject("HellPreviewCamera", typeof(Camera)); SceneManager.MoveGameObjectToScene(go, scene);
        var camera = go.GetComponent<Camera>(); camera.cullingMask = 1 << 31; camera.orthographic = true; camera.orthographicSize = size;
        camera.transform.position = new Vector3(center.x, center.y, -30); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f, .025f, .035f);
        int height = name.Contains("player-view") ? Mathf.RoundToInt(1280f / (Camera.main != null ? Camera.main.aspect : 2.05f)) : 800;
        var target = new RenderTexture(1280, height, 24); camera.targetTexture = target; camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(1280, height, TextureFormat.RGBA32, false); image.ReadPixels(new Rect(0, 0, 1280, height), 0, 0); image.Apply();
        File.WriteAllBytes(Path.Combine(Output, name + "-hell.png"), image.EncodeToPNG());
        RenderTexture.active = previous; camera.targetTexture = null; target.Release();
        UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(go);
        foreach (var entry in layers) entry.Key.layer = entry.Value;
    }
}
#endif
