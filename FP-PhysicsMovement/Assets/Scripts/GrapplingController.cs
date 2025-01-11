using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GrapplingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCam;
    [SerializeField] private Transform gunTip;
    [SerializeField] private LayerMask whatIsGrappleable;
    [SerializeField] private LineRenderer lineRenderer;
    private CharacterMovementController cmc;
    private MonoSwingingController swc;

    [Header("Grappling")]
    [SerializeField] private float maxGrappleDistance;
    [SerializeField] private float grappleDelayTime;
    [SerializeField] private float overShootYAxis;
    private Vector3 grapplePoint;

    [Header("Cooldown")]
    [SerializeField] private float grappleCooldown;
    private float grappleCdTimer;

    [Header("Prediction")]
    [SerializeField] private float predicitonSphereCastRadius;
    [SerializeField] private GameObject predictionVisual;
    private Transform predictionPoint;
    private RaycastHit predictionHit;

    private bool grappling;

    private void Start()
    { 
        cmc = GetComponent<CharacterMovementController>();

        if (TryGetComponent(out MonoSwingingController swingCon))
        { swc = swingCon; }
    }

    public void CreatePredictionPoint()
    {
        if (predictionVisual)
        { predictionPoint = Instantiate(predictionVisual).transform; }
    }


    private bool grappleInput;
    private bool previousGrappleInput = false;
    private int InputHasChanged()
    {
        // with slide input
        // false -> true is down (0)
        // true -> false is up (1)

        int result = -1;

        if (!previousGrappleInput && grappleInput)
        { result = 0; }
        else if (previousGrappleInput && !grappleInput)
        { result = 1; }

        previousGrappleInput = grappleInput;
        return result;
    }

    public void HandlePlayerInputs(bool grapple)
    {
        grappleInput = grapple;
    }

    private void Update()
    {
        if (InputHasChanged() == 0)
        { StartGrapple(); }

        CheckForSwingPoints();

        if (grappleCdTimer > 0)
        { grappleCdTimer -= Time.deltaTime; }
    }

    private void LateUpdate()
    {
        if (grappling)
        { lineRenderer.SetPosition(0, gunTip.position); }
    }

    private void CheckForSwingPoints()
    {
        if (grappleCdTimer > 0) { return; }

        RaycastHit sphereCastHit;
        Physics.SphereCast(
            playerCam.position, predicitonSphereCastRadius, playerCam.forward,
            out sphereCastHit, maxGrappleDistance, whatIsGrappleable);

        RaycastHit raycastHit;
        Physics.Raycast(
            playerCam.position, playerCam.forward, out raycastHit,
            maxGrappleDistance, whatIsGrappleable);

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

    private void ExecuteGrapple()
    {
        cmc.isFrozen = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1, transform.position.z);
        
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overShootYAxis;
        
        if (grapplePointRelativeYPos < 0)
        { highestPointOnArc = overShootYAxis; }

        cmc.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 1f);
    }

    private void StartGrapple()
    {
        if (grappleCdTimer > 0)
        { return; }

        // deactivate active swinging
        if (swc)
        { swc.StopSwing(); }

        grappling = true;
        cmc.isFrozen = true;
        
        if (predictionHit.point != Vector3.zero)
        {
            grapplePoint = predictionHit.point;
            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            grapplePoint = playerCam.position + playerCam.forward * maxGrappleDistance;
            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        //lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(1, grapplePoint);
    }

    public void StopGrapple()
    {
        cmc.isFrozen = false;
        grappling = false;
        grappleCdTimer = grappleCooldown;
        //lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }
}
