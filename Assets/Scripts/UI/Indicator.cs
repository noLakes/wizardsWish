using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indicator : MonoBehaviour
{
    private float lifetime;
    private float elapsed = 0f;

    public void Initialize(float lifetime, Color color)
    {
        this.lifetime = lifetime;

        color = new Color(color.r, color.g, color.b, 0.8f);
        
        foreach (Transform child in transform)
        {
            MeshRenderer mesh = child.GetComponent<MeshRenderer>();
            Material[] materials = mesh.materials;
            materials[0].color = color;
            mesh.materials = materials;
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime) Destroy(gameObject);
    }

    private void OnIndicatorPlaced()
    {
        Destroy(gameObject);
    }

    private void OnEnable()
    {
        EventManager.AddListener("IndicatorPlaced", OnIndicatorPlaced);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("IndicatorPlaced", OnIndicatorPlaced);
    }

}
