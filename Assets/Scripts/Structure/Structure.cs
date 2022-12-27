using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum StructurePlacement
{
    Fixed,
    Valid,
    Invalid
}

public class Structure : Unit
{
    public StructurePlacement placement;
    public bool isFixed { get => placement == StructurePlacement.Fixed; }
    //public List<Material> baseMaterials;
    public Dictionary<Transform, List<Material>> baseMaterialData;
    private StructureManager structureManager;
    public bool HasValidPlacement
    {
        get => structureManager.HasValidPlacement();
    }

    public Structure(StructureData initialData, int owner) : this(initialData, owner, new List<ResourceValue>() { }) { }
    public Structure(StructureData initialData, int owner, List<ResourceValue> production) : base(initialData, owner, production)
    {
        structureManager = transform.GetComponent<StructureManager>();

        // set up general structure ability managers
        generalAbilityManagers = new Dictionary<string, AbilityManager>();
        AbilityManager am; GameObject g = transform.gameObject;
        foreach (AbilityData ability in Game.GENERAL_STRUCTURE_ABILITY_DATA)
        {
            am = g.AddComponent<AbilityManager>();
            am.Initialize(ability, g);
            abilityManagers.Add(am.ability.code, am);
        }

        /*
        baseMaterials = new List<Material>();

        foreach (Material material in transform.Find("Mesh").GetComponent<Renderer>().materials)
        {
            baseMaterials.Add(new Material(material));
        }
        */

        baseMaterialData = new Dictionary<Transform, List<Material>>();
        CollectBaseMaterialData(transform);

        placement = StructurePlacement.Valid;
        SetMaterials();

        Game.Instance.UNITS.Add(this);
    }

    public int DataIndex
    {
        get
        {
            for (int i = 0; i < Game.STRUCTURE_DATA.Length; i++)
            {
                if (Game.STRUCTURE_DATA[i].code == code)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    private void CollectBaseMaterialData(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if(child.name == "FOV")
            {
                continue;
            }

            if (child.TryGetComponent<Renderer>(out Renderer renderer))
            {
                baseMaterialData.Add(child, new List<Material>(renderer.materials));
            }

            if (child.childCount > 0)
            {
                CollectBaseMaterialData(child);
            }
        }
    }

    private void ApplyMaterialsToAllRenderers(List<Material> mats)
    {
        foreach (Transform t in baseMaterialData.Keys)
        {
            t.GetComponent<Renderer>().materials = mats.ToArray();
        }
    }

    private void RevertAllBaseMaterials()
    {
        foreach (Transform t in baseMaterialData.Keys)
        {
            t.GetComponent<Renderer>().materials = baseMaterialData[t].ToArray();
        }
    }

    public void SetMaterials() { SetMaterials(placement); }

    public void SetMaterials(StructurePlacement placement)
    {
        List<Material> newMaterials;

        if (placement == StructurePlacement.Valid)
        {
            Material refMaterial = Resources.Load("Materials/Valid") as Material;
            newMaterials = new List<Material>();
            newMaterials.Add(refMaterial);
        }
        else if (placement == StructurePlacement.Invalid)
        {
            Material refMaterial = Resources.Load("Materials/Invalid") as Material;
            newMaterials = new List<Material>();
            newMaterials.Add(refMaterial);
        }
        else if (placement == StructurePlacement.Fixed)
        {
            RevertAllBaseMaterials();
            structureManager.SetOutline();
            return;
        }
        else return;

        ApplyMaterialsToAllRenderers(newMaterials);
    }

    public override void Place(bool pay = true)
    {
        base.Place(pay);

        // set placement state
        placement = StructurePlacement.Fixed;

        Activate();

        // set initial waypoint in front of structure
        structureManager.SetRallyPoint(transform.position + (transform.forward * 2));

        //structureManager.ShowHealthBar(true);

        structureManager.GetComponent<NavMeshObstacle>().enabled = true;

        // reactivate behavior tree
        structureManager.behaviorTree.enabled = true;

        SetMaterials();
        structureManager.GenerateMapDot();
    }

    public void CheckValidPlacement()
    {
        if (placement == StructurePlacement.Fixed) return;

        placement = structureManager.CheckPlacement() ? StructurePlacement.Valid : StructurePlacement.Invalid;
    }
}
