using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Source for class: https://discussions.unity.com/t/checking-if-a-layer-is-in-a-layer-mask/860331/7
public class LayerMaskCheck
{
    public static bool IsInLayerMask(GameObject obj, LayerMask mask) => (mask.value & (1 << obj.layer)) != 0;
    public static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
}

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
    [SerializeField] private bool allowVeritcalClimbing;
    private float wallRunTimer;

    private bool upwardsRunning;
    private bool downwardsRunning;
    private Vector2 movementInputs;
    private InputDetector jumpInput;

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

    // WARNING: Does not account for which face/side of the wall you are running on, just the object
    // (bad if two different walls are the same object) consider this: https://discussions.unity.com/t/checking-what-face-an-object-collided-with/516678/4
    private void OnCollisionEnter(Collision collision)
    {
        if (LayerMaskCheck.IsInLayerMask(collision.collider.gameObject, whatIsGround))
        { lastWall = null; }
    }

    private GameObject lastWall;

    private GameObject GetCurrentWall()
    {
        GameObject wall = null;

        if (wallRight)
        { wall = rightWallHit.transform.gameObject; }
        else if (wallLeft)
        { wall = leftWallHit.transform.gameObject; }

        return wall;
    }

    private void SetLastWall()
    {
        if (wallRight)
        { lastWall = rightWallHit.transform.gameObject; }
        else if (wallLeft)
        { lastWall = leftWallHit.transform.gameObject; }
        else
        { lastWall = null; }

        return;
    }

    private bool IsNewWall(GameObject wall)
    { return wall != lastWall; }


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
        jumpInput.inputState = jump;
        movementInputs = movement;
    }

    private void StateMachine()
    {
        int inputStateChange = jumpInput.HasStateChanged();
        
        // State 1 - Wall running
        if ((wallLeft || wallRight) && 
            movementInputs.y > 0 && 
            AboveGround() && 
            !exitingWall && 
            (IsNewWall(GetCurrentWall()) || cmc.isWallRunning)) 
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

            if (inputStateChange == 0)
            { 
                WallJump(true);
                inputStateChange = -1;
            }
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

            if ((wallLeft || wallRight) && AboveGround())
            {
                if (inputStateChange == 0)
                {
                    WallJump();
                    inputStateChange = -1; // prevents repeatedly triggering jumps
                }
            }
        }
        // State 3 - None
        else
        {
            if (cmc.isWallRunning)
            { StopWallRun(); }
        }

        if ((wallLeft || wallRight) && AboveGround())
        {
            if (inputStateChange == 0)
            {
                WallJump();
                inputStateChange = -1; // prevents repeatedly triggering jumps
            }
        }
    }

    private void StartWallRun()
    {
        cmc.isWallRunning = true;
        SetLastWall();
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

        if (allowVeritcalClimbing)
        {
            if (upwardsRunning)
            { rb.linearVelocity = new Vector3(rb.linearVelocity.x, wallClimbSpeed, rb.linearVelocity.z); }
            if (downwardsRunning)
            { rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallClimbSpeed, rb.linearVelocity.z); }
        }
        
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

    private void WallJump(bool leavingWall = false)
    {
        if (IsLedgeGrabbing() || (!wallRight && !wallLeft)) { return; }

        if (leavingWall)
        {
            exitingWall = true;
            exitWallTimer = exitWallTime;
        }
        
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 forceToApply = (transform.up * wallJumpUpForce) + (wallNormal * wallJumpSideForce);
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply * rb.mass, ForceMode.Impulse);
    }
}
