using UnityEngine;
using System.Collections;

public class PlayerColliderPart : MonoBehaviour
{
    public enum PartType { Upper, Lower }
    public PartType partType;
    [HideInInspector] public PlayerController owner;
    public float bounceForce = 5f;
    float crashCooldown = 1f;

    void Start()
    {
        owner = GetComponentInParent<PlayerController>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {

        var otherPart = collision.collider.GetComponent<PlayerColliderPart>();
        if (otherPart == null) return;

        if (!owner.isPlayerLocal) return;


        

        if (otherPart.owner == owner) return;

        Rigidbody2D rbOwner = owner.GetComponent<Rigidbody2D>();
        Rigidbody2D rbOther = otherPart.owner.GetComponent<Rigidbody2D>();

        rbOwner.constraints = RigidbodyConstraints2D.None;

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

            StartCoroutine(StopBounceAfterDelay(rbOwner));
            StartCoroutine(StopBounceAfterDelay(rbOther));
        }


    }
    IEnumerator StopBounceAfterDelay(Rigidbody2D rb)
    {   
        yield return new WaitForSeconds(crashCooldown);

        // Detiene el movimiento (el rebote)
     rb.linearVelocity = Vector2.zero;

        rb.constraints = RigidbodyConstraints2D.FreezePosition
            | RigidbodyConstraints2D.FreezeRotation;
    }
}
