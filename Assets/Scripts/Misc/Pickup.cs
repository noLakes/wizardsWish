using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    public List<ResourceValue> values;
    float rotateSpeed = 100f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<UnitManager>(out UnitManager uManager))
        {
            if (uManager.owner != Game.Instance.humanPlayerID) return;

            foreach (ResourceValue r in values)
            {
                Game.GAME_RESOURCES[r.code].AddAmount(r.amount);
            }

            EventManager.TriggerEvent("ResourcesChanged");

            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * Time.deltaTime * rotateSpeed, Space.World);
    }

    public static void Spawn(Vector3 location, InGameResource resource, int value)
    {
        GameObject prefab;

        switch (resource)
        {
            case InGameResource.Gold:
                prefab = Resources.Load<GameObject>("Prefabs/Pickups/Resources/gold");
                break;
            case InGameResource.Mana:
                prefab = Resources.Load<GameObject>("Prefabs/Pickups/Resources/mana");
                break;
            default:
                prefab = null;
                break;
        }

        if (prefab != null)
        {
            GameObject pickup = GameObject.Instantiate(prefab, location, Quaternion.identity);
            pickup.GetComponent<Pickup>().values.Add(new ResourceValue(resource, value));
        }
    }
}
