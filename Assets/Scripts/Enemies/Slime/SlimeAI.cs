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

        [SerializeField] private float detectionRange = .1f; // roughly 50 pixels assuming 100 px/unit

        private void Awake()
        {
            slimePathFinding = GetComponent<SlimePathFinding>();
            state = State.Roaming;
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
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
            return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        private IEnumerator AIBehaviour()
        {
            while (true)
            {
                if (playerTransform != null)
                {
                    float distance = Vector2.Distance(transform.position, playerTransform.position);
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
                yield return new WaitForSeconds(0.2f);
            }
        }

    }
