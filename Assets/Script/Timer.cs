using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    float timer = 0;
    public Text speedruntimer;

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        speedruntimer.text = timer.ToString();
    }

}
