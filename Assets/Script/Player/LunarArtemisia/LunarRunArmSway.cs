using UnityEngine;

// Damps only the locomotion pose before the skin and weapon renderers follow the bones.
[DefaultExecutionOrder(19000)]
public sealed class LunarRunArmSway : MonoBehaviour
{
    Animator animator;
    LunarBowSkill skill;
    Transform[] arms;
    Quaternion[] restRotation;
    Vector3[] restPosition;
    public void Initialize(Animator source,Transform skeleton,LunarBowSkill lunar)
    {
        animator=source;skill=lunar;
        arms=new[]{skeleton.Find("BodySet/P_Body/ArmSet/ArmL/P_LArm"),skeleton.Find("BodySet/P_Body/ArmSet/ArmR/P_RArm")};
        restRotation=new Quaternion[arms.Length];restPosition=new Vector3[arms.Length];
        for(int i=0;i<arms.Length;i++)if(arms[i]!=null){restRotation[i]=arms[i].localRotation;restPosition[i]=arms[i].localPosition;}
    }
    void LateUpdate()
    {
        if(animator==null || arms==null || skill==null || !skill.IsEquipped || !animator.GetBool("Run") || animator.GetInteger("Attack")!=-1 || animator.GetInteger("Attacked")!=-1)return;
        for(int i=0;i<arms.Length;i++)if(arms[i]!=null)
        {
            arms[i].localRotation=Quaternion.Slerp(restRotation[i],arms[i].localRotation,.8f);
            arms[i].localPosition=Vector3.Lerp(restPosition[i],arms[i].localPosition,.8f);
        }
    }
}
