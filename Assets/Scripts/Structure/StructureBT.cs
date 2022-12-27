using System.Collections.Generic;
using BehaviorTree;
using UnityEngine;

[UnityEngine.RequireComponent(typeof(StructureManager))]
public class StructureBT : BehaviorTree.Tree
{
    StructureManager manager;
    public Timer buildTimer = null;

    private void Awake()
    {
        manager = GetComponent<StructureManager>();
    }

    protected override Node SetupTree()
    {
        Node root;

        root = new Parallel();

        // prepare build timer
        buildTimer = new Timer(0f, new List<Node>
        {
            new TaskProduceBuildOrder(manager)
        });

        // prepare ability casting timer with reference
        castingTimer = new Timer(0f, new List<Node>
        {
            new TaskTriggerAbility(manager)
        });

        Sequence buildOrderSequenece = new Sequence(new List<Node> {
            new CheckHasBuildOrder(manager, buildTimer),
            buildTimer
        });

        Sequence abilityCastingSequence = new Sequence(new List<Node> {
            new CheckHasCastingAbility(),
            new CheckAbilityTargetIsValid(),
            new CheckCastingRange(manager),
            castingTimer
        });

        if (manager.owner == Game.Instance.humanPlayerID) root.Attach(buildOrderSequenece);

        root.Attach(abilityCastingSequence);

        if (manager.Unit.data.attackDamage > 0)
        {
            attackTimer = new Timer(
                manager.Unit.attackRate,
                new List<Node>()
                {
                    new TaskAttack(manager)
                }
            );

            Sequence attackSequence = new Sequence(new List<Node> {
                new CheckEnemyInAttackRange(manager),
                attackTimer
            });

            root.Attach(attackSequence);
            root.Attach(new CheckEnemyInFOVRange(manager));
        }

        root.SetData("buildQueue", new List<AbilityManager>());

        return root;
    }

    public override void StartCastingAbility(AbilityManager abilityManager, TargetData tData = null)
    {
        if (abilityManager.ability.type == AbilityType.INSTANTIATE_CHARACTER)
        {
            List<AbilityManager> buildQueue = (List<AbilityManager>)root.GetData("buildQueue");

            if (buildQueue.Count < 5)
            {
                buildQueue.Add(abilityManager);

                if (abilityManager.cost.Count > 0)
                {
                    // pay
                    foreach (ResourceValue value in abilityManager.cost)
                    {
                        Game.GAME_RESOURCES[value.code].AddAmount(-value.amount);
                    }
                    EventManager.TriggerEvent("ResourcesChanged");
                }

                EventManager.TriggerEvent("BuildQueueUpdated", buildQueue);
            }
            else
            {
                Debug.Log("BUILD QUEUE FULL");
            }
        }
        else
        {
            StopCasting();

            castingTimer.SetTimer(abilityManager.ability.castTime);

            if (abilityManager.cost.Count > 0)
            {
                // pay
                foreach (ResourceValue value in abilityManager.cost)
                {
                    Game.GAME_RESOURCES[value.code].AddAmount(-value.amount);
                }
                EventManager.TriggerEvent("ResourcesChanged");
            }

            SetData("castingAbility", abilityManager);
            SetData("castingTargetData", tData);
        }


        /*
        base.StartCastingAbility(abilityManager, tData);

        if (abilityManager.ability.type == AbilityType.INSTANTIATE_CHARACTER)
        {
            castingTimer.SetTimer(abilityManager.ability.castTime, manager.UpdateProgressBar);
        }
        else
        {
            castingTimer.SetTimer(abilityManager.ability.castTime);
        }
        */
    }

    public override void StopCasting()
    {
        AbilityManager currentCasting = (AbilityManager)root.GetData("castingAbility");
        if (currentCasting != null && currentCasting.ability.type == AbilityType.INSTANTIATE_CHARACTER)
        {
            manager.ToggleProgressBar(false);
        }

        base.StopCasting();
        castingTimer.SetTimer(3f);
    }

    public void StopBuilding(int index)
    {
        List<AbilityManager> buildQueue = (List<AbilityManager>)root.GetData("buildQueue");

        AbilityManager buildItemAbilityManager = buildQueue[index];

        // refund
        if (buildItemAbilityManager.cost.Count > 0)
        {
            // refund
            foreach (ResourceValue value in buildItemAbilityManager.cost)
            {
                Game.GAME_RESOURCES[value.code].AddAmount(value.amount);
            }
            EventManager.TriggerEvent("ResourcesChanged");
        }

        buildQueue.RemoveAt(index);

        if (index == 0)
        {
            root.ClearData("activeBuildOrder");
        }

        manager.progressBarSlider.value = 0f;
        manager.ToggleProgressBar(false);
    }
}