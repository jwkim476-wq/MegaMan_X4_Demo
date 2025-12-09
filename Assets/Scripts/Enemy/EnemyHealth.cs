using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("설정")]
    public int maxHealth = 3;
    public GameObject deathEffectPrefab; // 폭발 이펙트
    public float flashDuration = 0.1f;   // 맞았을 때 하얗게 변하는 시간

    int currentHealth;
    SpriteRenderer sr;
    Material originalMaterial; // 원래 재질 저장용
    public Material whiteFlashMaterial; // ★ (선택) 하얗게 만드는 쉐이더가 있다면 사용, 없으면 색상 변경

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        // 원래 색상(또는 재질) 저장
        if (sr != null) originalMaterial = sr.material;
    }

    // 플레이어의 총알이 이 함수를 호출함
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 안 죽었으면 반짝임 효과
            StartCoroutine(FlashRoutine());
        }
    }

    void Die()
    {
        // 1. 폭발 이펙트 생성
        if (deathEffectPrefab)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. 적 오브젝트 삭제 (사망 모션 없이 바로 사라짐)
        Destroy(gameObject);
    }

    IEnumerator FlashRoutine()
    {
        // 간단하게 빨간색(또는 투명)으로 깜빡이거나
        sr.color = new Color(1f, 0.5f, 0.5f, 1f); // 피격 색상 (연한 빨강)

        yield return new WaitForSeconds(flashDuration);

        sr.color = Color.white; // 원래 색 복구
    }
}