using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterMovementController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Transform orientation;

    private float horizontalMovement;
    private float verticalMovement;

    private Vector3 movementDirection;
    private Rigidbody rb;

    public bool enableSprint { get; private set; } = false;
    public bool enableCrouch { get; private set; } = false;
    public bool enableSlide { get; private set; } = false;
    public bool enableClimbing { get; private set; } = false;
    public bool enableWallRunning { get; private set; } = false;
    public bool enableLedgeGrabbing { get; private set; } = false;
    public bool enableDashing { get; private set; } = false;
    public bool enableGrabble { get; private set; } = false;
    public bool enableMonoSwing { get; private set; } = false;
    public bool enableDualSwing { get; private set; } = false;

    [SerializeField] private MovementState state;
    public enum MovementState
    {
        Freeze,
        Unlimited,
        Walking,
        Sprinting,
        WallRunning,
        Grappling,
        Swinging,
        Crouching,
        Dashing,
        Climbing,
        Sliding,
        Air
    }

    public bool isDashing { get; set; } = false;
    public bool isSliding { get; set; } = false;
    public bool isClimbing { get; set; } = false;
    public bool isWallRunning { get; set; } = false;
    public bool isExitingWall { get; set; } = false;
    public bool isFrozen { get; set; } = false;
    public bool isUnlimited { get; set; } = false;
    public bool restricted { get; set; } = false;
    public bool activeGrapple { get; set; } = false;
    public bool activeSwinging { get; set; } = false;

    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashSpeedChangeFactor;
    [SerializeField] private float slideSpeed;
    [SerializeField] private float climbSpeed;
    [SerializeField] private float wallRunSpeed;
    [SerializeField] private float grappleSpeed;
    [SerializeField] private float swingSpeed;
    [SerializeField] private float airMinSpeed;
    private float moveSpeed;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    public float maxYSpeed { get; set; }

    [SerializeField] private float speedIncreaseMultiplier;
    [SerializeField] private float slopeIncreaseMultiplier;
    [SerializeField] private float groundDrag;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown = 0.45f;
    [SerializeField] private float airMultiplier;
    [SerializeField] private int jumpLimit = 2; // number of times the player can jump
    private int jumpCount;
    private bool canJump = true;

    [Header("Crouching")]
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float crouchYScale;
    private float startYScale;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject playerModel;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private float lengthOfCheck = -1.1f;
    [SerializeField, Range(0.001f, 3f)] private float groundCheckBoxSizeMultiplier = 0.8f;
    public float defaultHeight { get; private set; }
    public float currentHeight { get; set; }
    private Vector3 groundCheckBoxSize;
    public bool grounded { get; private set; } = false;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    public void SetCapabilities(
        bool sprint, 
        bool crouch, 
        bool slide, 
        bool climb, 
        bool wallRun,
        bool ledgeGrab,
        bool dashing,
        bool grapple,
        bool monoSwing,
        bool dualSwing)
    {
        enableSprint = sprint;
        enableCrouch = crouch;
        enableSlide = slide;
        enableClimbing = climb;
        enableWallRunning = wallRun;
        enableLedgeGrabbing = ledgeGrab;
        enableDashing = dashing;
        enableGrabble = grapple;
        enableMonoSwing = monoSwing;
        enableDualSwing = dualSwing;
    }

    private void OnDrawGizmos()
    {
        float maxDistance = currentHeight * 0.5f + lengthOfCheck;
        /*
        RaycastHit hit;

        bool isHit = Physics.BoxCast(
            playerCollider.bounds.center,
            new Vector3(0.2f, 0.2f, 0.2f),
            Vector3.down,
            out hit,
            transform.rotation,
            maxDistance,
            groundLayer);

        if (isHit)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector3.down * hit.distance);
            Gizmos.DrawWireSphere(transform.position + (Vector3.down * maxDistance), groundCheckBoxSize.x / 2);
        }
        */
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * maxDistance);
        Gizmos.DrawWireSphere(transform.position + (Vector3.down * maxDistance), groundCheckBoxSize.x / 2);

        // slope detection ray
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector3.down * maxDistance);
        Gizmos.DrawWireSphere(transform.position + (Vector3.down * maxDistance), groundCheckBoxSize.x / 2);
    }
    private void SetGroundCheckBoxSize()
    {
        defaultHeight = 1.8f * 2f; // TODO: maybe not hard coded
        currentHeight = defaultHeight;
        // Note: also used for Sphere Cast
        groundCheckBoxSize = new Vector3(
            playerModel.transform.localScale.x * groundCheckBoxSizeMultiplier
            , 0.05f,
            playerModel.transform.localScale.z * groundCheckBoxSizeMultiplier);
    }
    private bool SphereCastCheck()
    {
        return Physics.SphereCast(
            transform.position,
            groundCheckBoxSize.x / 2,
            Vector3.down, out RaycastHit hit,
            currentHeight * 0.5f + lengthOfCheck,
            groundLayer);
    }

    // Controls how the player moves up and down slopes
    public bool OnSlope()
    {
        if (Physics.SphereCast(
            transform.position,
            groundCheckBoxSize.x / 2,
            Vector3.down, out slopeHit,
            currentHeight * 0.5f + lengthOfCheck))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0f;
        }

        return false;
    }

    private bool enableMovementOnNextTouch;

    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f);

        Invoke(nameof(ResetRestrictions), 3f);
    }

    private Vector3 velocityToSet;

    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.velocity = velocityToSet; 
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();

            if (gapC)
            { gapC.StopGrapple(); }

            if (dsc)
            { dsc.CancelActiveGrapples(); }
        }
    }

    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) + 
            Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }

    // Get the direct the player is moving on a slope
    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    { return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized; }

    private LedgeGrabbingController lgc; // this needs serious refactoring
    private GrapplingController gapC;
    private DualHookController dsc;

    private void Start()
    {
        SetGroundCheckBoxSize();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        startYScale = playerModel.transform.localScale.y;

        canJump = true;

        if (TryGetComponent(out DualHookController DualSwinCon))
        { dsc = DualSwinCon; }

        if (TryGetComponent(out GrapplingController grapCon))
        { gapC = grapCon; }

        if (TryGetComponent(out LedgeGrabbingController ledgCon))
        { lgc = ledgCon; }
    }

    private Vector2 movementInput;
    private bool jumpInput;
    private bool sprintInput;
    private bool crouchInput;

    // called every frame
    public void HandlePlayerInputs(
        Vector2 moving, bool jumping, bool sprinting, bool crouching)
    {
        if ((!lgc || !lgc.isHolding) && !isUnlimited)
        {
            movementInput = moving;
            jumpInput = jumping;
            sprintInput = sprinting;
            crouchInput = crouching;
            
            return;
        }
        movementInput = Vector2.zero;
        jumpInput = false;
        sprintInput = false;
        crouchInput = false;
    }

    private void Update()
    {
        grounded = SphereCastCheck();

        HandleMovement();
        HandleJump();
        HandleCrouch();

        SpeedControl();

        StateHandler();

        GroundDrag();
    }

    private bool keepMomentum;
    private MovementState lastState;

    private void StateHandler()
    { 
        // Mode - Freeze
        if (isFrozen)
        { 
            state = MovementState.Freeze; 
            rb.velocity = Vector3.zero;
            desiredMoveSpeed = 0f;
        }
        // Mode - Unlimited
        else if (isUnlimited)
        { 
            state = MovementState.Unlimited;
            desiredMoveSpeed = 999f;
        }
        // Mode - Grappling
        else if (activeGrapple)
        {
            state = MovementState.Grappling;
            desiredMoveSpeed = grappleSpeed;
        }
        // Mode - Swinging
        else if (activeSwinging)
        {
            state = MovementState.Swinging;
            desiredMoveSpeed = swingSpeed;
        }
        // Mode - Dashing
        else if (enableDashing && isDashing)
        {
            state = MovementState.Dashing;
            desiredMoveSpeed = dashSpeed;
            speedChangeFactor = dashSpeedChangeFactor;
        }
        // Mode - Climbing
        else if (enableClimbing && isClimbing)
        {
            state = MovementState.Climbing;
            desiredMoveSpeed = climbSpeed;
        }
        // Mode - Wall Running
        else if (enableWallRunning && isWallRunning)
        {
            state = MovementState.WallRunning;
            desiredMoveSpeed = wallRunSpeed;
        }
        // Mode - Sliding
        else if (enableSlide && isSliding)
        {
            state = MovementState.Sliding;
            if (OnSlope() && rb.velocity.y < 0.1f)
            { 
                desiredMoveSpeed = slideSpeed; 
                keepMomentum = true;
            }
            else
            { desiredMoveSpeed = sprintSpeed; }
        }
        // Mode - Crouch
        else if (enableCrouch && crouchInput)
        {
            state = MovementState.Crouching;
            desiredMoveSpeed = crouchSpeed;
        }
        // Mode - Sprinting
        else if (enableSprint && grounded && sprintInput)
        {
            state = MovementState.Sprinting;
            desiredMoveSpeed = sprintSpeed;
        }
        // Mode - Walk
        else if (grounded)
        {
            state = MovementState.Walking;
            desiredMoveSpeed = walkSpeed;
        }
        // Mode - Air
        else
        {
            state = MovementState.Air;

            if (moveSpeed < airMinSpeed)
                desiredMoveSpeed = airMinSpeed;
        }

        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;

        if (lastState == MovementState.Dashing)
        { keepMomentum = true; }

        if (desiredMoveSpeedHasChanged)
        {
            if (keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                StopAllCoroutines();
                moveSpeed = desiredMoveSpeed;
            }
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
        lastState = state;

        if (Mathf.Abs(desiredMoveSpeed - moveSpeed) < 0.1f)
        { keepMomentum = false; }
    }

    private float speedChangeFactor;
    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        float boostFactor = speedChangeFactor;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * boostFactor * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
            { time += Time.deltaTime * boostFactor; }
            
            yield return null;
        }

        speedChangeFactor = speedIncreaseMultiplier;
        moveSpeed = desiredMoveSpeed;
        keepMomentum = false;
    }

    private void HandleMovement()
    {
        horizontalMovement = movementInput.x;
        verticalMovement = movementInput.y;
    }

    private bool stillCrouching = false;

    private void HandleCrouch()
    {
        if (!enableCrouch) { return; }
        
        if (crouchInput && !stillCrouching)
        {
            // Forces the player character to the ground

            // Modify character model (NOTE: may get swapped with an animation)
            playerModel.transform.localScale =
                new Vector3(
                    playerModel.transform.localScale.x,
                    crouchYScale,
                    playerModel.transform.localScale.z);

            currentHeight *= crouchYScale + 0.25f;

            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            stillCrouching = true;
        }
        else if (!crouchInput && stillCrouching)
        {
            playerModel.transform.localScale =
               new Vector3(
                   playerModel.transform.localScale.x,
                   startYScale,
                   playerModel.transform.localScale.z);

            currentHeight = defaultHeight;

            stillCrouching = false;
        }
    }

    private void HandleJump()
    {
        if (lgc && (lgc.isHolding || lgc.exitingLedge)) { return; }

        if (jumpInput && canJump)
        {
            if (grounded || (!grounded && jumpCount < jumpLimit))
            {
                if (!grounded && jumpCount == 0)
                { jumpCount++; }

                canJump = false;
                Jump();
                jumpCount++;
                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }

        TryResetJumpLimit();
    }

    public void UpdateOrientationRotation(float yRotation)
    { orientation.rotation = Quaternion.Euler(0, yRotation, 0); }

    public void UpdateMovementDirection()
    { 
        movementDirection = 
            (orientation.forward * verticalMovement) + 
            (orientation.right * horizontalMovement); 
    }

    private void MoveCharacter()
    {
        if (state == MovementState.Dashing || activeGrapple || activeSwinging)
        { return; }

        UpdateMovementDirection();
        
        // on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(movementDirection) * moveSpeed * 20f * rb.mass, ForceMode.Force);

            if (rb.velocity.y > 0)
            { rb.AddForce(Vector3.down * 80f, ForceMode.Force); }
        }
        // on ground
        else if (grounded)
        { rb.AddForce(movementDirection.normalized * moveSpeed * 10f * rb.mass, ForceMode.Force); }
        // in air
        else if (!grounded)
        { rb.AddForce(movementDirection.normalized * moveSpeed * 10f * rb.mass * airMultiplier, ForceMode.Force); }

        // Turn off gravity when onslopes
        if (!isWallRunning) { rb.useGravity = !OnSlope(); }
    }

    // Rate in which the agent slows down to a stop when there is no movement input
    [SerializeField, Range(0.01f, 1f)] private float decelerationRate = 0.5f;    // Must be between 0 and 1

    // Reduces the velocity of the game object to zero over time
    private void DecelerateVelocity()
    {
        rb.velocity = rb.velocity * decelerationRate * Time.deltaTime;
        rb.angularVelocity = rb.angularVelocity * decelerationRate * Time.deltaTime;
    }

    private void SpeedControl()
    {
        if (activeGrapple) { return; }

        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
            { rb.velocity = rb.velocity.normalized * moveSpeed; }
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }

        if (maxYSpeed != 0 && rb.velocity.y > maxYSpeed)
        { rb.velocity = new Vector3(rb.velocity.x, maxYSpeed, rb.velocity.z); }
    }

    private void Jump()
    {
        exitingSlope = true;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce * rb.mass, ForceMode.Impulse);
    }
    
    private void TryResetJumpLimit()
    {
        if (grounded && canJump)
        { jumpCount = 0; }
    }

    private void ResetJump()
    {
        canJump = true;

        exitingSlope = false;
    }

    private void GroundDrag()
    {
        if ((state == MovementState.Walking || 
            state == MovementState.Sprinting || 
            state == MovementState.Crouching ||
            state == MovementState.Sliding) && 
            !activeGrapple && rb.drag != groundDrag)
        { rb.drag = groundDrag; }
        else
        { rb.drag = 0; }
    }

    private void FixedUpdate()
    {
        if (isExitingWall || restricted) return;

        MoveCharacter();
    }
}
