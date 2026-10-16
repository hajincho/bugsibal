using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    public float lifetime=.8f;
    public float floatSpeed=2f;
    public TextMeshPro textMesh;
    TMP_Text label;
    Color color;
    float age;
    Vector3 origin;
    CombatPresentation pool;
    void Awake(){label=GetComponent<TMP_Text>();textMesh=GetComponent<TextMeshPro>();}
    public void Setup(int damage,int dmgmode)
    {pool=null;age=0;origin=transform.position;Configure(damage,dmgmode==2?CombatDamageKind.Heal:dmgmode==1?CombatDamageKind.Skill:CombatDamageKind.Normal);}
    public void SetupPooled(float damage,CombatDamageKind kind,Vector3 position,CombatPresentation owner)
    {pool=owner;age=0;origin=position;Configure(damage,kind);}
    void Configure(float damage,CombatDamageKind kind)
    {
        if(label==null)label=GetComponent<TMP_Text>();
        label.text=(kind==CombatDamageKind.Heal?"+":"")+damage.ToString("0.#");label.enabled=true;
        color=kind==CombatDamageKind.Heal?new Color(.5f,1,.6f):kind==CombatDamageKind.PlayerHit?new Color(1,.3f,.3f):kind==CombatDamageKind.Skill?new Color(1,.65f,.2f):new Color(1,.96f,.75f);label.color=color;
    }
    void Update()
    {
        age+=Time.deltaTime;if(age>=lifetime){Recycle();return;}
        var world=origin+Vector3.up*(floatSpeed*age);
        if(pool!=null){Vector2 pos;if(pool.Project(world,out pos)){((RectTransform)transform).anchoredPosition=pos;label.enabled=true;}else label.enabled=false;}
        else transform.position=world;
        var faded=color;faded.a=1-Mathf.Clamp01(age/lifetime);label.color=faded;
    }
    public void Recycle(){if(pool!=null)pool.ReturnNumber(this);else Destroy(gameObject);}
}
