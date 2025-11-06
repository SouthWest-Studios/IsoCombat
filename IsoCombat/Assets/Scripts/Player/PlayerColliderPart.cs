using UnityEngine;

public class PlayerColliderPart : MonoBehaviour
{
    public enum PartType { Upper, Lower }
    public PartType partType;
    [HideInInspector] public PlayerController owner;

    void Start()
    {
        owner = GetComponentInParent<PlayerController>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var otherPart = collision.collider.GetComponent<PlayerColliderPart>();
        if (otherPart == null) return;

        if (otherPart.owner == owner) return;

        if (partType == PartType.Upper && otherPart.partType == PartType.Lower)
        {
            Debug.Log($"{owner.name} golpeó a {otherPart.owner.name}");
            otherPart.owner.TakeDamage(1);
        }


    }
}
