using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("거리 설정")]
    public float activationRange = 15f;
    public float detectionRange = 5f;
    public float respawnRange = 20f; // 리스폰 거리 (이만큼 멀어지면 부활)

    [Header("이동 설정")]
    public float moveSpeed = 2f;
    public float patrolTime = 2.0f;
    public float idleTime = 2.0f;

    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("스프라이트 방향 보정")]
    public bool spriteFaceLeft = false;

    [Header("공격 설정")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 2.0f;
    public float shootDelay = 0.5f;

    Rigidbody2D rb;
    Animator anim;
    Transform player;
    EnemyHealth health;

    Vector3 startPosition; // 태어난 위치 저장

    float fireTimer;
    float stateTimer;

    int facingDir = 1;
    bool isAttacking = false;

    public enum State { Idle, Patrol, Attack }
    public State currentState = State.Patrol;

    readonly int hashSpeed = Animator.StringToHash("Speed");
    readonly int hashShoot = Animator.StringToHash("Shoot");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        startPosition = transform.position; // 시작 위치 기억
        ResetPatrol();
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // ================================================================
        //  [1순위] 리스폰 로직 (죽었더라도 거리는 계속 재야 함!)
        // ================================================================
        if (distToPlayer > respawnRange)
        {
            // 죽어있거나, 원래 위치에서 너무 벗어났다면 -> 리셋(부활)
            if (health.IsDead || Vector3.Distance(transform.position, startPosition) > 2f)
            {
                ResetEnemy();
            }
            return; // 멀리 있으면 AI 연산 중단
        }

        // ================================================================
        //  [2순위] 죽음 체크 (리스폰 체크 다음으로 와야 함)
        // ================================================================
        if (health.IsDead)
        {
            StopMoving();
            return; // 죽었으니 움직임 로직은 패스
        }

        // 3. 공격 중이면 대기
        if (isAttacking)
        {
            StopMoving();
            return;
        }

        // 4. 거리 체크 및 상태 전환
        if (distToPlayer < detectionRange)
        {
            currentState = State.Attack;
            AttackLogic();
        }
        else
        {
            if (currentState == State.Attack) ResetPatrol();
            PatrolLogic();
        }
    }

    // 적 초기화 (부활) 함수
    void ResetEnemy()
    {
        transform.position = startPosition; // 원래 위치로 텔레포트
        health.Revive(); // 체력과 모습 복구

        StopMoving();
        ResetPatrol();
        isAttacking = false;

        // 방향 초기화
        facingDir = 1;
        UpdateFacing();
    }

    void StopMoving()
    {
        rb.velocity = Vector2.zero;
        anim.SetFloat(hashSpeed, 0);
    }

    void ResetPatrol()
    {
        currentState = State.Patrol;
        stateTimer = patrolTime;
    }

    void PatrolLogic()
    {
        stateTimer -= Time.deltaTime;

        if (currentState == State.Patrol)
        {
            rb.velocity = new Vector2(moveSpeed * facingDir, rb.velocity.y);
            anim.SetFloat(hashSpeed, Mathf.Abs(rb.velocity.x));

            bool shouldTurn = (stateTimer <= 0);
            if (groundCheck != null)
            {
                bool isGroundAhead = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
                if (!isGroundAhead) shouldTurn = true;
            }

            if (shouldTurn)
            {
                StopMoving();
                currentState = State.Idle;
                stateTimer = idleTime;
            }
        }
        else if (currentState == State.Idle)
        {
            StopMoving();
            if (stateTimer <= 0)
            {
                Flip();
                ResetPatrol();
            }
        }
    }

    void AttackLogic()
    {
        StopMoving();
        if (player.position.x > transform.position.x && facingDir == -1) Flip();
        else if (player.position.x < transform.position.x && facingDir == 1) Flip();

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0)
        {
            StartCoroutine(ShootRoutine());
            fireTimer = fireRate;
        }
    }

    IEnumerator ShootRoutine()
    {
        isAttacking = true;
        anim.SetTrigger(hashShoot);
        yield return new WaitForSeconds(shootDelay);
        if (bulletPrefab && firePoint) Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    void Flip()
    {
        facingDir *= -1;
        UpdateFacing();
    }

    void UpdateFacing()
    {
        float scaleX = facingDir;
        if (spriteFaceLeft) scaleX *= -1;
        transform.localScale = new Vector3(scaleX, 1, 1);
        if (firePoint)
        {
            if (facingDir == 1) firePoint.localRotation = Quaternion.Euler(0, 0, 0);
            else firePoint.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.gray; // 리스폰 범위 (회색)
        Gizmos.DrawWireSphere(transform.position, respawnRange);

        if (groundCheck) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(groundCheck.position, 0.1f); }
    }
}