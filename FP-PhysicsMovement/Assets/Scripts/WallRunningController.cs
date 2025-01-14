using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: fix the wall running limit for the same wall and 
// the wall run timer doesn't account for reseting on the same wall
public class WallRunningController : MonoBehaviour
{
    [Header("Wall Running")]
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallJumpUpForce;
    [SerializeField] private float wallJumpSideForce;
    [SerializeField] private float wallClimbSpeed;
    [SerializeField] private float maxWallRunTime;
    private float wallRunTimer;

    private bool upwardsRunning;
    private bool downwardsRunning;
    private Vector2 movementInputs;
    private bool jumpInput;

    [Header("Detection")]
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    [Header("Exiting")]
    [SerializeField] private float exitWallTime;
    private bool exitingWall;
    private float exitWallTimer;

    [Header("Gravity")]
    [SerializeField] private bool useGravity;
    [SerializeField, Range(.01f, 9.81f)] private float gravityCounterForce;

    [Header("References")]
    [SerializeField] private Transform orientation;
    private CharacterMovementController cmc;
    private LedgeGrabbingController lgc;
    private Rigidbody rb;

    private void Start()
    {
        cmc = GetComponent<CharacterMovementController>();
        rb = GetComponent<Rigidbody>();

        if (TryGetComponent(out LedgeGrabbingController lGC))
        { lgc = lGC; }
    }

    private void CheckForWall()
    { 
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    { return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround); }

    private void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void FixedUpdate()
    {
        if (cmc.isWallRunning)
        { WallRunningMovement(); }
    }

    public void HandlePlayerInputs(Vector2 movement, bool upward, bool downward, bool jump)
    {
        upwardsRunning = upward;
        downwardsRunning = downward;
        jumpInput = jump;
        movementInputs = movement;
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

    private void StateMachine()
    {
        // State 1 - Wall running
        if ((wallLeft || wallRight) && movementInputs.y > 0 && AboveGround() && !exitingWall) 
        {
            if (!cmc.isWallRunning)
            { StartWallRun(); }
            
            if (wallRunTimer > 0)
            { wallRunTimer -= Time.deltaTime; }

            if (wallRunTimer <= 0 && cmc.isWallRunning)
            { 
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            if (InputHasChanged() == 0)
            { WallJump(); }
        }
        // State 2 - Exiting
        else if (exitingWall)
        {
            if (cmc.isWallRunning)
            { StopWallRun(); }

            if (exitWallTimer > 0)
            { exitWallTimer -= Time.deltaTime; }

            if (exitWallTimer <= 0)
            { exitingWall = false; }
        }
        // State 3 - None
        else
        {
            if (cmc.isWallRunning)
            { StopWallRun(); }
        }
    }

    private void StartWallRun()
    {
        cmc.isWallRunning = true;

        wallRunTimer = maxWallRunTime;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
    }

    private void StopWallRun()
    {
        cmc.isWallRunning = false;
    }

    private void WallRunningMovement()
    {
        rb.useGravity = useGravity;
        
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        { wallForward = -wallForward; }

        // Forward force
        rb.AddForce(wallForward * wallRunForce * rb.mass, ForceMode.Force);

        if (upwardsRunning)
        { rb.linearVelocity = new Vector3(rb.linearVelocity.x, wallClimbSpeed, rb.linearVelocity.z); }
        if (downwardsRunning)
        { rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallClimbSpeed, rb.linearVelocity.z); }

        if (!(wallLeft && movementInputs.x > 0) && !(wallRight && movementInputs.x > 0))
        { rb.AddForce(-wallNormal * 100f * rb.mass, ForceMode.Force); }

        // weaken gravity
        if (useGravity)
        { rb.AddForce(transform.up * gravityCounterForce * rb.mass, ForceMode.Force); }
    }

    private bool IsLedgeGrabbing()
    {
        return lgc &&
            (lgc.isHolding || lgc.exitingLedge);
    }

    private void WallJump()
    {
        if (IsLedgeGrabbing()) { return; }

        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 forceToApply = (transform.up * wallJumpUpForce) + (wallNormal * wallJumpSideForce);
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply * rb.mass, ForceMode.Impulse);
    }
}
