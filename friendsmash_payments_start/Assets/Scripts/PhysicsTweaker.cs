using UnityEngine;
using System.Collections;

public class PhysicsTweaker : MonoBehaviour
{

    public Vector3 MyGravity;

    // Update is called once per frame
    void Update()
    {
        Physics.gravity = MyGravity;
    }
}
