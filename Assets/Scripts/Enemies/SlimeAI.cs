    using System.Collections;
    using UnityEngine;

    public class SlimeAI : MonoBehaviour
    {
        private enum State
        {
            Roaming,
            Chasing
        }

        private State state;
        private SlimePathFinding slimePathFinding;
        private Transform playerTransform;
        private float spawnTime;

    [SerializeField] private float detectionRange = 1f; // reduced scan radius for testing
    [SerializeField] private float roamRadius = 2f; // maximum distance from current position when roaming
    [SerializeField] private float spawnGracePeriod = 2f; // seconds after spawn to force roaming

        private void Awake()
        {
            slimePathFinding = GetComponent<SlimePathFinding>();
            state = State.Roaming;
        }

        private void Start()
        {
            spawnTime = Time.time;
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("SlimeAI: No GameObject tagged 'Player' found. Slime will only roam.");
            }

            StartCoroutine(AIBehaviour());
        }

        //private IEnumerator CheckForPlayerRoutine()
        //{
        //    while (true)
        //    {
        //        if (playerTransform != null)
        //        {
        //            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        //            if (distanceToPlayer < detectionRange)
        //            {
        //                state = State.Chasing;
        //                slimePathFinding.MoveTo((playerTransform.position - transform.position).normalized);
        //            }
        //            else
        //            {
        //                if (state != State.Roaming)
        //                {
        //                    StartCoroutine(RoamingRoutine());
        //                }
        //            }
        //        }
        //        else
        //        {
        //            Debug.LogWarning("SlimeAI: playerTransform is still null. Waiting for player to appear.");
        //        }

        //        yield return new WaitForSeconds(0.2f);
        //    }
        //}

        private IEnumerator RoamingRoutine()
        {
            state = State.Roaming;
            while (state == State.Roaming)
            {
                Vector2 roamPosition = GetRoamingPosition();
                slimePathFinding.MoveTo(roamPosition);
                yield return new WaitForSeconds(2f);
            }
        }

        private Vector2 GetRoamingPosition()
        {
            // Return a world-space roaming target near the slime's current position.
            // Use insideUnitCircle so roaming positions are distributed within a radius.
            Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
            return (Vector2)transform.position + randomOffset;
        }

        private IEnumerator AIBehaviour()
        {
            while (true)
            {
                // During the grace period after spawn, force roaming so slimes don't immediately chase the player.
                if (Time.time - spawnTime < spawnGracePeriod)
                {
                    if (state != State.Roaming)
                        state = State.Roaming;
                    Vector2 roamPosition = GetRoamingPosition();
                    slimePathFinding.MoveTo(roamPosition);
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                if (playerTransform != null)
                {
                    float distance = Vector2.Distance(transform.position, playerTransform.position);
                    // If player is within detection, chase; otherwise roam.
                    if (distance < detectionRange)
                    {
                        state = State.Chasing;
                        slimePathFinding.MoveTo(playerTransform.position);
                    }
                    else
                    {
                        if (state != State.Roaming)
                        {
                            state = State.Roaming;
                        }
                        Vector2 roamPosition = GetRoamingPosition();
                        slimePathFinding.MoveTo(roamPosition);
                    }
                }
                else
                {
                    // no player found, just roam
                    if (state != State.Roaming)
                        state = State.Roaming;
                    Vector2 roamPosition = GetRoamingPosition();
                    slimePathFinding.MoveTo(roamPosition);
                }

                yield return new WaitForSeconds(0.2f);
            }
        }

    }
