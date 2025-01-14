using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlidingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerModel;
    private Rigidbody rb;
    private CharacterMovementController cmc;

    private float horizontalInput;
    private float verticalInput;
    private bool slideInput;

    [Header("Sliding")]
    [SerializeField] private float maxSlideTime;
    [SerializeField] private float slideForce;
    private float slideTimer;

    [SerializeField] private float slideYScale;
    private float startYScale;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cmc = GetComponent<CharacterMovementController>();
        startYScale = playerModel.transform.localScale.y;
    }

    private bool previousSlideInput = false;

    private int InputHasChanged()
    {
        // with slide input
        // false -> true is down
        // true -> false is up

        int result = -1;

        if (!previousSlideInput && slideInput)
        { result = 0; }
        else if (previousSlideInput && !slideInput)
        { result = 1; }

        previousSlideInput = slideInput;

        return result;
    }

    public void HandlePlayerInputs(bool sliding, Vector2 movementInput)
    {
        horizontalInput = movementInput.x;
        verticalInput = movementInput.y;
        slideInput = sliding;
    }

    private void Update()
    {
        int inputChange = InputHasChanged();

        if (!cmc.isSliding && inputChange == 0 &&
            (horizontalInput != 0 || verticalInput != 0))
        { StartSlide(); }

        if (inputChange == 1 && cmc.isSliding)
        { StopSlide(); }
    }

    private void FixedUpdate()
    {
        if (cmc.isSliding)
        { SlidingMovement(); }
    }

    private void SlidingMovement()
    {
        if (slideTimer <= 0 || !cmc.isSliding) { return; }

        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // Normal
        if (!cmc.OnSlope() || rb.linearVelocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce * 10f * rb.mass, ForceMode.Force);

            slideTimer -= Time.deltaTime;
        }
        // Slope
        else
        { 
            rb.AddForce(
                cmc.GetSlopeMoveDirection(inputDirection) * 
                slideForce * 10f * rb.mass, ForceMode.Force); 
        }

        if (slideTimer <= 0)
        { StopSlide(); }
    }
    private void StartSlide()
    {
        if (cmc.isWallRunning) { return; }

        cmc.isSliding = true;

        playerModel.transform.localScale =
                new Vector3(
                    playerModel.transform.localScale.x,
                    slideYScale,
                    playerModel.transform.localScale.z);

        cmc.currentHeight *= slideYScale + 0.25f;

        rb.AddForce(Vector3.down * 5f * rb.mass, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }
    private void StopSlide()
    {
        cmc.isSliding = false;

        playerModel.transform.localScale =
                new Vector3(
                    playerModel.transform.localScale.x,
                    startYScale,
                    playerModel.transform.localScale.z);

        cmc.currentHeight = cmc.defaultHeight;
    }
}
