using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StructureManager : UnitManager
{
    private Structure structure;
    private int collisionCount = 0;
    private float exclusiveRadius;
    //public Renderer myRenderer;
    [HideInInspector]
    public List<Renderer> myRenderers;

    public Vector3 rallyPoint { get; private set; }

    public override Unit Unit
    {
        get { return structure; }
        set { structure = value is Structure ? (Structure)value : null; }
    }

    public override void Initialize(Unit unit)
    {
        base.Initialize(unit);
        exclusiveRadius = unit.data.exclusiveRadius;
        if (!structure.isFixed)
        {
            behaviorTree.enabled = false;
            structure.Deactivate(false);
            GetComponent<NavMeshObstacle>().enabled = false;
            ShowHealthBar(false);
        }

        myRenderers = new List<Renderer>();
        Utility.CollectComponentsInChildren<Renderer>(myRenderers, transform, "FOV");
    }

    // add in rallyPoint drawing when selected?

    public void SetRallyPoint(Vector3 location)
    {
        rallyPoint = location;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Terrain") return;
        collisionCount++;
        CheckPlacement();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Terrain") return;
        collisionCount--;
        CheckPlacement();
    }

    public bool CheckPlacement()
    {
        if (structure == null) return false;

        if (structure.isFixed) return false;

        bool validPlacement = HasValidPlacement();

        if (!validPlacement)
        {
            structure.SetMaterials(StructurePlacement.Invalid);
        }
        else
        {
            structure.SetMaterials(StructurePlacement.Valid);
        }

        return validPlacement;
    }

    public bool HasValidPlacement()
    {
        if (collisionCount > 0) return false;

        // check if p outside visible range
        if (!myRenderers[0].enabled)
        {
            Debug.Log("Outside of visible range. CANNOT PLACE.");
            return false;
        }

        // check for placement near similar buildings if exclusion radius was specified
        if (exclusiveRadius > 0)
        {
            Collider[] nearbyUnits = Physics.OverlapSphere(transform.position, exclusiveRadius, Game.PLAYER_UNIT_MASK);

            foreach (Collider collider in nearbyUnits)
            {
                if (collider.transform.TryGetComponent<UnitManager>(out var um))
                {
                    Unit otherUnit = um.Unit;
                    if (otherUnit == Unit) continue;
                    if (otherUnit.code == Unit.code && otherUnit.owner == Game.Instance.humanPlayerID) return false;
                }
            }
        }

        // get 4 bottom corner positions
        Vector3 p = transform.position;

        // check if structure on flat ground
        if (transform.position.y > 0f && transform.position.y != Game.terrainMaxHeight) return false;

        Vector3 c = collider.center;
        Vector3 e = collider.size / 2f;
        //float bottomHeight = c.y - e.y + 0.5f;
        float bottomHeight = c.y - e.y + 0.5f;

        Vector3[] bottomCorners = new Vector3[]
        {
            new Vector3(c.x - e.x, bottomHeight, c.z - e.z),
            new Vector3(c.x - e.x, bottomHeight, c.z + e.z),
            new Vector3(c.x + e.x, bottomHeight, c.z - e.z),
            new Vector3(c.x + e.x, bottomHeight, c.z + e.z)
        };

        // cast a small ray beneath the corner to check for a close ground
        // (if at least two are not valid, then placement is invalid)
        int invalidCornersCount = 0;

        foreach (Vector3 corner in bottomCorners)
        {
            if (!Physics.Raycast(p + corner, Vector3.up * -1f, 2f, Game.TERRAIN_MASK))
            {
                invalidCornersCount++;
            }
        }

        return invalidCornersCount < 3;
    }

    public override void EnableUnitUI(bool friendly = true)
    {
        if (structure.placement != StructurePlacement.Fixed) return;

        base.EnableUnitUI(friendly);
    }

    public override void DisableUnitUI()
    {
        if (structure.placement != StructurePlacement.Fixed) return;

        friendlyUIRing.gameObject.SetActive(false);
        enemyUIRing.gameObject.SetActive(false);
        unitCanvas.gameObject.SetActive(false);
    }

}
