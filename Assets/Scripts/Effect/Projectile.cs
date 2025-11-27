using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float _speed = 16f;
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _lifeTime = 1.5f;

    private Rigidbody2D _rb;
    private Vector2 _direction;
    private float _timer;
    private ObjectPool _pool;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Init(Vector2 dir, ObjectPool pool)
    {
        _direction = dir.normalized;
        _pool = pool;
        _timer = 0f;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lifeTime)
        {
            Despawn();
            return;
        }

        _rb.MovePosition(_rb.position + _direction * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IDamageable damageable))
        {
            damageable.TakeDamage(_damage);
            Despawn();
        }
        // 필요하면 타일/벽 레이어에 부딪혀도 사라지게 여기서 처리
    }

    private void Despawn()
    {
        if (_pool != null)
            _pool.Return(gameObject);
        else
            Destroy(gameObject);
    }
}