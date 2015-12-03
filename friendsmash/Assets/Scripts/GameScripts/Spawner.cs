/**
 * Copyright (c) 2014-present, Facebook, Inc. All rights reserved.
 *
 * You are hereby granted a non-exclusive, worldwide, royalty-free license to use,
 * copy, modify, and distribute this software in source code or binary form for use
 * in connection with the web services and APIs provided by Facebook.
 *
 * As with any software that integrates with the Facebook platform, your use of
 * this software is subject to the Facebook Developer Principles and Policies
 * [http://developers.facebook.com/policy/]. This copyright notice shall be
 * included in all copies or substantial portions of the software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public GameObject Target;
    
    void Start()
    {
        StartCoroutine(SpawnTarget());
    }

    public Vector3 forceScale = new Vector3(); // (35, 110, 1);
    public float minSpawnTime; // = .75f;
    public float maxSpawnTime; // = 1.25f;
    public float minYForceFraction; // = .45
    public float maxYForceFraction; // = 2
    public float minXForceFraction; // = .25f
    public float maxXForceFraction; // = 2

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
                force.x *= Random.Range(minXForceFraction, maxYForceFraction);
                force.y = Random.Range(minYForceFraction, maxXForceFraction);
                force.z = 0;
                force.Scale(forceScale);

                Rigidbody rb = curr.GetComponent<Rigidbody>();
                rb.AddForce(force, ForceMode.Impulse);
                rb.angularVelocity = new Vector3(0,0,Random.Range(-150, 150));
            }
        }
    }
}
