using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSwingingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCam;
    [SerializeField] private Transform gunTip;
    [SerializeField] private Transform player;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask whatIsGrappleable;
    private CharacterMovementController cmc;
    private GrapplingController gapc;

    [Header("Swinging")]
    [SerializeField] private float maxSwingDistance = 25f;
    [SerializeField] private float jointSpring = 4.5f;
    [SerializeField] private float jointDamper = 7f;
    [SerializeField] private float jointMassScale = 4.5f;
    private Vector3 swingPoint;
    private SpringJoint joint;

    [Header("OdmGear")]
    [SerializeField] private bool enableSwingingWithForces = true;
    [SerializeField] private Transform orientation;
    [SerializeField] private float horizontalThrustForce;
    [SerializeField] private float forwardThrustForce;
    [SerializeField] private float extendCableSpeed;
    private Rigidbody rb;

    [Header("Prediction")]
    [SerializeField] private float predicitonSphereCastRadius;
    [SerializeField] private GameObject predictionVisual;
    private Transform predictionPoint;
    private RaycastHit predictionHit;

    private Vector2 moveInput;
    private bool jumpInput;
    private bool swingInput;
    private bool previousSwingInput = false;

    private Vector3 currentGrapplePosition;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cmc = GetComponent<CharacterMovementController>();

        if (TryGetComponent(out GrapplingController grappleCon))
        { gapc = grappleCon; }
    }

    public void CreatePredictionPoint()
    {
        if (predictionVisual)
        { predictionPoint = Instantiate(predictionVisual).transform; }
    }

    private void DrawRope()
    {
        if (!joint) return;

        currentGrapplePosition = Vector3.Lerp(
            currentGrapplePosition, swingPoint, Time.deltaTime * 8f);

        lineRenderer.SetPosition(0, gunTip.position);
        lineRenderer.SetPosition(1, swingPoint);
    }

    private int InputHasChanged()
    {
        // with slide input
        // false -> true is down (0)
        // true -> false is up (1)

        int result = -1;

        if (!previousSwingInput && swingInput)
        { result = 0; }
        else if (previousSwingInput && !swingInput)
        { result = 1; }

        previousSwingInput = swingInput;
        return result;
    }

    public void HandlePlayerInputs(Vector2 move, bool jump, bool swing)
    {
        moveInput = move;
        jumpInput = jump;
        swingInput = swing;
    }

    private void OdmGearMovement()
    {
        // right
        if (moveInput.x > 0)
        { rb.AddForce(orientation.right * horizontalThrustForce * rb.mass * Time.deltaTime); }
        // left
        if (moveInput.x < 0)
        { rb.AddForce(-orientation.right * horizontalThrustForce * rb.mass * Time.deltaTime); }

        // forward
        if (moveInput.y > 0)
        { rb.AddForce(orientation.forward * forwardThrustForce * rb.mass * Time.deltaTime); }

        // shorten cable
        if (jumpInput)
        {
            Vector3 directionToPoint = swingPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * rb.mass * Time.deltaTime);

            float distanceFromPoint = Vector3.Distance(transform.position, swingPoint);

            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
        }

        // extend cable
        if (moveInput.y < 0)
        {
            float extendedDistanceFromPoint = Vector3.Distance(
                transform.position, swingPoint) + extendCableSpeed;

            joint.maxDistance = extendedDistanceFromPoint * 0.8f;
            joint.minDistance = extendedDistanceFromPoint * 0.25f;
        }
    }

    private void CheckForSwingPoints()
    {
        if (joint) { return; }

        RaycastHit sphereCastHit;
        Physics.SphereCast(
            playerCam.position, predicitonSphereCastRadius, playerCam.forward,
            out sphereCastHit, maxSwingDistance, whatIsGrappleable);

        RaycastHit raycastHit;
        Physics.Raycast(
            playerCam.position, playerCam.forward, out raycastHit,
            maxSwingDistance, whatIsGrappleable);

        Vector3 realHitPoint = Vector3.zero;

        if (raycastHit.point != Vector3.zero)
        { realHitPoint = raycastHit.point; }
        else if (sphereCastHit.point != Vector3.zero)
        { realHitPoint = sphereCastHit.point; }

        if (predictionPoint)
        {
            if (realHitPoint != Vector3.zero)
            {
                predictionPoint.gameObject.SetActive(true);
                predictionPoint.position = realHitPoint;
            }
            else
            { predictionPoint.gameObject.SetActive(false); }
        }

        predictionHit = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
    }

    private void Update()
    {
        int inputChange = InputHasChanged();

        if (inputChange == 0)
        { StartSwing(); }
        else if (inputChange == 1)
        { StopSwing(); }

        CheckForSwingPoints();

        if (enableSwingingWithForces && joint)
        { OdmGearMovement(); }
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void StartSwing()
    {
        if (predictionHit.point == Vector3.zero) return;

        if (gapc)
        { gapc.StopGrapple(); }
        cmc.ResetRestrictions();

        cmc.activeSwinging = true;

        swingPoint = predictionHit.point;
        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

        joint.spring = jointSpring;
        joint.damper = jointDamper;
        joint.massScale = jointMassScale;
        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;

        lineRenderer.positionCount = 2;
        currentGrapplePosition = gunTip.position;
    }

    public void StopSwing()
    {
        cmc.activeSwinging = false;
        lineRenderer.positionCount = 0;
        Destroy(joint);
    }
}