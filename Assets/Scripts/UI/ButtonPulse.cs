using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// ボタンをパルスアニメーションで目立たせるコンポーネント
    /// </summary>
    public class ButtonPulse : MonoBehaviour
    {
        [Header("Pulse Settings")]
        [SerializeField] private float pulseDuration = 0.8f;
        [SerializeField] private float pulseScale = 1.15f;
        [SerializeField] private Ease pulseEase = Ease.InOutSine;
        
        private Tween pulseTween;
        private bool isPulsing = false;
        
        /// <summary>
        /// パルスアニメーションを開始
        /// </summary>
        public void StartPulse()
        {
            if (isPulsing) return;
            
            isPulsing = true;
            
            // 既存のTweenをキャンセル
            pulseTween?.Kill();
            
            // スケールを元に戻す
            transform.localScale = Vector3.one;
            
            // 無限ループのパルスアニメーション
            pulseTween = transform.DOScale(pulseScale, pulseDuration)
                .SetEase(pulseEase)
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        /// <summary>
        /// パルスアニメーションを停止
        /// </summary>
        public void StopPulse()
        {
            if (!isPulsing) return;
            
            isPulsing = false;
            
            // Tweenをキャンセル
            pulseTween?.Kill();
            
            // スケールを元に戻す（スムーズに）
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad);
        }
        
        private void OnDestroy()
        {
            // クリーンアップ
            pulseTween?.Kill();
        }
    }
}
