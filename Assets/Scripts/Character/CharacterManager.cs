using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum CharacterState
{
    Idle,
    Moving,
    Chasing,
    Attacking,
    Casting,
}

public class CharacterManager : UnitManager
{
    private Character character;

    public override Unit Unit
    {
        get { return character; }
        set { character = value is Character ? (Character)value : null; }
    }

    // navigation
    public NavMeshAgent navMeshAgent { get; private set; }
    public NavMeshObstacle navMeshObstacle { get; private set; }
    private IEnumerator navAgentPreparationRoutine;
    private NavMeshPath path;
    public float pathRefreshRate;
    private float pathRefreshTimer;
    public bool moving
    {
        get => navMeshAgent.enabled &&
        navMeshAgent.hasPath ||
        navAgentPreparationRoutine != null;
    }

    public bool isIdle
    {
        get => !navMeshAgent.enabled &&
        navAgentPreparationRoutine == null;
    }
    private Vector3 agentVelocityOnPause = Vector3.zero;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshObstacle = GetComponent<NavMeshObstacle>();
        behaviorTree = GetComponent<CharacterBT>();

        ActAsNavAgent();

        path = new NavMeshPath();
        pathRefreshTimer = 0f;
        navAgentPreparationRoutine = null;
    }

    private void Start()
    {
        Unit.Place(false);
    }

    public override void Initialize(Unit unit)
    {
        base.Initialize(unit);
        character = (Character)unit;

        // disallow player character movement in enemy only areas such as spawn environments
        if (Unit.owner == Game.Instance.humanPlayerID)
        {
            int areaMask = navMeshAgent.areaMask;
            areaMask -= 1 << NavMesh.GetAreaFromName("EnemyOnly");
            navMeshAgent.areaMask = areaMask;
        }
        else
        {
            Game.Instance.aiManager.AddCharacter(this);
        }

        SetOutline();
        GenerateMapDot();
    }


    void Update()
    {
        if (navMeshAgent.hasPath) TickPathTimer();
    }

    private void TickPathTimer()
    {
        pathRefreshTimer += Time.deltaTime;
        if (pathRefreshTimer > pathRefreshRate)
        {
            pathRefreshTimer = 0;
            NavMesh.CalculatePath(transform.position, navMeshAgent.destination, NavMesh.AllAreas, path);
        }
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
        }
    }


    public bool TryMove(Vector3 location)
    {
        bool validPath;

        if (ValidPathTo(location))
        {
            validPath = true;
            SetDestination(location);
        }
        else
        {
            validPath = false;
            //Debug.Log("Invalid path for " + gameObject.name + " going from " + transform.position + " to " + location);
        }

        return validPath;
    }

    public void SetDestination(Vector3 position)
    {
        if (!navMeshAgent.enabled)
        {
            navAgentPreparationRoutine = UpdateDestinationCoroutine(position);
            StartCoroutine(navAgentPreparationRoutine);
        }
        else
        {
            navMeshAgent.CalculatePath(position, path);
            navMeshAgent.SetPath(path);
        }
    }

    IEnumerator UpdateDestinationCoroutine(Vector3 position)
    {
        if (!navMeshAgent.enabled)
        {
            navMeshObstacle.carving = false;
            navMeshObstacle.enabled = false;
        }
        yield return null;

        navMeshAgent.enabled = true;
        navMeshAgent.CalculatePath(position, path);
        navMeshAgent.SetPath(path);

        yield return null;

        navAgentPreparationRoutine = null;
    }

    public bool ValidPathTo(Vector3 location)
    {
        Vector3 pathStart = gameObject.transform.position;

        if (navMeshObstacle.enabled)
        {
            if (NavMesh.CalculatePath(pathStart + transform.forward, location, NavMesh.AllAreas, path)) return true;
            if (NavMesh.CalculatePath(pathStart + -transform.forward, location, NavMesh.AllAreas, path)) return true;
            if (NavMesh.CalculatePath(pathStart + transform.right, location, NavMesh.AllAreas, path)) return true;
            if (NavMesh.CalculatePath(pathStart + -transform.right, location, NavMesh.AllAreas, path)) return true;


            return false;
        }

        return NavMesh.CalculatePath(pathStart, location, navMeshAgent.areaMask, path);
    }

    public bool ValidPathTo(Transform target)
    {
        Vector3 location;

        if (target.TryGetComponent<NavMeshObstacle>(out NavMeshObstacle obstacle))
        {
            Debug.Log("pathingToTarget");
            location = Vector3.MoveTowards(target.position, transform.position, obstacle.radius + 0.1f);
        }
        else location = target.position;

        return ValidPathTo(location);
    }

    public void ActAsNavAgent()
    {
        navMeshObstacle.carving = false;
        navMeshObstacle.enabled = false;
        navMeshAgent.enabled = true;
        pathRefreshTimer = 0f;
        navMeshAgent.ResetPath();
    }

    public void ActAsNavObstacle()
    {
        StopMoving();
        navMeshAgent.enabled = false;
        navMeshObstacle.enabled = true;
        navMeshObstacle.carving = true;
        pathRefreshTimer = 0f;
    }

    public override void StopMoving()
    {
        if (navMeshAgent.isActiveAndEnabled) navMeshAgent.ResetPath();
        pathRefreshTimer = 0f;
        behaviorTree.ClearData("destinationPoint");
    }

    public void Stop()
    {
        StopMoving();
        pathRefreshTimer = 0f;
        behaviorTree.StopAttacking();
        behaviorTree.StopCasting();
    }

    public override void TryCastAbility(string abilityName, TargetData tData = null)
    {
        AbilityManager abilityManager = Unit.GetAbility(abilityName);
        if (abilityManager != null && abilityManager.ready)
        {
            //Debug.Log(transform.name + " started casting " + abilityName);
            behaviorTree.ClearData("attackMove");
            StopMoving();
            behaviorTree.StartCastingAbility(abilityManager, tData);
        }
        else
        {
            //Debug.Log(transform.name + " failed to start casting " + abilityName);
        }
    }

    protected override void Die()
    {
        if (owner != Game.Instance.humanPlayerID) Game.Instance.aiManager.RemoveCharacter(this);
        base.Die();
    }

    public void Wander()
    {
        Vector3 movePoint = Vector3.zero;
        Vector3 direction;
        float distance = 0f;
        bool valid = false;

        while (!valid)
        {
            distance = Random.Range((Unit.data.fieldOfView * 0.2f), Unit.data.fieldOfView);
            direction = Random.insideUnitCircle * distance;
            movePoint = transform.position + new Vector3(direction.x, 0f, direction.y);

            if (ValidPathTo(movePoint)) valid = true;
        }

        //Debug.Log("Wandering to: " + movePoint);

        behaviorTree.SetData("attackMove", (object)movePoint);
        behaviorTree.Wake();
        behaviorTree.SetDirty(true);
    }

    public void OnSleep()
    {
        ActAsNavObstacle();
    }

    public void OnWake()
    {
        // nothing yet
    }

    private void OnPauseGame()
    {
        if (navMeshAgent.enabled && !navMeshAgent.isStopped)
        {
            agentVelocityOnPause = navMeshAgent.velocity;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.isStopped = true;
        }
    }

    private void OnResumeGame()
    {
        // resume movement
        if (navMeshAgent.enabled && navMeshAgent.isStopped) navMeshAgent.isStopped = false;
        navMeshAgent.velocity = agentVelocityOnPause;
    }

    private void OnEnable()
    {
        EventManager.AddListener("PauseGame", OnPauseGame);
        EventManager.AddListener("ResumeGame", OnResumeGame);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("PauseGame", OnPauseGame);
        EventManager.RemoveListener("ResumeGame", OnResumeGame);
    }
}
