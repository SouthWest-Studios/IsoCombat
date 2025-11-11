using UnityEngine;

public class PlayerColliderPart : MonoBehaviour
{
    public enum PartType { Upper, Lower }
    public PartType partType;
    [HideInInspector] public PlayerController owner;
    public float bounceForce = 5f;

    void Start()
    {
        owner = GetComponentInParent<PlayerController>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var otherPart = collision.collider.GetComponent<PlayerColliderPart>();
        if (otherPart == null) return;

        if (otherPart.owner == owner) return;

        Rigidbody2D rbOwner = owner.GetComponent<Rigidbody2D>();
        Rigidbody2D rbOther = otherPart.owner.GetComponent<Rigidbody2D>();

        if (partType == PartType.Lower && otherPart.partType == PartType.Upper)
        {
            Debug.Log($"{owner.name} golpeó a {otherPart.owner.name}");
            owner.TakeDamage(1);
        }

        if (rbOwner != null && rbOther != null)
        {
            // Dirección normal del impacto
            Vector2 normal = collision.contacts[0].normal;

            // Aplicar fuerza opuesta a cada uno
            rbOwner.AddForce(-normal * bounceForce, ForceMode2D.Impulse);
            rbOther.AddForce(normal * bounceForce, ForceMode2D.Impulse);
        }


    }
}
