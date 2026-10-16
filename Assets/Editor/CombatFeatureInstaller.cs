using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class CombatFeatureInstaller
{
    const string Folder="Assets/Resources/CombatRewards";
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Build configuration in edit mode.");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var config=AssetDatabase.LoadAssetAtPath<CombatFeatureConfig>(Folder+"/Config.asset");
        if(config==null){config=ScriptableObject.CreateInstance<CombatFeatureConfig>();AssetDatabase.CreateAsset(config,Folder+"/Config.asset");}
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"/RewardKorean.asset");
        if(font==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/NotoSansKR-Medium.otf");
            if(source==null)throw new InvalidOperationException("Project Korean font is missing.");
            font=TMP_FontAsset.CreateFontAsset(source,48,5,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,false);
            font.name="RewardKorean";
            AssetDatabase.CreateAsset(font,Folder+"/RewardKorean.asset");
            AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
            string characters="응급 회복 공격력 강화 속도 생명 이동 최대 현재 체력 스킬 스테이지 클리어! 보상 하나를 선택하세요 · 는 다음 에도 유지됩니다 선택하고 로 중간 보스 최종 기본 장비의 증가 %p 즉시 검 활 최대 누적 +30% +15% 0123456789.,:/%+→()-";
            // Bake only labels used by these features; preserve existing project fonts.
            foreach(var path in Directory.GetFiles("Assets/Script/Rewards","*.cs"))characters+=File.ReadAllText(path);
            string missing;
            characters+=File.ReadAllText("Assets/Editor/CombatFeatureInstaller.cs");
            font.TryAddCharacters(new string(characters.Where(c=>!char.IsControl(c)).Distinct().ToArray()),out missing);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font);
        }
        config.font=font;
        font.atlasPopulationMode=AtlasPopulationMode.Dynamic;
        string allLabels=File.ReadAllText("Assets/Editor/CombatFeatureInstaller.cs");
        foreach(var path in Directory.GetFiles("Assets/Script/Rewards","*.cs"))allLabels+=File.ReadAllText(path);
        string missingLabels;
        font.TryAddCharacters(new string(allLabels.Where(c=>!char.IsControl(c)).Distinct().ToArray()),out missingLabels);
        font.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(font);
        config.healthFill=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        config.panelSprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        var kinds=new[]{StageRewardKind.Heal,StageRewardKind.Power,StageRewardKind.AttackSpeed,StageRewardKind.MaxHealth,StageRewardKind.MoveSpeed};
        var titles=new[]{"응급 회복","공격력 강화","공격 속도 강화","생명력 강화","이동 속도 강화"};
        var descriptions=new[]{"최대 체력의 20% 즉시 회복","일반·스킬 공격력 +5%\n기본·장비 공격력 기준","검·활 공격 속도 +5%\n최대 누적 +30%","최대 체력 +10\n현재 체력도 +10 회복","이동 속도 +3%\n최대 누적 +15%"};
        var amounts=new[]{.2f,.05f,.05f,10f,.03f};
        var caps=new[]{0f,0f,.3f,0f,.15f};
        var iconPaths=new[]{"Assets/2D Pixel Item Pack/No Outline/S_ItemNoOutline_PotionRed_02.png","Assets/images/KakaoTalk_20250822_171943352.png","Assets/images/KakaoTalk_20250823_211104018_01.png","Assets/2D Pixel Item Pack/No Outline/S_ItemNoOutline_HeartRed_00.png","Assets/Sprites/Dark UI/Free/Right BTN.png"};
        config.rewards=new StageRewardDefinition[kinds.Length];
        for(int i=0;i<kinds.Length;i++)
        {
            string path=Folder+"/"+kinds[i]+".asset";
            var reward=AssetDatabase.LoadAssetAtPath<StageRewardDefinition>(path);
            if(reward==null){reward=ScriptableObject.CreateInstance<StageRewardDefinition>();AssetDatabase.CreateAsset(reward,path);}
            reward.kind=kinds[i];reward.title=titles[i];reward.description=descriptions[i];reward.amount=amounts[i];reward.cap=caps[i];
            reward.icon=AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);config.rewards[i]=reward;EditorUtility.SetDirty(reward);
        }
        EditorUtility.SetDirty(config);
        var root=new GameObject("CombatFeatureRoot");
        try
        {
            root.AddComponent<StageRewardController>().config=config;
            root.AddComponent<CombatPresentation>().config=config;
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/CombatFeatureRoot.prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        return "Created 5 reward definitions, Korean font, feature configuration and runtime root prefab.";
    }
}
