using System.Linq;
using Naninovel;
using OnlyFarms.Locations.Domain;
using OnlyFarms.Locations.Input;
using OnlyFarms.Locations.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace OnlyFarms.Locations
{
    //TODO: Этот класс имеет кучу херовых практик, пройтись и отрефакторить!
    // это скорее спаунер или факбрика хотспотов
    public class HotspotManager
    {
        private GameObject _container;
        private readonly MouseHotspotInput _mouseInput;
        private HotSpotView[] _hotspotViews;

        public HotspotManager(MouseHotspotInput mouseInput)
        {
            _mouseInput = mouseInput;
        }

        //TODO: Этот метод делает слишком много всего, нужно разбить на несколько
        //  Этот очень непонятные переменные, я думаю сюда нужно отдавать только то что
        //  реально нужно этому классу, ссылки на полные структуры данных тут лишние

        public async UniTask LoadAsync(AssetReference hotspotParentPrefabRef,
            HotspotData[] activeHotspots, float duration, AsyncToken ct)
        {
            Unload();

            _mouseInput.Clear();

            if (hotspotParentPrefabRef == null || !hotspotParentPrefabRef.RuntimeKeyIsValid())
                return;

            var handle = Addressables.LoadAssetAsync<GameObject>(hotspotParentPrefabRef);
            var prefab = await handle.Task.AsUniTask();

            _container = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(_container);
            SetAlpha(0f);

            _hotspotViews = _container.GetComponentsInChildren<HotSpotView>();

            foreach (var view in _hotspotViews)
            {
                var spot = activeHotspots.FirstOrDefault(h => h.Id == view.GetId());
                var active = spot != null;
                
                view.gameObject.SetActive(active);
                if (active)
                {
                    _mouseInput.Register(view);
                    view.SetShimmer(spot.Type == HotspotType.Item);
                }
            }

            await FadeAsync(0f, 1f, duration, ct);
        }

        public void DeactivateHotspot(string id)
        {
            if (_container == null)
                return;

            _hotspotViews.FirstOrDefault(v => v.GetId() == id)?.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (_container == null) 
                return;
            
            _container.SetActive(visible);
        }

        public void Unload()
        {
            if (_container == null)
                return;

            Object.Destroy(_container); //TODO: Обьект должен сам отвечать за свое уничтожение
            _container = null;
        }

        //TODO: Этот франкенштейн тут явно не к месту, класс не должен отвечать за детали отображения
        // к томуже почему тут не ислльзуется DoTween?
        private async UniTask FadeAsync(float from, float to, float duration, AsyncToken ct)
        {
            if (duration <= 0f)
            {
                SetAlpha(to);
                return;
            }

            var t = 0f;
            while (t < duration)
            {
                if (ct.Canceled)
                {
                    return;
                }

                t += Time.deltaTime;
                SetAlpha(Mathf.Lerp(from, to, t / duration));
                await UniTask.Yield(token: ct);
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            if (_container == null)
            {
                return;
            }

            //TODO: Это явно стоит отдать или самой View или кому то более специализированному,
            //  этот класс не должен отвечать за детали отображения
            var renderers = _container.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }
    }
}