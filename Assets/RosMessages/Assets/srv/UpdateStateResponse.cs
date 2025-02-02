//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.Assets
{
    [Serializable]
    public class UpdateStateResponse : Message
    {
        public const string k_RosMessageName = "Assets/UpdateState";
        public override string RosMessageName => k_RosMessageName;

        public bool response;

        public UpdateStateResponse()
        {
            this.response = false;
        }

        public UpdateStateResponse(bool response)
        {
            this.response = response;
        }

        public static UpdateStateResponse Deserialize(MessageDeserializer deserializer) => new UpdateStateResponse(deserializer);

        private UpdateStateResponse(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.response);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.response);
        }

        public override string ToString()
        {
            return "UpdateStateResponse: " +
            "\nresponse: " + response.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize, MessageSubtopic.Response);
        }
    }
}
