using System;
using UnityEngine;

public class BossEncounterController : MonoBehaviour
{
    public static BossEncounterController Instance { get; private set; }

    public event Action BossCleared;

    private EnemyDeathNotifier registeredBossNotifier;
    private bool encounterActive;
    private bool bossCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDisable()
    {
        ResetEncounter();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterBoss(EnemyDeathNotifier notifier)
    {
        ResetEncounter();

        if (notifier == null)
        {
            return;
        }

        registeredBossNotifier = notifier;
        encounterActive = true;
        registeredBossNotifier.Died += OnRegisteredBossDied;
    }

    public void ResetEncounter()
    {
        if (registeredBossNotifier != null)
        {
            registeredBossNotifier.Died -= OnRegisteredBossDied;
            registeredBossNotifier = null;
        }

        encounterActive = false;
        bossCleared = false;
    }

    private void OnRegisteredBossDied(EnemyDeathNotifier notifier)
    {
        if (!encounterActive || bossCleared || notifier != registeredBossNotifier)
        {
            return;
        }

        bossCleared = true;
        encounterActive = false;

        if (registeredBossNotifier != null)
        {
            registeredBossNotifier.Died -= OnRegisteredBossDied;
            registeredBossNotifier = null;
        }

        BossCleared?.Invoke();
    }
}