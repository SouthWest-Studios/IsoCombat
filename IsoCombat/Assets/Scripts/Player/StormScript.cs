using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerColliderPart;
using UnityEngine.SceneManagement;
public class StormScript : MonoBehaviour
{

    CircleCollider2D stormCollider;
    public Transform circleTransform;
    public List<float> timeBeforeClosing;
    public float shrinkAmount = 2f;
    public float shrinkDuration = 3f;
    private Coroutine damageRoutine;
    public bool isShrinking = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stormCollider = GetComponent<CircleCollider2D>();
        //StartCoroutine(StormRoutine());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.CompareTag("PlayerCollision"))
        {
            if (other.GetComponent<PlayerColliderPart>().owner && !other.GetComponent<PlayerColliderPart>().owner.isPlayerLocal)
            {
                return;
            }
            
        }
        else
        {
            return;
        }

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
        
        if (!other.CompareTag("PlayerCollision") || !other.GetComponent<PlayerColliderPart>().owner.isPlayerLocal) return;
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

    public IEnumerator Shrink(int level)
    {
        UnityEngine.Debug.Log("leveeeel:"+level);
        Vector3 initialScale = circleTransform.localScale;
        Vector3 finalScale = Vector3.zero;
        switch (level)
        {
            case 1:
                finalScale = new Vector3(-3.865f, -3.865f, -4.82f);
                break;
            case 2:
                finalScale = new Vector3(-2.91f, -2.91f, -4.82f);
                break;
            case 3:
                finalScale = new Vector3(-1.955f, -1.955f, -4.82f);
                break;
            case 4:
                finalScale = new Vector3(-1f, -1f, -4.82f);
                break;
            default:
                break;
        }
        //Vector3 finalScale = initialScale + new Vector3(shrinkAmount, shrinkAmount, 0);


        float t = 0f;
        isShrinking = true;
        
        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            float p = t / shrinkDuration;

            circleTransform.localScale = Vector3.Lerp(initialScale, finalScale, p);

            yield return null;
        }
        isShrinking = false;
        circleTransform.localScale = finalScale; // asegurar el valor final
    }



}
