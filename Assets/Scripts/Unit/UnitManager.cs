using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BehaviorTree;
using UnityEngine.AI;

public class UnitManager : MonoBehaviour
{
    protected new BoxCollider collider;
    public virtual Unit Unit { get; set; }
    public HealthBar healthbar;
    public bool isSelected { get; protected set; }
    public GameObject fov;
    public int owner => Unit.owner;
    public float targetSize;

    // cached UI components
    public Transform friendlyUIRing;
    public Transform enemyUIRing;
    public Transform unitCanvas;
    public Transform progressBar;
    public Slider progressBarSlider;

    public List<Transform> ownerColoredItems;

    // attack effects
    private List<Affect> attackAffects;

    // behavior
    [HideInInspector]
    public BehaviorTree.Tree behaviorTree;

    private IEnumerator outlineRoutine;

    // used to show units FOV sphere for debug
    // OnDrawGizmos must also be un-commented
    //private bool drawFov = false;

    public virtual void Initialize(Unit unit)
    {
        collider = GetComponent<BoxCollider>();
        Unit = unit;
        unit.Activate();
        healthbar?.SetMaxHealth(Unit.maxHealth);
        healthbar?.SetHealth(Unit.health);
        SetOwnerColor();
        progressBarSlider = progressBar.GetComponent<Slider>();
        behaviorTree = GetComponent<BehaviorTree.Tree>();

        if (TryGetComponent<FogRendererToggler>(out FogRendererToggler fogToggler))
        {
            fogToggler.enabled = Game.Instance.gameGlobalParameters.enableFOV;
        }

        attackAffects = Affect.ParseAffect(unit.data.attackOnHitAffects);
        if (unit.attackDamage > 0)
        {
            attackAffects.Insert(0, new Dmg(unit.attackDamage));
        }

        if (transform.TryGetComponent<NavMeshObstacle>(out NavMeshObstacle obstacle))
        {
            Vector3 o = obstacle.size;
            targetSize = Mathf.Max(o.x, o.z) + 0.1f;
        }
        else
        {
            Vector3 s = transform.GetComponent<BoxCollider>().size;
            targetSize = Mathf.Max(s.x, s.z);
        }


        DisableUnitUI();
    }

    public void SetOwnerColor()
    {
        Color playerColor = Game.Instance.gamePlayersParameters.players[owner].color;

        foreach (Transform child in ownerColoredItems)
        {
            MeshRenderer mesh = child.GetComponent<MeshRenderer>();
            Material[] materials = mesh.materials;
            materials[0].color = playerColor;
            if (materials.Length > 1) materials[1].color = playerColor;
            mesh.materials = materials;
        }
    }

    public void SetOutline()
    {
        Color c;
        if (owner == Game.Instance.humanPlayerID)
        {
            c = Game.Instance.playerSilhouetteColor;
        }
        else
        {
            c = Game.Instance.enemySilhouetteColor;
        }

        Outline outline = transform.GetComponentInChildren<Outline>();
        if (outline == null) outline = GetComponent<Outline>();
        if (outline != null)
        {
            outlineRoutine = SetOutlineRoutine(outline, c);
            StartCoroutine(outlineRoutine);
        }
    }

    public void GenerateMapDot()
    {
        Color playerColor = Game.Instance.gamePlayersParameters.players[owner].color;
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dot.transform.parent = transform;
        Vector3 scaleChange = new Vector3(10f, -0.95f, 10f);
        dot.transform.localScale += scaleChange;
        dot.transform.rotation.Set(-90f, 0f, 0f, 0f);
        dot.transform.position = dot.transform.parent.position + new Vector3(0f, 30f, 0f);
        dot.layer = 10;
        MeshRenderer mesh = dot.transform.GetComponent<MeshRenderer>();
        mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Material[] materials = mesh.materials;
        materials[0].color = new Color(playerColor.r, playerColor.g, playerColor.b, 0.7f);
        mesh.materials = materials;

        if (TryGetComponent<FogRendererToggler>(out FogRendererToggler myFogToggler))
        {
            myFogToggler.AddRendererReference(dot.GetComponent<Renderer>());
        }
    }

    private IEnumerator SetOutlineRoutine(Outline outline, Color color)
    {
        outline.enabled = true;
        outline.OutlineColor = color;
        outline.OutlineMode = Outline.Mode.SilhouetteOnly;
        yield return null;

        outline.enabled = false;
        yield return null;

        outline.enabled = true;
        yield return null;

        outlineRoutine = null;
    }

    public virtual void EnableUnitUI(bool friendly = true)
    {
        if (friendly) friendlyUIRing.gameObject.SetActive(true);
        else enemyUIRing.gameObject.SetActive(true);

        unitCanvas.gameObject.SetActive(true);
        ShowHealthBar(true);
    }

    public virtual void DisableUnitUI()
    {
        friendlyUIRing.gameObject.SetActive(false);
        enemyUIRing.gameObject.SetActive(false);
        unitCanvas.gameObject.SetActive(false);
        ShowHealthBar(false);
    }

    public void Select()
    {
        Debug.Log("CurrentTarget: " + behaviorTree.GetData("currentTarget"));
        Debug.Log("DestinationPoint: " + behaviorTree.GetData("destinationPoint"));

        EnableUnitUI(IsPlayerOwned());
        isSelected = true;
        EventManager.TriggerEvent("SelectUnit", Unit);
    }

    public void Deselect()
    {
        DisableUnitUI();
        isSelected = false;
        EventManager.TriggerEvent("DeselectUnit", Unit);
    }

    public void Attack(Transform target)
    {
        UnitManager um = target.GetComponent<UnitManager>();
        if (um == null) return;

        if (Unit.data.attackProjectile == null)
        {
            Affect.Apply(um, attackAffects);
        }
        else
        {
            Vector3 direction = target.transform.position - transform.position;

            Projectile.Spawn(
            Unit.data.attackProjectile,
            // maybe update to center of target?
            transform.position + direction.normalized + (Vector3.up * (targetSize * 0.75f)),
            transform.rotation,
            attackAffects,
            target.transform
            );
        }
        Unit.AddXp(1);
        if (isSelected)
            EventManager.TriggerEvent("UpdateSelectedUnitPanel");
    }

    public void Damage(int damage)
    {
        if (!Unit.isActive) return;
        Unit.health -= damage;
        healthbar?.SetHealth(Unit.health);
        if (isSelected)
            EventManager.TriggerEvent("UpdateSelectedUnitPanel");
        if (Unit.health <= 0) Die();
    }

    public void Heal(int healAmount)
    {
        Unit.health = Mathf.Min(Unit.health + healAmount, Unit.maxHealth);
        healthbar?.SetHealth(Unit.health);
        if (Unit.health <= 0)
            Die();
        else if (isSelected)
            EventManager.TriggerEvent("UpdateSelectedUnitPanel");
    }

    protected virtual void Die()
    {
        if (isSelected)
        {
            Deselect();
            Game.SELECTED_UNITS.Remove(this);
        }

        if (owner != Game.Instance.humanPlayerID) RollPickupDropChance();
        Unit.Deactivate();
        Destroy(gameObject);
    }

    public bool CanSee(Transform target)
    {
        return Vector3.Distance(transform.position, target.position) <= Unit.data.fieldOfView + 1f;
    }

    public void ChangeMaxHealth(int amount, bool adjustCurrentHealth = true)
    {
        Unit.maxHealth += amount;
        healthbar?.SetMaxHealth(Unit.maxHealth);
        if (adjustCurrentHealth)
        {
            if (Unit.health + amount <= 0)
            {
                Unit.health = 1;
            }
            else
            {
                Unit.health += amount;
            }
        }

        healthbar?.SetHealth(Unit.health);

        if (isSelected) EventManager.TriggerEvent("UpdateSelectedUnitPanel");
    }

    public void ChangeAttack(int amount)
    {
        Unit.attackDamage += amount;
        if (Unit.attackDamage < 0) Unit.attackDamage = 0;
        attackAffects[0] = new Dmg(Unit.attackDamage);
        if (isSelected) EventManager.TriggerEvent("UpdateSelectedUnitPanel");
    }

    public void ChangeAttackRate(float amount)
    {
        Unit.attackRate += amount;
        behaviorTree.SetAttackRate(Unit.attackRate);
    }

    private void OnMouseEnter()
    {
        if (!isSelected)
        {
            if (Unit.owner == Game.Instance.humanPlayerID) EnableUnitUI();
            else EnableUnitUI(false);
        }
    }

    private void OnMouseExit()
    {
        if (!isSelected) DisableUnitUI();
    }

    public void ShowHealthBar(bool show)
    {
        healthbar?.transform.gameObject.SetActive(show);
    }

    public bool IsFriendly(Unit other) => other.owner == Unit.owner;
    public bool IsFriendly(UnitManager other) => IsFriendly(other.Unit);
    public bool IsFriendly(GameObject other)
    {
        if (other.TryGetComponent<UnitManager>(out UnitManager m))
        {
            return IsFriendly(m);
        }
        else return false;
    }

    public void ToggleFOV(bool toggle)
    {
        fov.SetActive(toggle);
    }

    public void ToggleProgressBar(bool toggle)
    {
        progressBar.gameObject.SetActive(toggle);
    }

    public void UpdateProgressBar(float taskTime, float timeRemaining)
    {
        if (timeRemaining <= 0f)
        {
            ToggleProgressBar(false);
            return;
        }

        ToggleProgressBar(true);
        progressBarSlider.maxValue = taskTime;
        progressBarSlider.value = taskTime - timeRemaining;
    }

    public virtual void TryCastAbility(string abilityCode, TargetData tData = null)
    {
        AbilityManager abilityManager = Unit.GetAbility(abilityCode);
        if (abilityManager != null && abilityManager.ready && abilityManager.CanBuy)
        {
            behaviorTree.StartCastingAbility(abilityManager, tData);
        }
        else
        {
            Debug.Log(transform.name + " failed to start casting " + abilityCode);
        }
    }

    private void RollPickupDropChance()
    {
        float chance = 0.05f;

        if (Unit.maxHealth > 20) chance += 0.05f;
        if (Unit.attackDamage > 5) chance += 0.05f;

        if (Random.Range(0f, 1f) <= chance)
        {
            InGameResource r = Random.Range(0, 2) == 0 ? InGameResource.Gold : InGameResource.Mana;
            Pickup.Spawn(transform.position, r, Random.Range(3, 16));
        }
    }

    public bool IsPlayerOwned() => owner == Game.Instance.humanPlayerID;

    public virtual void StopMoving()
    {
        // do nothing at base level
    }

    private void OnProduceResources()
    {
        if (Unit.isActive && owner == Game.Instance.humanPlayerID)
        {
            Unit.ProduceResources();
        }
    }

    private void OnEnable()
    {
        EventManager.AddListener("ProduceResources", OnProduceResources);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("ProduceResources", OnProduceResources);
    }

    /*
    // used to show units FOV sphere for debug
    void OnDrawGizmos()
    {
        if(!drawFov) return;
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Unit.data.fieldOfView);
    }
    */
}
