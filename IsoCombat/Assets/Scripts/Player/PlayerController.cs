using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



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
    public bool canMove = true;

    //Movement
    public Camera mainCamera;
    private Vector2 targetPosition;

    public float rotationSpeed;
    private float screenMargin = 0.5f;

    //Dash
    public float dashMultiplier = 2f;
    public float dashDuration = 2f;
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;

    //Timer
    private float timeCounter = 0;
    private bool isRegenRunning = false;

    //Invulnerability
    [HideInInspector] public float invulnerabilityTimer = 0f;
    [HideInInspector] public float invulnerabilityCoolDownTimer = 0f;
    [HideInInspector] public float invulnerabilityCoolDown = 2f;

    //Physics

    PolygonCollider2D upperBody;
    BoxCollider2D lowerBody;

    public float haveDamage = 0;

    //Shooting

    public GameObject bulletPrefab;
    public float bulletSpeed;
    public float shootCooldown = 2f;

    private bool canShoot = true;

    public List<Rigidbody2D> bullets;


    private Transform worldMin;
    private Transform worldMax;

    //Invisible 
    public bool isInvisble = false;


    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        targetPosition = transform.position;
        timeCounter = 0;
        currentHealth = stats.Get(StatId.MaxHP);
        float scale = stats.Get(StatId.Scale);
        bulletSpeed = stats.Get(StatId.BulletSpeed);
        shootCooldown = stats.Get(StatId.BulletRate);
        transform.localScale = new Vector3(scale, scale, scale);
        playerSprite.GetComponent<SpriteRenderer>().material.SetFloat("_FillAmount", currentHealth / stats.Get(StatId.MaxHP));


        StartCoroutine(RegenRoutine());
        StartCoroutine(Invisible());


        worldMin = WorldControler.I.worldMin;
        worldMax = WorldControler.I.worldMax;

        if (bullets == null) bullets = new List<Rigidbody2D>();

    }

    // Update is called once per frame
    void Update()
    {
        if (isPlayerLocal && !isDead)
        {
            if (Input.GetMouseButton(0) && canDash && !isDashing)
                StartCoroutine(Dash());

            if (Input.GetMouseButton(1) && canShoot)
            {
                print("shoot");
                ShootBullet();
            }
        }

        UpdateTime();

        if (isPlayerLocal)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                PlayerDie();
            }

            if (canMove)
            {
                UpdateMovement();
            }


            if (Input.GetKeyDown(KeyCode.K))
            {
                TakeDamage(1);
            }

        }

        UpdateTime();
        UpdateInvulnerability();


    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        float dashTimer = 0f;

        while (Input.GetMouseButton(0))
        {
            if (dashTimer < dashDuration)
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
            else
            {
                canMove = false;
                yield return new WaitForSeconds(stats.Get(StatId.Stun));
                canMove = true;
                break;
            }
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
        {
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRot = Quaternion.AngleAxis(targetAngle, Vector3.forward);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }

        Vector2 forward = transform.up;
        Vector2 newPosition = currentPosition + forward * stats.Get(StatId.MoveSpeed) * Time.deltaTime;

        float minX = worldMin.position.x + screenMargin;
        float maxX = worldMax.position.x - screenMargin;
        float maxY = worldMin.position.y - screenMargin;
        float minY = worldMax.position.y + screenMargin;

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


     
        //if (timeCounter >= stats.Get(StatId.RegenSpeed))
        //{
        //    RegenRoutine();
        //    timeCounter = 0f;
        //}
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
        haveDamage += amount;
        if (isDead) return;

        SetHealth(amount);

        if (currentHealth <= 0)
        {
            PlayerDie();
        }
    }

    public void SetHealth(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
    }

    public void ShootBullet()
    {
        Vector3 offset = transform.up * 0.8f;
        Vector3 spawnPos = transform.position + offset;
        Quaternion bulletRotation = transform.rotation * Quaternion.Euler(0, 0, 90);

        canShoot = false;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, bulletRotation);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        bulletRb.linearVelocity = transform.up * bulletSpeed;
        bullets.Add(bulletRb);

        BulletNetInfo info = bullet.GetComponent<BulletNetInfo>();
        if (info != null)
        {
            info.ownerId = SessionConfig.ClientId;
            info.bulletId = bulletRb.GetInstanceID().ToString();
        }

        StartCoroutine(DestroyBullet(bulletRb, 5f));
        Invoke(nameof(ResetShoot), shootCooldown);
    }

    private IEnumerator DestroyBullet(Rigidbody2D bulletRb, float time)
    {
        yield return new WaitForSeconds(time);

        DestroyBullet(bulletRb);
    }

    public void DestroyBullet(Rigidbody2D bulletRb)
    {
        if (bulletRb != null)
        {
            bullets.Remove(bulletRb);
            Destroy(bulletRb.gameObject);
        }
    }

    void ResetShoot()
    {
        canShoot = true;
    }
    IEnumerator RegenRoutine()
    {
        
        while (isPlayerLocal && !isDead)
        {
            Debug.Log("currentHealth" + stats.Get(StatId.MaxHP));
            yield return new WaitForSeconds(stats.Get(StatId.RegenSpeed));
            if (currentHealth < stats.Get(StatId.MaxHP)) {
                currentHealth += stats.Get(StatId.Regen);
                haveDamage -= stats.Get(StatId.Regen);
            }
            else
            {
                currentHealth = stats.Get(StatId.MaxHP);
            }
        }
    }

    IEnumerator Invisible() {
       
        while (isPlayerLocal && !isDead)
        {

            Color OriColor = NetRuntime.colors[SessionConfig.ClientId];
            OriColor.a = 0.6f;
            isInvisble = true;
            this.GetComponentInChildren<SpriteRenderer>().color = OriColor;
            yield return new WaitForSeconds(stats.Get(StatId.InvisSpeed));
            Debug.Log("No Invisible");
            isInvisble = false;
            OriColor.a = 1f;
            this.GetComponentInChildren<SpriteRenderer>().color = OriColor;
            yield return new WaitForSeconds(stats.Get(StatId.InvisCount));
        }
    }




}
