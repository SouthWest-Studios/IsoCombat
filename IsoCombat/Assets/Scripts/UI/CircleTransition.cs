using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CircleTransition : MonoBehaviour
{
    private Transform player = null;

    public bool showByDefault = false;
    public bool openedByDefault = false;

    public AudioClip fadeInSFX, fadeOutSFX;
    public float transitionDuration = 1;
    private AudioSource _audioSource;

    private Canvas _canvas;
    private Image _blackScreen;

    private Vector2 _playerCanvasPos;

    public static CircleTransition instance;

    private static readonly int RADIUS = Shader.PropertyToID("_Radius");
    private static readonly int CENTER_X = Shader.PropertyToID("_CenterX");
    private static readonly int CENTER_Y = Shader.PropertyToID("_CenterY");
    private static readonly int GLITCH_INTENSITY = Shader.PropertyToID("_GlitchIntensity");
    private static readonly int SCAN_LINES = Shader.PropertyToID("_ScanLines");
    private static readonly int NOISE_SCALE = Shader.PropertyToID("_NoiseScale");

    private float counter = 0;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _blackScreen = GetComponentInChildren<Image>();
        _audioSource = GetComponent<AudioSource>();

        if (GameObject.FindWithTag("Player"))
        {
            player = GameObject.FindWithTag("Player").transform;
        }


        if (instance == null && !showByDefault)
        {
            instance = this;
        }
    }

    private void Start()
    {
        DrawBlackScreen();
        if (!showByDefault)
        {
            if (!openedByDefault)
            {
                OpenBlackScreen();
            }
            else
            {
                var mat = _blackScreen.material;
                mat.SetFloat(RADIUS, 1f);
                mat.SetFloat(GLITCH_INTENSITY, 0.5f);
                mat.SetFloat(SCAN_LINES, 150f);
                mat.SetFloat(NOISE_SCALE, 15f);
            }

        }
        else
        {
            var mat = _blackScreen.material;
            mat.SetFloat(RADIUS, -0.5f);
            mat.SetFloat(GLITCH_INTENSITY, 0.5f);
            mat.SetFloat(SCAN_LINES, 150f);
            mat.SetFloat(NOISE_SCALE, 15f);
        }
    }

    private void Update()
    {
        if (showByDefault)
        {
            counter += Time.deltaTime;

            var mat = _blackScreen.material;

            mat.SetFloat(SCAN_LINES, 150f + Mathf.Sin(counter * 2f) * 10f);
            mat.SetFloat(NOISE_SCALE, 15f + Mathf.Sin(counter * 1.5f) * 5f);
            mat.SetFloat(GLITCH_INTENSITY, 0.5f + Mathf.Sin(counter * 3f) * 0.1f);
        }
    }

    public void OpenBlackScreen()
    {
        _audioSource.PlayOneShot(fadeInSFX);
        DrawBlackScreen();
        StartCoroutine(Transition(transitionDuration, 0, 1));
    }

    public void CloseBlackScreen(string sceneTarget = "")
    {
        _audioSource.PlayOneShot(fadeOutSFX);
        DrawBlackScreen();
        StartCoroutine(Transition(transitionDuration, 1, -0.5f, sceneTarget));
    }

    private void DrawBlackScreen()
    {
        var screenWidth = Screen.width;
        var screenHeight = Screen.height;
        var playerScreenPos = new Vector3(screenWidth / 2, screenHeight / 2, 0);


        if (player)
        {
            playerScreenPos = Camera.main.WorldToScreenPoint(player.position);
        }

        var canvasRect = _canvas.GetComponent<RectTransform>().rect;
        var canvasWidth = canvasRect.width;
        var canvasHeight = canvasRect.height;

        _playerCanvasPos = new Vector2
        {
            x = (playerScreenPos.x / screenWidth) * canvasWidth,
            y = (playerScreenPos.y / screenHeight) * canvasHeight,
        };

        var squareValue = 0f;
        if (canvasWidth > canvasHeight)
        {
            // Landscape
            squareValue = canvasWidth;
            _playerCanvasPos.y += (canvasWidth - canvasHeight) * 0.5f;
        }
        else
        {
            // Portrait            
            squareValue = canvasHeight;
            _playerCanvasPos.x += (canvasHeight - canvasWidth) * 0.5f;
        }

        _playerCanvasPos /= squareValue;

        var mat = _blackScreen.material;
        mat.SetFloat(CENTER_X, _playerCanvasPos.x);
        mat.SetFloat(CENTER_Y, _playerCanvasPos.y);

        _blackScreen.rectTransform.sizeDelta = new Vector2(squareValue, squareValue);
    }

    private IEnumerator Transition(float duration, float beginRadius, float endRadius, string sceneTarget = "")
    {


        var mat = _blackScreen.material;
        float time = 0f;

        // Set glitch parameters based on transition direction
        float targetGlitch = endRadius == 1 ? 0.5f : 0.8f; // More glitch when closing
        float targetScanLines = endRadius == 1 ? 150f : 200f;
        float targetNoiseScale = endRadius == 1 ? 15f : 25f;

        while (time <= duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Animate radius
            float radius = Mathf.Lerp(beginRadius, endRadius, t);
            mat.SetFloat(RADIUS, radius);

            // Animate glitch effects
            float glitchIntensity = Mathf.Lerp(targetGlitch, 0.1f, t);
            float scanLines = Mathf.Lerp(targetScanLines, 100f, t);
            float noiseScale = Mathf.Lerp(targetNoiseScale, 10f, t);

            mat.SetFloat(GLITCH_INTENSITY, glitchIntensity);
            mat.SetFloat(SCAN_LINES, scanLines);
            mat.SetFloat(NOISE_SCALE, noiseScale);

            yield return null;
        }

        if (!string.IsNullOrEmpty(sceneTarget))
        {

            switch (sceneTarget)
            {
                case "Quit":
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                                        Application.Quit();
#endif
                    break;

                case "NewGame":
                    DeleteProgress();
                    //if (PlayerRuntimeStats.instance)
                    //{
                    //    PlayerRuntimeStats.instance.ResetToBase();
                    //}
                    //SceneManager.LoadScene("Gameplay");

                    break;
                default:
                    SceneManager.LoadScene(sceneTarget);
                    break;
            }
        }

    }

    private void DeleteProgress()
    {
        string path = Application.persistentDataPath + "/save.json";

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            Debug.Log("Progreso borrado.");
        }
        else
        {
            Debug.Log("No hay progreso que borrar.");
        }

    }

    private void OnDestroy()
    {
        instance = null;
    }
}
