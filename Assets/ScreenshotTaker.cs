using System;
using System.IO;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Scripts;

public class ScreenshotTaker : MonoBehaviour
{
    public string participantNumber;
    public int taskNumber;
    public int trialNumber;
    private bool recordingState = false;
    private float intervalTimer;
    private string baseFolderPath;
    private float totalElapsedTime;
    private float lastResumeTime;

    void Start()
    {
        ROSConnection.GetOrCreateInstance().ImplementService<UpdateStateRequest, UpdateStateResponse>("/image_writer/resume_recording", ResumeRecordingCallback);
        ROSConnection.GetOrCreateInstance().ImplementService<UpdateStateRequest, UpdateStateResponse>("/image_writer/pause_recording", PauseRecordingCallback);

        baseFolderPath = UnityEngine.Application.dataPath + "/data/" + "p" + participantNumber + "/m" + taskNumber + "/t" + trialNumber;

        UnityEngine.Debug.Log(baseFolderPath);
        Directory.CreateDirectory(baseFolderPath);
        totalElapsedTime = 0.0f;
        intervalTimer = 0.0f;
    }

    private UpdateStateResponse ResumeRecordingCallback(UpdateStateRequest request)
    {
        recordingState = true;
        lastResumeTime = Time.time;

        UpdateStateResponse updateReceived = new UpdateStateResponse();
        updateReceived.response = true;

        return updateReceived;
    }

    private UpdateStateResponse PauseRecordingCallback(UpdateStateRequest request)
    {
        recordingState = false;
        totalElapsedTime += Time.time - lastResumeTime;

        UpdateStateResponse updateReceived = new UpdateStateResponse();
        updateReceived.response = true;

        return updateReceived;
    }

    void Update()
    {
        if (recordingState)
        {
            intervalTimer += Time.deltaTime;

            if (intervalTimer >= 0.5f)
            {
                TakeScreenshot();
                intervalTimer = 0.0f;
            }
        }
    }

    void TakeScreenshot()
    {
        float currentSessionTime = Time.time - lastResumeTime;
        float elapsed = totalElapsedTime + currentSessionTime;
        string timestamp = elapsed.ToString("F1");
        string filePath = baseFolderPath + "/" + timestamp + ".png";

        int count = 1;
        while (File.Exists(filePath))
        {
            filePath = baseFolderPath + "/" + timestamp + "(" + count + ").png";
            count++;
        }

        ScreenCapture.CaptureScreenshot(filePath);
    }
}
