using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Note: fix most of the bugs, but it is still possible to get stuck in the UNlimited state

public class LedgeGrabbingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform cameraObject;
    private CharacterMovementController cmc;
    private Rigidbody rb;

    [Header("Ledge Grabbing")]
    [SerializeField] private float moveToLedgeSpeed;
    [SerializeField] private float maxLedgeGrabDistance;
    [SerializeField] private float minTimeOnLedge;
    private float timeOnLedge;
    public bool isHolding { get; private set; }

    [Header("Ledge Jumping")]
    [SerializeField] private float ledgeJumpForwardForce;
    [SerializeField] private float ledgeJumpUpwardForce;

    [Header("Ledge Detection")]
    [SerializeField] private float ledgeDetectionLength;
    [SerializeField] private float ledgeSphereCastRadius;
    [SerializeField] private LayerMask whatIsLedge;

    private Transform lastLedge;
    private Transform currentLedge;

    private RaycastHit ledgeHit;

    [Header("Exiting")]
    [SerializeField] private float exitLedgeTime;
    private float exitLedgeTimer;
    public bool exitingLedge { get; private set; }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, cameraObject.forward * ledgeDetectionLength);
    }

    private void Start()
    {
        cmc = GetComponent<CharacterMovementController>();
        rb = GetComponent<Rigidbody>();
    }

    private bool moving;
    private bool jumpInput;

    private void Update()
    {
        LedgeDetection();
        StateMachine();
    }

    private bool previousJumpInput = false;
    private int InputHasChanged()
    {
        // with slide input
        // false -> true is down (0)
        // true -> false is up (1)

        int result = -1;

        if (!previousJumpInput && jumpInput)
        { result = 0; }
        else if (previousJumpInput && !jumpInput)
        { result = 1; }

        previousJumpInput = jumpInput;

        return result;
    }

    public void HandlePlayerInputs(Vector2 move, bool jump)
    {
        moving = move.x != 0 || move.y != 0;
        jumpInput = jump;
    }

    private void StateMachine()
    {
        // State 1 - Holding onto ledge
        if (isHolding)
        {
            FreezeRigidbodyOnLedge();

            timeOnLedge += Time.deltaTime;

            //if (timeOnLedge < minTimeOnLedge)
            //{ timeOnLedge += Time.deltaTime; }

            if (timeOnLedge > minTimeOnLedge &&
                moving)
            { ExitLedgeHold(); }

            if (InputHasChanged() == 0)
            { LedgeJump(); }
        }
        // State 2 - Exiting Ledge
        else if (exitingLedge)
        {
            if (exitLedgeTimer > 0)
            { exitLedgeTimer -= Time.deltaTime; }
            else
            { exitingLedge = false; }
        }
    }

    private void LedgeDetection()
    {
        bool detected = Physics.SphereCast(
            transform.position, ledgeSphereCastRadius, 
            cameraObject.forward, out ledgeHit, ledgeDetectionLength, whatIsLedge);
        
        if (!detected) { return; }
        
        float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.transform.position);

        if (ledgeHit.transform == lastLedge) { return; }

        if (distanceToLedge < maxLedgeGrabDistance && !isHolding)
        { EnterLedgeHold(); }
    }

    private void LedgeJump()
    {
        ExitLedgeHold();
        //DelayedJumpForce();
        Invoke(nameof(DelayedJumpForce), 0.05f);
    }

    // Jump is still buggy
    private void DelayedJumpForce()
    {
        rb.linearVelocity = Vector3.zero;

        Vector3 forceToAdd =
            (cameraObject.forward * ledgeJumpForwardForce) +
            (orientation.up * ledgeJumpUpwardForce);

        rb.AddForce(forceToAdd * rb.mass, ForceMode.Impulse);
    }

    private void EnterLedgeHold()
    {
        if (exitingLedge) return;

        isHolding = true;

        cmc.isUnlimited = true;
        cmc.restricted = true;
        //NOTE: Current ledge is never null
        currentLedge = ledgeHit.transform;
        lastLedge = ledgeHit.transform;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
    }

    private void ExitLedgeHold()
    {
        //currentLedge = null;

        exitingLedge = true;
        exitLedgeTimer = exitLedgeTime;

        isHolding = false;
        timeOnLedge = 0f;

        cmc.restricted = false;
        cmc.isFrozen = false;

        rb.useGravity = true;

        StopAllCoroutines();
        Invoke(nameof(ResetLastLedge), 1f);
    }
    private void ResetLastLedge()
    { lastLedge = null; }
    private void FreezeRigidbodyOnLedge()
    {
        rb.useGravity = false;
           
        Vector3 directionToLedge = currentLedge.position - transform.position;
        float distanceToLedge = Vector3.Distance(transform.position, currentLedge.position);

        if (distanceToLedge > 1f)
        {
            if (rb.linearVelocity.magnitude < moveToLedgeSpeed)
            {
                rb.AddForce(directionToLedge.normalized *
                    moveToLedgeSpeed * rb.mass * 1000f * Time.deltaTime);
            }
        }
        else
        {
            if (!cmc.isFrozen)
            { cmc.isFrozen = true; }

            if (cmc.isUnlimited)
            { cmc.isUnlimited = false; }
        }

        if (distanceToLedge > maxLedgeGrabDistance)
        { ExitLedgeHold(); }
    }
}
