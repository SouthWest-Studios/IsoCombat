using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class HealthBarScript : MonoBehaviour
{
    public SpriteRenderer fillBar;
    public PlayerController player;
    private Vector3 initialScale;
    public SpriteRenderer playerGraphs;
   
    void Start()
    {
        initialScale = fillBar.gameObject.transform.localScale;
        fillBar.color = playerGraphs.color;
    }

    void Update()
    {
        float fillPercent = player.currentHealth / player.stats.Get(StatId.MaxHP);
        fillPercent = Mathf.Clamp01(fillPercent);
        Debug.Log(player.stats.name + " h: " + fillPercent);
        //Scale only on X (or the axis you use)
        fillBar.transform.localScale = new Vector3(initialScale.x * fillPercent, initialScale.y, initialScale.z);
    }



    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;

    }
}
