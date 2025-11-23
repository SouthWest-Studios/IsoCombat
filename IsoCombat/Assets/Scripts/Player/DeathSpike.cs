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

        float cross = Vector3.Cross(dir, Vector2.up).z; // >0 o <0 según el lado
        float sign = Mathf.Sign(cross);
        if (sign == 0) sign = 1f;
        rb.AddTorque(sign * torqueForce, ForceMode2D.Impulse);
    }
}
