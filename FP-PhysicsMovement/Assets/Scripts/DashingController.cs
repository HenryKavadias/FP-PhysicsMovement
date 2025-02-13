using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Manages the controls for the dash ability for the player
public class DashingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    private Transform playerCam;
    private Rigidbody rb;
    private CharacterMovementController cmc;

    [Header("Dashing")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashUpwardForce;
    [SerializeField] private float maxDashYSpeed;
    [SerializeField] private float dashDuration;

    [Header("Settings")]
    [SerializeField] private bool useCameraForward = true;
    [SerializeField] private bool allowAllDirections = true;
    [SerializeField] private bool disableGravity = false;
    [SerializeField] private bool resetVelocity = true;
    [SerializeField] private bool canOnlyDashAgainOnceGrounded = false;

    private bool canDashAgain = true;

    [Header("Cooldown")]
    [SerializeField] private float dashCooldown;
    private float dashCdTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cmc = GetComponent<CharacterMovementController>();
    }

    public void SetPlayerCamera(Transform cam)
    { playerCam = cam; }

    private Vector2 movementInput;
    private InputDetector dashInput = new();

    public void HandlePlayerInputs(Vector2 movement, bool dash)
    {
        dashInput.inputState = dash;
        movementInput = movement;
    }
    // Only able to dash when it's off cooldown
    private void Update()
    {
        int inputChange = dashInput.HasStateChanged();

        if (canOnlyDashAgainOnceGrounded && !canDashAgain)
        { canDashAgain = cmc.grounded; }

        if (inputChange == 0 && !cmc.isWallRunning)
        { Dash(); }

        // Controls dash cooldown
        if (dashCdTimer > 0)
        { dashCdTimer -= Time.deltaTime; }
    }
    // Holds force to apply to player character for next dash
    private void Dash()
    {
        // Checks dash cooldown
        if ((dashCdTimer > 0 || !canDashAgain)) { return; }
        else { dashCdTimer = dashCooldown; }

        cmc.isDashing = true;
        cmc.maxYSpeed = maxDashYSpeed;

        Transform forwardT = orientation;

        if (useCameraForward)
        { forwardT = playerCam; }

        // Get the direction of the dash, then apply the force
        Vector3 direction = GetDirection(forwardT);
        Vector3 forceToApply = (direction * dashForce) + (orientation.up * dashUpwardForce);
        
        if (disableGravity)
        { rb.useGravity = false; }

        if (canOnlyDashAgainOnceGrounded)
        { canDashAgain = false; }

        // Applies the force and invokes the required functions under a delay
        delayedForceToApply = forceToApply;
        Invoke(nameof(DelayedDashForce), 0.025f);
        Invoke(nameof(ResetDash), dashDuration);
    }

    // Holds force to apply to player character for next dash
    private Vector3 delayedForceToApply;
    // Applies an impulse dash force to player character 
    private void DelayedDashForce()
    {
        if (resetVelocity)
        { rb.linearVelocity = Vector3.zero; }

        rb.AddForce(delayedForceToApply * rb.mass, ForceMode.Impulse);
    }
    // Resets the Dash ability (not cooldown)
    private void ResetDash()
    {
        cmc.isDashing = false;
        cmc.maxYSpeed = 0;

        if (disableGravity)
        { rb.useGravity = true; }
    }
    // Controls and gets the direction for the dash ability
    private Vector3 GetDirection(Transform forwardT)
    {
        Vector3 direction = new Vector3();

        if (allowAllDirections)
        { direction = forwardT.forward * movementInput.y + forwardT.right * movementInput.x; }
        else
        { direction = forwardT.forward; }

        if (movementInput.y == 0 && movementInput.x == 0)
        { direction = forwardT.forward;  }

        return direction.normalized;
    }
}

