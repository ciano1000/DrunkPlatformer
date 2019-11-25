using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControl: MonoBehaviour
{
   
    public float targetSpeed = 10.0f;
    public float speedSmoothTime = 0.1f;
    public float jumpSpeed = 8f;
    public float maxLeanAngle = 20f;
    public float maxSlopeAngle = 60f;
    public AnimationCurve leanCurve;

    private float speedSmoothVelocity;
    private float currentSpeed;
    private float verticalSpeed = 0;
    private Vector3 velocity;
    private Quaternion currentRotation;

  
    private Animator myAnimator;

    public int numHorizontalRays = 4;
    public LayerMask collisionMask;
    private float horizontalRaySpacing;
    CapsuleCollider capsuleCollider;

    RaycastPositions raycastPositions;
    CollisionInfo collisions;

    private GameManager gameManager;
    private bool isGameOver;

    const float skinWidth = .015f;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        isGameOver = gameManager.isGameOver;
        myAnimator = GetComponent<Animator>();
        capsuleCollider = gameObject.GetComponent<CapsuleCollider>();

        velocity = new Vector3();
        CalculateRaySpacing();
        Physics.IgnoreLayerCollision(9, 10);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        isGameOver = gameManager.isGameOver;
        if (!isGameOver)
        {
            if (collisions.above || collisions.below)
            {
                verticalSpeed = 0;
                velocity.y = 0;
            }

            float input = Input.GetAxisRaw("Horizontal");
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed * input, ref speedSmoothVelocity, speedSmoothTime);
            velocity.z = currentSpeed;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;


            float animationSpeedPercent = currentSpeed / targetSpeed;
            myAnimator.SetFloat("speedPercent", animationSpeedPercent, speedSmoothTime, Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space) && collisions.below)
            {
                verticalSpeed = jumpSpeed;
                myAnimator.SetTrigger("Jump");
            }

            velocity.y = verticalSpeed;
            Move(velocity * Time.deltaTime);
        }
        else if (gameManager.isLevelOver)
        {
            myAnimator.SetTrigger("Jump");
        }
        else
        {
            velocity.y = 0;
            myAnimator.enabled = false;
        }
       
    }

    public void Move(Vector3 velocity)
    {
        collisions.Reset();
        UpdateRaycastPositions();
        if (velocity.z != 0)
        {
            HorizontalCollisions(ref velocity);
        }
        if (velocity.y != 0)
        {
            VerticalCollisions(ref velocity);

        }
        transform.Translate(velocity);
    }

    void VerticalCollisions(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + (skinWidth *2);

        RaycastHit hit;

        Vector3 raycastOrigin = (directionY == -1) ? raycastPositions.colliderBottom : raycastPositions.colliderTop;

        if (Physics.SphereCast(raycastOrigin, capsuleCollider.radius+skinWidth, transform.up * directionY, out hit, rayLength, collisionMask))
        {
            velocity.y = (hit.distance - skinWidth) * directionY;
            rayLength = hit.distance;
            collisions.above = directionY == 1;
            collisions.below = directionY == -1;
        }

        RaycastHit rearHit;
        RaycastHit frontHit;
        bool rearCollision = Physics.Raycast(raycastPositions.rearSlope, transform.up * -1, out rearHit, capsuleCollider.radius * 2, collisionMask);
        bool frontCollision = Physics.Raycast(raycastPositions.frontSlope, transform.up * -1, out frontHit, capsuleCollider.radius * 2, collisionMask);

        collisions.rightSlope = frontCollision;
        collisions.leftSlope = rearCollision;

        Debug.DrawRay(raycastPositions.rearSlope, (transform.up * -1) * capsuleCollider.radius * 2);
        Debug.DrawRay(raycastPositions.frontSlope, (transform.up * -1) * capsuleCollider.radius * 2);

        if (rearCollision && frontCollision)
        {
           

            float rearSlope = Vector3.Angle(rearHit.normal, Vector3.up);
            float frontSlope = Vector3.Angle(frontHit.normal, Vector3.up);

            float slopeToVerify = (Mathf.Sign(velocity.z) == 1) ? frontSlope : rearSlope;

            if (!Mathf.Approximately(rearSlope, frontSlope) && slopeToVerify <= maxSlopeAngle && collisions.below)
            {
                Vector3 upright = Vector3.Cross(transform.right, -(frontHit.point - rearHit.point).normalized);
                transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, upright));
            }
            else if (Mathf.Approximately(rearSlope, frontSlope) && collisions.below)
            {
                Vector3 upright = Vector3.Cross(transform.right, -(frontHit.point - rearHit.point).normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Vector3.Cross(transform.right, upright)), 60f * Time.deltaTime);
            }
        }
        else if (rearCollision || frontCollision)
        { 

            RaycastHit tempHit = (rearCollision) ? rearHit : frontHit;
            float slope = Vector3.Angle(tempHit.normal, Vector3.up);
            if (!collisions.below)
            {
                //Vector3 upright = Vector3.Cross(transform.right, -(tempHit.point).normalized);
                //transform.rotation = Quaternion.LookRotation(Vector3.Cross(transform.right, upright));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(new Vector3(0f, 0f, 0f)), 60f * Time.deltaTime);
                velocity.y = (tempHit.distance - skinWidth) * directionY;
                rayLength = hit.distance;
                collisions.below = true;
            }
        }
        else if(!rearCollision && !frontCollision && !collisions.below)
        {
            RaycastHit groundProbe;

            if(Physics.Raycast(raycastPositions.colliderBottom,Vector3.down,out groundProbe, Mathf.Infinity, collisionMask))
            {
                transform.rotation = Quaternion.Euler(new Vector3(Vector3.Angle(groundProbe.normal, Vector3.up),0,0));
            }           
        }

    }

    void HorizontalCollisions(ref Vector3 velocity)
    {
        float directionZ = Mathf.Sign(velocity.z);
        float rayLength = Mathf.Abs(velocity.z) + skinWidth;
        for (int i = 0; i < numHorizontalRays; i++)
        {
            Vector3 rayOrigin = (directionZ == -1) ? raycastPositions.bottomLeft : raycastPositions.bottomRight;
            rayOrigin += transform.up * (horizontalRaySpacing * i);

            RaycastHit hit;

            if (Physics.Raycast(rayOrigin, transform.forward * directionZ, out hit, rayLength, collisionMask))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                if (slopeAngle > maxSlopeAngle)
                {
                    velocity.z = (hit.distance - skinWidth) * directionZ;
                    rayLength = hit.distance;
                    collisions.left = directionZ == -1;
                    collisions.right = directionZ == 1;
                }
            }
            Debug.DrawRay(rayOrigin, Vector3.forward * directionZ * rayLength);

        }
    }

    void CalculateRaySpacing()
    {
        numHorizontalRays = Mathf.Clamp(numHorizontalRays, 2, int.MaxValue);
        horizontalRaySpacing = capsuleCollider.height / (numHorizontalRays - 1);
    }

    void UpdateRaycastPositions()
    {

        Vector3 horizontalRadiusVector = capsuleCollider.radius * transform.forward;
        Vector3 verticalRadiusVector = capsuleCollider.radius * transform.up;
        Vector3 heightVector = (capsuleCollider.height / 2) * transform.up;
        Vector3 colliderPos = transform.TransformPoint(capsuleCollider.center) - heightVector;
        raycastPositions.rearSlope = colliderPos - horizontalRadiusVector + verticalRadiusVector;
        raycastPositions.frontSlope = colliderPos + horizontalRadiusVector + verticalRadiusVector;
        raycastPositions.bottomLeft = colliderPos - horizontalRadiusVector;
        raycastPositions.bottomRight = colliderPos + horizontalRadiusVector;
        raycastPositions.colliderBottom = colliderPos + verticalRadiusVector;
        raycastPositions.colliderTop = colliderPos + (2 * heightVector);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(raycastPositions.bottomLeft, 0.05f);
        Gizmos.DrawSphere(raycastPositions.bottomRight, 0.05f);

    }

    struct RaycastPositions
    {
        public Vector3 rearSlope, frontSlope;
        public Vector3 bottomLeft, bottomRight;
        public Vector3 colliderBottom, colliderTop;
    }

    public struct CollisionInfo
    {
        public bool climbingSlope;
        public bool above, below;
        public bool left, right;
        public bool leftSlope, rightSlope;

        public float slopeAngle, slopeAngleOld;

        public Vector3 forward;

        public void Reset()
        {
            above = below = false;
            right = left = false;
            climbingSlope = false;
            leftSlope = false;
            rightSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
            forward = Vector3.zero;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
     if(collision.collider.name == "van")
        {
            StartCoroutine(gameManager.HandleGameOver());
        }   
    }
}
