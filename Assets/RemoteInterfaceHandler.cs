using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Scripts;
using TMPro;
using System.Diagnostics;
using UnityEngine.EventSystems;
using System;
using System.Collections.Specialized;

public class RemoteInterfaceHandler : MonoBehaviour
{

    public bool withLocal;
    float awaitingResponseUntilTimestamp = -1;

    public RectTransform rawImageRectTransform, RightBackgroundRectTransform; // Assign this in the inspector
    public RawImage rawImage;

    private Texture2D imageTexture;
    private Vector2 startMousePosition;
    private Rect lastRect; // Rect to store the last drawn rectangle
    private bool isDrawing;
    private bool rhHelp = false;
    private Color rectangleColor = Color.green; // Set rectangle color to green
    private int interfaceButton = 0;
    private int taskState = 0, hasGrasped;

    public Canvas canvas;
    public GameObject MainBox;
    public GameObject HeadingBox;
    public GameObject ErrorBox;

    public GameObject ButtonGrasp, ButtonSetAside, ButtonMoveHere, ButtonBin, ButtonCallLocal;
    public GameObject TaskStatusPanel;
    private TextMeshProUGUI MaintextBox;
    private TextMeshProUGUI HeadingtextBox;
    private TextMeshProUGUI ErrorTextBox;

    private AudioSource audioNotification, audioError, audioGrasping;
    private string medicineName, expirationMonth, expirationYear, message;
    private int markerID, previousTaskState;
    private short requestID;
    float minimumDragDistance = 5.0f; // 5 pixels
    private bool positionEstimated = true, setAside = false, notify = false;

    // Start is called before the first frame update
    void Start()
    {
        ROSConnection.GetOrCreateInstance().RegisterRosService<UpdateStateRequest, UpdateStateResponse>("/local_request");
        ROSConnection.GetOrCreateInstance().RegisterRosService<UpdateStateRequest, UpdateStateResponse>("/move_chest");
        ROSConnection.GetOrCreateInstance().RegisterRosService<UpdateStateRequest, UpdateStateResponse>("/change_task_state_service");
        ROSConnection.GetOrCreateInstance().RegisterRosService<UpdateStateRequest, UpdateStateResponse>("/resume_task");
        ROSConnection.GetOrCreateInstance().RegisterRosService<UpdateStateRequest, UpdateStateResponse>("/move_medicine");

        ROSConnection.GetOrCreateInstance().RegisterPublisher<Int32Msg>("/remote_interface/buttons");
        ROSConnection.GetOrCreateInstance().RegisterPublisher<BoolMsg>("/rh_help");

        ROSConnection.GetOrCreateInstance().RegisterRosService<ItemPositionFOVRequest, ItemPositionFOVResponse>("/calculate_world_position_service");
        
        ROSConnection.GetOrCreateInstance().ImplementService<UpdateStateRequest, UpdateStateResponse>("/remote_help_request_service", RequestHelpCallback);
        
        ROSConnection.GetOrCreateInstance().Subscribe<TargetInfoMsg>("/target_identifier", TargetIdentifierCallback);
        ROSConnection.GetOrCreateInstance().Subscribe<Int32Msg>("/my_gen3/pick_and_place", TaskStateCallback);
        ROSConnection.GetOrCreateInstance().Subscribe<Int32Msg>("/my_gen3/grasping", GraspingCallback);


        MaintextBox = MainBox.GetComponent<TextMeshProUGUI>();
        HeadingtextBox = HeadingBox.GetComponent<TextMeshProUGUI>();
        ErrorTextBox = ErrorBox.GetComponentInChildren<TextMeshProUGUI>();
        imageTexture = new Texture2D((int)rawImageRectTransform.rect.width, (int)rawImageRectTransform.rect.height);

        ButtonCallLocal.SetActive(false);
        ButtonGrasp.SetActive(false);
        ButtonMoveHere.SetActive(false);
        ButtonBin.SetActive(false);
        ButtonSetAside.SetActive(false);

        audioNotification = TaskStatusPanel.GetComponent<AudioSource>();
        audioError = ErrorBox.GetComponent<AudioSource>();
        audioGrasping = MainBox.GetComponent<AudioSource>();
        // You may want to clear the texture or set an initial color
        ClearTexture(imageTexture, Color.clear);

    }

    void ClearTexture(Texture2D texture, Color color)
    {
        Color[] colorArray = texture.GetPixels();
        for (int i = 0; i < colorArray.Length; i++)
        {
            colorArray[i] = color;
        }
        texture.SetPixels(colorArray);
        texture.Apply();
    }

    private UpdateStateResponse RequestHelpCallback(UpdateStateRequest request)
    {
        requestID = request.state;

        // Switch to help state
        if (requestID == 0)
        {
            rhHelp = false;
        }
        else
        {
            rhHelp = true;

            if (requestID != 3)
            {
                audioNotification.Play();
            }
        }

        UpdateStateResponse updateReceived = new UpdateStateResponse();
        updateReceived.response = true;

        return updateReceived;
    }

    void TaskStateCallback(Int32Msg state)
    {
        taskState = state.data;
    }

    void GraspingCallback(Int32Msg graspingState)
    {
        hasGrasped = graspingState.data;
        UnityEngine.Debug.Log(hasGrasped);
    }

    void TargetIdentifierCallback(TargetInfoMsg target)
    {
        medicineName = target.name;
        expirationMonth = target.expiration.Substring(0, 2);
        expirationYear = target.expiration.Substring(2);
        markerID = target.id;
    }

    public void SendToLocal()
    {
        UpdateStateRequest resumeRequest = new UpdateStateRequest(requestID);

        ROSConnection.GetOrCreateInstance().SendServiceMessage<UpdateStateResponse>("/local_request", resumeRequest, Callback_Service);
        awaitingResponseUntilTimestamp = Time.time + 0.2f;

        // Delete last drawn rectangle
        lastRect = new Rect(0, 0, 0, 0);

        ButtonGrasp.SetActive(false);
        ButtonSetAside.SetActive(false);

        interfaceButton = 2;
    }

    public void SendToRobot()
    {
        if (positionEstimated)
        {
            //rhHelp = false;

            UpdateStateRequest resumeRequest = new UpdateStateRequest(0);

            ROSConnection.GetOrCreateInstance().SendServiceMessage<UpdateStateResponse>("/resume_task", resumeRequest, Callback_Service);
            awaitingResponseUntilTimestamp = Time.time + 0.2f;

            // Delete last drawn rectangle
            lastRect = new Rect(0, 0, 0, 0);


            notify = true;
            
            interfaceButton = 3;
        }

        ButtonGrasp.SetActive(false);
        ButtonSetAside.SetActive(false);

    }

    void MoveMedicine(short id)
    {        
        ButtonMoveHere.SetActive(false);
        ButtonSetAside.SetActive(false);
        ButtonBin.SetActive(false);

        UpdateStateRequest moveRequest = new UpdateStateRequest(id);

        ROSConnection.GetOrCreateInstance().SendServiceMessage<UpdateStateResponse>("/move_medicine", moveRequest, Callback_Service);
        awaitingResponseUntilTimestamp = Time.time + 0.2f;

        lastRect = new Rect(0, 0, 0, 0);

        if (id == 1)
        {
            rhHelp = false;
        }

        setAside = false;
    }

    public void MoveToBin()
    {
        MoveMedicine(1);

        interfaceButton = 4;
    }

    public void MoveHere()
    {
        if (positionEstimated)
        {
            MoveMedicine(2);
        }
        
        interfaceButton = 5;

    }

    public void SetAside()
    {
        MoveMedicine(3);

        interfaceButton = 6;

        setAside = true;

        ButtonGrasp.SetActive(false);
        ButtonSetAside.SetActive(false);
        ButtonMoveHere.SetActive(false);
        ButtonBin.SetActive(false);
    }

    void Callback_Service(UpdateStateResponse response)
    {
        awaitingResponseUntilTimestamp = -1;

        interfaceButton = 0;

        if (response.response)
        {
            // Response handling
        }
    }

    void Callback_FOV_Service(ItemPositionFOVResponse response)
    {
        awaitingResponseUntilTimestamp = -1;

        if (response.response == false)
        {
            positionEstimated = false;
            audioError.Play();
        }
        else
        {
            positionEstimated = true;
        }
    }

    private void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color color)
    {
        for (int x = -radius; x <= radius; x++)
        {
            int height = (int)Mathf.Sqrt(radius * radius - x * x);

            for (int y = -height; y <= height; y++)
                texture.SetPixel(cx + x, cy + y, color);
        }
        texture.Apply();
    }

    private IEnumerator BlinkText()
    {
        float duration = 5f; // 3 seconds
        float blinkTime = 0.5f;

        while (duration > 0f)
        {
            ErrorTextBox.enabled = !ErrorTextBox.enabled;
            yield return new WaitForSeconds(blinkTime);
            duration -= blinkTime;
        }

        ErrorTextBox.enabled = true; // Ensure it's visible after blinking
    }

    void UpdateText()
    {
        if (rhHelp)
        {
            MainBox.SetActive(true);
            HeadingBox.SetActive(true);

            if (requestID == 1)
            {
                message = "Medicine was not detected";

                ErrorTextBox.text = $"Look for {medicineName} (ID {markerID}) and place it in the bin.";
                ErrorBox.SetActive(true);

            }
            else if (requestID == 2)
            {

                message = "Medicine is expired";

                if (setAside)
                {
                    ErrorTextBox.text = $"Select a valid {medicineName} and place it in the bin.";
                    ErrorBox.SetActive(true);

                    if ((previousTaskState != taskState) && taskState == 4)
                    {                    
                        audioGrasping.Play();
                        StartCoroutine(BlinkText());
                    }

                }
                else
                {
                    ErrorTextBox.text = $"Select the expired {medicineName} (ID {markerID}) and set it aside.";
                    ErrorBox.SetActive(true);
                }
                
            }
            else if (requestID == 3)
            {
                message = "Medicine was not detected";

                ErrorTextBox.text = "Local operator is solving the issue.";
                ErrorBox.SetActive(true);
            }
            else
            {
                ErrorBox.SetActive(false);
            }

            if (!positionEstimated)
            {
                ErrorTextBox.text = "ERROR: Cannot estimate target position";
                ErrorBox.SetActive(true);
            }
            //else
            //{
            //    ErrorBox.SetActive(false);
            //}
        }
        else
        {
            MainBox.SetActive(false);
            HeadingBox.SetActive(false);
            ErrorBox.SetActive(false);
        }

        MaintextBox.text = message;

    }

    void OnGUI()
    {   
        // Set the GUI color to green
        GUI.contentColor = Color.green;

        if (imageTexture != null)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), imageTexture);
        }

        // Draw the rectangle while dragging or if the final rectangle has been drawn
        if (isDrawing || (lastRect.width != 0 && lastRect.height != 0))
        {
            GUI.Box(lastRect, GUIContent.none);

        }

        // Reset the GUI color to default
        GUI.color = Color.white;
    }

    Vector2 GetRectangleCenterInImageCoordinates(Rect screenRectangle)
    {
        // Convert the corners of the RectTransform to screen space
        Vector3[] corners = new Vector3[4];
        rawImageRectTransform.GetWorldCorners(corners);

        // Get Canvas Scale factor
        float scale = canvas.scaleFactor;
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]/scale);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]/scale);

        // Calculate the RawImage's size on the screen
        float rawImageWidth = topRight.x - bottomLeft.x;
        float rawImageHeight = topRight.y - bottomLeft.y;

        // Convert each corner of the screen rectangle to texture coordinates
        float leftX = (screenRectangle.x - bottomLeft.x) / rawImageWidth;
        float rightX = (screenRectangle.x + screenRectangle.width - bottomLeft.x) / rawImageWidth;

        // Invert the Y-coordinate as texture's origin is at the top left
        float bottomY = ((screenRectangle.y - bottomLeft.y) / rawImageHeight);
        float topY = ((screenRectangle.y + screenRectangle.height - bottomLeft.y) / rawImageHeight);

        // Calculate the center in texture coordinates
        Texture tex = rawImage.texture;
        Vector2 centerTex = new Vector2(
            (leftX + rightX) / 2 * tex.width/scale,
            (bottomY + topY) / 2 * tex.height/scale
        );

        return centerTex;
    }

    public void MoveChestUp()
    {
        ChestService(0);
    }

    public void MoveChestDown()
    {
        ChestService(1);
    }

    private void ChestService(short value)
    {
        if (rhHelp && (taskState == 0 || taskState == 2 || taskState == 4))

        {
            UpdateStateRequest moveRequest = new UpdateStateRequest(value);

            ROSConnection.GetOrCreateInstance().SendServiceMessage<UpdateStateResponse>("/move_chest", moveRequest, Callback_Service);
            awaitingResponseUntilTimestamp = Time.time + 0.2f;
        }

        interfaceButton = 1;
    }

    bool IsMouseOverRawImage()
    {
        Vector2 localPoint;
        // Convert the mouse position to a local point in the RawImage RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRectTransform, Input.mousePosition, null, out localPoint);
        // Check if the local point is inside the RectTransform
        return rawImageRectTransform.rect.Contains(localPoint);
    }

    bool IsMouseOverRightBackground()
    {
        Vector2 localPoint;
        // Convert the mouse position to a local point in the RawImage RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(RightBackgroundRectTransform, Input.mousePosition, null, out localPoint);
        // Check if the local point is inside the RectTransform
        return RightBackgroundRectTransform.rect.Contains(localPoint);
    }

    void Update()
    {
        UpdateText();

        if (taskState == 0)
        {
            hasGrasped = 0;
        }

        if (rhHelp)
        {
            if (IsMouseOverRawImage())
            {

                // Detect mouse down
                if (Input.GetMouseButtonDown(0))
                {
                    startMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                    isDrawing = true;

                    ButtonGrasp.SetActive(false);
                    ButtonSetAside.SetActive(false);
                    ButtonMoveHere.SetActive(false);
                    ButtonBin.SetActive(false);
                }

                // Update rectangle while dragging
                if (isDrawing && Input.GetMouseButton(0))
                {
                    Vector2 currentMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                    float dragDistance = Vector2.Distance(startMousePosition, currentMousePosition);

                    if (dragDistance > minimumDragDistance) // minimumDragDistance is a float you define
                    {
                        lastRect = new Rect(startMousePosition.x, Screen.height - startMousePosition.y,
                                        Input.mousePosition.x - startMousePosition.x,
                                        -1 * (Input.mousePosition.y - startMousePosition.y));

                        interfaceButton = 7;
                    }
                }

                // Detect mouse up
                if (Input.GetMouseButtonUp(0) && isDrawing)
                {

                    isDrawing = false;
                    interfaceButton = 0;

                    Vector2 endMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

                    float dragDistance = Vector2.Distance(startMousePosition, endMousePosition);

                    if (dragDistance > minimumDragDistance)
                    {
                        Vector2 imageCoordinatesRect = GetRectangleCenterInImageCoordinates(lastRect);

                        Float32MultiArrayMsg floatArrayMsg = new Float32MultiArrayMsg();

                        // Initialize the data array
                        floatArrayMsg.data = new float[] { imageCoordinatesRect.x, imageCoordinatesRect.y };
                        
                        ItemPositionFOVRequest FOVPositionRequest = new ItemPositionFOVRequest(floatArrayMsg);

                        ROSConnection.GetOrCreateInstance().SendServiceMessage<ItemPositionFOVResponse>("/calculate_world_position_service", FOVPositionRequest, Callback_FOV_Service);
                        awaitingResponseUntilTimestamp = Time.time + 0.2f;

                        if (taskState == 0)
                        {
                            ButtonGrasp.SetActive(true);

                            //if (withLocal)
                            //{
                            //    ButtonLocal.SetActive(true);
                            //}
                        }
                        else if (taskState == 2)
                        {
                            ButtonMoveHere.SetActive(true);
                            ButtonBin.SetActive(false);
                            ButtonSetAside.SetActive(false);
                        }

                    }
                    else
                    {
                        lastRect = new Rect(0, 0, 0, 0);

                        positionEstimated = true;

                    }

                    interfaceButton = 0;

                }
            }

            if (taskState == 2 && (lastRect.width == 0 && lastRect.height == 0))
            {
                
                if (previousTaskState != taskState)
                {
                    ButtonSetAside.SetActive(true);
                    ButtonBin.SetActive(true);
                }                    
                
                ButtonMoveHere.SetActive(false);

            }

            if (taskState == 2 && notify && hasGrasped == 1)
            {
                UnityEngine.Debug.Log("Here " + hasGrasped);
                StartCoroutine(PlayGraspingAudioAfterDelay(5.0f)); // 3.0f is the delay in seconds

                //audioGrasping.Play();
                notify = false;
            }

            if (withLocal)
            {   

                if (taskState == 0)
                {
                    ButtonCallLocal.SetActive(true);
                }
                else
                {
                    ButtonCallLocal.SetActive(false);
                }
            }
        }

        previousTaskState = taskState;

        //UnityEngine.Debug.Log(taskState);
    }

    private IEnumerator PlayGraspingAudioAfterDelay(float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);
        audioGrasping.Play();
    }

    void FixedUpdate()
    {

        Int32Msg intMsg = new Int32Msg { data = interfaceButton };
        ROSConnection.GetOrCreateInstance().Publish("/remote_interface/buttons", intMsg);

        BoolMsg rhMsg = new BoolMsg { data = rhHelp };
        ROSConnection.GetOrCreateInstance().Publish("/rh_help", rhMsg);
    }
}
