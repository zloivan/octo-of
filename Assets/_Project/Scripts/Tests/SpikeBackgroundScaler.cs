using _Project.Scripts.Tests;
using UnityEngine;
using Naninovel;

public class SpikeBackgroundScaler : MonoBehaviour
{
    [SerializeField] private string backgroundId = "MainBackground";
    [SerializeField] private Vector2 testUV = Vector2.zero; // (0,0) = режим чтения UV
    [SerializeField] private RectTransform greenSquare;

    private void Start()
    {
        if (Engine.Initialized) StartCoroutine(WaitAndLog());
        else Engine.OnInitializationFinished += OnEngineInitialized;
    }

    private void OnDestroy()
    {
        Engine.OnInitializationFinished -= OnEngineInitialized;
    }

    private void OnEngineInitialized()
    {
        Engine.OnInitializationFinished -= OnEngineInitialized;
        StartCoroutine(WaitAndLog());
    }

    private System.Collections.IEnumerator WaitAndLog()
    {
        yield return new WaitForSeconds(1f);

        var bgManager = Engine.GetService<IBackgroundManager>();
        if (!bgManager.ActorExists(backgroundId))
        {
            Debug.LogError($"[Spike] Actor '{backgroundId}' not found");
            yield break;
        }

        var uiManager = Engine.GetService<IUIManager>();
        var overlayUI = uiManager.GetUI<SpikeOverlayUI>();
        if (overlayUI == null)
        {
            Debug.LogError("[Spike] SpikeOverlayUI не найден");
            yield break;
        }

        var transitional = GetTransitionalRenderer();
        if (transitional == null) yield break;

        var canvasRect = overlayUI.GetComponent<RectTransform>();
        var bounds = transitional.Bounds;

        Debug.Log($"[Spike] === {Screen.width}x{Screen.height} (AR {(float)Screen.width / Screen.height:F2}) ===");

        if (testUV == Vector2.zero)
        {
            // Режим 1: читаем UV текущей позиции квадрата
            if (greenSquare == null)
            {
                Debug.LogError("[Spike] greenSquare не назначен");
                yield break;
            }
            var uv = ScreenPosToUV(greenSquare, transitional);
            Debug.Log($"[Spike] Green square UV = {uv}  → скопируй в testUV");
        }
        else
        {
            // Режим 2: позиционируем квадрат по UV
            PositionByUV(canvasRect, bounds);
        }
    }

    private void PositionByUV(RectTransform canvasRect, Rect bounds)
    {
        if (greenSquare == null)
        {
            Debug.LogError("[Spike] greenSquare не назначен");
            return;
        }

        var cam = Engine.GetService<ICameraManager>().Camera;
        var uiCam = Engine.GetService<ICameraManager>().UICamera;

        Vector3 worldPos = new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, testUV.x),
            Mathf.Lerp(bounds.min.y, bounds.max.y, testUV.y),
            0f
        );

        Vector2 screenPoint = cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPoint, uiCam, out Vector2 localPoint
        );

        greenSquare.anchoredPosition = localPoint;
        Debug.Log($"[Spike] Positioned at UV={testUV} → localPos={localPoint}");
    }

    private Vector2 ScreenPosToUV(RectTransform rectTransform, TransitionalSpriteRenderer transitional)
    {
        var cam = Engine.GetService<ICameraManager>().Camera;
        var uiCam = Engine.GetService<ICameraManager>().UICamera;

        // UI world pos → screen
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, rectTransform.position);
    
        // Screen → world (используем z камеры фона)
        float z = cam.transform.position.z + cam.nearClipPlane;
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, z));

        // Bounds в world space через трансформ меша
        var t = transitional.transform;
        var localBounds = transitional.Bounds;
        Vector3 worldMin = t.TransformPoint(new Vector3(localBounds.min.x, localBounds.min.y, 0));
        Vector3 worldMax = t.TransformPoint(new Vector3(localBounds.max.x, localBounds.max.y, 0));

        return new Vector2(
            Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x),
            Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.y)
        );
    }

    private TransitionalSpriteRenderer GetTransitionalRenderer()
    {
        var bgGO = GameObject.Find(backgroundId);
        if (bgGO == null)
        {
            Debug.LogError($"[Spike] GameObject '{backgroundId}' not found");
            return null;
        }

        var transitional = bgGO.GetComponentInChildren<TransitionalSpriteRenderer>();
        if (transitional == null)
            Debug.LogError("[Spike] TransitionalSpriteRenderer not found");

        return transitional;
    }
}