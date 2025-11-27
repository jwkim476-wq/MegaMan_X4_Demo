using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int _maxHp = 20;
    [SerializeField] private float _lowHpRatio = 0.3f;

    [Header("Invincible")]
    [SerializeField] private float _invincibleDuration = 1.0f;
    [SerializeField] private float _blinkInterval = 0.1f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer _spriteRenderer; // Player 스프라이트

    private Player _player;
    private Animator _animator;

    public int CurrentHp { get; private set; }
    private bool _isInvincible;
    private bool _isDead;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _animator = _player.Animator;
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        CurrentHp = _maxHp;
        UpdateLowHpParam();
    }

    public void TakeDamage(int amount)
    {
        if (_isInvincible || _isDead) return;

        CurrentHp -= amount;
        if (CurrentHp < 0) CurrentHp = 0;

        UpdateLowHpParam();

        if (CurrentHp <= 0)
        {
            Die();
            return;
        }

        // 피격 상태로 전환
        _player.StateMachine.ChangeState(_player.HitState);

        // 무적 시작
        StartCoroutine(InvincibleRoutine());
    }

    private void UpdateLowHpParam()
    {
        if (_animator == null) return;

        // 너가 말한 것처럼 float LowHP 파라미터 사용한다고 가정
        float ratio = (float)CurrentHp / _maxHp;
        _animator.SetFloat("LowHP", ratio); // BlendTree에서 이 값 사용
    }

    private void Die()
    {
        _isDead = true;
        _player.StateMachine.ChangeState(_player.DeadState);
        // 여기서 GameManager 통해서 리트라이, 씬 리로드 등 호출 가능
    }

    private IEnumerator InvincibleRoutine()
    {
        _isInvincible = true;

        float endTime = Time.time + _invincibleDuration;

        while (Time.time < endTime)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(1f, 1f, 1f, 0.3f);
            }
            yield return new WaitForSeconds(_blinkInterval);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
            yield return new WaitForSeconds(_blinkInterval);
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
        }

        _isInvincible = false;
    }

    // 디버그용으로 테스트하고 싶으면
    private void Update()
    {
        // 예시: 키보드로 피격 테스트
        // if (Input.GetKeyDown(KeyCode.H))
        // {
        //     TakeDamage(1);
        // }
    }
}