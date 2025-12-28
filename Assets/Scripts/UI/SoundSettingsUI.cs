using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// サウンド設定UIコンポーネント
    /// シーンごとに配置し、AudioManager.Instanceを参照する
    /// </summary>
    public class SoundSettingsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.3f;
        
        [Header("Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider seSlider;
        
        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        
        private bool isInitialized = false;
        
        private void Start()
        {
            // 初期状態で非表示
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// パネルを表示
        /// </summary>
        public void Show()
        {
            if (settingsPanel == null) return;
            
            var audio = Core.AudioManager.Instance;
            if (audio == null)
            {
                Debug.LogError("[SoundSettingsUI] AudioManager.Instance is null!");
                return;
            }
            
            settingsPanel.SetActive(true);
            
            // スライダーの初期値を設定
            InitializeSliders(audio);
            
            // フェードイン
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.DOKill();
                canvasGroup.DOFade(1f, fadeDuration).OnComplete(() => {
                    canvasGroup.blocksRaycasts = true;
                });
            }
            
            audio.PlaySE(Data.SEType.ButtonClick);
        }
        
        /// <summary>
        /// パネルを非表示
        /// </summary>
        public void Hide()
        {
            if (settingsPanel == null) return;
            
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, fadeDuration).OnComplete(() => {
                    settingsPanel.SetActive(false);
                });
            }
            else
            {
                settingsPanel.SetActive(false);
            }
            
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
        }
        
        private void InitializeSliders(Core.AudioManager audio)
        {
            // リスナーを一度削除してから追加（重複防止）
            if (masterSlider != null)
            {
                masterSlider.onValueChanged.RemoveAllListeners();
                masterSlider.value = audio.GetMasterVolume();
                masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveAllListeners();
                bgmSlider.value = audio.GetBGMVolume();
                bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }
            
            if (seSlider != null)
            {
                seSlider.onValueChanged.RemoveAllListeners();
                seSlider.value = audio.GetSEVolume();
                seSlider.onValueChanged.AddListener(OnSEVolumeChanged);
                
                // SEスライダーにPointerUpイベントを追加（マウスを離したときにテスト音再生）
                AddPointerUpEvent(seSlider.gameObject, OnSESliderPointerUp);
            }
            
            // 閉じるボタン
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
            
            isInitialized = true;
        }
        
        private void AddPointerUpEvent(GameObject target, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            var eventTrigger = target.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = target.AddComponent<EventTrigger>();
            }
            
            // 既存のPointerUpエントリーを探すか、新規作成
            var entry = eventTrigger.triggers.Find(e => e.eventID == EventTriggerType.PointerUp);
            if (entry == null)
            {
                entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                eventTrigger.triggers.Add(entry);
            }
            
            entry.callback.RemoveAllListeners();
            entry.callback.AddListener(callback);
        }
        
        private void OnSESliderPointerUp(BaseEventData data)
        {
            // マウスを離したときにテスト音を再生
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
        }
        
        private void OnMasterVolumeChanged(float value)
        {
            Core.AudioManager.Instance?.SetMasterVolume(value);
        }
        
        private void OnBGMVolumeChanged(float value)
        {
            Core.AudioManager.Instance?.SetBGMVolume(value);
        }
        
        private void OnSEVolumeChanged(float value)
        {
            Core.AudioManager.Instance?.SetSEVolume(value);
        }
    }
}

