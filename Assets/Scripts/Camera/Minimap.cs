using UnityEngine.EventSystems;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class Minimap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Vector2 terrainSize;
    private Vector2 halfTerrainSize;

    private Vector2 lastViewportClick;
    private bool dragging = false;

    public Camera miniMapCamera; //Camera that renders to the texture
    private RectTransform textureRectTransform; //RawImage RectTransform that shows the RenderTexture on the UI
    LayerMask flatTerrainLayer;

    PointerEventData clickData;

    private void Start()
    {
        flatTerrainLayer = LayerMask.GetMask("FlatTerrain");
        lastViewportClick = Input.mousePosition;
        halfTerrainSize = new Vector2(terrainSize.x / 2, terrainSize.y / 2);

        textureRectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (!dragging) return;

        //get the point of the RawImage where I click
        RectTransformUtility.ScreenPointToLocalPointInRectangle(textureRectTransform, clickData.position, null, out Vector2 localClick);
        localClick.x = (textureRectTransform.rect.xMin * -1) - (localClick.x * -1);
        localClick.y = (textureRectTransform.rect.yMin * -1) - (localClick.y * -1);

        //normalize the click coordinates so I get the viewport point to cast a Ray
        Vector2 viewportClick = new Vector2(localClick.x / textureRectTransform.rect.size.x, localClick.y / textureRectTransform.rect.size.y);

        Vector2 delta = viewportClick - lastViewportClick;
        lastViewportClick = viewportClick;

        if (delta.magnitude > Mathf.Epsilon)
        {
            //cast the ray from the camera which rends the texture
            Ray ray = miniMapCamera.ViewportPointToRay(new Vector3(viewportClick.x, viewportClick.y, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, flatTerrainLayer))
            {
                EventManager.TriggerEvent("MoveCamera", hit.point);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragging = true;

        clickData = eventData;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
    }

    private Vector3 ClampToTerrain(Vector3 pos)
    {
        if (pos.x > halfTerrainSize.x) pos.x = halfTerrainSize.x - 0.5f;
        else if (pos.x < -halfTerrainSize.x) pos.x = -halfTerrainSize.x + 0.5f;

        if (pos.y > halfTerrainSize.y) pos.y = halfTerrainSize.y - 0.5f;
        else if (pos.y < -halfTerrainSize.y) pos.y = -halfTerrainSize.y + 0.5f;

        return pos;
    }
}