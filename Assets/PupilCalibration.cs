using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Scripts;
using System.Diagnostics;

namespace Tobii.Research.Unity
{
    public class PupilCalibration : MonoBehaviour
    {
        private AudioSource doneAudio;

        // Gaze tracker
        private IEyeTracker _eyeTracker;
        private LockedQueue<GazeDataEventArgs> _originalGazeData =
            new LockedQueue<GazeDataEventArgs>(maxCount: 130);
        private GazeDataEventArgs _eyeData;
        public Vector2 pixel;
        public float leftPupilDiameter;
        public float rightPupilDiameter;

        private List<float> rightPupilDiameters = new List<float>();
        private List<float> leftPupilDiameters = new List<float>();

        void Start()
        {
            ROSConnection.GetOrCreateInstance().RegisterPublisher<Float32MultiArrayMsg>("/remote_interface/gaze");
            ROSConnection.GetOrCreateInstance().RegisterPublisher<Float32MultiArrayMsg>("/remote_interface/pupil_diameter");

            var eyeTrackers = EyeTrackingOperations.FindAllEyeTrackers();
            foreach (var eyeTrackerEntry in eyeTrackers)
            {
                _eyeTracker = eyeTrackerEntry;
                _eyeTracker.GazeDataReceived += GazeDataReceivedCallback;
                UnityEngine.Debug.Log(_eyeTracker.SerialNumber);
            }


            doneAudio = GetComponent<AudioSource>();

            StartCoroutine(DelayedStart(5));
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

        private void GazeDataReceivedCallback(
            object sender, GazeDataEventArgs eventArgs
        )
        {
            _originalGazeData.Next = eventArgs;

            // Temporary variables for pupil diameters
            float tempRightPupilDiameter = eventArgs.RightEye.Pupil.PupilDiameter;
            float tempLeftPupilDiameter = eventArgs.LeftEye.Pupil.PupilDiameter;

            // Check if pupil diameters are NaN and replace with 0
            rightPupilDiameter = float.IsNaN(tempRightPupilDiameter) ? 0 : tempRightPupilDiameter;
            leftPupilDiameter = float.IsNaN(tempLeftPupilDiameter) ? 0 : tempLeftPupilDiameter;

        }

        private IEnumerator DelayedStart(float delay)
        {
            // Wait for 5 seconds before starting the pupil diameter recording
            yield return new WaitForSeconds(delay);
            StartCoroutine(RecordPupilDiameterForDuration(30));
        }

        private IEnumerator RecordPupilDiameterForDuration(float duration)
        {
            float startTime = Time.time;
            float endTime = startTime + duration;
            float nextPrintTime = startTime; // Initialize next print time

            while (Time.time < endTime)
            {
                if (rightPupilDiameter != 0 && leftPupilDiameter != 0)
                {
                    rightPupilDiameters.Add(rightPupilDiameter);
                    leftPupilDiameters.Add(leftPupilDiameter);
                }

                // Check if it's time to print the remaining time
                if (Time.time >= nextPrintTime)
                {
                    float remainingTime = endTime - Time.time;
                    UnityEngine.Debug.Log($"Recording... {remainingTime:F2} seconds left.");
                    nextPrintTime = Time.time + 5; // Schedule the next print time
                }

                yield return new WaitForSeconds(0.1f);
            }

            // Calculate and log the average pupil diameter
            float averageRightPupilDiameter = CalculateAverage(rightPupilDiameters);
            float averageLeftPupilDiameter = CalculateAverage(leftPupilDiameters);

            UnityEngine.Debug.Log("Right: " + averageRightPupilDiameter);
            UnityEngine.Debug.Log("Left: " + averageLeftPupilDiameter);

            doneAudio.Play();
        }

        private float CalculateAverage(List<float> values)
        {
            float sum = 0;
            foreach (float value in values)
            {
                sum += value;
            }
            return values.Count > 0 ? sum / values.Count : 0;
        }
    }
}
