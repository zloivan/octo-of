using System.Threading;
using DG.Tweening;
using Naninovel;
using OnlyFarms.Presentation;
using TMPro;
using UnityEngine;

namespace OnlyFarms.UI
{
    public class QuestEntryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _displayText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Animation")]
        [SerializeField] private float _fadeOutDuration = 0.3f;

        [SerializeField] private float _fadeOutTargetScale = 0.85f;
        [SerializeField] private float _completePunchDuration = 0.2f;


        private QuestEntryViewModel _vm;

        public void Bind(QuestEntryViewModel vm)
        {
            _vm = vm;
            RefreshProgress();

            vm.OnProgressChanged += RefreshProgress;
            vm.OnCompleted += OnCompleted;
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);
            DOTween.Kill(_canvasGroup);

            if (_vm == null)
                return;

            _vm.OnProgressChanged -= RefreshProgress;
            _vm.OnCompleted -= OnCompleted;
        }

        private void RefreshProgress()
        {
            _displayText.text = _vm.GetObjectiveText();
            
            if (_vm.GetRequiredCount() > 1)
            {
                _progressText.gameObject.SetActive(true);
                _progressText.text = $" ({_vm.GetCurrentCount()} / {_vm.GetRequiredCount()})";
            }
            else
            {
                _progressText.gameObject.SetActive(false);
            }
        }

        private void OnCompleted()
        {
            transform
                .DOPunchScale(Vector3.one * 0.15f, _completePunchDuration, vibrato: 1, elasticity: 0.5f)
                .SetLink(gameObject);
        }

        public UniTask PlayFadeOutAsync(CancellationToken ct = default)
        {
            var cs = new UniTaskCompletionSource();

            var seq = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(0f, _fadeOutDuration))
                .Join(transform.DOScale(_fadeOutTargetScale, _fadeOutDuration).SetEase(Ease.InBack))
                .SetLink(gameObject)
                .OnComplete(() => cs.TrySetResult())
                .OnKill(() => cs.TrySetResult());

            ct.Register(() =>
            {
                if (seq.IsActive())
                    seq.Kill();
                cs.TrySetCanceled();
            });

            return cs.Task;
        }

        public void SelfDestroy() =>
            Destroy(gameObject);
    }
}