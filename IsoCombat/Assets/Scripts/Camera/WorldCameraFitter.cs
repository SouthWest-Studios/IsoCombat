using UnityEngine;

public class WorldCameraFitter : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Transform worldMin;
    [SerializeField] private Transform worldMax;

    private float lastAspect = -1f;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        FitCameraToWorld();
    }

    void FitCameraToWorld()
    {
        float worldWidth = worldMax.position.x - worldMin.position.x;
        float worldHeight = worldMax.position.y - worldMin.position.y;

        float screenAspect = (float)Screen.width / Screen.height;
        float orthoByHeight = worldHeight / 2f;
        float orthoByWidth = worldWidth / (2f * screenAspect);

        cam.orthographicSize = Mathf.Max(orthoByHeight, orthoByWidth);

        Vector3 center = (worldMin.position + worldMax.position) * 0.5f;
        cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);

        lastAspect = screenAspect;
    }

    void Update()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        if (!Mathf.Approximately(currentAspect, lastAspect))
        {
            FitCameraToWorld();
        }
    }
}
