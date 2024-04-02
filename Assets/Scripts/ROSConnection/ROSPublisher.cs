using System.Collections;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;

public class ROSPublisher : MonoBehaviour
{

    void Start()
    {
        // start the ROS connection
        ROSConnection.GetOrCreateInstance();
    }

    public void Pose(string topicName, Vector3 objectPosition, Quaternion objectOrientation)
    {
        // Decompose Vector3 and Quaternion into their components
        PointMsg position = new PointMsg
        {
            x = objectPosition.x,
            y = objectPosition.y,
            z = objectPosition.z
        };

        QuaternionMsg orientation = new QuaternionMsg
        {
            x = objectOrientation.x,
            y = objectOrientation.y,
            z = objectOrientation.z,
            w = objectOrientation.w
        };

        // Create message
        PoseMsg pose = new PoseMsg
        {
            position = position,
            orientation = orientation
        };

        ROSConnection.GetOrCreateInstance().Publish(topicName, pose);

    }

    public void Position(string topicName, Vector3 objectPosition)
    {
        // Create message
        PointMsg position = new PointMsg(
            objectPosition.x,
            objectPosition.y,
            objectPosition.z
            );

        // Publish the message
        ROSConnection.GetOrCreateInstance().Publish(topicName, position);
    }

    /// <summary>
    /// Send a Quaternion Message to server_endpoint.py running in ROS.
    /// The rotation expressed in quaternions of the GameObject is converted to Right-Handed Coordinate System.
    /// <param name="topicName"></param>
    /// <param name="objectToPublish"></param>
    public void RotationMessage(string topicName, GameObject objectToPublish)
    {
        //// Covert to ROS Coordinate Frame
        //QuaternionMsg rotationFLU = objectToPublish.transform.rotation.To<FLU>();

        //// Create message
        //QuaternionMsg rotation = new QuaternionMsg(
        //    rotationFLU.x,
        //    rotationFLU.y,
        //    rotationFLU.z,
        //    rotationFLU.w
        //    );

        // Create message
        QuaternionMsg rotation = new QuaternionMsg(
            objectToPublish.transform.rotation.x,
            objectToPublish.transform.rotation.y,
            objectToPublish.transform.rotation.z,
            objectToPublish.transform.rotation.w
            );

        // Publish the message
        ROSConnection.GetOrCreateInstance().Publish(topicName, rotation);
    }

    public void StringMessage(string topicName, string message)
    {
        StringMsg stringMsg = new StringMsg { data = message };
        ROSConnection.GetOrCreateInstance().Publish(topicName, stringMsg);
    }

    public void Int32Message(string topicName, int message)
    {
        Int32Msg intMsg = new Int32Msg { data = message };
        ROSConnection.GetOrCreateInstance().Publish(topicName, intMsg);
    }

    public void Float32Message(string topicName, float message)
    {
        Float32Msg floatMsg = new Float32Msg { data = message };
        ROSConnection.GetOrCreateInstance().Publish(topicName, floatMsg);
    }

    public void BoolMessage(string topicName, bool message)
    {
        BoolMsg booleanMsg = new BoolMsg { data = message };
        ROSConnection.GetOrCreateInstance().Publish(topicName, booleanMsg);
    }
}