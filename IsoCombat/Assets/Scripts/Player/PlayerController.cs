using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;


public class PlayerController : MonoBehaviour
{

    public StatsRuntime stats;

    [Header("Health References")]
    public float currentHealth = 0f;
    //public float maxHealth = 10f;
    //public float speed = 2;

    [Header("Player References")]
    public Transform playerSprite;
    public Transform playerGraphics;

    public bool isPlayerLocal = false;
    public bool isDead = false;

    
    //Movement
    public Camera mainCamera;
    private Vector2 targetPosition;
    private float screenMargin = 0.5f;

    //Dash
    public float dashMultiplier = 2f;
    public float dashDuration = 2f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;

    //Timer
    private float timeCounter = 0;

    //Invulnerability
    [HideInInspector] public float invulnerabilityTimer = 0f;
    [HideInInspector] public float invulnerabilityCoolDownTimer = 0f;
    [HideInInspector] public float invulnerabilityCoolDown = 2f;

    //Physics

    PolygonCollider2D upperBody;
    BoxCollider2D lowerBody;


    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        targetPosition = transform.position;
        timeCounter = 0;
        currentHealth = stats.Get(StatId.MaxHP);
        float scale = stats.Get(StatId.Scale);
        transform.localScale = new Vector3(scale, scale, scale);
        playerSprite.GetComponent<SpriteRenderer>().material.SetFloat("_FillAmount", currentHealth / stats.Get(StatId.MaxHP));
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlayerLocal && !isDead)
        {
            if (Input.GetMouseButton(0) && canDash && !isDashing)
                StartCoroutine(Dash());
        }

        UpdateTime();

        if (isPlayerLocal)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                PlayerDie();
            }

            UpdateMovement();

        }
        UpdateInvulnerability();


    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        float dashTimer = 0f;

        while (Input.GetMouseButton(0) && dashTimer < dashDuration)
        {
            dashTimer += Time.deltaTime;

            Vector2 currentPosition = transform.position;
            Vector3 mouseWorldPos3 = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos3.z = transform.position.z;
            Vector2 mouseWorldPos = mouseWorldPos3;

            Vector2 dir = mouseWorldPos - currentPosition;
            if (dir.sqrMagnitude > 0.0001f)
                transform.up = dir;

            Vector2 newPosition = Vector2.MoveTowards(currentPosition, mouseWorldPos, stats.Get(StatId.MoveSpeed) * dashMultiplier * Time.deltaTime);
            transform.position = newPosition;

            yield return null;
        }

        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void AssignColor(Color c)
    {
        var sr = playerSprite.GetComponent<SpriteRenderer>();
        if (sr) sr.color = c;
    }

    void UpdateMovement()
    {

        Vector2 currentPosition = transform.position;
        Vector3 mouseWorldPos3 = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos3.z = transform.position.z;           
        Vector2 mouseWorldPos = mouseWorldPos3;

        Vector2 dir = mouseWorldPos - currentPosition;
        if (dir.sqrMagnitude > 0.0001f)
            transform.up = dir;                            

        Vector2 newPosition = Vector2.MoveTowards(currentPosition, mouseWorldPos, stats.Get(StatId.MoveSpeed) * Time.deltaTime);

        float camHeight = mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        float minX = mainCamera.transform.position.x - camWidth + screenMargin;
        float maxX = mainCamera.transform.position.x + camWidth - screenMargin;
        float minY = mainCamera.transform.position.y - camHeight + screenMargin;
        float maxY = mainCamera.transform.position.y + camHeight - screenMargin;

        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

        transform.position = newPosition;
    }

    void UpdateInvulnerability()
    {
        bool inputActivado = Input.GetMouseButtonDown(1) || Input.GetAxis("Fire2") > 0.1f;

        
    }



    void PlayerDie()
    {
        //GetComponent<CircleCollider2D>().enabled = false;
        playerGraphics.gameObject.SetActive(false);
        //CameraShake.Instance.ShakeCamera(0.3f, 0.2f);
        isDead = true;
    }

    void UpdateTime()
    {
        timeCounter += Time.deltaTime;
        //if (timeCounter >= PlayerRuntimeStats.instance.realTimeStats.currentMaxTime && !dieDoned) {
        //    dieDoned = true;
        //    PlayerDie();
        //    CircleTransition.instance.CloseBlackScreen("AbilityTree");
        //}
        //timeImage.fillAmount = timeCounter / PlayerRuntimeStats.instance.realTimeStats.currentMaxTime;
        //timeText.text = GetTimeString();
        //PlayerRuntimeStats.instance.gameStats.elapsedTime = timeCounter;
    }


    public string GetTimeString()
    {
        //int totalSeconds = (Mathf.CeilToInt(PlayerRuntimeStats.instance.realTimeStats.currentMaxTime - timeCounter));
        //int minutes = (totalSeconds / 60);
        //int seconds = totalSeconds % 60;


        //return string.Format("{0:00}:{1:00}", minutes, seconds);
        return "";
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        if (currentHealth <= 0)
        {
            PlayerDie();
        }
    }



}
