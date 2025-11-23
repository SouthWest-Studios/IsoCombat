using UnityEngine;

public class DeathSpike : MonoBehaviour
{
    public float knockbackForce = 10f;   
    public float torqueForce = 20f;      

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 dir = (rb.position - collision.GetContact(0).point).normalized;

        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);

        float sign = Random.value < 0.5f ? -1f : 1f;
        rb.AddTorque(sign * torqueForce, ForceMode2D.Impulse);
    }
}
