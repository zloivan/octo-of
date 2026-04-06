#if UNITY_EDITOR
using UnityEngine;

public class ProfilingHelper : MonoBehaviour
{
    [Header("Set 5-10 for profiling, 0 = unlimited")]
    public int targetFPS = 10;

    void Start()
    {
        Application.targetFrameRate = targetFPS;
        QualitySettings.vSyncCount = 0; // vSync может перебить targetFrameRate
    }
}
#endif