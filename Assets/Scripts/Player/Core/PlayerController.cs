using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    // =========================================================
    // 1. 설정 변수 (인스펙터에서 조절)
    // =========================================================
    [Header("Movement Stats")]
    public float moveSpeed = 8f;
    public float jumpForce = 15f;
    public float gravity = 40f;          // 록맨은 중력이 강하고 점프가 빠릅니다
    public float maxFallSpeed = 20f;     // 낙하 최대 속도 제한

    [Header("Dash Settings")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.45f;
    public float dashCooldown = 0.2f;

    [Header("Wall Action Settings")]
    public float wallSlideSpeed = 2.5f;
    public Vector2 wallJumpPower = new Vector2(12f, 16f); // 벽 점프 시 (X반동, Y점프력)
    public float wallJumpInputFreeze = 0.15f;             // 벽 점프 직후 조작 불가 시간 (자연스러운 궤적용)

    [Header("Physics & Collision (Raycast)")]
    public LayerMask groundLayer;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;
    public float skinWidth = 0.015f;      // 레이캐스트가 내부에 파묻히지 않게 하는 여유값

    [Header("Afterimage (Blue Ghost)")]
    public Color ghostColor = new Color(0.2f, 0.4f, 1f, 0.7f); // 록맨 특유의 파란 잔상
    public float ghostFadeTime = 0.3f;    // 잔상이 사라지는 속도
    public float recordInterval = 0.04f;  // 잔상 간격 (짧을수록 촘촘함)
    public int ghostDelayIndex = 4;       // 몇 프레임 전의 잔상을 띄울지

    // =========================================================
    // 2. 내부 변수
    // =========================================================
    private BoxCollider2D boxCollider;
    private Animator anim;
    private SpriteRenderer sr;

    private Vector3 velocity;
    private float velocityXSmoothing; // 부드러운 방향전환용 (사용 안하면 즉각 반응)

    // 입력 및 상태
    private Vector2 input;
    private float direction = 1;      // 1: 오른쪽, -1: 왼쪽

    // 상태 플래그
    private bool isGrounded;
    private bool isCeilingHit;
    private bool isWallHit;
    private int wallDirX;             // 벽이 어느 쪽에 있는가 (-1:좌, 1:우)

    // 타이머
    private float dashTimer;
    private float dashCooldownTimer;
    private float wallJumpTimer;

    // FSM 상태 정의
    public enum State { Intro, Idle, Run, Jump, Fall, Dash, WallSlide, WallJump, Shoot, Hurt, Dead }
    [Header("Debug Info")]
    [SerializeField] private State currentState = State.Intro;

    // --- 잔상(Afterimage) 관련 구조체 및 풀 ---
    private struct GhostSnapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Sprite sprite;
        public bool flipX;
    }
    private List<GhostSnapshot> history = new List<GhostSnapshot>();
    private float historyTimer;
    private List<SpriteRenderer> ghostPool = new List<SpriteRenderer>();

    // 애니메이터 해시 최적화
    private readonly int hashSpeed = Animator.StringToHash("Speed");
    private readonly int hashYSpeed = Animator.StringToHash("YSpeed");
    private readonly int hashIsGrounded = Animator.StringToHash("IsGrounded");
    private readonly int hashIsDashing = Animator.StringToHash("IsDashing");
    private readonly int hashIsWallSliding = Animator.StringToHash("IsWallSliding");
    private readonly int hashIsShooting = Animator.StringToHash("IsShooting");

    // =========================================================
    // 3. 초기화 (Awake/Start)
    // =========================================================
    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    // =========================================================
    // 4. 메인 루프 (Update) - 요청하신 7단계 구조 적용
    // =========================================================
    void Update()
    {
        float dt = Time.deltaTime;

        // [1] 키보드 입력 처리
        ProcessInput();

        // [2] 중력 처리 (상태에 따라 다르게 적용)
        ProcessGravity(dt);

        // [3] 상태 전이 (FSM: 조건에 따른 상태 변경)
        CalculateState(dt);

        // 예상 이동량 계산
        Vector3 moveAmount = velocity * dt;

        // [4, 5, 6] 충돌 체크 (머리/지면/벽) - Raycast로 이동량 보정
        // 물리적 안정성을 위해 Vertical(Y축) 먼저, 그 다음 Horizontal(X축) 처리
        CheckVerticalCollision(ref moveAmount);   // 4(천장) & 6(지면)
        CheckHorizontalCollision(ref moveAmount); // 5(벽)

        // [7] 적 충돌 체크 (간단한 Trigger 처리 혹은 Ray)
        CheckEnemyCollision();

        // 최종 위치 이동
        transform.Translate(moveAmount);

        // 부가 기능: 방향 전환 및 잔상, 애니메이션
        HandleSpriteFlip();
        HandleAfterImage(dt);
        UpdateAnimations();
    }

    // =========================================================
    // [1] 입력 처리
    // =========================================================
    void ProcessInput()
    {
        // Intro 상태거나, 벽 점프 경직 중이거나, 사망 시 입력 차단
        if (currentState == State.Intro || currentState == State.Dead || wallJumpTimer > 0)
        {
            velocity.x = 0; // 움직임 멈춤
            return;
        }

        input.x = Input.GetAxisRaw("Horizontal");
        // 벽 점프 직후 잠시 동안은 방향 키 입력 무시 (벽에서 튕겨나가는 느낌 유지)
        if (wallJumpTimer > 0) return;

        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        // 점프 (C키)
        if (Input.GetKeyDown(KeyCode.C))
        {
            OnJumpInput();
        }

        // 점프 중단 (키를 떼면 점프 높이 조절)
        if (Input.GetKeyUp(KeyCode.C) && velocity.y > 0 && currentState != State.WallJump)
        {
            velocity.y = velocity.y * 0.5f;
        }

        // 대쉬 (Z키)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (CanDash())
            {
                dashTimer = dashDuration;
                // 대쉬 시작 시 방향 고정 (입력이 없으면 바라보는 방향)
                if (input.x != 0) direction = Mathf.Sign(input.x);
            }
        }

        // 공격 (X키)
        if (Input.GetKeyDown(KeyCode.X))
        {
            anim.SetTrigger(hashIsShooting);
            // 필요 시 공격 중 이동 정지 로직 추가 가능
        }
    }

    void OnJumpInput()
    {
        if (currentState == State.WallSlide)
        {
            // 벽 점프
            WallJump();
        }
        else if (isGrounded)
        {
            // 일반 점프
            velocity.y = jumpForce;
            // 대쉬 점프 테크닉: 대쉬 중에 점프하면 대쉬 속도 유지
            if (currentState == State.Dash)
            {
                // 여기서 별도 처리는 필요 없고 State가 Jump로 바뀌어도 
                // X축 속도가 유지되도록 CalculateState에서 처리함
            }
        }
    }

    bool CanDash()
    {
        // 쿨타임 중이 아니고, 지상이며, 이미 대쉬 중이 아닐 때
        return dashCooldownTimer <= 0 && isGrounded && currentState != State.Dash;
    }

    // =========================================================
    // [2] 중력 처리
    // =========================================================
    void ProcessGravity(float dt)
    {
        if (currentState == State.Dash)
        {
            velocity.y = 0;
        }
        else if (currentState == State.WallSlide)
        {
            velocity.y = -wallSlideSpeed;
        }
        else
        {
            // 땅에 있을 때는 중력을 계속 누적시키지 말고, 
            // 바닥에 딱 붙어있을 정도의 미세한 힘(-1f)만 유지합니다.
            if (isGrounded && velocity.y <= 0)
            {
                velocity.y = -1f;
            }
            else
            {
                velocity.y -= gravity * dt;
                if (velocity.y < -maxFallSpeed) velocity.y = -maxFallSpeed;
            }
        }
    }

    // =========================================================
    // [3] 상태 전이 (FSM)
    // =========================================================
    void CalculateState(float dt)
    {
        // 타이머 갱신
        if (wallJumpTimer > 0) wallJumpTimer -= dt;
        if (dashTimer > 0) dashTimer -= dt;
        if (dashCooldownTimer > 0) dashCooldownTimer -= dt;

        // 상태 전환 로직
        switch (currentState)
        {
            case State.Intro:
                // 1. 땅에 닿았는지 확인 (빛줄기로 내려오다가 착지)
                if (isGrounded)
                {
                    // 2. 애니메이션이 끝났는지 확인
                    // (Animator의 현재 상태가 Intro이고, 재생 시간이 1(100%)을 넘었으면)
                    if (anim.GetCurrentAnimatorStateInfo(0).IsName("Intro") &&
                        anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
                    {
                        currentState = State.Idle; // 조작 가능 상태로 전환
                    }
                }
                else if (Time.timeSinceLevelLoad > 2.0f)
                {
                    currentState = State.Idle;
                }
                break;
            case State.Idle:
            case State.Run:
                if (!isGrounded) currentState = State.Fall;
                else if (dashTimer > 0) currentState = State.Dash;
                else if (input.x != 0) currentState = State.Run;
                else currentState = State.Idle;
                break;

            case State.Jump:
            case State.Fall:
                if (isGrounded)
                {
                    currentState = State.Idle;
                    velocity.x = 0; // 착지 시 미끄러짐 방지
                }
                // 벽 슬라이드 조건: 공중 + 벽에 닿음 + 하강 중 + 벽 쪽으로 키 입력
                else if (isWallHit && velocity.y < 0 && input.x == wallDirX)
                {
                    currentState = State.WallSlide;
                }
                break;

            case State.WallSlide:
                if (isGrounded) currentState = State.Idle;
                else if (!isWallHit || input.x != wallDirX) currentState = State.Fall;
                else if (Input.GetKeyDown(KeyCode.C)) WallJump(); // 여기서도 점프 체크
                break;

            case State.Dash:
                // 대쉬 종료 조건: 시간 종료 or 벽 충돌 or 공중(단, 대쉬점프는 허용해야 하므로 Jump로 전이)
                if (isWallHit || !isGrounded)
                {
                    dashTimer = 0;
                    currentState = isGrounded ? State.Idle : State.Fall;
                }
                else if (dashTimer <= 0)
                {
                    dashCooldownTimer = dashCooldown;
                    currentState = State.Idle; // 대쉬 끝 자연스럽게 정지
                    velocity.x = 0;
                }
                else
                {
                    // 대쉬 이동 실행
                    velocity.x = direction * dashSpeed;
                }
                break;

            case State.WallJump:
                if (wallJumpTimer <= 0) currentState = State.Fall;
                break;
        }

        // 일반 이동 속도 적용 (대쉬나 벽점프 강제 이동이 아닐 때)
        if (currentState != State.Dash && wallJumpTimer <= 0)
        {
            // 록맨은 가속/감속 없이 즉각적인 이동 (GetAxisRaw 사용)
            velocity.x = input.x * moveSpeed;
        }
    }

    void WallJump()
    {
        wallJumpTimer = wallJumpInputFreeze;
        currentState = State.WallJump;

        // 벽 반대 방향으로 튕겨나감
        velocity.x = -wallDirX * wallJumpPower.x;
        velocity.y = wallJumpPower.y;

        // 캐릭터 방향 즉시 반전
        direction = -wallDirX;
    }

    // =========================================================
    // [4, 6] 수직 충돌 체크 (천장 & 지면)
    // =========================================================
    void CheckVerticalCollision(ref Vector3 moveAmount)
    {
        float dirY = Mathf.Sign(moveAmount.y);
        // 레이 길이를 조금 더 여유있게 줍니다 (+0.05f). 
        // 속도가 0이어도 바닥을 감지할 수 있게 하기 위함입니다.
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth + 0.05f;

        isGrounded = false;
        isCeilingHit = false;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (dirY == -1) ? GetRayOriginBottom(i) : GetRayOriginTop(i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * dirY, rayLength, groundLayer);

            Debug.DrawRay(rayOrigin, Vector2.up * dirY * rayLength, Color.red); // 디버그용 선

            if (hit)
            {
                moveAmount.y = (hit.distance - skinWidth) * dirY;
                rayLength = hit.distance;

                if (dirY == -1) // 바닥 착지
                {
                    isGrounded = true;
                }
                else // 천장 충돌
                {
                    isCeilingHit = true;
                    velocity.y = 0;
                }
            }
        }
    }

    // =========================================================
    // [5] 수평 충돌 체크 (벽)
    // =========================================================
    void CheckHorizontalCollision(ref Vector3 moveAmount)
    {
        float dirX = Mathf.Sign(moveAmount.x);
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

        // 벽에 붙어있을 때(이동량이 0일 때)도 벽 타기를 위해 감지해야 함
        if (Mathf.Abs(moveAmount.x) < skinWidth) rayLength = 2 * skinWidth;

        isWallHit = false;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (dirX == -1) ? GetRayOriginLeft(i) : GetRayOriginRight(i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * dirX, rayLength, groundLayer);

            Debug.DrawRay(rayOrigin, Vector2.right * dirX * rayLength, Color.red);

            if (hit)
            {
                // 대쉬나 이동 중 벽에 막힘
                if (Mathf.Abs(moveAmount.x) > 0)
                {
                    moveAmount.x = (hit.distance - skinWidth) * dirX;
                }
                rayLength = hit.distance;

                isWallHit = true;
                wallDirX = (int)dirX;
            }
        }
    }

    // =========================================================
    // [7] 적 충돌 체크 (예시)
    // =========================================================
    void CheckEnemyCollision()
    {
        // BoxCollider2D의 IsTrigger가 체크된 Enemy Hitbox와 닿았을 때 처리하거나
        // 여기서 Physics2D.OverlapBox 등을 사용해 능동적으로 체크 가능
    }

    // 피격 처리 예시 (외부에서 호출 가능)
    public void OnTakeDamage(int damage, Vector2 damageSourcePos)
    {
        if (currentState == State.Dead) return;

        currentState = State.Hurt;
        anim.SetTrigger("Hurt");

        // 넉백 (데미지 소스 반대 방향으로 튕김)
        float knockbackDir = transform.position.x - damageSourcePos.x > 0 ? 1 : -1;
        velocity = new Vector2(knockbackDir * 5f, 5f);

        // 일정 시간 후 Idle 복귀 로직 필요 (Coroutine 권장)
    }

    // =========================================================
    // [부가 기능] 잔상 (Afterimage) - 오브젝트 풀링
    // =========================================================
    void HandleAfterImage(float dt)
    {
        // 1. 스냅샷 기록
        historyTimer += dt;
        if (historyTimer >= recordInterval)
        {
            history.Add(new GhostSnapshot
            {
                position = transform.position,
                rotation = transform.rotation,
                scale = transform.localScale,
                sprite = sr.sprite,
                flipX = sr.flipX
            });
            if (history.Count > 30) history.RemoveAt(0); // 메모리 관리
            historyTimer = 0;
        }

        // 2. 대쉬 중에만 잔상 표시
        if (currentState == State.Dash && history.Count > ghostDelayIndex)
        {
            GhostSnapshot snapshot = history[history.Count - 1 - ghostDelayIndex];

            // 풀에서 가져오기
            SpriteRenderer ghost = GetGhostFromPool();

            // 데이터 적용
            ghost.transform.position = snapshot.position;
            ghost.transform.rotation = snapshot.rotation;
            ghost.transform.localScale = snapshot.scale;
            ghost.sprite = snapshot.sprite;
            ghost.flipX = snapshot.flipX;
            ghost.color = ghostColor;

            ghost.gameObject.SetActive(true);
            StartCoroutine(FadeOutGhost(ghost));
        }
    }

    SpriteRenderer GetGhostFromPool()
    {
        foreach (var g in ghostPool)
        {
            if (!g.gameObject.activeInHierarchy) return g;
        }

        // 없으면 새로 생성
        GameObject obj = new GameObject("Ghost");
        SpriteRenderer r = obj.AddComponent<SpriteRenderer>();
        r.sortingLayerID = sr.sortingLayerID;
        r.sortingOrder = sr.sortingOrder - 1; // 플레이어 뒤
        obj.SetActive(false);
        ghostPool.Add(r);
        return r;
    }

    IEnumerator FadeOutGhost(SpriteRenderer ghost)
    {
        float elapsed = 0;
        Color startColor = ghostColor;
        while (elapsed < ghostFadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0, elapsed / ghostFadeTime);
            ghost.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        ghost.gameObject.SetActive(false);
    }

    // =========================================================
    // 유틸리티 및 헬퍼 함수
    // =========================================================
    void HandleSpriteFlip()
    {
        // 벽타기나 벽점프 중에는 방향 고정
        if (currentState == State.WallSlide || currentState == State.WallJump) return;

        if (velocity.x > 0.1f)
        {
            direction = 1;
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (velocity.x < -0.1f)
        {
            direction = -1;
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void UpdateAnimations()
    {
        anim.SetFloat(hashSpeed, Mathf.Abs(velocity.x));
        anim.SetFloat(hashYSpeed, velocity.y);
        anim.SetBool(hashIsGrounded, isGrounded);
        anim.SetBool(hashIsDashing, currentState == State.Dash);
        anim.SetBool(hashIsWallSliding, currentState == State.WallSlide);
    }

    // --- Raycast Origins (충돌 박스 모서리 계산) ---
    Vector2 GetRayOriginBottom(int i)
    {
        Bounds b = boxCollider.bounds; b.Expand(skinWidth * -2);
        return new Vector2(Mathf.Lerp(b.min.x, b.max.x, (float)i / (verticalRayCount - 1)), b.min.y);
    }
    Vector2 GetRayOriginTop(int i)
    {
        Bounds b = boxCollider.bounds; b.Expand(skinWidth * -2);
        return new Vector2(Mathf.Lerp(b.min.x, b.max.x, (float)i / (verticalRayCount - 1)), b.max.y);
    }
    Vector2 GetRayOriginLeft(int i)
    {
        Bounds b = boxCollider.bounds; b.Expand(skinWidth * -2);
        return new Vector2(b.min.x, Mathf.Lerp(b.min.y, b.max.y, (float)i / (horizontalRayCount - 1)));
    }
    Vector2 GetRayOriginRight(int i)
    {
        Bounds b = boxCollider.bounds; b.Expand(skinWidth * -2);
        return new Vector2(b.max.x, Mathf.Lerp(b.min.y, b.max.y, (float)i / (horizontalRayCount - 1)));
    }
}