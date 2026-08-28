using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class drum : MonoBehaviour
{
    
    EnemyData enemyData;
    Transform transform;
    public GameObject boomPrepab;
    int randomValue;
    
    // Start is called before the first frame update
    void Start()
    {
        enemyData = GetComponent<EnemyData>();
        transform = GetComponent<Transform>();
    }
    bool able= true;
    // Update is called once per frame
    void Update()
    {
        if (enemyData.enemy_current_HP <= 0 && able )
        {
            able = false;
            spwan();
            Destroy(gameObject, 0f);
        }
    }
    void spwan()
    {
        
        int random = Random.Range(0, 10);
        int random2 = Random.Range(0, 10);
        for (int i = 0; i <= 10; i++)
        {
            Instantiate(boomPrepab , new Vector2(transform.position.x + random - 5 , transform.position.y + random2 - 5), Quaternion.identity);
            random = Random.Range(0, 10);
            random2 = Random.Range(0, 10);
        }
            
       
    }
}
