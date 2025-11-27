using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Player : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _airMoveSpeed = 7f;
    [SerializeField] private float _jumpForce = 16f;

    [Header("Dash")]
    [SerializeField] private float _dashSpeed = 18f;
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private float _dashCooldown = 0.15f;

    [Header("Ground / Wall / Ceiling Check")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundCheckRadius = 0.1f;
    [SerializeField] private Transform _ceilingCheck;
    [SerializeField] private float _ceilingCheckRadius = 0.1f;
    [SerializeField] private Transform _wallCheck;
    [SerializeField] private float _wallCheckDistance = 0.2f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private LayerMask _ceilingLayer;

    [Header("Components")]
    [SerializeField] private Animator _animator;


    public Rigidbody2D Rb { get; private set; }

    // 입력값
    public float XInput { get; private set; }
    public bool IsJumpPressed { get; private set; }
    public bool IsDashPressed { get; private set; }

    // 환경 체크 결과
    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public bool IsTouchingCeiling { get; private set; }
    public int WallDirectionX { get; private set; } = 1; // 벽이 있는 방향

    // 대쉬 상태
    public bool IsDashAvailable { get; private set; } = true;
    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;

    // 이동 파라미터
    public float MoveSpeed => _moveSpeed;
    public float AirMoveSpeed => _airMoveSpeed;
    public float JumpForce => _jumpForce;

    public PlayerStateMachine StateMachine { get; private set; }

    // 상태 인스턴스
    [HideInInspector] public PlayerIdleState IdleState;
    [HideInInspector] public PlayerRunState RunState;
    [HideInInspector] public PlayerInAirState InAirState;
    [HideInInspector] public PlayerDashState DashState;
    [HideInInspector] public PlayerWallSlideState WallSlideState;
    [HideInInspector] public PlayerHitState HitState;
    [HideInInspector] public PlayerDeadState DeadState;

    private float _lastDashTime = -999f;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();

        StateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, StateMachine);
        RunState = new PlayerRunState(this, StateMachine);
        InAirState = new PlayerInAirState(this, StateMachine);
        DashState = new PlayerDashState(this, StateMachine);
        WallSlideState = new PlayerWallSlideState(this, StateMachine);
        HitState = new PlayerHitState(this, StateMachine);
        DeadState = new PlayerDeadState(this, StateMachine);
    }

    private void Start()
    {
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        ReadInput();
        CheckEnvironment();

        UpdateDashAvailability();

        StateMachine.LogicUpdate();

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        StateMachine.PhysicsUpdate();
    }

    private void ReadInput()
    {
        XInput = Input.GetAxisRaw("Horizontal");
        IsJumpPressed = Input.GetButtonDown("Jump");
        IsDashPressed = Input.GetKeyDown(KeyCode.LeftShift); // 일단 Shift로
    }

    private void CheckEnvironment()
    {
        // 땅
        IsGrounded = Physics2D.OverlapCircle(
            _groundCheck.position,
            _groundCheckRadius,
            _groundLayer
        );

        // 천장
        IsTouchingCeiling = Physics2D.OverlapCircle(
            _ceilingCheck.position,
            _ceilingCheckRadius,
            _ceilingLayer
        );

        // 벽 (캐릭터 바라보는 방향 기준)
        float facing = Mathf.Sign(transform.localScale.x);
        RaycastHit2D hit = Physics2D.Raycast(
            _wallCheck.position,
            Vector2.right * facing,
            _wallCheckDistance,
            _wallLayer
        );

        IsTouchingWall = hit.collider != null;
        if (IsTouchingWall)
        {
            WallDirectionX = (int)facing;
        }
    }

    private void UpdateDashAvailability()
    {
        // 간단하게: 땅에 닿으면 다시 대쉬 가능, 아니면 쿨타임 체크
        if (IsGrounded)
        {
            IsDashAvailable = true;
        }
        else if (!IsDashAvailable && Time.time >= _lastDashTime + _dashCooldown)
        {
            IsDashAvailable = true;
        }
    }

    public void ConsumeDash()
    {
        IsDashAvailable = false;
        _lastDashTime = Time.time;
    }

    public void SetHorizontalVelocity(float xVelocity)
    {
        Rb.velocity = new Vector2(xVelocity, Rb.velocity.y);
    }

    public void SetVelocity(Vector2 velocity)
    {
        Rb.velocity = velocity;
    }

    public void StopVerticalIfCeilingHit()
    {
        if (IsTouchingCeiling && Rb.velocity.y > 0f)
        {
            Rb.velocity = new Vector2(Rb.velocity.x, 0f);
        }
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        _animator.SetFloat("Speed", Mathf.Abs(Rb.velocity.x));
        _animator.SetFloat("YSpeed", Rb.velocity.y);
        _animator.SetBool("IsGrounded", IsGrounded);
        _animator.SetBool("IsDashing", StateMachine.CurrentState == DashState);
        _animator.SetBool("IsWallSliding", StateMachine.CurrentState == WallSlideState);
        // 나중에 Shooting, Hit, Dead 등 추가
    }

    private void OnDrawGizmosSelected()
    {
        if (_groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
        }
        if (_ceilingCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_ceilingCheck.position, _ceilingCheckRadius);
        }
        if (_wallCheck != null)
        {
            Gizmos.color = Color.blue;
            float facing = Mathf.Sign(transform.localScale.x);
            Gizmos.DrawLine(
                _wallCheck.position,
                _wallCheck.position + Vector3.right * facing * _wallCheckDistance
            );
        }
    }

    public Animator Animator => _animator;
}