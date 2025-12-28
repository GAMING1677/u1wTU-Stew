using UnityEngine;
using UnityEngine.UI;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// 背景画像を斜め方向に無限スクロールさせるコンポーネント
    /// 6枚の画像を2x3グリッドで配置し、Updateで移動
    /// </summary>
    public class InfiniteScrollBackground : MonoBehaviour
    {
        [Header("Images (2x3 Grid)")]
        [Tooltip("左上の画像")]
        [SerializeField] private RectTransform image0;
        [Tooltip("右上の画像")]
        [SerializeField] private RectTransform image1;
        [Tooltip("左中の画像")]
        [SerializeField] private RectTransform image2;
        [Tooltip("右中の画像")]
        [SerializeField] private RectTransform image3;
        [Tooltip("左下の画像")]
        [SerializeField] private RectTransform image4;
        [Tooltip("右下の画像")]
        [SerializeField] private RectTransform image5;
        
        [Header("Scroll Settings")]
        [Tooltip("スクロール速度（ピクセル/秒）")]
        [SerializeField] private float scrollSpeed = 50f;
        
        [Tooltip("スクロール方向（正規化される）")]
        [SerializeField] private Vector2 scrollDirection = new Vector2(-1f, 1f); // 右上から左下
        
        private RectTransform[] images;
        private Vector2 imageSize;
        private Vector2 normalizedDirection;
        
        private void Start()
        {
            // 画像配列を初期化（6枚）
            images = new RectTransform[] { image0, image1, image2, image3, image4, image5 };
            
            // 有効性チェック
            foreach (var img in images)
            {
                if (img == null)
                {
                    Debug.LogError("[InfiniteScrollBackground] One or more images are not assigned!");
                    enabled = false;
                    return;
                }
            }
            
            // 画像サイズを取得（全画像同じサイズを想定）
            imageSize = image0.sizeDelta;
            
            // 方向を正規化
            normalizedDirection = scrollDirection.normalized;
            
            Debug.Log($"[InfiniteScrollBackground] Started. ImageSize: {imageSize}, Direction: {normalizedDirection}");
        }
        
        private void Update()
        {
            if (images == null) return;
            
            // 移動量を計算
            Vector2 movement = normalizedDirection * scrollSpeed * Time.deltaTime;
            
            // 全画像を移動
            foreach (var img in images)
            {
                if (img == null) continue;
                img.anchoredPosition += movement;
            }
            
            // 画面外に出た画像を反対側にワープ
            WrapImages();
        }
        
        private void WrapImages()
        {
            foreach (var img in images)
            {
                if (img == null) continue;
                
                Vector2 pos = img.anchoredPosition;
                bool wrapped = false;
                
                // 横方向: 2枚分でワープ
                // 左に出すぎた → 右にワープ
                if (pos.x < -imageSize.x)
                {
                    pos.x += imageSize.x * 2;
                    wrapped = true;
                }
                // 右に出すぎた → 左にワープ
                else if (pos.x > imageSize.x)
                {
                    pos.x -= imageSize.x * 2;
                    wrapped = true;
                }
                
                // 縦方向: 3枚分でワープ
                // 上に出すぎた → 下にワープ
                if (pos.y > imageSize.y)
                {
                    pos.y -= imageSize.y * 3;
                    wrapped = true;
                }
                // 下に出すぎた → 上にワープ
                else if (pos.y < -imageSize.y * 2)
                {
                    pos.y += imageSize.y * 3;
                    wrapped = true;
                }
                
                if (wrapped)
                {
                    img.anchoredPosition = pos;
                }
            }
        }
        
        /// <summary>
        /// スクロール速度を変更
        /// </summary>
        public void SetScrollSpeed(float speed)
        {
            scrollSpeed = speed;
        }
        
        /// <summary>
        /// スクロールを一時停止
        /// </summary>
        public void Pause()
        {
            enabled = false;
        }
        
        /// <summary>
        /// スクロールを再開
        /// </summary>
        public void Resume()
        {
            enabled = true;
        }
    }
}

