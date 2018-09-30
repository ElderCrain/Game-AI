using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour {

    
    public float timeToSpotPlayer = .5f;

    Transform target;
    NavMeshAgent agent;
    public Transform home;
    public float viewDistance;
    public float maxviewDistance;
    public LayerMask viewMask;
    float viewAngle;
    public float maxviewAngle;
    float playerVisibleTimer;
    Transform player;
    Color originalSpotlightColor2;
    public Light spotlight2;
    bool playerIsSpotted;
    bool hasSeenPlayer;
    bool hasArrivedAtLastPlayerPosition;
    bool hasReturnedToHomePosition;
    position lastSeenPlayerPosition;
    


    // Use this for initialization
    void Start () {

        agent = GetComponent<NavMeshAgent>();
        target = playerManager.instance.player.transform;
        
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        viewAngle = spotlight2.spotAngle;

        originalSpotlightColor2 = spotlight2.color;

        hasSeenPlayer = false
        hasArrivedAtLastPlayerPosition = false
        hasReturnedToHomePosition = false
    }
	
	// Update is called once per frame
	void Update () {

        float distance = Vector3.Distance(target.position, transform.position);

        if (distance <= 4f)
        {
            FaceTarget();
        }

        if (CanSeePlayer())
        {
            playerVisibleTimer += Time.deltaTime;
            viewAngle += Time.deltaTime;
            viewDistance += Time.deltaTime;
            spotlight2.spotAngle = 120f;
        }
        else
        {
            playerVisibleTimer -= Time.deltaTime;
            viewAngle -= Time.deltaTime;
            viewDistance -= Time.deltaTime;
            spotlight2.spotAngle = 80f;
        }
        playerVisibleTimer = Mathf.Clamp(playerVisibleTimer, 0, timeToSpotPlayer);
        viewAngle = Mathf.Clamp(viewAngle, 80, maxviewAngle);
        viewDistance = Mathf.Clamp(viewDistance, 10, maxviewDistance);
        spotlight2.color = Color.Lerp(originalSpotlightColor2, Color.red, playerVisibleTimer / timeToSpotPlayer);

        // it helps to encapsulate this kind of conditional state as a bool so that it's easier to reference repeatedly later
        if (playerVisibleTimer >= timeToSpotPlayer)
        {
            playerIsSpotted = true
        }
        else
        {
            playerIsSpotted = false
        }

        if (playerIsSpotted == true && hasSeenPlayer == false)
        {
            hasSeenPlayer = true
            lastSeenPlayerLocation = player.position
            StartCoroutine(MoveToPoint());
        }
        if (hasSeenPlayer == true && hasArrivedAtLastPlayerPosition == false)
        {
            // continue moving towards last seen position even though playerIsSpotted is no longer true
            StartCoroutine(MoveToPoint());
        }
        if (playerIsSpotted == true && hasSeenPlayer == true && hasArrivedAtLastPlayerPosition == false)
        {
            // we've spotted or are currently spotting the player, we've seen them previously, but we have not arrived at the position we saw them at last
            // this is where you might want to determine how many times / how far they're willing to persue someone
            lastSeenPlayerLocation = player.position // update to the new last seen location
            StartCoroutine(MoveToPoint());
        }
        if (hasArrivedAtLastPlayerPosition == true)
        {
            StartCoroutine(ReturnToHome());
        }
    }

    bool CanSeePlayer()
    {
        if (Vector3.Distance(transform.position, player.position) < viewDistance)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angleBetweenGuardAndPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            if (angleBetweenGuardAndPlayer < viewAngle / 2f)
            {
                if (!Physics.Linecast(transform.position, player.position, viewMask))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool AtPoint()
    {
        float dist = agent.remainingDistance;
        if (dist != Mathf.Infinity && agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    IEnumerator MoveToPoint()
    {
        agent.destination = lastSeenPlayerLocation;
        yield return new WaitUntil(AtPoint);
        hasArrivedAtLastPlayerPosition = true
        // this would be a good time to call a "look left and right" function or w/e
        yield return new WaitForSecondsRealtime(1);
        // you could also keep track of how many times he's spotted the player and increase the wait time/effort he puts into his cursory search before returning home 
        hasSeenPlayer = false
    }
    IEnumerator ReturnToHome()
    {
        hasArrivedAtLastPlayerPosition = false
        agent.destination = home.position;
        yield return new WaitUntil(AtPoint);
        hasReturnedToHomePosition = true
    }
    void FaceTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime *5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * viewDistance);
    }
}
