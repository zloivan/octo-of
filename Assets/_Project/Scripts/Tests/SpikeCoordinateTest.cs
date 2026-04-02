using Naninovel;
using UnityEngine;

namespace OnlyFarms.Tests
{
    public class SpikeCoordinateTest : MonoBehaviour
    {
        [Header("Assign in Inspector after Naninovel init")]
        public RectTransform canvasRect;    // RectTransform корневого Canvas UI
        public RectTransform[] markers;    // 4 маркера (UI Image 20x20px), по одному на угол

        private void Update()
        {
            if (!Engine.Initialized) return;

            var cameraManager = Engine.GetService<ICameraManager>();
            var mainCamera    = cameraManager.Camera;
            var uiCamera      = cameraManager.UICamera;

            // Найти Background actor в сцене
            var spriteRenderer = FindObjectOfType<TransitionalSpriteRenderer>();
            if (spriteRenderer == null) return;

            var mesh = spriteRenderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) return;

            // Матрица как в Graphics.DrawMesh — только localScale объекта
            var t = spriteRenderer.transform;
            var drawMatrix = Matrix4x4.TRS(t.position, t.rotation, t.localScale);

            // Четыре угла bounds меша в local space
            var b = mesh.bounds;
            Vector3[] localCorners = {
                new(b.min.x, b.min.y, 0), // bottom-left
                new(b.max.x, b.min.y, 0), // bottom-right
                new(b.max.x, b.max.y, 0), // top-right
                new(b.min.x, b.max.y, 0), // top-left
            };

            for (int i = 0; i < 4; i++)
            {
                // Local → World (через drawMatrix, не через Transform hierarchy)
                var worldPos  = drawMatrix.MultiplyPoint3x4(localCorners[i]);

                // World → Screen (через MainCamera)
                var screenPos = mainCamera.WorldToScreenPoint(worldPos);

                // Screen → Local в RectTransform Canvas
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, uiCamera, out var localUI);

                if (i < markers.Length)
                    markers[i].anchoredPosition = localUI;

                // Debug.Log($"Corner {i}: world={worldPos}, screen={screenPos}, UI local={localUI}");
            }
        }
    }
}