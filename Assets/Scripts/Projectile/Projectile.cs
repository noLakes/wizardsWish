using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 8.5f; // Speed of projectile.
    public float radius = 1f; // Collision radius.
    float radiusSq; // Radius squared; optimisation.
    Transform target; // Who we are homing at.
    Vector3? pointTarget = null;
    float aoeRadius;
    public List<Affect> onHitAffects;
    private AbilityData sourceAbility = null;
    private UnitManager caster = null;

    Vector3 currentPosition; // Store the current position we are at.
    float distanceTravelled; // Record the distance travelled.

    public float arcFactor = 0.5f; // Higher number means bigger arc.
    Vector3 origin; // To store where the projectile first spawned.

    void OnEnable()
    {
        // Pre-compute the value. 
        radiusSq = radius * radius;
        origin = currentPosition = transform.position;
    }

    void Update()
    {
        if (Game.Instance.gameIsPaused) return;
        
        // If there is no target, destroy itself and end execution.
        if (!target && pointTarget == null)
        {
            Destroy(gameObject);
            // Write your own code to spawn an explosion / splat effect.
            return; // Stops executing this function.
        }

        Vector3 targetLocation;
        if(target != null)
        {
            targetLocation = target.position;
            targetLocation += new Vector3(0f, target.transform.localScale.y / 2, 0f);
        }
        else
        {
            targetLocation = (Vector3)pointTarget;
        }

        // Move ourselves towards the target at every frame.
        Vector3 direction = targetLocation - currentPosition;
        currentPosition += direction.normalized * speed * Time.deltaTime;
        distanceTravelled += speed * Time.deltaTime; // Record the distance we are travelling.

        // Set our position to <currentPosition>, and add a height offset to it.
        float totalDistance = Vector3.Distance(origin, targetLocation);
        float heightOffset = arcFactor * totalDistance * Mathf.Sin(distanceTravelled * Mathf.PI / totalDistance);
        transform.position = currentPosition + new Vector3(0, heightOffset, 0);

        transform.LookAt(transform.position + (targetLocation - transform.position).normalized);
        //transform.rotation = Quaternion.LookRotation(currentPosition + new Vector3(0, heightOffset, 0));

        // Destroy the projectile if it is close to the target.
        if (direction.sqrMagnitude < radiusSq)
        {
            OnHit();
            Destroy(gameObject);
            // Write your own code to spawn an explosion / splat effect.
        }
    }

    private void OnHit()
    {
        if(target != null && target.TryGetComponent<UnitManager>(out UnitManager tUnit))
        {
            Affect.Apply(tUnit, onHitAffects);
        }
        else if (pointTarget != null && sourceAbility != null && caster != null)
        {
            Affect.Apply((Vector3)pointTarget, aoeRadius, onHitAffects, sourceAbility, caster);
        }
    }

    // So that other scripts can use Projectile.Spawn to spawn a projectile.
    public static Projectile Spawn(GameObject prefab, Vector3 position, Quaternion rotation, List<Affect> onHitAffects, Transform target = null, Vector3? pointTarget = null, float aoeRadius = 0f)
    {
        // Spawn a GameObject based on a prefab, and returns its Projectile component.
        GameObject go = Instantiate(prefab, position, rotation);
        Projectile p = go.GetComponent<Projectile>();

        // Rightfully, we should throw an error here instead of fixing the error for the user. 
        if (!p) p = go.AddComponent<Projectile>();

        // Set the projectile's target, so that it can work.
        p.onHitAffects = onHitAffects;
        p.target = target;
        p.pointTarget = pointTarget;
        p.aoeRadius = aoeRadius;
        
        return p;
    }

    public static Projectile Spawn(AbilityData ability, UnitManager caster, Quaternion rotation, Transform target = null, Vector3? pointTarget = null)
    {
        // Spawn a GameObject based on a prefab, and returns its Projectile component.
        GameObject go = Instantiate(ability.projectilePrefab, caster.transform.position, rotation);
        Projectile p = go.GetComponent<Projectile>();

        // Rightfully, we should throw an error here instead of fixing the error for the user. 
        if (!p) p = go.AddComponent<Projectile>();

        // Set the projectile's target, so that it can work.
        p.onHitAffects = Affect.ParseAffect(ability.projectileAffects);
        p.caster = caster;
        p.target = target;
        p.pointTarget = pointTarget;
        p.aoeRadius = ability.targetingRadius;
        p.sourceAbility = ability;
        
        return p;
    }
}