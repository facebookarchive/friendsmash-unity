using UnityEngine;
using System.Collections;

public class Destroyer : MonoBehaviour
{

    public float YMin = -120.0f;

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y <= YMin)
        {
            if (gameObject.tag == "Friend" && !GameStateManager.ScoringLockout) GameStateManager.onFriendDie();
            Destroy(gameObject);
        }
    }

    void OnMouseDown()
    {
        if (gameObject.tag == "Friend")
        {
            GameStateManager.onFriendSmash();
            Destroy(gameObject);
        }
        else GameStateManager.onEnemySmash(gameObject);
    }
}
