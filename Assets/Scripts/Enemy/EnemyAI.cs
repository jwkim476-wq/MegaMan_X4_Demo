using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 2f;
    public float patrolTime = 2.0f; // 걷는 시간
    public float idleTime = 2.0f;   // 멈춰있는 시간 (방향 바꾸기 전)
    public float detectionRange = 6f;

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

    float fireTimer;
    float patrolTimer;
    float idleTimer; // 대기 타이머

    int facingDir = 1;
    bool isAttacking = false;
    bool isIdling = false; // 현재 멈춰있는지 체크

    readonly int hashSpeed = Animator.StringToHash("Speed");
    readonly int hashShoot = Animator.StringToHash("Shoot");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        patrolTimer = patrolTime;
    }

    void Update()
    {
        if (isAttacking || player == null)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            anim.SetFloat(hashSpeed, 0);
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (distToPlayer < detectionRange)
        {
            // 플레이어를 발견하면 대기 상태를 즉시 풀고 공격 모드로
            isIdling = false;
            AttackBehavior();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        // [대기 상태] : 멈춰서 Idle 애니메이션 재생
        if (isIdling)
        {
            rb.velocity = new Vector2(0, rb.velocity.y); // 멈춤
            anim.SetFloat(hashSpeed, 0); // Idle 애니메이션

            idleTimer -= Time.deltaTime;

            // 대기 시간이 끝나면
            if (idleTimer <= 0)
            {
                Flip(); // 뒤로 돌고
                isIdling = false; // 다시 걷기 시작
                patrolTimer = patrolTime; // 걷는 시간 리필
            }
        }
        // [이동 상태] : 걷기
        else
        {
            rb.velocity = new Vector2(moveSpeed * facingDir, rb.velocity.y);
            anim.SetFloat(hashSpeed, Mathf.Abs(rb.velocity.x));

            patrolTimer -= Time.deltaTime;

            // 걷는 시간이 끝나면?
            if (patrolTimer <= 0)
            {
                isIdling = true; // 대기 상태로 전환
                idleTimer = idleTime; // 대기 시간 리필
            }

            // 낭떠러지 체크 (떨어질 것 같으면 시간 무시하고 즉시 턴)
            if (groundCheck != null)
            {
                bool isGroundAhead = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
                if (!isGroundAhead)
                {
                    isIdling = true;
                    idleTimer = idleTime;
                }
            }
        }
    }

    void AttackBehavior()
    {
        rb.velocity = Vector2.zero;
        anim.SetFloat(hashSpeed, 0);

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

        if (bulletPrefab && firePoint)
        {
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    void Flip()
    {
        facingDir *= -1;

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
        if (groundCheck) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(groundCheck.position, 0.1f); }
    }
}