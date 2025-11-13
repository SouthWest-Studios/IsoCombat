using UnityEngine;

public class WorldControler : MonoBehaviour
{

    public static WorldControler I;

    public Transform worldMin, worldMax;

    private void Awake()
    {
        I = this;
    }

}
