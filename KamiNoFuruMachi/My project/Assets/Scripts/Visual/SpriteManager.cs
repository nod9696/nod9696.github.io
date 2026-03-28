// Assets/Scripts/Visual/SpriteManager.cs
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KamiNoFuruMachi
{
    public class SpriteManager : MonoBehaviour
    {
        [SerializeField] private Canvas _targetCanvas;
        [SerializeField] private float _leftX   = -400f;
        [SerializeField] private float _centerX =    0f;
        [SerializeField] private float _rightX  =  400f;
        [SerializeField] private float _baseY   = -100f;
        [SerializeField] private float _fadeInDuration  = 0.4f;
        [SerializeField] private float _fadeOutDuration = 0.4f;

        private readonly Dictionary<string, Image> _activeSprites = new();

        public async UniTask ShowCharacter(string charId, string pose, string position)
        {
            var req = Resources.LoadAsync<Sprite>($"Sprites/Characters/{charId}_{pose}");
            await req.ToUniTask();
            var sprite = req.asset as Sprite;
            if (sprite == null) { Debug.LogWarning($"[SpriteManager] Not found: {charId}_{pose}"); return; }

            if (!_activeSprites.TryGetValue(charId, out var image) || image == null)
                { image = CreateImage(charId); _activeSprites[charId] = image; }

            image.sprite = sprite;
            image.SetNativeSize();
            image.rectTransform.anchoredPosition = new Vector2(ResolveX(position), _baseY);
            image.gameObject.SetActive(true);
            image.color = new Color(1, 1, 1, 0);
            await image.DOFade(1f, _fadeInDuration).SetEase(Ease.OutQuad).ToUniTask();
        }

        public async UniTask HideCharacter(string charId)
        {
            if (!_activeSprites.TryGetValue(charId, out var image) || image == null) return;
            await image.DOFade(0f, _fadeOutDuration).SetEase(Ease.InQuad).ToUniTask();
            image.gameObject.SetActive(false);
        }

        public void ClearAll()
        {
            foreach (var kv in _activeSprites) if (kv.Value != null) Destroy(kv.Value.gameObject);
            _activeSprites.Clear();
        }

        private Image CreateImage(string charId)
        {
            var go  = new GameObject($"Character_{charId}");
            go.transform.SetParent(_targetCanvas.transform, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            var rt  = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0); rt.pivot = new Vector2(0.5f, 0);
            return img;
        }

        private float ResolveX(string pos) => pos switch { "left" => _leftX, "right" => _rightX, _ => _centerX };
    }
}
