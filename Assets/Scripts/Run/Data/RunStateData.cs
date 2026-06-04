using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class holds data that exists only for the duration of a SINGLE RUN.
/// It resets back to baseline when a new run begins.
/// </summary>
public class RunStateData
{
    // Inventory & Modifiers
    public List<SinData> currentSins = new List<SinData>();
    public List<RewardData> currentRewards = new List<RewardData>();

    // Run-Specific Combat State
    public float currentHP;
    public float currentMana;

    // Dungeon 
    public int currentFloor = 1;
    public string floorSeed;

    public void ResetRun(MetaProgressionData metaData)
    {
        currentSins.Clear();
        currentRewards.Clear();
        currentFloor = 1;
        floorSeed = System.Guid.NewGuid().ToString();

        // Restore HP and Mana to the baseline Meta values
        currentHP = metaData.baseMaxHP;
        currentMana = metaData.baseMaxMana;
    }
}
