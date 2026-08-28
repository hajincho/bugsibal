using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stage0 : MonoBehaviour
{
    StageManager stageManager;
    public GameObject script;
    public Text text;

    void Start()
    {
        stageManager = GetComponent<StageManager>();
        //튜토리얼 스크립트 출력 전 일시정지
        stageManager.progress = false;
        PlayerMove.moveable = false;
        StartCoroutine(Tutorial());
    }

    IEnumerator Tutorial()
    {
        script.SetActive(true);

        text.text = "W A S D 키를 통해 이동할 수 있습니다.";
        PlayerMove.moveable = true;
        yield return new WaitForSeconds(3f);
        
        text.text = "마우스 버튼을 통해 공격할 수 있습니다.";
        PlayerMove.moveable = true;
        yield return new WaitForSeconds(3f);
        
        text.text = "Q E R F C X 키를 통해 스킬을 사용할 수 있습니다.";
        PlayerMove.moveable = true;
        yield return new WaitForSeconds(3f);

        stageManager.progress = true;//적 소환
        yield return new WaitForSeconds(3f);


        script.SetActive(false);
    }
}
