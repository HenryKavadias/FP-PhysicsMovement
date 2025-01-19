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

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cmc = GetComponent<CharacterMovementController>();

        ListSetup();
    }

    private void ListSetup()
    {
        springs = new List<Spring>();

        predictionHits = new List<RaycastHit>();

        swingPoints = new List<Vector3>();
        joints = new List<SpringJoint>();

        activeSwings = new List<bool>();
        activeGrapples = new List<bool>();

        currentGrapplePositions = new List<Vector3>();

        for (int i = 0; i < grapplers.Count; i++)
        {
            predictionHits.Add(new RaycastHit());
            joints.Add(null);
            swingPoints.Add(Vector3.zero);
            activeSwings.Add(false);
            activeGrapples.Add(false);
            currentGrapplePositions.Add(Vector3.zero);

            springs.Add(new Spring());
            springs[i].Target = 0;
        }
    }

    public void CreatePredictionPoints()
    {
        predictionPoints = new List<Transform>();

        for (int i = 0; i < grapplers.Count; i++)
        {
            GameObject pv = Instantiate(predictionVisual);
            predictionPoints.Add(pv.transform); 
        }
    }

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

        if (alternateInput)
        { if (inputChange == 0) { StartGrapple(); } }
        else
        { if (inputChange == 0) { StartSwing(); } }

        if (inputChange == 1) { StopSwing(); }
    }

    private void Update()
    {
        ManageInputs();

        if (enableSwingingWithForces)
        {
            for (int i = 0; i < grapplers.Count; i++)
            {
                if (joints[i])
                {
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

    [Header("Rope Visuals")]
    public int ropeQuality = 2;
    public float strength;
    public float damper;
    public float velocity;
    public float waveCount;
    public float waveHeight;
    public AnimationCurve affectCurve;
    private List<Vector3> currentGrapplePositions;
    private List<Spring> springs;

    private void DrawRope()
    {
        for (int i = 0; i < grapplers.Count; i++)
        {
            if (!activeSwings[i] && !activeGrapples[i])
            {
                currentGrapplePositions[i] = grapplers[i].gunTip.position;
                springs[i].Reset();

                if (grapplers[i].lineRenderer.positionCount > 0)
                { grapplers[i].lineRenderer.positionCount = 0; }

                continue;
            }

            if (grapplers[i].lineRenderer.positionCount == 0)
            {
                springs[i].Velocity = velocity;
                grapplers[i].lineRenderer.positionCount = ropeQuality + 1;
            }

            springs[i].Damper = damper;
            springs[i].Strength = strength;
            springs[i].Update(Time.deltaTime);

            Vector3 grapplePoint = swingPoints[i];
            Vector3 gunTipPosition = grapplers[i].gunTip.position;
            Vector3 up = Quaternion.LookRotation((grapplePoint - gunTipPosition).normalized) * Vector3.up;

            currentGrapplePositions[i] = Vector3.Lerp(
                currentGrapplePositions[i], grapplePoint, Time.deltaTime * 12f);

            for (int j = 0; j < ropeQuality + 1; j++)
            {
                float deltaRange = j / (float)ropeQuality;
                Vector3 offset =
                    up * waveHeight *
                    Mathf.Sin(deltaRange * waveCount * Mathf.PI) *
                    springs[i].Value * affectCurve.Evaluate(deltaRange);

                grapplers[i].lineRenderer.SetPosition(j,
                    Vector3.Lerp(gunTipPosition, currentGrapplePositions[i], deltaRange) + offset);
            }
        }
    }

    #region OdmGear
    private Vector3 pullPoint;

    private void FindPullPoint(List<bool> activeHooks)
    {
        Vector3 directionSum = Vector3.zero;
        int numOfActiveSwings = 0;

        for (int i = 0; i < activeHooks.Count; i++)
        {
            if (!activeHooks[i]) { continue; }
            numOfActiveSwings++;
            directionSum += swingPoints[i];
        }
        if (numOfActiveSwings <= 0 || directionSum == Vector3.zero)
        { pullPoint = Vector3.zero; }

        pullPoint = directionSum / numOfActiveSwings;
    }

    private void OdmGearMovement()
    {
        FindPullPoint(activeSwings);

        if (pullPoint == Vector3.zero) { return; }

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
        for (int i = 0; i < grapplers.Count; i++)
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

            //SetGrappleRopeValues(i);
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
    private void StartGrapple()//(int grappleIndex)
    {
        if (grapplingCdTimer > 0) return;

        CancelActiveSwings();
        CancelActiveGrapples(grappleDelayTime + 0.1f);// CancelAllGrapplesExcept(grappleIndex);

        bool canGrapple = false;

        for (int i = 0; i < grapplers.Count; i++)
        {
            if (predictionHits[i].point == Vector3.zero) continue;

            canGrapple = true;

            activeGrapples[i] = true;

            swingPoints[i] = predictionHits[i].point;

            //SetGrappleRopeValues(i);
        }

        // ExecuteGrapple if at least one point is valid
        if (canGrapple)
        {
            FindPullPoint(activeGrapples);

            Invoke(nameof(DelayedFreeze), 0.05f);

            StartCoroutine(ExecuteGrapple());
        }
    }

    private void DelayedFreeze()
    { cmc.isFrozen = true; }

    private IEnumerator ExecuteGrapple()
    {
        yield return new WaitForSeconds(grappleDelayTime);

        cmc.isFrozen = false;

        if (pullPoint != Vector3.zero) {
            Vector3 lowestPoint = new Vector3(
                transform.position.x, transform.position.y - 1f, transform.position.z);

            float grapplePointRelativeYPos = pullPoint.y - lowestPoint.y;
            float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

            if (grapplePointRelativeYPos < 0) 
            { highestPointOnArc = overshootYAxis; }

            cmc.JumpToPosition(pullPoint, highestPointOnArc);
        }
    }

    public IEnumerator StopGrapple(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        cmc.isFrozen = false;
        cmc.ResetRestrictions();

        for (int i = 0; i < activeGrapples.Count; i++)
        { activeGrapples[i] = false; }
        
        grapplingCdTimer = grapplingCd;
    }
    #endregion

    #region CancleAbilities
    public void CancelActiveGrapples(float delay = 0f)
    {
        StartCoroutine(StopGrapple(delay));
    }

    private void CancelActiveSwings()
    {
        StopSwing();
    }
    #endregion
}
