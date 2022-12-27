using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public abstract class Tree : MonoBehaviour
    {
        protected Node root = null;
        protected Timer attackTimer = null;
        protected Timer castingTimer = null;
        private float tickTimer;
        public float tickRate;

        public bool awake { get => root.awake; }

        protected void Start()
        {
            root = SetupTree();
            root.PassRootReferenceDown(root);
            tickTimer = 0f;

            // add random jitter to prevent all units instantiated at same time from ticking at same time
            tickRate += Random.Range(-0.05f, 0.05f);
        }

        private void Update()
        {
            if (!Game.Instance.gameIsPaused)
            {
                if (root.dirty)
                {
                    Debug.DrawLine(transform.position, transform.position + (Vector3.up * 5f + Vector3.forward * 5f), Color.green, 2f);
                    RunTree();
                }
                else
                {
                    tickTimer += Time.deltaTime;
                    if (tickTimer >= tickRate)
                    {
                        RunTree();
                        tickTimer = 0f;
                    }
                }
            }
        }

        protected void RunTree()
        {
            root.SetDirty(false);
            root.Evaluate();
        }

        protected abstract Node SetupTree();

        public void SetData(string key, object value)
        {
            root.SetData(key, value);
            Wake();
        }

        public void SetDataNextFrame(string key, object value)
        {
            StartCoroutine(NextFramePushDataRoutine(key, value));
        }

        private IEnumerator NextFramePushDataRoutine(string key, object value)
        {
            yield return null;
            SetData(key, value);
            Wake();
        }

        public object GetData(string key)
        {
            return root.GetData(key);
        }

        public bool ClearData(string key)
        {
            return root.ClearData(key);
        }

        public void ClearAllData()
        {
            root.ClearAllData();
        }

        public virtual void StartCastingAbility(AbilityManager abilityManager, TargetData tData = null)
        {
            Wake();
            StopCasting();

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
            SetDirty(true);
        }

        public virtual void StopCasting()
        {
            if (GetData("castingAbility") == null) return;

            AbilityManager abilityManager = (AbilityManager)GetData("castingAbility");
            if (abilityManager.cost.Count > 0)
            {
                // refund
                foreach (ResourceValue value in abilityManager.cost)
                {
                    Game.GAME_RESOURCES[value.code].AddAmount(value.amount);
                }
                EventManager.TriggerEvent("ResourcesChanged");
            }

            ClearData("castingAbility");
            ClearData("castingTargetData");
        }

        public virtual void StopAttacking()
        {
            ClearData("currentTarget");
        }

        public void SetAttackRate(float rate)
        {
            if (attackTimer != null)
            {
                attackTimer.SetTimer(rate);
            }
        }

        public void RunTreeNow()
        {
            tickTimer = 0f;
            RunTree();
        }

        public void Wake() => root.Wake();
        public void Sleep() => root.Sleep();
        public void SetDirty(bool value) => root.SetDirty(value);
    }
}
