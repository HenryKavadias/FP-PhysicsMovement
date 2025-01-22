using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private GameObject playerCamera;
    private Transform cameraFollowPoint;
    private GameObject playerComponentHolder; // object with all the movement components

    private CharacterMovementController characterMovementController;
    private SlidingController slidingController;
    private ClimbingController climbingController;
    private WallRunningController wallRunningController;   // works but needs improvement
    private DashingController dashingController;
    private MultiHookController multiHookController; // needs to be changed for 1 input and multiple hooks

    [Header("Modify Movement Inputs")]
    [SerializeField] private bool toggleCrouch = false;
    [SerializeField] private bool toggleSprint = false;

    [Header("Bonus Movement Capabilities")]
    [SerializeField] private bool enableSprint = false;
    [SerializeField] private bool enableCrouch = false;
    [SerializeField] private bool enableSlide = false;
    [SerializeField] private bool enableClimbing = false;
    [SerializeField] private bool enableWallRunning = false;
    [SerializeField] private bool enableDashing = false;

    [SerializeField] private bool enableMultiHook = false;
    [SerializeField] private bool enableGrapple = false;
    [SerializeField] private bool enableRopeSwing = false;

    #region InputHandleFunctions

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
    #endregion
    private void OnDisable()
    { UnassignInputs(); }
    private void OnEnable()
    { AssignInputs(); }

    private void GetMovementComponents()
    {
        if (playerComponentHolder)
        {
            if (playerComponentHolder.TryGetComponent(out CharacterMovementController cmc))
            {
                characterMovementController = cmc;

                if (playerComponentHolder.TryGetComponent(out SlidingController sc))
                { slidingController = sc; }
                if (playerComponentHolder.TryGetComponent(out ClimbingController cc))
                { climbingController = cc; }
                if (playerComponentHolder.TryGetComponent(out WallRunningController wrc))
                { wallRunningController = wrc; }
                if (playerComponentHolder.TryGetComponent(out DashingController dc))
                { dashingController = dc; }
                if (playerComponentHolder.TryGetComponent(out MultiHookController mhc))
                { multiHookController = mhc; }
            }
        }
        else
        { Debug.LogError("ERROR: Missing component holder, no player object assigned."); }
    }

    public void AssignAndSetupPlayerCharacter(GameObject player = null)
    {
        if (player) { playerComponentHolder = player; }

        if (!playerComponentHolder) 
        {
            Debug.LogError("ERROR: Missing player character object"); 
            return; 
        }

        GetMovementComponents();

        if (!characterMovementController)
        {
            Debug.LogError("ERROR: Missing primary movement component (Character Movement Controller).");
            return;
        }

        cameraFollowPoint = characterMovementController.GetCameraPoint();

        if (!cameraFollowPoint)
        {
            Debug.LogError("ERROR: Missing camera follow point, camera can't follow the player.");
            return;
        }

        SetUpCapabilities();
    }

    private void SetUpCapabilities()
    {
        characterMovementController.SetCapabilities(
            enableSprint, enableCrouch);

        // For disabling unused scripts
        if (slidingController) { slidingController.enabled = enableSlide; }
        if (climbingController) { climbingController.enabled = enableClimbing; }
        if (wallRunningController) { wallRunningController.enabled = enableWallRunning; }
        if (dashingController) 
        { 
            dashingController.enabled = enableDashing; 
            // assign camera transform if enabled
            dashingController.SetPlayerCamera(playerCamera.transform);
        }

        if (multiHookController)
        {
            multiHookController.SetGrappleHookModels(enableMultiHook);
            multiHookController.enabled = enableMultiHook;
            if (enableMultiHook)
            {
                dashingController.SetPlayerCamera(playerCamera.transform);
                multiHookController.SetActivation(enableGrapple, enableRopeSwing);
                if (!enableRopeSwing && !enableGrapple) { return; }
                multiHookController.CreatePredictionPoints();
            }
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ResetInputValues()
    {
        movementInput = Vector2.zero;
        sprintInput = false;
    }
    private void AssignInputs()
    {
        inputEnabled = true;
        inputReader.MoveEvent += HandleMove;
        inputReader.LookEvent += HandleLook;
        inputReader.JumpEvent += HandleJump;
        inputReader.CrouchEvent += HandleCrouch;
        inputReader.SprintEvent += HandleSprint;
        inputReader.DashEvent += HandleUpwardsWallRun;
        inputReader.SlideEvent += HandleDownwardsWallRun;
        inputReader.SlideEvent += HandleSlide;
        inputReader.DashEvent += HandleDash;
        inputReader.GrappleEvent += HandleGrapple;
        inputReader.SwingEvent += HandleSwing;
        inputReader.AlternateEvent += HandleAlternate;
    }
    private void UnassignInputs()
    {
        inputEnabled = false;
        inputReader.MoveEvent -= HandleMove;
        inputReader.LookEvent -= HandleLook;
        inputReader.JumpEvent -= HandleJump;
        inputReader.CrouchEvent -= HandleCrouch;
        inputReader.SprintEvent -= HandleSprint;
        inputReader.DashEvent -= HandleUpwardsWallRun;
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
    }
    private void CameraMove()
    {
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        // Controls character body face rotation
        if (characterMovementController)
        {
            characterMovementController.UpdateOrientationRotation(yRotation);
            characterMovementController.UpdateGrappleOrientationRotation(xRotation, yRotation);
        }
    }

    private void UpdateCameraFollowPoint()
    {
        if (!cameraFollowPoint) { return; }
        transform.position = cameraFollowPoint.position;
    }
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
            {
                wallRunningController.HandlePlayerInputs(
                movementInput, upwardWallRun, downwardWallRun, jumpInput);
            }

            if (dashingController && enableDashing)
            { dashingController.HandlePlayerInputs(movementInput, dashInput); }

            if (multiHookController && enableMultiHook)
            {
                multiHookController.HandlePlayerInputs(
                movementInput, jumpInput, swingInput, alternateInput);
            }
        }
    }

    private void Update()
    {
        if (inputEnabled)
        { MovementControl(); }
        
    }

    private void LateUpdate()
    {
        if (inputEnabled)
        {
            CameraControl();
            CameraMove();
        }
        UpdateCameraFollowPoint();
    }
}
