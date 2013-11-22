using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{

    public GameObject Target;

    // Use this for initialization
    void Start()
    {
        StartCoroutine(SpawnTarget());
    }

    public Vector3 forceScale = new Vector3(); // (35, 110, 1);
    public float minSpawnTime; // = .75f;
    public float maxSpawnTime; // = 1.25f;
    public float minYForceFraction; // = .45
    public float minXForceFraction; // = .25f

    IEnumerator SpawnTarget()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));
            if (Target != null)
            {
                GameObject curr = (GameObject)Instantiate(
                    Target,
                    new Vector3(Random.Range(-150, 150), -100, 0),
                    Quaternion.identity
                );
                Vector3 force = new Vector3();
                force.x = curr.transform.position.x <= 0.0f ? 1 : -1;
                force.x *= Random.Range(minXForceFraction, 2);
                force.y = Random.Range(minYForceFraction, 3);
                force.z = 0;
                force.Scale(forceScale);
                curr.rigidbody.AddForce(force, ForceMode.Impulse);
                curr.rigidbody.angularVelocity = new Vector3(0,0,Random.Range(-150, 150));
            }
        }
    }
}
