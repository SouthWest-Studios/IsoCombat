using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerColliderPart;
public class StormScript : MonoBehaviour
{

    CircleCollider2D stormCollider;
    public Transform circleTransform;
    public List<float> timeBeforeClosing;
    public float shrinkAmount = 2f;
    public float shrinkDuration = 3f;
    private Coroutine damageRoutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stormCollider = GetComponent<CircleCollider2D>();
        StartCoroutine(StormRoutine());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.transform.parent.parent.CompareTag("Player") && other.transform.parent.parent.GetComponent<PlayerController>().isPlayerLocal) return;

        var otherPart = other.GetComponent<PlayerColliderPart>();

       if( otherPart.partType == PartType.Upper) return;

        if (damageRoutine != null)
        {
            StopCoroutine(damageRoutine);
            damageRoutine = null;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        
        if (!other.transform.parent.parent.CompareTag("Player") && other.transform.parent.parent.GetComponent<PlayerController>().isPlayerLocal) return;
        var otherPart = other.GetComponent<PlayerColliderPart>();
        if (otherPart.partType == PartType.Upper) return;
        damageRoutine = StartCoroutine(ApplyDamage(other.transform.parent.parent.GetComponent<PlayerController>()));
    }

    IEnumerator ApplyDamage(PlayerController player)
    {
        while (true)
        {
            Debug.Log("recbio dañooooo");
            player.TakeDamage(1);
            yield return new WaitForSeconds(2f);
        }
    }

    IEnumerator StormRoutine()
    {
        // Recorremos cada tiempo de la lista
        foreach (float waitTime in timeBeforeClosing)
        {
            // Espera antes de cerrarse
            yield return new WaitForSeconds(waitTime);

            // Inicia el cierre de esta fase
            yield return StartCoroutine(Shrink());
        }
    }

    IEnumerator Shrink()
    {
        Vector3 initialScale = circleTransform.localScale;
        Vector3 finalScale = initialScale + new Vector3(shrinkAmount, shrinkAmount, 0);

        float t = 0f;

        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            float p = t / shrinkDuration;

            circleTransform.localScale = Vector3.Lerp(initialScale, finalScale, p);

            yield return null;
        }

        circleTransform.localScale = finalScale; // asegurar el valor final
    }



}
