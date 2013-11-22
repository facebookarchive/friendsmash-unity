using UnityEngine;

public class EnemyExploder : MonoBehaviour
{
    static Vector3 target = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0));
    Vector3 ourScale;

    // Use this for initialization
    void Start()
    {
        GameStateManager.ScoringLockout = true;
        Vector3 direction = (EnemyExploder.target - gameObject.transform.position);
        float scalar = 1f / 2;
        direction.Scale(new Vector3(scalar, scalar, 0));
        gameObject.rigidbody.useGravity = false;
        gameObject.rigidbody.velocity = direction;
        ourScale = gameObject.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.localScale += new Vector3(1.5f, 1.5f, 1.5f);
        if (gameObject.transform.localScale.x >= 4 * ourScale.x)
        {
            GameStateManager.EndGame();
        }
    }
}
