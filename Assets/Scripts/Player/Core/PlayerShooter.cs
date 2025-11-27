using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerShooter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private ObjectPool _normalPool;
    [SerializeField] private ObjectPool _chargeLv1Pool;
    [SerializeField] private ObjectPool _chargeLv2Pool;

    [Header("Charge")]
    [SerializeField] private float _level1Time = 0.35f;
    [SerializeField] private float _level2Time = 0.9f;
    [SerializeField] private GameObject _chargeEffectRoot; // 차징 애니메이션 오브젝트

    private Player _player;
    private Animator _anim;
    private float _chargeTimer;
    private bool _isCharging;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _anim = _player.Animator;

        if (_chargeEffectRoot != null)
            _chargeEffectRoot.SetActive(false);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Z키 기준 (원하면 InputSystem으로 바꿔도 됨)
        if (Input.GetKeyDown(KeyCode.Z))
            StartCharge();

        if (Input.GetKey(KeyCode.Z))
            UpdateCharge();

        if (Input.GetKeyUp(KeyCode.Z))
            ReleaseCharge();
    }

    private void StartCharge()
    {
        _isCharging = true;
        _chargeTimer = 0f;

        if (_chargeEffectRoot != null)
            _chargeEffectRoot.SetActive(true);
    }

    private void UpdateCharge()
    {
        if (_isCharging)
            _chargeTimer += Time.deltaTime;
    }

    private void ReleaseCharge()
    {
        if (_chargeEffectRoot != null)
            _chargeEffectRoot.SetActive(false);

        _isCharging = false;

        int level = GetChargeLevel();

        if (level == 0)
            FireNormal();
        else
            FireCharge(level);

        _chargeTimer = 0f;
    }

    private int GetChargeLevel()
    {
        if (_chargeTimer < _level1Time) return 0;
        if (_chargeTimer < _level2Time) return 1;
        return 2;
    }

    private void FireNormal()
    {
        _anim.SetBool("IsShooting", true);

        ObjectPool pool = _normalPool;
        GameObject obj = pool.Get();
        Vector2 dir = _player.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        obj.transform.position = _firePoint.position;
        obj.GetComponent<Projectile>().Init(dir, pool);
    }

    private void FireCharge(int level)
    {
        bool isIdle = _player.IsGrounded && Mathf.Abs(_player.XInput) < 0.01f;

        if (isIdle)
            _anim.SetBool("IsChargeShot", true);
        else
            _anim.SetBool("IsShooting", true);

        ObjectPool pool = (level == 1) ? _chargeLv1Pool : _chargeLv2Pool;

        GameObject obj = pool.Get();
        Vector2 dir = _player.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        obj.transform.position = _firePoint.position;
        obj.GetComponent<Projectile>().Init(dir, pool);
    }

    // 애니메이션 마지막 프레임에서 Animation Event로 호출
    public void OnShootAnimationEnd()
    {
        _anim.SetBool("IsShooting", false);
        _anim.SetBool("IsChargeShot", false);
    }
}