using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// チュートリアル用スポットライト効果
    /// 画面の一部だけを明るく（穴を開ける）して注目させる
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TutorialSpotlight : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float defaultDimAlpha = 0.7f;
        [SerializeField] private float animationDuration = 0.3f;
        
        private Image _image;
        private Material _material;
        private RectTransform _rectTransform;
        private Canvas _canvas;
        
        // シェーダープロパティID
        private static readonly int SpotlightCenterProperty = Shader.PropertyToID("_SpotlightCenter");
        private static readonly int SpotlightRadiusProperty = Shader.PropertyToID("_SpotlightRadius");
        private static readonly int EdgeSoftnessProperty = Shader.PropertyToID("_EdgeSoftness");
        
        private void Awake()
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            
            // マテリアルのインスタンスを作成
            if (_image.material != null)
            {
                _material = new Material(_image.material);
                _image.material = _material;
            }
        }
        
        /// <summary>
        /// スポットライトを表示（ターゲットなし = 全体を暗くする）
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            SetSpotlightRadius(0); // 半径0でスポットライト無効
        }
        
        /// <summary>
        /// スポットライトを特定のRectTransformにフォーカス
        /// </summary>
        public void FocusOn(RectTransform target, float radiusMultiplier = 1f, float edgeSoftness = 0.02f)
        {
            if (target == null)
            {
                SetSpotlightRadius(0);
                return;
            }
            
            gameObject.SetActive(true);
            
            // ターゲットの中心をスクリーン座標（0-1）に変換
            Vector2 screenCenter = GetNormalizedScreenPosition(target);
            
            // ターゲットのサイズから半径を計算
            float radius = CalculateRadius(target, radiusMultiplier);
            
            // アニメーション付きで設定
            AnimateSpotlight(screenCenter, radius, edgeSoftness);
        }
        
        /// <summary>
        /// スポットライトを特定の位置にフォーカス（手動指定）
        /// </summary>
        public void FocusOnPosition(Vector2 normalizedPosition, float normalizedRadius, float edgeSoftness = 0.02f)
        {
            gameObject.SetActive(true);
            AnimateSpotlight(normalizedPosition, normalizedRadius, edgeSoftness);
        }
        
        /// <summary>
        /// スポットライトを非表示
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// スポットライトをクリア（暗転のみにする）
        /// </summary>
        public void ClearFocus()
        {
            DOTween.To(GetSpotlightRadius, SetSpotlightRadius, 0f, animationDuration);
        }
        
        private Vector2 GetNormalizedScreenPosition(RectTransform target)
        {
            if (_canvas == null) return new Vector2(0.5f, 0.5f);
            
            // ワールド座標を取得
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) / 2f;
            
            // スクリーン座標に変換
            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
            
            // 正規化（0-1）
            return new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
        }
        
        private float CalculateRadius(RectTransform target, float multiplier)
        {
            // ターゲットのサイズを取得
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            
            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            
            float width = Mathf.Abs(screenMax.x - screenMin.x) / Screen.width;
            float height = Mathf.Abs(screenMax.y - screenMin.y) / Screen.height;
            
            // 対角線の半分を半径として使用
            float radius = Mathf.Sqrt(width * width + height * height) / 2f * multiplier;
            
            return radius;
        }
        
        private void AnimateSpotlight(Vector2 center, float radius, float edgeSoftness)
        {
            if (_material == null) return;
            
            // 現在の値を取得
            Vector2 currentCenter = _material.GetVector(SpotlightCenterProperty);
            float currentRadius = _material.GetFloat(SpotlightRadiusProperty);
            
            // DOTweenでアニメーション
            DOTween.To(() => currentCenter, v => _material.SetVector(SpotlightCenterProperty, v), center, animationDuration);
            DOTween.To(() => currentRadius, v => _material.SetFloat(SpotlightRadiusProperty, v), radius, animationDuration);
            _material.SetFloat(EdgeSoftnessProperty, edgeSoftness);
        }
        
        private float GetSpotlightRadius()
        {
            return _material != null ? _material.GetFloat(SpotlightRadiusProperty) : 0f;
        }
        
        private void SetSpotlightRadius(float value)
        {
            if (_material != null)
            {
                _material.SetFloat(SpotlightRadiusProperty, value);
            }
        }
        
        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
