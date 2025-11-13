using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class PlayerColliderPart : MonoBehaviour
{
    public enum PartType { Upper, Lower }
    public PartType partType;
    [HideInInspector] public PlayerController owner;
    public float bounceForce = 5f;
    float crashCooldown = 0.3f;

    void Start()
    {
        owner = GetComponentInParent<PlayerController>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!owner.isPlayerLocal) return;

        if (collision.gameObject.CompareTag("Bullet"))
        {
            
            owner.TakeDamage(1);
            GameplayNet.I.BulletHit(collision.gameObject.GetComponent<BulletNetInfo>());
            owner.DestroyBullet(collision.gameObject.GetComponent<Rigidbody2D>());
            return;
        }


        var otherPart = collision.collider.GetComponent<PlayerColliderPart>();

        if (otherPart == null) return;
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

            Vector2 dir = rbOwner.transform.position - rbOther.transform.position;
            dir.Normalize();

            // Aplicar fuerza opuesta a cada uno
            rbOwner.AddForce(dir * bounceForce, ForceMode2D.Impulse);
            rbOther.AddForce(-dir * bounceForce, ForceMode2D.Impulse);

            StartCoroutine(StopBounceAfterDelay(rbOwner));
            StartCoroutine(StopBounceAfterDelay(rbOther));
        }


    }
    IEnumerator StopBounceAfterDelay(Rigidbody2D rb)
    {
       
        owner.canMove = false;
        yield return new WaitForSeconds(crashCooldown);
        rb.constraints = RigidbodyConstraints2D.FreezePosition
           | RigidbodyConstraints2D.FreezeRotation;

        owner.canMove = true;
        // Detiene el movimiento (el rebote)
        rb.linearVelocity = Vector2.zero;

        
    }
}
