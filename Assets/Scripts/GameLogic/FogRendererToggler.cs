using UnityEngine;
using System.Collections.Generic;

public class FogRendererToggler : MonoBehaviour
{
    //private Renderer myRenderer; // reference to the render you want toggled based on the position of this transform
    private List<Renderer> myRenderers;
    private Outline outline;
    private UnitManager unitManager;
    [Range(0f, 1f)] public float threshold = 0.5f; //the threshold for when this script considers myRenderer should render

    private new Camera camera; //The Camera using the masked render texture

    // made so all instances share the same texture, reducing texture reads
    private static Texture2D shadowTexture;
    private static Rect rect;
    private static bool isDirty = true;// used so that only one instance will update the RenderTexture per frame

    /*
    public float sleepTickRate;
    public float awakeTickRate;

    private float sleepTickTimer;
    private float awakeTickTimer;
    */

    public float tickRate = 1f;
    private float tickTimer;

    private void Awake()
    {
        if (!Game.Instance.gameGlobalParameters.enableFOV)
        {
            this.enabled = false;
            return;
        }

        myRenderers = new List<Renderer>();
        Utility.CollectComponentsInChildren<Renderer>(myRenderers, transform, "FOV");

        //Debug.Log("Renderers found: " + myRenderers.Count);
        camera = GameObject.Find("FogOfWarCam").GetComponent<Camera>();
        unitManager = GetComponent<UnitManager>();
        outline = transform.GetComponentInChildren<Outline>();

        tickTimer = 0f;
    }

    private Color GetColorAtPosition()
    {
        if (!camera)
        {
            // if no camera is referenced script assumes there no fog and will return white (which should show the entity)
            return Color.white;
        }

        RenderTexture renderTexture = camera.targetTexture;
        if (!renderTexture)
        {
            //fallback to Camera's Color
            return camera.backgroundColor;
        }

        if (shadowTexture == null || renderTexture.width != rect.width || renderTexture.height != rect.height)
        {
            rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
            shadowTexture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        }

        if (isDirty)
        {
            RenderTexture.active = renderTexture;
            shadowTexture.ReadPixels(rect, 0, 0);
            RenderTexture.active = null;
            isDirty = false;
        }

        var pixel = camera.WorldToScreenPoint(transform.position);
        return shadowTexture.GetPixel((int)pixel.x, (int)pixel.y);
    }

    private void Update()
    {
        isDirty = true;
    }

    void LateUpdate()
    {
        if(myRenderers.Count == 0)
        {
            this.enabled = false;
            return;
        }

        // tick
        tickTimer += Time.deltaTime;

        // skip if timer not ready
        if(tickTimer < tickRate) return;
        
        // reset tick rate
        tickTimer = 0f;
        //Debug.Log("TICKING FOG CHECK");

        if (GetColorAtPosition().grayscale >= threshold)
        {
            //Debug.Log("Enabling renderers");
            ToggleMeshRenderers(true);
            if (outline != null) outline.enabled = true;
            //unitManager.EnableUnitUI();
        }
        else
        {
            //Debug.Log("Disabling renderers: " + GetColorAtPosition().grayscale);
            ToggleMeshRenderers(false);
            if (outline != null) outline.enabled = false;
            unitManager.DisableUnitUI();
        }

        //myRenderer.enabled = GetColorAtPosition().grayscale >= threshold;

    }

    private void ToggleMeshRenderers(bool status)
    {
        foreach(Renderer r in myRenderers) r.enabled = status;
    }

    public void AddRendererReference(Renderer newRenderer)
    {
        if(myRenderers == null) myRenderers = new List<Renderer>();

        if(!myRenderers.Contains(newRenderer)) myRenderers.Add(newRenderer);
        newRenderer.enabled = myRenderers[0].enabled;
    }

    public void AddRendererReference(Transform transform)
    {
        if (transform.TryGetComponent<Renderer>(out Renderer renderer))
        {
            AddRendererReference(renderer);
        }
    }

    public void RemoveRenderReference(Renderer renderer)
    {
        if(myRenderers == null)
        {
            myRenderers = new List<Renderer>();
            return;
        }

        if (myRenderers.Contains(renderer)) myRenderers.Remove(renderer);
        //renderer.enabled = myRenderers[0].enabled;
        renderer.enabled = true;
    }

    public void RemoveRenderReference(Transform transform)
    {
        if (transform.TryGetComponent<Renderer>(out Renderer renderer))
        {
            RemoveRenderReference(renderer);
        }
    }

    public bool IsVisible()
    {
        return myRenderers.Count > 0 && myRenderers[0].enabled;
    }
}