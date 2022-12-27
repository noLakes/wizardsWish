using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaTargetingPreview : MonoBehaviour
{
    private Ray ray;
    private RaycastHit raycastHit;

    public void Initialize(float targetingRadius, Color color)
    {
        SetRadius(targetingRadius);
        SetColor(color);
    }

    public void SetRadius(float targetingRadius)
    {
        transform.localScale = new Vector3(targetingRadius * 2, transform.localScale.y, targetingRadius * 2);
    }

    public void SetColor(Color color)
    {
        MeshRenderer mesh = GetComponent<MeshRenderer>();
        Material[] materials = mesh.materials;
        materials[0].color = color;
        mesh.materials = materials;
    }

    // Update is called once per frame
    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out raycastHit, 1000f, Game.TERRAIN_MASK))
        {
            transform.position = raycastHit.point;
        }
    }

    private void OnTargetDataSent(object data)
    {
        Debug.Log("destroying target area preview");
        Destroy(gameObject);
    }

    private void OnCancelTargeting()
    {
        Destroy(gameObject);
    }

    private void OnEnable()
    {
        EventManager.AddListener("TargetDataSent", OnTargetDataSent);
        EventManager.AddListener("CancelTargeting", OnCancelTargeting);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("TargetDataSent", OnTargetDataSent);
        EventManager.RemoveListener("CancelTargeting", OnCancelTargeting);
    }
}
