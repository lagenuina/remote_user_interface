using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Scripts;

namespace Tobii.Research.Unity
{
    public class GazeTracking : MonoBehaviour
    {
        // Gaze tracker
        private IEyeTracker _eyeTracker;
        private LockedQueue<GazeDataEventArgs> _originalGazeData = 
            new LockedQueue<GazeDataEventArgs>(maxCount: 130);
        private GazeDataEventArgs _eyeData;
        public Vector2 pixel;
        private float leftPupilDiameter;
        private float rightPupilDiameter;
        public float calibrationRightPupil, calibrationLeftPupil;

        // Blink detection 
        /*
        private float rightblink_detect = 0;
        private float leftblink_detect = 0;
        private float TimeT;
        private float old_time = 0;
        private float change_time = 0;
        private int right_a;
        private int left_a;
        */


        void Start()
        {
            ROSConnection.GetOrCreateInstance().RegisterPublisher<Float32MultiArrayMsg>("/remote_interface/gaze");
            ROSConnection.GetOrCreateInstance().RegisterPublisher<Float32MultiArrayMsg>("/remote_interface/pupil_diameter");
            ROSConnection.GetOrCreateInstance().RegisterPublisher<Float32MultiArrayMsg>("/remote_interface/calibrated_pupil_diameter");

            var eyeTrackers = EyeTrackingOperations.FindAllEyeTrackers();
            foreach (var eyeTrackerEntry in eyeTrackers)
            {
                _eyeTracker = eyeTrackerEntry;
                _eyeTracker.GazeDataReceived += GazeDataReceivedCallback;
                Debug.Log(_eyeTracker.SerialNumber);
            }
        }

        void OnEnable()
        {
            if (_eyeTracker != null)
            {
                _eyeTracker.GazeDataReceived += GazeDataReceivedCallback;
            }
        }

        void OnDisable()
        {
            _eyeTracker.GazeDataReceived -= GazeDataReceivedCallback;
        }

        void OnDestroy()
        {
            EyeTrackingOperations.Terminate();
        }

        void FixedUpdate()
        {   
            float[] gazeArray = new float[2];
            gazeArray[0] = pixel.x;
            gazeArray[1] = pixel.y;
            Float32MultiArrayMsg gazeArrayMsg = new Float32MultiArrayMsg { data = gazeArray };
            ROSConnection.GetOrCreateInstance().Publish("/remote_interface/gaze", gazeArrayMsg);

            float[] pupilArray = new float[2];
            pupilArray[0] = rightPupilDiameter;
            pupilArray[1] = leftPupilDiameter;

            Float32MultiArrayMsg pupilArrayMsg = new Float32MultiArrayMsg { data = pupilArray };
            ROSConnection.GetOrCreateInstance().Publish("/remote_interface/pupil_diameter", pupilArrayMsg);

            float[] calibratedPupilArray = new float[2];
            calibratedPupilArray[0] = calibrationRightPupil;
            calibratedPupilArray[1] = calibrationLeftPupil;

            Float32MultiArrayMsg calibratedPupilArrayMsg = new Float32MultiArrayMsg { data = calibratedPupilArray };
            ROSConnection.GetOrCreateInstance().Publish("/remote_interface/calibrated_pupil_diameter", calibratedPupilArrayMsg);
        }

        private void GazeDataReceivedCallback(
            object sender, GazeDataEventArgs eventArgs
        )
        {
            _originalGazeData.Next = eventArgs;

            //Debug.Log("Connected to Eye Tracker: " + Screen.width * eventArgs.RightEye.GazePoint.PositionOnDisplayArea.X + "," + Screen.height * (1 - eventArgs.RightEye.GazePoint.PositionOnDisplayArea.Y) + "," + eventArgs.RightEye.Pupil.PupilDiameter);
            //return eventArgs;
            //_eyeData = eventArgs;

            float tempPixelX = 1920 * (
                eventArgs.RightEye.GazePoint.PositionOnDisplayArea.X 
                + eventArgs.LeftEye.GazePoint.PositionOnDisplayArea.X
            ) / 2;
            float tempPixelY = 1080 * (
                (1 - eventArgs.RightEye.GazePoint.PositionOnDisplayArea.Y) 
                + (1 - eventArgs.LeftEye.GazePoint.PositionOnDisplayArea.Y)
            ) / 2;

            // Check if tempPixelX or tempPixelY is NaN and replace with 0
            pixel.x = float.IsNaN(tempPixelX) ? 0 : tempPixelX;
            pixel.y = float.IsNaN(tempPixelY) ? 0 : tempPixelY;

            // Temporary variables for pupil diameters
            float tempRightPupilDiameter = eventArgs.RightEye.Pupil.PupilDiameter;
            float tempLeftPupilDiameter = eventArgs.LeftEye.Pupil.PupilDiameter;

            // Check if pupil diameters are NaN and replace with 0
            rightPupilDiameter = float.IsNaN(tempRightPupilDiameter) ? 0 : tempRightPupilDiameter;
            leftPupilDiameter = float.IsNaN(tempLeftPupilDiameter) ? 0 : tempLeftPupilDiameter;
            //Debug.Log(Screen.width + "," + Screen.height);
        }
    }
}


