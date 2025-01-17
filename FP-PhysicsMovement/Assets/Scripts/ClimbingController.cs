using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private LayerMask whatIsWall;
    private Rigidbody rb;
    private CharacterMovementController cmc;
    private LedgeGrabbingController lgc;


    [Header("Climbing")]
    [SerializeField] private float climbSpeed;
    [SerializeField] private float maxClimbTime;
    private float climbTimer;

    private bool climbing;

    [Header("ClimbJumping")]
    [SerializeField] private float climbJumpUpForce;
    [SerializeField] private float climbJumpBackForce;
    [SerializeField] private int climbJumps;
    private int climbJumpsLeft;

    [Header("Detection")]
    [SerializeField] private float detectionLength;
    [SerializeField] private float sphereCastRadius;
    [SerializeField] private float maxWallLookAngle;
    private float wallLookAngle;

    private RaycastHit frontWallHit;
    private bool wallFront;

    private Transform lastWall;
    private Vector3 lastWallNormal;
    [SerializeField] private float minWallNormalAngleChange;

    [Header("Exiting")]
    [SerializeField] private float exitWallTime;
    private float exitWallTimer;
    private bool exitingWall;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cmc = GetComponent<CharacterMovementController>();

        if (TryGetComponent(out LedgeGrabbingController lGC))
        { lgc = lGC; }
    }

    private bool climbingInput;
    private bool jumpInput;

    public void HandlePlayerInputs(Vector2 moveInput, bool jump)
    {
        climbingInput = moveInput.y > 0f;
        jumpInput = jump;
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

    private void Update()
    {
        WallCheck();
        StateMachine();

        if (climbing && !exitingWall)
        { ClimbingMovement(); }
    }

    private void StateMachine()
    {
        // State 0 - Ledge Grabbing
        if (lgc && lgc.isHolding)
        {
            if (climbing) { StopClimbing(); }

            // everything else is handled by ledge grabbing controller in its state machine
        }
        // State 1 - Climbing
        else if (wallFront && climbingInput && wallLookAngle < maxWallLookAngle && !exitingWall)
        {
            if (!climbing & climbTimer > 0)
            { StartClimbing(); }

            // timer
            if (climbTimer > 0)
            { climbTimer -= Time.deltaTime; }

            if (climbTimer < 0)
            { StopClimbing(); }
        }
        // State 2 - Exiting
        else if (exitingWall)
        {
            if (climbing) { StopClimbing(); }
            if (exitWallTimer > 0) { exitWallTimer -= Time.deltaTime; }
            if (exitWallTimer < 0) 
            { 
                exitingWall = false;
                cmc.isExitingWall = false;
            }
        }
        // State 3 - None
        else
        {
            if (climbing) { StopClimbing(); }
        }

        if (wallFront && InputHasChanged() == 0 && climbJumpsLeft > 0) { ClimbJump(); }
    }

    private void WallCheck()
    {
        wallFront = Physics.SphereCast(
            transform.position, 
            sphereCastRadius, 
            orientation.forward, 
            out frontWallHit, 
            detectionLength, 
            whatIsWall);
        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);

        bool newWall = frontWallHit.transform != lastWall || Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;

        if ((wallFront && newWall) || cmc.grounded) 
        { 
            climbTimer = maxClimbTime;
            climbJumpsLeft = climbJumps;
        }
    }

    private void ClimbingMovement()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, climbSpeed, rb.linearVelocity.z);

        // sound effect
    }

    private void StartClimbing()
    {
        climbing = true;
        cmc.isClimbing = true;

        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;

        // change camera view
    }

    private void StopClimbing()
    {
        climbing = false;
        cmc.isClimbing = false;
        // revert camera view
    }

    private bool IsLedgeGrabbing()
    {
        return lgc && 
            (lgc.isHolding || lgc.exitingLedge);
    }

    private void ClimbJump()
    {
        if (cmc.grounded) { return; }
        if (IsLedgeGrabbing()) { return; }

        exitingWall = true;
        cmc.isExitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 forceToApply = (transform.up * climbJumpUpForce) + (frontWallHit.normal * climbJumpBackForce);

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply * rb.mass, ForceMode.Impulse);

        climbJumpsLeft--;
    }
}
