using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int _maxHp = 5;
    private int _currentHp;

    private void Awake()
    {
        _currentHp = _maxHp;
    }

    public void TakeDamage(int damage)
    {
        _currentHp -= damage;
        if (_currentHp <= 0)
            Die();
    }

    private void Die()
    {
        // 나중에 피격 이펙트 / 사운드 추가
        gameObject.SetActive(false);
    }
}