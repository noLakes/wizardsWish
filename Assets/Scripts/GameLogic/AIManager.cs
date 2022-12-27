using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public List<CharacterManager> characters { get; private set; }
    public float wanderChance;
    int readIndex;
    
    public void Initialize()
    {
        characters = new List<CharacterManager>();
        readIndex = 0;
        wanderChance = Game.Instance.gameGlobalParameters.enemyWanderChance;
    }

    void Update()
    {
        // handles wandering one character per frame
        if(readIndex >= characters.Count) readIndex = 0;

        if(characters.Count == 0 ) return;

        CharacterManager c = characters[readIndex];
        if(!c.behaviorTree.awake) RollWanderChance(c);
        readIndex++;
    }

    private void RollWanderChance(CharacterManager c)
    {
        float roll = Random.value;
        if(roll <= wanderChance)
        {
            c.Wander();
        }
    }

    public void AddCharacter(CharacterManager c)
    {
        if(!characters.Contains(c)) characters.Add(c);
    }

    public void RemoveCharacter(CharacterManager c)
    {
        if(characters.Contains(c)) characters.Remove(c);
    }
}
