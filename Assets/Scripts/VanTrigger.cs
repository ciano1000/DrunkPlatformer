using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VanTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject van;

    Rigidbody vanRigidbody;
    void Start()
    {
        vanRigidbody = van.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter(Collider other)
    {
        vanRigidbody.AddForce(Vector3.right * 12000);
        Debug.Log("Entered");
    }
}
