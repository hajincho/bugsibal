using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsSkillDamaged : MonoBehaviour
{
    public bool damaged_skill1 = false;
    public bool damaged_skill2 = false;
    public bool damaged_skill3 = false;
    public bool damaged_skill4 = false;
    public bool damaged_skill5 = false;
    public bool damaged_normal = false;
    public bool ult1 = false;
    public bool ult2 = false;
    public bool ult3 = false;

    void Awake()
    {
        damaged_skill1 = false;
        damaged_skill2 = false;
        damaged_skill3 = false;
        damaged_skill4 = false;
        damaged_skill5 = false;
        damaged_normal = false;
        ult1 = false;
        ult2 = false;
        ult3 = false;

}
    public void SkillAble(int skill)
    {
        switch (skill)
        {
            case 0:
                damaged_normal = false;
                break;
            case 1:
                damaged_skill1 = false;
                break;
            case 2:
                damaged_skill2 = false;
                break;
            case 3:
                damaged_skill3 = false;
                break;
            case 4:
                damaged_skill4 = false;
                break;
            case 5:
                damaged_skill5 = false;
                break;
            case 6:
                ult1 = false;
                break;
            case 7:
                ult2 = false;
                break;
            case 8:
                ult3 = false;
                break;


        }
    }
}
