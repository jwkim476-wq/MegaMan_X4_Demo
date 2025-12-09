using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 2f;
    public int damage = 1;
    public GameObject hitEffect; // 맞았을 때 이펙트

    void Start()
    {
        // 생성되면 방향대로 날아감 (플레이어가 생성할 때 방향을 돌려줘야 함)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.velocity = transform.right * speed * transform.localScale.x; // 좌우 반전 대응

        Destroy(gameObject, lifeTime); // 시간 지나면 삭제
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 적에게 데미지 주는 로직 (나중에 추가)
            // other.GetComponent<Enemy>().TakeDamage(damage);
            DestroyBullet();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            DestroyBullet();
        }
    }

    void DestroyBullet()
    {
        if (hitEffect) Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}

