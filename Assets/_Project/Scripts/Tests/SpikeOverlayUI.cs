using Naninovel.UI;
using UnityEngine;

namespace _Project.Scripts.Tests
{
    public class SpikeOverlayUI : CustomUI
    {
        [SerializeField] private RectTransform bottomLeft;
        [SerializeField] private RectTransform topLeft;
        [SerializeField] private RectTransform topRight;
        [SerializeField] private RectTransform bottomRight;

        public RectTransform[] GetCorners() =>
            new[] { bottomLeft, topLeft, topRight, bottomRight };
    }
}