using UnityEngine;
using UnityEngine.UI;
using ApprovalMonster.Core;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    public class TitleManager : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        
        [Header("Pulse Animation")]
        [SerializeField] private float pulseDuration = 1.2f;
        [SerializeField] private float pulseScale = 1.08f;
        
        private Tween pulseTween;

        private void Start()
        {
            // タイトル画面でメインテーマBGMを再生
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMainTheme();
            }
            
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartDates);
                Debug.Log("[TitleManager] Start button listener added.");
                
                // スタートボタンのパルスアニメーション開始
                StartPulseAnimation(startButton.transform);
            }
            else
            {
                Debug.LogError("[TitleManager] Start Button reference is missing!");
            }
        }
        
        private void StartPulseAnimation(Transform target)
        {
            target.localScale = Vector3.one;
            pulseTween = target.DOScale(pulseScale, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void OnStartDates()
        {
            // パルスを停止
            pulseTween?.Kill();
            
            // クリックリアクション（縮小→元に戻る）
            startButton.transform.DOKill();
            startButton.transform.localScale = Vector3.one;
            startButton.transform.DOPunchScale(Vector3.one * -0.1f, 0.2f, 5, 0.5f)
                .OnComplete(() => {
                    AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
                    Debug.Log("[TitleManager] Start Button Clicked. Going to Stage Select.");
                    SceneNavigator.Instance.GoToStageSelect();
                });
        }
        
        private void OnDestroy()
        {
            pulseTween?.Kill();
        }
    }
}

