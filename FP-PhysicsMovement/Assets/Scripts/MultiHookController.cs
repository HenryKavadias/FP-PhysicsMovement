using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Grappler
{
    public Transform gunTip;
    public LineRenderer lineRenderer;
    public Transform pointAimer;
}

public class MultiHookController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCam;
    [SerializeField] private List<Grappler> grapplers;
    //[SerializeField] private List<Transform> gunTips;
    [SerializeField] private Transform player;
    //[SerializeField] private List<LineRenderer> lineRenderers;
    [SerializeField] private LayerMask whatIsGrappleable;
    private CharacterMovementController cmc;

    [Header("Swinging")]
    [SerializeField] private float maxSwingDistance = 25f;
    [SerializeField] private float jointSpring = 4.5f;
    [SerializeField] private float jointDamper = 7f;
    [SerializeField] private float jointMassScale = 4.5f;
    private List<Vector3> swingPoints;
    private List<SpringJoint> joints;

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
    private List<Transform> predictionPoints;
    private List<RaycastHit> predictionHits;

    [Header("Dual Swinging")]
    //[SerializeField] private List<Transform> pointAimers;
    private int amountOfSwingPoints = 2;
    private List<bool> activeSwings;

    [Header("Grappling")]
    [SerializeField] private float maxGrappleDistance;
    [SerializeField] private float grappleDelayTime;
    [SerializeField] private float overshootYAxis;

    private List<bool> activeGrapples;

    [Header("Cooldown")]
    [SerializeField] private float grapplingCd;
    private float grapplingCdTimer;

    private Vector2 moveInput;
    private bool jumpInput;
    private bool alternateInput;
    private InputDetector grappleInput = new();

    private enum GrappleSide { Left, Right };

    private List<Vector3> currentGrapplePositions;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cmc = GetComponent<CharacterMovementController>();

        ListSetup();
    }

    private void ListSetup()
    {
        amountOfSwingPoints = grapplers.Count;
        Debug.Log("number of grapples is " + amountOfSwingPoints);
        predictionHits = new List<RaycastHit>();

        swingPoints = new List<Vector3>();
        joints = new List<SpringJoint>();

        activeSwings = new List<bool>();
        activeGrapples = new List<bool>();

        currentGrapplePositions = new List<Vector3>();

        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            predictionHits.Add(new RaycastHit());
            joints.Add(null);
            swingPoints.Add(Vector3.zero);
            activeSwings.Add(false);
            activeGrapples.Add(false);
            currentGrapplePositions.Add(Vector3.zero);
        }
    }

    public void CreatePredictionPoints()
    {
        predictionPoints = new List<Transform>();

        for (int i = 0; i < amountOfSwingPoints; i++)
        { predictionPoints.Add(Instantiate(predictionVisual).transform); }
    }


    //private int InputHasChanged(GrappleSide side)
    //{
    //    int result = -1;

    //    if (side == GrappleSide.Left)
    //    {
    //        if (!previousLeftSwingInput && leftSwingInput)
    //        { result = 0; }
    //        else if (previousLeftSwingInput && !leftSwingInput)
    //        { result = 1; }

    //        previousLeftSwingInput = leftSwingInput;
    //    }
    //    else if (side == GrappleSide.Right)
    //    {
    //        if (!previousRightSwingInput && rightSwingInput)
    //        { result = 0; }
    //        else if (previousRightSwingInput && !rightSwingInput)
    //        { result = 1; }

    //        previousRightSwingInput = rightSwingInput;
    //    }

    //    return result;
    //}

    public void HandlePlayerInputs(
        Vector2 move, bool jump, 
        bool grapple, bool alternate)
    {
        moveInput = move;
        jumpInput = jump;
        grappleInput.inputState = grapple;
        alternateInput = alternate;
    }

    private void ManageInputs()
    {
        int inputChange = grappleInput.InputHasChanged();

        if (false) //if (alternateInput)
        { if (inputChange == 0) { StartGrapple(0); } }
        else
        { if (inputChange == 0) { StartSwing(); } }

        if (inputChange == 1) { StopSwing(); }
    }

    private void Update()
    {
        ManageInputs();

        if (enableSwingingWithForces)
        {
            for (int i = 0; i < amountOfSwingPoints; i++)
            {
                if (joints[i])
                {
                    Debug.Log(i);
                    OdmGearMovement();
                    break;
                }
            }
        }

        CheckForSwingPoints();

        if (grapplingCdTimer > 0)
        { grapplingCdTimer -= Time.deltaTime; }
    }

    private void LateUpdate()
    {
        DrawRope();
    }
    private void DrawRope()
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            if (!activeSwings[i] && !activeGrapples[i])
            { grapplers[i].lineRenderer.positionCount = 0; }
            else
            {
                currentGrapplePositions[i] = Vector3.Lerp(
                    currentGrapplePositions[i], swingPoints[i], Time.deltaTime * 8f);

                grapplers[i].lineRenderer.positionCount = 2;
                grapplers[i].lineRenderer.SetPosition(0, grapplers[i].gunTip.position);
                grapplers[i].lineRenderer.SetPosition(1, swingPoints[i]);
            }
        }
    }


    #region OdmGear
    private Vector3 pullPoint;
    private void OdmGearMovement()
    {
        //if (activeSwings[0] && !activeSwings[1]) pullPoint = swingPoints[0];
        //if (activeSwings[1] && !activeSwings[0]) pullPoint = swingPoints[1];
        //// get midpoint if both swing points are active
        //if (activeSwings[0] && activeSwings[1])
        //{
        //    Vector3 dirToGrapplePoint1 = swingPoints[1] - swingPoints[0];
        //    pullPoint = swingPoints[0] + dirToGrapplePoint1 * 0.5f;
        //}

        Vector3 directionSum = Vector3.zero;
        int numOfActiveSwings = 0;

        for (int i = 0; i < activeSwings.Count; i++)
        {
            if (!activeSwings[i]) { continue; }
            numOfActiveSwings++;
            directionSum += swingPoints[i];
        }

        pullPoint = directionSum / numOfActiveSwings;

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
            Vector3 directionToPoint = pullPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * rb.mass * Time.deltaTime);

            // calculate the distance to the grapplePoint
            float distanceFromPoint = Vector3.Distance(transform.position, pullPoint);

            // the distance grapple will try to keep from grapple point.
            UpdateJoints(distanceFromPoint);
        }

        // extend cable
        if (moveInput.y < 0)
        {
            // calculate the distance to the grapplePoint
            float extendedDistanceFromPoint = Vector3.Distance(
                transform.position, pullPoint) + extendCableSpeed;

            // the distance grapple will try to keep from grapple point.
            UpdateJoints(extendedDistanceFromPoint);
        }
    }

    private void UpdateJoints(float distanceFromPoint)
    {
        for (int i = 0; i < joints.Count; i++)
        {
            if (joints[i] != null)
            {
                joints[i].maxDistance = distanceFromPoint * 0.8f;
                joints[i].minDistance = distanceFromPoint * 0.25f;
            }
        }
    }

    private void CheckForSwingPoints()
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
        {
            if (!activeSwings[i])
            {
                RaycastHit sphereCastHit;
                Physics.SphereCast(
                    grapplers[i].pointAimer.position, predicitonSphereCastRadius, grapplers[i].pointAimer.forward,
                    out sphereCastHit, maxSwingDistance, whatIsGrappleable);

                RaycastHit raycastHit;
                Physics.Raycast(
                    grapplers[i].pointAimer.position, grapplers[i].pointAimer.forward, out raycastHit,
                    maxSwingDistance, whatIsGrappleable);

                Vector3 realHitPoint = Vector3.zero;

                if (raycastHit.point != Vector3.zero)
                { realHitPoint = raycastHit.point; }
                else if (sphereCastHit.point != Vector3.zero)
                { realHitPoint = sphereCastHit.point; }

                if (predictionPoints[i])
                {
                    if (realHitPoint != Vector3.zero)
                    {
                        predictionPoints[i].gameObject.SetActive(true);
                        predictionPoints[i].position = realHitPoint;
                    }
                    else
                    { predictionPoints[i].gameObject.SetActive(false); }
                }

                predictionHits[i] = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
            }
        }
    }
    #endregion

    #region Swinging
    private void StartSwing()
    {
        for (int i = 0; i < grapplers.Count; i++)
        {
            if (predictionHits[i].point == Vector3.zero) continue;

            activeSwings[i] = true;

            swingPoints[i] = predictionHits[i].point;
            joints[i] = player.gameObject.AddComponent<SpringJoint>();
            joints[i].autoConfigureConnectedAnchor = false;
            joints[i].connectedAnchor = swingPoints[i];

            float distance = Vector3.Distance(
                player.position, swingPoints[i]);

            joints[i].spring = jointSpring;
            joints[i].damper = jointDamper;
            joints[i].massScale = jointMassScale;
            joints[i].maxDistance = distance * 0.8f;
            joints[i].minDistance = distance * 0.25f;

            grapplers[i].lineRenderer.positionCount = 2;
            currentGrapplePositions[i] = grapplers[i].gunTip.position;
        }

        CancelActiveGrapples();
        cmc.ResetRestrictions();

        cmc.activeSwinging = true;
    }

    public void StopSwing()
    {
        for (int i = 0; i < grapplers.Count; i++)
        {
            CheckIfStillSwinging(i);
            Destroy(joints[i]);
        }
    }

    private void CheckIfStillSwinging(int swingIndex)
    {
        activeSwings[swingIndex] = false;

        bool stillActive = false;
        foreach (bool active in activeSwings)
        { if (active) { stillActive = true; break; } }
        cmc.activeSwinging = stillActive;
    }

    #endregion

    #region Grappling
    private void StartGrapple(int grappleIndex)
    {
        if (grapplingCdTimer > 0) return;

        CancelActiveSwings();
        CancelAllGrapplesExcept(grappleIndex);

        // Case 1 - target point found
        if (predictionHits[grappleIndex].point != Vector3.zero)
        {
            Invoke(nameof(DelayedFreeze), 0.05f);

            activeGrapples[grappleIndex] = true;

            swingPoints[grappleIndex] = predictionHits[grappleIndex].point;

            StartCoroutine(ExecuteGrapple(grappleIndex));
        }

        // Case 2 - target point not found
        else
        {
            swingPoints[grappleIndex] = playerCam.position + playerCam.forward * maxGrappleDistance;

            StartCoroutine(StopGrapple(grappleIndex, grappleDelayTime));
        }

        grapplers[grappleIndex].lineRenderer.positionCount = 2;
        currentGrapplePositions[grappleIndex] = grapplers[grappleIndex].gunTip.position;
    }

    private void DelayedFreeze()
    { cmc.isFrozen = true; }

    private IEnumerator ExecuteGrapple(int grappleIndex)
    {
        yield return new WaitForSeconds(grappleDelayTime);

        cmc.isFrozen = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = swingPoints[grappleIndex].y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        cmc.JumpToPosition(swingPoints[grappleIndex], highestPointOnArc);
    }

    public IEnumerator StopGrapple(int grappleIndex, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        cmc.isFrozen = false; // may need to change, should only been when all grapples are inactive

        cmc.ResetRestrictions();

        activeGrapples[grappleIndex] = false;

        grapplingCdTimer = grapplingCd;
    }
    #endregion

    #region CancleAbilities
    public void CancelActiveGrapples()
    {
        StartCoroutine(StopGrapple(0));
        StartCoroutine(StopGrapple(1));
    }

    private void CancelAllGrapplesExcept(int grappleIndex)
    {
        for (int i = 0; i < amountOfSwingPoints; i++)
            if (i != grappleIndex) StartCoroutine(StopGrapple(i));
    }

    private void CancelActiveSwings()
    {
        StopSwing();
    }
    #endregion
}