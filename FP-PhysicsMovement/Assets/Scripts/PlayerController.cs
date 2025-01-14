using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;

    [SerializeField] private Transform cameraFollowPoint;

    [SerializeField] private CharacterMovementController characterMovementController;
    [SerializeField] private SlidingController slidingController;
    [SerializeField] private ClimbingController climbingController;
    [SerializeField] private WallRunningController wallRunningController;
    [SerializeField] private LedgeGrabbingController ledgeGrabbingController;
    [SerializeField] private DashingController dashingController;
    [SerializeField] private GrapplingController grapplingController;
    [SerializeField] private MonoSwingingController monoSwingingController;
    [SerializeField] private DualHookController dualHookController;

    [SerializeField] private bool toggleCrouch = false;
    [SerializeField] private bool toggleSprint = false;

    [Header("Bonus Movement Capabilities")]
    [SerializeField] private bool enableSprint = false;
    [SerializeField] private bool enableCrouch = false;
    [SerializeField] private bool enableSlide = false;
    [SerializeField] private bool enableClimbing = false;
    [SerializeField] private bool enableWallRunning = false;
    [SerializeField] private bool enableLedgeGrabbing = false;
    [SerializeField] private bool enableDashing = false;
    [SerializeField] private bool enableDualHook = false;   
    [SerializeField] private bool enableGrapple = false;
    [SerializeField] private bool enableMonoSwing = false;

    private float xRotation;
    private float yRotation;

    bool inputEnabled = false;

    private Vector2 movementInput = Vector2.zero;
    private void HandleMove(Vector2 val)
    { movementInput = val; }
    private Vector2 lookInput = Vector2.zero;
    private void HandleLook(Vector2 val)
    { lookInput = val; }
    private bool jumpInput = false;
    private void HandleJump(bool val)
    { jumpInput = val; }
    private bool sprintInput = false;
    private void HandleSprint(bool val)
    {
        if (!toggleSprint) 
        {
            sprintInput = val;
            return; 
        }

        if (toggleSprint && !val)
        { return; }

        if (sprintInput)
        { sprintInput = false; }
        else
        { sprintInput = true; }
    }
    private bool crouchInput = false;
    private void HandleCrouch(bool val)
    {
        if (!toggleCrouch)
        {
            crouchInput = val;
            return;
        }

        if (toggleCrouch && !val) 
        { return; }

        if (crouchInput)
        { crouchInput = false; }
        else
        { crouchInput = true; }
    }
    private bool upwardWallRun = false;
    private void HandleUpwardsWallRun(bool val)
    { upwardWallRun = val; }
    private bool downwardWallRun = false;
    private void HandleDownwardsWallRun(bool val)
    { downwardWallRun = val; }
    private bool slideInput = false;
    private void HandleSlide(bool val)
    { slideInput = val; }
    private bool dashInput = false;
    private void HandleDash(bool val)
    { dashInput = val; }
    private bool grappleInput = false;
    private void HandleGrapple(bool val)
    { grappleInput = val; }
    private bool swingInput = false;
    private void HandleSwing(bool val)
    { swingInput = val; }
    private bool alternateInput = false;
    private void HandleAlternate(bool val)
    { alternateInput = val; }

    private void OnDisable()
    { UnassignInputs(); }
    private void OnEnable()
    { AssignInputs(); }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (!characterMovementController)
        {
            Debug.LogError("ERROR: Missing primary movement component (Character Movement Controller).");
            return;
        }

        characterMovementController.SetCapabilities(
            enableSprint, enableCrouch);

        // For disabling unused scripts
        if (slidingController) { slidingController.enabled = enableSlide; }
        if (climbingController) { climbingController.enabled = enableClimbing; }
        if (wallRunningController) { wallRunningController.enabled = enableWallRunning; }
        if (ledgeGrabbingController) { ledgeGrabbingController.enabled = enableLedgeGrabbing; }
        if (dashingController) { dashingController.enabled = enableDashing; }

        bool dualHookOnly = false;
        if (dualHookController) 
        { 
            dualHookController.enabled = enableDualHook; 
            if (enableDualHook)
            {
                dualHookController.CreatePredictionPoints();
                dualHookOnly = true; 
            }
        }

        if (grapplingController) 
        { 
            if (dualHookOnly)
            { grapplingController.enabled = false; }
            else
            { 
                grapplingController.enabled = enableGrapple;
                if (enableGrapple)
                { grapplingController.CreatePredictionPoint(); }
            }
        }

        if (monoSwingingController) 
        { 
            if (dualHookOnly)
            { monoSwingingController.enabled = false; }
            else
            { 
                monoSwingingController.enabled = enableMonoSwing;
                if (enableMonoSwing)
                { monoSwingingController.CreatePredictionPoint();}
            }
        }
    }

    private void ResetInputValues()
    {
        movementInput = Vector2.zero;
        sprintInput = false;
    }
    public void AssignInputs()
    {
        inputEnabled = true;
        inputReader.MoveEvent += HandleMove;
        inputReader.LookEvent += HandleLook;
        inputReader.JumpEvent += HandleJump;
        inputReader.CrouchEvent += HandleCrouch;
        inputReader.SprintEvent += HandleSprint;
        inputReader.SprintEvent += HandleUpwardsWallRun;
        inputReader.SlideEvent += HandleDownwardsWallRun;
        inputReader.SlideEvent += HandleSlide;
        inputReader.DashEvent += HandleDash;
        inputReader.GrappleEvent += HandleGrapple;
        inputReader.SwingEvent += HandleSwing;
        inputReader.AlternateEvent += HandleAlternate;
    }
    public void UnassignInputs()
    {
        inputEnabled = false;
        inputReader.MoveEvent -= HandleMove;
        inputReader.LookEvent -= HandleLook;
        inputReader.JumpEvent -= HandleJump;
        inputReader.CrouchEvent -= HandleCrouch;
        inputReader.SprintEvent -= HandleSprint;
        inputReader.SprintEvent -= HandleUpwardsWallRun;
        inputReader.SlideEvent -= HandleDownwardsWallRun;
        inputReader.SlideEvent -= HandleSlide;
        inputReader.DashEvent -= HandleDash;
        inputReader.GrappleEvent -= HandleGrapple;
        inputReader.SwingEvent -= HandleSwing;
        inputReader.AlternateEvent -= HandleAlternate;

        ResetInputValues();
    }

    private void CameraControl()
    {
        yRotation += lookInput.x * inputReader.mouseSensitivityX * Time.deltaTime;
        xRotation -= lookInput.y * inputReader.mouseSensitivityY * Time.deltaTime;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        // Controls character body face rotation
        if (characterMovementController)
        { 
            characterMovementController.UpdateOrientationRotation(yRotation); 
            characterMovementController.UpdateGrappleOrientationRotation(xRotation, yRotation);
        }
    }

    private void UpdateCameraFollowPoint()
    { transform.position = cameraFollowPoint.position; }
    private void MovementControl()
    {
        if (characterMovementController)
        { 
            characterMovementController.HandlePlayerInputs(
                movementInput, jumpInput, sprintInput, crouchInput);

            if (slidingController && enableSlide)
            { slidingController.HandlePlayerInputs(slideInput, movementInput); }

            if (climbingController && enableClimbing)
            { climbingController.HandlePlayerInputs(movementInput, jumpInput); }

            if (wallRunningController && enableWallRunning)
            { wallRunningController.HandlePlayerInputs(
                movementInput, upwardWallRun, downwardWallRun, jumpInput); }

            if (ledgeGrabbingController && enableLedgeGrabbing)
            { ledgeGrabbingController.HandlePlayerInputs(movementInput, jumpInput); }

            if (dashingController && enableDashing)
            { dashingController.HandlePlayerInputs(movementInput, dashInput); }

            if (dualHookController && enableDualHook)
            { dualHookController.HandlePlayerInputs(
                movementInput, jumpInput, grappleInput, swingInput, alternateInput); }
            else
            {
                if (grapplingController && enableGrapple)
                { grapplingController.HandlePlayerInputs(grappleInput); }

                if (monoSwingingController && enableMonoSwing)
                { monoSwingingController.HandlePlayerInputs(movementInput, jumpInput, swingInput); }
            }
        }
    }

    private void Update()
    {
        if (inputEnabled)
        {
            CameraControl();
            MovementControl();
        }
        UpdateCameraFollowPoint();
    }
}
