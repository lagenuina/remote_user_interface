using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class TempGUI : MonoBehaviour
{
    // Camera source
    [SerializeField] private ImageSubscriber mainCameraSubscriber;

    // Main camera
    [SerializeField] private GameObject cameraDisplay;
    private Vector2 cameraResolution;
    private RectTransform cameraDisplayRect;
    private RenderTexture cameraRendertexture;

    void Start()
    {
        // main render texture
        cameraDisplayRect = cameraDisplay.GetComponent<RectTransform>();
        cameraRendertexture = new RenderTexture((int)cameraDisplayRect.rect.width, 
                                                (int)cameraDisplayRect.rect.height, 24);
        cameraRendertexture.name = "Main Camera Render Texture";
        cameraDisplay.GetComponent<RawImage>().texture = cameraRendertexture;

        // subscribe to camera source
        mainCameraSubscriber.TargetTexture = cameraRendertexture;
    }

    void Update()
    {
        
    }
}
