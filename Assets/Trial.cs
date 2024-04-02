using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;

public class Trial : MonoBehaviour
{
    [SerializeField] private ROSPublisher publisher;

    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().RegisterPublisher<StringMsg>("/aloha");
    }

    // Update is called once per frame
    void Update()
    {
        publisher.StringMessage("/aloha", gameObject.transform.position.ToString());
    }
}
