using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ScreenspaceFog : MonoBehaviour
{

    public Material imageEffectMaterial;
    public Camera camera;
    private void Start()
    {
        camera.depthTextureMode = DepthTextureMode.Depth;
    }

    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        
        Graphics.Blit(source, destination, imageEffectMaterial);
    }

}
