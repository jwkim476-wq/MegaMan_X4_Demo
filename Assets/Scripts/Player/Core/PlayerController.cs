using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(Animator))]
public class MMXPlayer : MonoBehaviour
{
    // =========================================================
    // 1. 기본 움직임
    // =========================================================
    [Header("1. 기본 움직임")]
    public float moveSpeed = 5f;
    public float jumpForce = 13f;
    public float gravity = 40f;
    public float maxFallSpeed = 20f;

    [Header("2. 대쉬 설정")]
    public float dashSpeed = 11f;
    public float dashJumpForce = 13f;
    public float dashDuration = 0.5f;
    public float dashCooldown = 0.15f;
    [Range(0, 1)] public float dashBrakeTime = 0.8f;

    [Header("3. 벽 액션")]
    public float wallSlideSpeed = 4f;
    public Vector2 wallJumpPower = new Vector2(10f, 14f);
    public Vector2 dashWallJumpPower = new Vector2(14f, 16f);
    public float wallKickTime = 0.12f;

    [Header("4. 충돌 감지")]
    public LayerMask groundLayer;
    public float skinWidth = 0.015f;
    public float maxSlopeAngle = 55f;

    // =========================================================
    // 5. 이동 VFX
    // =========================================================
    [Header("5. 이동 VFX")]
    public Color ghostColor = new Color(0.2f, 0.4f, 1f, 0.9f);
    public float ghostRate = 0.03f;
    public float ghostFadeSpeed = 2.0f;

    [Space(5)]
    public GameObject dashJetEffect;
    public GameObject wallSlideEffect;
    public GameObject dashDustPrefab;
    public GameObject wallKickSparkPrefab;

    [Header("VFX 위치 미세조정")]
    public Vector3 dashJetOffset = new Vector3(-0.5f, -0.8f, 0);
    public Vector3 wallSlideOffset = new Vector3(0.5f, -0.8f, 0);
    public Vector3 dashDustOffset = new Vector3(0, -0.5f, 0);
    public Vector3 wallSparkOffset = new Vector3(0.3f, 0, 0);

    // =========================================================
    // 6. 공격 (Attack)
    // =========================================================
    [Header("6. 공격 (Attack & Charge)")]
    public Transform firePoint;

    [Header("총구 위치 미세조정")]
    public Vector3 standFirePos = new Vector3(0.6f, 0.1f, 0);
    public Vector3 runFirePos = new Vector3(0.7f, 0.05f, 0);
    public Vector3 dashFirePos = new Vector3(0.8f, -0.2f, 0);
    public Vector3 jumpFirePos = new Vector3(0.6f, 0.3f, 0);
    public Vector3 wallFirePos = new Vector3(0.6f, 0.3f, 0);

    [Header("공격 속도")]
    public float fireRate = 0.15f;

    [Header("프리팹 & 이펙트")]
    public GameObject bulletPrefab;
    public GameObject charged1Prefab;
    public GameObject charged2Prefab;

    [Tooltip("Lv1 파란색 차지 (자식)")]
    public GameObject chargeEffectLv1;
    [Tooltip("Lv2 혼합색 차지 (자식)")]
    public GameObject chargeEffectLv2;

    public GameObject muzzleFlashNormal;
    public GameObject muzzleFlashLv1;

    [Header("설정값")]
    public float lv1ChargeTime = 0.5f;
    public float lv2ChargeTime = 1.5f;
    public float shootAnimDuration = 0.3f;
    public float flashSpeed = 25f;
    public Color colorLv1 = new Color(0.4f, 0.8f, 1f);
    public Color colorLv2_A = new Color(0.2f, 0.5f, 1f);
    public Color colorLv2_B = new Color(0.4f, 1f, 0.4f);

    // ★ [추가] 인트로 시간 설정
    [Header("인트로 설정")]
    public float introDuration = 2.0f; // 이 시간 동안은 조작 불가능

    // =========================================================
    // 내부 변수
    // =========================================================
    BoxCollider2D boxCollider;
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    Vector2 velocity;
    Vector2 input;
    float direction = 1;

    bool isGrounded;
    bool isTouchingWall;
    bool isWallHit;
    int wallDir;

    bool isDashing;
    bool isDashJumping;
    bool isGhostJumping;
    bool isWallKicking;

    bool isCharging;
    float chargeTimer;
    float shootTimer;
    bool isShooting;
    float nextFireTime;

    bool wasGrounded;
    public bool isIntroPlaying = true;

    float dashTimer;
    float wallKickTimer;
    float ghostTimer;

    List<SpriteRenderer> ghostPool = new List<SpriteRenderer>();
    Animator chargeAnimator;

    // Hashes
    readonly int hashSpeed = Animator.StringToHash("Speed");
    readonly int hashYSpeed = Animator.StringToHash("YSpeed");
    readonly int hashIsGrounded = Animator.StringToHash("IsGrounded");
    readonly int hashIsDashing = Animator.StringToHash("IsDashing");
    readonly int hashIsWallSliding = Animator.StringToHash("IsWallSliding");
    readonly int hashIsShooting = Animator.StringToHash("IsShooting");
    readonly int hashChargeLevel = Animator.StringToHash("ChargeLevel");
    readonly int hashYInput = Animator.StringToHash("YInput");

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (dashJetEffect) { dashJetEffect.transform.localPosition = dashJetOffset; dashJetEffect.SetActive(false); }
        if (wallSlideEffect) { wallSlideEffect.transform.localPosition = wallSlideOffset; wallSlideEffect.SetActive(false); }
        if (chargeEffectLv1) chargeEffectLv1.SetActive(false);
        if (chargeEffectLv2) chargeEffectLv2.SetActive(false);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ★ [인트로 처리: 시간제 잠금]
        if (isIntroPlaying)
        {
            velocity.x = 0;
            ProcessGravity(dt);

            Vector2 gMove = velocity * dt;
            Move(ref gMove);
            transform.Translate(gMove);

            UpdateAnimation();

            // ★ 설정한 시간(introDuration)이 지나면 해제
            if (Time.timeSinceLevelLoad > introDuration)
            {
                isIntroPlaying = false;
            }

            // 인트로 중엔 모든 이펙트 끔
            if (dashJetEffect && dashJetEffect.activeSelf) dashJetEffect.SetActive(false);
            if (wallSlideEffect && wallSlideEffect.activeSelf) wallSlideEffect.SetActive(false);
            if (chargeEffectLv1) chargeEffectLv1.SetActive(false);
            if (chargeEffectLv2) chargeEffectLv2.SetActive(false);
            return;
        }

        ProcessInput(dt);
        CalculatePhysics(dt);

        Vector2 moveAmount = velocity * dt;
        wasGrounded = isGrounded;
        Move(ref moveAmount);

        transform.Translate(moveAmount);

        HandleGhostTrail(dt);
        UpdateAnimation();
        HandleEffects();
        HandleAttack();
        HandleCharacterBlinking();

        UpdateFirePointPosition();
    }

    void ProcessGravity(float dt) { if (isGrounded && velocity.y <= 0) velocity.y = -1f; else { velocity.y -= gravity * dt; if (velocity.y < -maxFallSpeed) velocity.y = -maxFallSpeed; } }

    void ProcessInput(float dt)
    {
        if (shootTimer > 0) { shootTimer -= dt; if (shootTimer <= 0) isShooting = false; }

        if (Input.GetKey(KeyCode.C))
        {
            isCharging = true;
            chargeTimer += dt;
        }
        else if (Input.GetKeyUp(KeyCode.C))
        {
            Fire();
            isCharging = false;
            chargeTimer = 0;
            sr.color = Color.white;
        }

        if (isWallKicking) { wallKickTimer -= dt; if (wallKickTimer <= 0) isWallKicking = false; return; }

        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        if (isDashing && input.x != 0 && Mathf.Sign(input.x) != direction) { isDashing = false; dashTimer = 0; }

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (isTouchingWall && !isGrounded) PerformWallKick();
            else if (isGrounded)
            {
                velocity.y = jumpForce; isGrounded = false;
                bool zHeld = Input.GetKey(KeyCode.Z);
                bool moving = input.x != 0;
                if (isDashing || (zHeld && moving)) { isDashJumping = true; velocity.y = dashJumpForce; if (moving) direction = Mathf.Sign(input.x); }
                else if (zHeld) { isGhostJumping = true; velocity.y = dashJumpForce; }
                isDashing = false;
            }
        }
        if (Input.GetKeyUp(KeyCode.X) && velocity.y > 0) velocity.y *= 0.5f;

        if (Input.GetKeyDown(KeyCode.Z) && isGrounded && !isDashing)
        {
            isDashing = true; dashTimer = dashDuration;
            if (input.x != 0) direction = Mathf.Sign(input.x);

            if (dashDustPrefab)
            {
                Vector3 spawnPos = transform.position + new Vector3(dashDustOffset.x * direction, dashDustOffset.y, 0);
                GameObject dust = Instantiate(dashDustPrefab, spawnPos, Quaternion.identity);
                Vector3 s = dust.transform.localScale; s.x = direction; dust.transform.localScale = s;
                dust.SetActive(true);
            }
        }
        if (Input.GetKeyUp(KeyCode.Z) && isDashing) { isDashing = false; if (anim.HasState(0, Animator.StringToHash("Dash"))) anim.Play("Dash", 0, dashBrakeTime); }
    }

    void Fire()
    {
        if (chargeTimer < 0.2f && Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;

        float chargeValue = 0f;
        int level = 0;
        if (chargeTimer >= lv2ChargeTime) { chargeValue = 1.0f; level = 2; }
        else if (chargeTimer >= lv1ChargeTime) { chargeValue = 0.5f; level = 1; }
        anim.SetFloat(hashChargeLevel, chargeValue);

        GameObject prefab = bulletPrefab;
        if (level == 1) prefab = charged1Prefab;
        else if (level == 2) prefab = charged2Prefab;

        float shootDir = direction;
        // 벽 타기 중이면 반대로 발사
        bool isSliding = isTouchingWall && !isGrounded && velocity.y < 0;
        if (isSliding) shootDir = -wallDir;

        if (prefab && firePoint)
        {
            Quaternion rot = (shootDir == -1) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
            GameObject b = Instantiate(prefab, firePoint.position, rot);
            b.SetActive(true);
        }

        GameObject flashPrefab = null;
        if (level == 0) flashPrefab = muzzleFlashNormal;
        else if (level == 1) flashPrefab = muzzleFlashLv1;

        if (flashPrefab && firePoint)
        {
            Quaternion flashRot = (shootDir == -1) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
            GameObject flash = Instantiate(flashPrefab, firePoint.position, flashRot);
            flash.transform.SetParent(firePoint);
            flash.SetActive(true);
        }

        isShooting = true;
        shootTimer = shootAnimDuration;

        // ★ [확인] anim.Play() 코드 완전히 제거됨 (연타 끊김 해결)
    }

    void HandleAttack()
    {
        if (isCharging)
        {
            // 이펙트 위치 고정
            if (chargeEffectLv1) chargeEffectLv1.transform.localPosition = Vector3.zero;
            if (chargeEffectLv2) chargeEffectLv2.transform.localPosition = Vector3.zero;

            if (chargeTimer >= lv2ChargeTime)
            {
                if (chargeEffectLv1) chargeEffectLv1.SetActive(false);
                if (chargeEffectLv2 && !chargeEffectLv2.activeSelf) chargeEffectLv2.SetActive(true);
            }
            else if (chargeTimer >= lv1ChargeTime)
            {
                if (chargeEffectLv1 && !chargeEffectLv1.activeSelf) chargeEffectLv1.SetActive(true);
                if (chargeEffectLv2) chargeEffectLv2.SetActive(false);
            }
            else
            {
                if (chargeEffectLv1) chargeEffectLv1.SetActive(false);
                if (chargeEffectLv2) chargeEffectLv2.SetActive(false);
            }
        }
        else
        {
            if (chargeEffectLv1) chargeEffectLv1.SetActive(false);
            if (chargeEffectLv2) chargeEffectLv2.SetActive(false);
        }
    }

    void HandleCharacterBlinking()
    {
        if (isCharging)
        {
            float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
            if (chargeTimer >= lv2ChargeTime) sr.color = Color.Lerp(colorLv2_A, colorLv2_B, t);
            else if (chargeTimer >= lv1ChargeTime) sr.color = Color.Lerp(Color.white, colorLv1, t);
            else sr.color = Color.white;
        }
        else
        {
            sr.color = Color.white;
        }
    }

    void CalculatePhysics(float dt) { if ((isGrounded && velocity.y <= 0) || isWallHit) { isDashJumping = false; isGhostJumping = false; } if (isDashing && !isGrounded) isDashing = false; if (isWallKicking) { } else if (isDashing) { dashTimer -= dt; if (dashTimer <= 0 || (isWallHit && !isGrounded)) isDashing = false; velocity.x = direction * dashSpeed; } else if (isDashJumping) { if (input.x == 0) velocity.x = 0; else { direction = Mathf.Sign(input.x); velocity.x = direction * dashSpeed; } if (isWallHit) isDashJumping = false; } else velocity.x = input.x * moveSpeed; if (!isWallKicking && velocity.x != 0) { if (!isDashing) { direction = Mathf.Sign(velocity.x); transform.localScale = new Vector3(direction, 1, 1); } } if (isDashing) velocity.y = 0; else if (isTouchingWall && !isGrounded && velocity.y < 0 && input.x == wallDir) { velocity.y = -wallSlideSpeed; isDashJumping = false; isGhostJumping = false; } else { velocity.y -= gravity * dt; if (velocity.y < -maxFallSpeed) velocity.y = -maxFallSpeed; } }
    void PerformWallKick() { isWallKicking = true; wallKickTimer = wallKickTime; isDashing = false; bool isDashKick = Input.GetKey(KeyCode.Z); Vector2 power = isDashKick ? dashWallJumpPower : wallJumpPower; velocity.x = -wallDir * power.x; velocity.y = power.y; direction = -wallDir; transform.localScale = new Vector3(direction, 1, 1); if (isDashKick) { isDashJumping = true; ghostTimer = 0; SpawnGhost(); } if (wallKickSparkPrefab) { Vector3 spawnPos = transform.position + new Vector3(wallSparkOffset.x * wallDir, wallSparkOffset.y, 0); Quaternion rot = (wallDir == 1) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity; GameObject spark = Instantiate(wallKickSparkPrefab, spawnPos, rot); spark.SetActive(true); } }
    void Move(ref Vector2 moveAmount) { isGrounded = false; isWallHit = false; isTouchingWall = false; CheckWallSensor(); if ((moveAmount.y <= 0 && wasGrounded) || (isDashing && wasGrounded)) DescendSlope(ref moveAmount); if (moveAmount.x != 0) { float dirX = Mathf.Sign(moveAmount.x); float dist = Mathf.Abs(moveAmount.x) + skinWidth; Vector2 boxSize = boxCollider.size; boxSize.x -= skinWidth * 2; boxSize.y -= skinWidth * 2; RaycastHit2D hit = Physics2D.BoxCast((Vector2)transform.position + boxCollider.offset, boxSize, 0, Vector2.right * dirX, dist, groundLayer); if (hit) { float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); if (slopeAngle <= maxSlopeAngle) ClimbSlope(ref moveAmount, slopeAngle); else { moveAmount.x = (hit.distance - skinWidth) * dirX; isWallHit = true; } } } float dirY = (moveAmount.y > 0) ? 1 : -1; if (moveAmount.y == 0) dirY = -1; float distY = Mathf.Abs(moveAmount.y) + skinWidth + 0.05f; Vector2 vBoxSize = boxCollider.size; vBoxSize.x -= skinWidth * 2; RaycastHit2D vHit = Physics2D.BoxCast((Vector2)transform.position + boxCollider.offset, vBoxSize, 0, Vector2.up * dirY, distY, groundLayer); if (vHit) { float realDist = vHit.distance - skinWidth; if (dirY == -1) { if (realDist <= Mathf.Abs(moveAmount.y) + 0.05f) { moveAmount.y = realDist * dirY; isGrounded = true; if (!isDashing) velocity.y = 0; } } else { if (realDist < Mathf.Abs(moveAmount.y)) { moveAmount.y = realDist * dirY; velocity.y = 0; } } } }
    void ClimbSlope(ref Vector2 moveAmount, float slopeAngle) { float moveDistance = Mathf.Abs(moveAmount.x); float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance; if (moveAmount.y <= climbVelocityY) { moveAmount.y = climbVelocityY; moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x); isGrounded = true; } }
    void DescendSlope(ref Vector2 moveAmount) { float maxDescendLen = Mathf.Abs(moveAmount.x) + skinWidth + 0.5f; Vector2 origin = (Vector2)transform.position + boxCollider.offset; Vector2 rayOriginL = origin + new Vector2(-boxCollider.size.x / 2 + skinWidth, -boxCollider.size.y / 2); Vector2 rayOriginR = origin + new Vector2(boxCollider.size.x / 2 - skinWidth, -boxCollider.size.y / 2); RaycastHit2D hitL = Physics2D.Raycast(rayOriginL, Vector2.down, maxDescendLen, groundLayer); RaycastHit2D hitR = Physics2D.Raycast(rayOriginR, Vector2.down, maxDescendLen, groundLayer); RaycastHit2D hit = hitL ? hitL : hitR; if (hit) { float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle) { if (Mathf.Sign(hit.normal.x) == Mathf.Sign(moveAmount.x)) { float moveDistance = Mathf.Abs(moveAmount.x); float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance; moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x); moveAmount.y -= descendVelocityY; isGrounded = true; } } } }
    void CheckWallSensor() { Vector2 size = boxCollider.size; size.x += 0.2f; size.y *= 0.8f; Collider2D hit = Physics2D.OverlapBox((Vector2)transform.position + boxCollider.offset, size, 0, groundLayer); if (hit) { wallDir = (hit.transform.position.x > transform.position.x) ? 1 : -1; isTouchingWall = true; } }
    void HandleEffects() { if (dashJetEffect) { bool showJet = isDashing; dashJetEffect.transform.localPosition = dashJetOffset; dashJetEffect.SetActive(showJet); } if (wallSlideEffect) { wallSlideEffect.transform.localPosition = wallSlideOffset; bool isSliding = isTouchingWall && !isGrounded && (input.x == wallDir) && velocity.y < 0; if (isSliding != wallSlideEffect.activeSelf) wallSlideEffect.SetActive(isSliding); } }
    void HandleGhostTrail(float dt) { if (isDashing || isDashJumping || isGhostJumping) { ghostTimer -= dt; if (ghostTimer <= 0) { SpawnGhost(); ghostTimer = ghostRate; } } foreach (var g in ghostPool) { if (g.gameObject.activeInHierarchy) { Color c = g.color; c.a -= ghostFadeSpeed * dt; g.color = c; if (c.a <= 0) g.gameObject.SetActive(false); } } }
    void SpawnGhost() { SpriteRenderer g = GetGhostFromPool(); g.transform.position = transform.position; g.transform.rotation = transform.rotation; g.transform.localScale = transform.localScale; g.sprite = sr.sprite; g.flipX = sr.flipX; g.color = ghostColor; g.gameObject.SetActive(true); }
    SpriteRenderer GetGhostFromPool() { foreach (var g in ghostPool) if (!g.gameObject.activeInHierarchy) return g; GameObject o = new GameObject("Ghost"); SpriteRenderer r = o.AddComponent<SpriteRenderer>(); r.sortingLayerID = sr.sortingLayerID; r.sortingOrder = sr.sortingOrder - 1; ghostPool.Add(r); return r; }

    // ★ [UpdateAnimation] 벽 타기 시 달리기 샷 방지 (Speed = 0 트릭)
    void UpdateAnimation()
    {
        anim.SetFloat(hashSpeed, Mathf.Abs(velocity.x));
        anim.SetFloat(hashYSpeed, velocity.y);
        bool isSliding = isTouchingWall && !isGrounded && (input.x == wallDir) && velocity.y < 0;
        if (isSliding) anim.SetFloat(hashSpeed, 0f); // 벽 탈 땐 속도 0으로 속여서 달리기 샷 방지
        anim.SetBool(hashIsGrounded, isGrounded);
        anim.SetBool(hashIsDashing, isDashing && isGrounded);
        anim.SetBool(hashIsWallSliding, isSliding);
        anim.SetBool(hashIsShooting, isShooting);
        anim.SetFloat(hashYInput, input.y);
    }

    void OnDrawGizmos() { Gizmos.color = Color.green; Gizmos.DrawWireSphere(transform.position + dashDustOffset, 0.1f); Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position + wallSparkOffset, 0.1f); Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position + new Vector3(dashJetOffset.x * direction, dashJetOffset.y, 0), 0.1f); Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position + new Vector3(wallSlideOffset.x * direction, wallSlideOffset.y, 0), 0.1f); if (firePoint) { Gizmos.color = Color.white; Gizmos.DrawWireSphere(firePoint.position, 0.1f); } }
    void UpdateFirePointPosition() { if (firePoint == null) return; Vector3 targetPos = standFirePos; if (isDashing || isDashJumping) targetPos = dashFirePos; else if (!isGrounded || isWallKicking) targetPos = jumpFirePos; else if (Mathf.Abs(velocity.x) > 0.1f) targetPos = runFirePos; if (isTouchingWall && !isGrounded && input.x == wallDir && velocity.y < 0) targetPos = wallFirePos; firePoint.localPosition = targetPos; }
}