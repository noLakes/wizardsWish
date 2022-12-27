using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DataHandler
{
    public static void LoadGameData()
    {
        Game.STRUCTURE_DATA = Resources.LoadAll<StructureData>("Scriptable Objects/Units/Structures");
        Game.CHARACTER_DATA = Resources.LoadAll<CharacterData>("Scriptable Objects/Units/Characters");
        Game.GENERAL_CHARACTER_ABILITY_DATA = Resources.LoadAll<AbilityData>("Scriptable Objects/Abilities/GeneralCharacter");
        Game.GENERAL_STRUCTURE_ABILITY_DATA = Resources.LoadAll<AbilityData>("Scriptable Objects/Abilities/GeneralStructure");
        Game.SPECIAL_ABILITY_DATA = Resources.LoadAll<AbilityData>("Scriptable Objects/Abilities/Special");

        GameParameters[] gameParametersList = Resources.LoadAll<GameParameters>("Scriptable Objects/Parameters");
        foreach (GameParameters parameters in gameParametersList)
            parameters.LoadFromFile();
    }

    public static void SaveGameData()
    {
        // save game parameters
        GameParameters[] gameParametersList = Resources.LoadAll<GameParameters>("Scriptable Objects/Parameters");
        foreach (GameParameters parameters in gameParametersList)
            parameters.SaveToFile();
    }

    public static CharacterData LoadCharacter(string name)
    {
        return Resources.Load<CharacterData>($"Scriptable Objects/Units/Characters/{name}");
    }

    public static StructureData LoadStructure(string name)
    {
        return Resources.Load<StructureData>($"Scriptable Objects/Units/Structures/{name}");
    }
}

