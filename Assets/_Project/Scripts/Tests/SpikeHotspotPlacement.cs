using System.Threading;
using Naninovel;
using UnityEngine;

namespace OnlyFarms.Tests
{
    public class SpikeHotspotPlacement : MonoBehaviour
    {
        public Sprite markerSprite;
        public Vector2 normalizedPosition = new(0.5f, 0.5f);

        private GameObject _marker;
        private MeshFilter _meshFilter;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            Engine.OnInitializationFinished -= OnEngineReady;
            Engine.OnInitializationFinished += OnEngineReady;
        }

        private void OnDisable()
        {
            Engine.OnInitializationFinished -= OnEngineReady;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            DestroyMarker();
        }

        private void OnEngineReady()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            WaitForBackground(_cts.Token).Forget();
        }

        private async UniTaskVoid WaitForBackground(CancellationToken token)
        {
            TransitionalSpriteRenderer bgRenderer = null;
            while (bgRenderer == null)
            {
                if (token.IsCancellationRequested) return;
                bgRenderer = FindObjectOfType<TransitionalSpriteRenderer>();
                await UniTask.Yield(token: token);
            }

            DestroyMarker();
            _spriteRenderer = bgRenderer;
            _meshFilter = bgRenderer.GetComponent<MeshFilter>();
            if (_meshFilter == null) { Debug.LogError("[Spike] MeshFilter не найден"); return; }

            _marker = new GameObject("HotspotMarker");
            _marker.transform.SetParent(bgRenderer.transform, worldPositionStays: false);
            var sr = _marker.AddComponent<SpriteRenderer>();
            sr.sprite = markerSprite;
            sr.sortingOrder = 10;
            _marker.transform.localScale = Vector3.one * 0.5f;
            UpdateMarkerPosition();
        }
        private TransitionalSpriteRenderer _spriteRenderer;
        private void Update()
        {
            if (_marker == null || _meshFilter == null) return;
    
            // Диагностика — убрать после теста
            Debug.Log($"[Spike] Resolution={Screen.width}x{Screen.height} | " +
                      $"bounds.size={_meshFilter.sharedMesh.bounds.size} | " +
                      $"PPU={_spriteRenderer.PixelsPerUnit}");
              
            UpdateMarkerPosition();
        }

        private void UpdateMarkerPosition()
        {
            var t = _spriteRenderer.transform;
            // Та же матрица что Naninovel использует в Graphics.DrawMesh
            var drawMatrix = Matrix4x4.TRS(t.position, t.rotation, t.localScale);
            var b = _meshFilter.sharedMesh.bounds;

            var meshLocalPos = new Vector3(
                (normalizedPosition.x - 0.5f) * b.size.x,
                (normalizedPosition.y - 0.5f) * b.size.y,
                0f
            );

            // Mesh local → World (минуя иерархию transform)
            var worldPos = drawMatrix.MultiplyPoint3x4(meshLocalPos);
            worldPos.z = t.position.z - 0.01f;

            // Ставим world position напрямую — parenting только для иерархии cleanup
            _marker.transform.position = worldPos;
            Debug.Log($"[Spike] t.localScale={t.localScale} | t.lossyScale={t.lossyScale} | " +
                      $"t.position={t.position} | parent.localScale={t.parent?.localScale}");
        }

        private void DestroyMarker()
        {
            if (_marker != null) Destroy(_marker);
            _marker = null;
           // _meshFilter = null;
        }
    }
}