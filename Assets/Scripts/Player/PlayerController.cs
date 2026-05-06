using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MetroidvaniaMVP.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(AbilityController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private AbilityController abilities;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Transform wallCheck;
        [SerializeField] private LayerMask groundLayers;

        [Header("Movement")]
        [SerializeField] private Key moveLeftKey = Key.A;
        [SerializeField] private Key moveRightKey = Key.D;
        [SerializeField] private Key jumpKey = Key.W;
        [SerializeField] private float moveSpeed = 7.5f;
        [SerializeField] private float groundAcceleration = 80f;
        [SerializeField] private float groundDeceleration = 70f;
        [SerializeField] private float airDeceleration = 5f;
        [SerializeField] private float jumpForce = 13f;
        [SerializeField] private float jumpCutMultiplier = 0.45f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Air Jumps")]
        [SerializeField] private int maxAirJumps = 1;

        [Header("Checks")]
        [SerializeField] private float groundCheckRadius = 0.12f;
        [SerializeField] private float wallCheckRadius = 0.28f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.22f;
        [SerializeField] private float dashCooldown = 0.45f;
        [SerializeField] private int maxAirDashes = 1;
        [SerializeField] private Key dashKey = Key.L;

        [Header("Wall Movement")]
        [SerializeField] private Vector2 wallJumpForce = new Vector2(6f, 13f);
        [SerializeField] private float wallJumpControlLock = 0.14f;

        [Header("Ledge Climb")]
        [SerializeField] private bool autoClimbLedges = true;
        [SerializeField, Range(0f, 1f)] private float ledgeRequiredTopBodyPercent = 0.2f;
        [SerializeField, Range(0f, 1f)] private float ledgeMaxTopBodyPercent = 0.65f;
        [SerializeField] private float ledgeStandForwardOffset = 0.65f;
        [SerializeField] private float ledgeStandClearance = 0.05f;
        [SerializeField] private float ledgeSurfaceProbeHeight = 0.35f;
        [SerializeField] private float ledgeClimbControlLockDuration = 0.5f;

        private float moveInput;
        private float coyoteCounter;
        private float jumpBufferCounter;
        private float dashCooldownCounter;
        private float wallJumpLockCounter;
        private float ledgeClimbLockCounter;
        private int airJumpsRemaining;
        private int airDashesRemaining;
        private bool isGrounded;
        private bool isTouchingWall;
        private bool isWallClinging;
        private bool canWallJumpFromWall;
        private bool isDashing;
        private bool isAirDashing;
        private float defaultGravityScale;

        public int FacingDirection { get; private set; } = 1;
        public bool IsDashing => isDashing;
        public bool IsActionLocked => isDashing || IsLedgeClimbLocked;
        private bool IsLedgeClimbLocked => ledgeClimbLockCounter > 0f;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            abilities = GetComponent<AbilityController>();
            bodyCollider = GetComponent<Collider2D>();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (abilities == null)
            {
                abilities = GetComponent<AbilityController>();
            }

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider2D>();
            }

            airJumpsRemaining = maxAirJumps;
            airDashesRemaining = maxAirDashes;
            defaultGravityScale = body.gravityScale;
        }

        private void Update()
        {
            ReadMovementInput();
            UpdateChecks();
            UpdateTimers();

            if (IsActionLocked)
            {
                jumpBufferCounter = 0f;
                return;
            }

            HandleFacing();
            HandleJumpInput();
            HandleDashInput();
            HandleJumpCut();
        }

        private void FixedUpdate()
        {
            if (IsLedgeClimbLocked)
            {
                body.gravityScale = 0f;
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (wallJumpLockCounter <= 0f && !isDashing)
            {
                ApplyHorizontalMovement();
            }

            if (isDashing)
            {
                float verticalVelocity = isAirDashing ? 0f : body.linearVelocity.y;
                body.linearVelocity = new Vector2(FacingDirection * dashSpeed, verticalVelocity);
            }
        }

        public void Configure(Rigidbody2D newBody, AbilityController newAbilities, Transform newGroundCheck, Transform newWallCheck, LayerMask newGroundLayers, Collider2D newBodyCollider = null)
        {
            body = newBody;
            abilities = newAbilities;
            bodyCollider = newBodyCollider;
            groundCheck = newGroundCheck;
            wallCheck = newWallCheck;
            groundLayers = newGroundLayers;
        }

        public void ResetMotion()
        {
            StopAllCoroutines();
            isDashing = false;
            isAirDashing = false;
            canWallJumpFromWall = false;
            wallJumpLockCounter = 0f;
            ledgeClimbLockCounter = 0f;
            dashCooldownCounter = 0f;
            airJumpsRemaining = maxAirJumps;
            airDashesRemaining = maxAirDashes;
            body.gravityScale = defaultGravityScale;
            body.linearVelocity = Vector2.zero;
        }

        private void ReadMovementInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = 0f;
                return;
            }

            float left = IsKeyPressed(keyboard, moveLeftKey) ? -1f : 0f;
            float right = IsKeyPressed(keyboard, moveRightKey) ? 1f : 0f;
            moveInput = left + right;
        }

        private void ApplyHorizontalMovement()
        {
            float currentX = body.linearVelocity.x;
            float targetX = 0f;
            float acceleration = isGrounded ? groundDeceleration : airDeceleration;

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                targetX = moveInput * moveSpeed;

                bool preservingAirMomentum = !isGrounded
                    && Mathf.Sign(currentX) == Mathf.Sign(moveInput)
                    && Mathf.Abs(currentX) > moveSpeed;

                if (preservingAirMomentum)
                {
                    targetX = currentX;
                }

                if (!isGrounded)
                {
                    body.linearVelocity = new Vector2(targetX, body.linearVelocity.y);
                    return;
                }

                acceleration = groundAcceleration;
            }

            float newX = Mathf.MoveTowards(currentX, targetX, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(newX, body.linearVelocity.y);
        }

        private void UpdateChecks()
        {
            isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayers);
            isTouchingWall = wallCheck != null && Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, groundLayers);
            canWallJumpFromWall = abilities.CanWallJump && isTouchingWall && !isGrounded && IsHoldingTowardWall();
            isWallClinging = false;

            if (canWallJumpFromWall)
            {
                isWallClinging = TryAutoClimbLedge();
            }

            if (isGrounded || canWallJumpFromWall)
            {
                airJumpsRemaining = maxAirJumps;
            }

            if (isGrounded || canWallJumpFromWall)
            {
                airDashesRemaining = maxAirDashes;
            }
        }

        private bool IsHoldingTowardWall()
        {
            return Mathf.Abs(moveInput) > 0.01f && Mathf.Sign(moveInput) == FacingDirection;
        }

        private bool TryAutoClimbLedge()
        {
            if (!autoClimbLedges || bodyCollider == null || wallCheck == null)
            {
                return false;
            }

            Bounds bounds = bodyCollider.bounds;
            float maxProbeBodyPercent = Mathf.Max(ledgeRequiredTopBodyPercent, ledgeMaxTopBodyPercent);

            Vector2 surfaceProbeStart = new Vector2(
                wallCheck.position.x + FacingDirection * ledgeStandForwardOffset,
                bounds.max.y + ledgeSurfaceProbeHeight);
            float surfaceProbeDistance = bounds.size.y * maxProbeBodyPercent + ledgeSurfaceProbeHeight + ledgeStandClearance;
            RaycastHit2D surfaceHit = Physics2D.Raycast(surfaceProbeStart, Vector2.down, surfaceProbeDistance, groundLayers);

            if (!surfaceHit.collider || Vector2.Dot(surfaceHit.normal, Vector2.up) < 0.9f)
            {
                return false;
            }

            float topBodyPercentAboveLedge = (bounds.max.y - surfaceHit.point.y) / bounds.size.y;
            if (topBodyPercentAboveLedge < ledgeRequiredTopBodyPercent || topBodyPercentAboveLedge > ledgeMaxTopBodyPercent)
            {
                return false;
            }

            Vector2 targetCenter = new Vector2(
                surfaceProbeStart.x,
                surfaceHit.point.y + bounds.extents.y + ledgeStandClearance);
            Vector2 clearanceSize = new Vector2(bounds.size.x * 0.9f, bounds.size.y * 0.9f);

            if (Physics2D.OverlapBox(targetCenter, clearanceSize, 0f, groundLayers))
            {
                return false;
            }

            Vector2 centerDelta = targetCenter - (Vector2)bounds.center;
            transform.position += (Vector3)centerDelta;
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
            coyoteCounter = coyoteTime;
            wallJumpLockCounter = 0f;
            ledgeClimbLockCounter = ledgeClimbControlLockDuration;
            isGrounded = true;
            isWallClinging = false;
            airJumpsRemaining = maxAirJumps;
            airDashesRemaining = maxAirDashes;
            return true;
        }

        private void UpdateTimers()
        {
            coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
            jumpBufferCounter -= Time.deltaTime;
            dashCooldownCounter -= Time.deltaTime;
            wallJumpLockCounter -= Time.deltaTime;

            float previousLedgeClimbLockCounter = ledgeClimbLockCounter;
            ledgeClimbLockCounter -= Time.deltaTime;
            if (previousLedgeClimbLockCounter > 0f && ledgeClimbLockCounter <= 0f && !isAirDashing)
            {
                body.gravityScale = defaultGravityScale;
            }
        }

        private void HandleFacing()
        {
            if (isDashing || Mathf.Abs(moveInput) < 0.01f)
            {
                return;
            }

            int newFacingDirection = moveInput > 0f ? 1 : -1;
            if (newFacingDirection == FacingDirection)
            {
                return;
            }

            FacingDirection = newFacingDirection;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * FacingDirection;
            transform.localScale = scale;
        }

        private void HandleJumpInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasKeyPressedThisFrame(keyboard, jumpKey))
            {
                jumpBufferCounter = jumpBufferTime;
            }

            if (jumpBufferCounter <= 0f)
            {
                return;
            }

            if (coyoteCounter > 0f)
            {
                Jump();
            }
            else if (canWallJumpFromWall && abilities.CanWallJump)
            {
                WallJump();
            }
            else if (airJumpsRemaining > 0)
            {
                AirJump();
            }
        }

        private void HandleDashInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !abilities.CanDash || isDashing || dashCooldownCounter > 0f)
            {
                return;
            }

            bool dashPressed = WasKeyPressedThisFrame(keyboard, dashKey);
            if (!dashPressed)
            {
                return;
            }

            if (!isGrounded && airDashesRemaining <= 0)
            {
                return;
            }

            if (dashPressed)
            {
                StartCoroutine(DashRoutine());
            }
        }

        private void HandleJumpCut()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasKeyReleasedThisFrame(keyboard, jumpKey) && body.linearVelocity.y > 0f)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * jumpCutMultiplier);
                coyoteCounter = 0f;
            }
        }

        private void Jump()
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        private void AirJump()
        {
            airJumpsRemaining--;
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        private void WallJump()
        {
            int jumpDirection = -FacingDirection;
            FacingDirection = jumpDirection;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * FacingDirection;
            transform.localScale = scale;

            wallJumpLockCounter = wallJumpControlLock;
            body.linearVelocity = new Vector2(jumpDirection * wallJumpForce.x, wallJumpForce.y);
            airJumpsRemaining = maxAirJumps;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        private IEnumerator DashRoutine()
        {
            isDashing = true;
            isAirDashing = !isGrounded;
            dashCooldownCounter = dashCooldown;

            if (isAirDashing)
            {
                airDashesRemaining = Mathf.Max(0, airDashesRemaining - 1);
                body.gravityScale = 0f;
            }

            float verticalVelocity = isAirDashing ? 0f : body.linearVelocity.y;
            body.linearVelocity = new Vector2(FacingDirection * dashSpeed, verticalVelocity);

            yield return new WaitForSeconds(dashDuration);

            if (isAirDashing)
            {
                float postDashVelocity = Mathf.Abs(moveInput) > 0.01f ? moveInput * moveSpeed : 0f;
                body.linearVelocity = new Vector2(postDashVelocity, body.linearVelocity.y);
            }

            body.gravityScale = defaultGravityScale;
            isAirDashing = false;
            isDashing = false;
        }

        private static bool IsKeyPressed(Keyboard keyboard, Key key)
        {
            return keyboard[key].isPressed;
        }

        private static bool WasKeyPressedThisFrame(Keyboard keyboard, Key key)
        {
            return keyboard[key].wasPressedThisFrame;
        }

        private static bool WasKeyReleasedThisFrame(Keyboard keyboard, Key key)
        {
            return keyboard[key].wasReleasedThisFrame;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            if (groundCheck != null)
            {
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            Gizmos.color = Color.cyan;
            if (wallCheck != null)
            {
                Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
            }
        }
    }
}
