using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Portal))]
public sealed class AutoEnterStage1 : MonoBehaviour
{
    IEnumerator Start()
    {
        // Allow initialization to finish even when the game is paused.
        yield return new WaitForSecondsRealtime(0.01f);

        while (StageRewardController.InputBlocked)
            yield return null;

        GetComponent<Portal>().Enter();
    }
}
