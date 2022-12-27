using System.Collections.Generic;

using BehaviorTree;

[UnityEngine.RequireComponent(typeof(CharacterManager))]
public class AICharacterBT : Tree
{
    CharacterManager manager;

    private void Awake()
    {
        manager = GetComponent<CharacterManager>();
    }

    // new test
    protected override Node SetupTree()
    {
        Node root;

        // prepare our subtrees...

        // prepare ability casting timer with reference
        castingTimer = new Timer(3f, new List<Node>
        {
            new TaskTriggerAbility(manager)
        });

        Sequence attackMoveResumeSequence = new Sequence(new List<Node> {
            new CheckHasAttackMove(),
            new Inverter(new List<Node>{
                new CheckReachedAttackMove(manager)
            }),
            new TaskResumeAttackMove()
        });

        Sequence attackMovePursueSequence = new Sequence(new List<Node> {
            new CheckHasAttackMove(),
            new Inverter(new List<Node> {
                new CheckHasTarget()
            }),
            new CheckEnemyInFOVRange(manager),
            new TaskStopMoving(manager)
        });

        Sequence abilityCastingSequence = new Sequence(new List<Node> {
            new CheckHasCastingAbility(),
            new Selector(new List<Node>
            {
                new Sequence(new List<Node>{
                    new CheckAbilityTargetIsValid(),
                    new CheckCastingRange(manager),
                    castingTimer
                }),
                //castingTimerParent,
                new Sequence(new List<Node>{
                    new CheckHasCastingAbility(),
                    new TaskMoveToCastingRange(manager)
                })
            })
        });

        Sequence moveToDestinationSequence = new Sequence(new List<Node> {
            new CheckHasDestination(),
            new TaskMoveToDestination(manager),
        });

        attackTimer = new Timer(
            manager.Unit.attackRate,
            new List<Node>()
            {
                new TaskAttack(manager)
            }
        );

        Sequence attackSequence = new Sequence(new List<Node> {
            new Inverter(new List<Node>
            {
                new CheckTargetIsMine(manager),
            }),
            new CheckEnemyInAttackRange(manager),
            attackTimer
        });

        Sequence moveToTargetSequence = new Sequence(new List<Node> {
            new CheckHasTarget(),
            new CheckTargetVisible(manager)
        });

        if (manager.Unit.data.attackDamage > 0)
        {
            moveToTargetSequence.Attach(new Selector(new List<Node> {
                attackSequence,
                //new TaskFollow(manager),
                new Selector(new List<Node>{
                    new CheckHasFollowDestination(manager),
                    new TaskSetFollowDestination(manager)
                })
            }));
        }
        else
        {
            //moveToTargetSequence.Attach(new TaskFollow(manager));
        }

        // build main branch with conditions
        Selector mainBranch = new Selector();
        if (manager.owner == Game.Instance.humanPlayerID)
        {
            mainBranch.Attach(abilityCastingSequence);
        }
        mainBranch.Attach(attackMovePursueSequence);
        mainBranch.Attach(moveToDestinationSequence);
        mainBranch.Attach(moveToTargetSequence);

        root = new Selector(new List<Node> {
                new Sequence(new List<Node> {
                    new CheckSleep(),
                    new Inverter(new List<Node> {
                        new CheckEnemyInFOVRange(manager)
                    })
                }),
                mainBranch,
                //new CheckEnemyInFOVRange(manager),
                attackMoveResumeSequence,
                new TaskSleep(manager)
            });

        return root;
    }

    public override void StartCastingAbility(AbilityManager abilityManager, TargetData tData = null)
    {
        base.StartCastingAbility(abilityManager, tData);
        castingTimer.SetTimer(abilityManager.ability.castTime);
    }

    public override void StopCasting()
    {
        base.StopCasting();
        castingTimer.SetTimer(3f);
    }
}