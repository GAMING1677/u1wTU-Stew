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
        [SerializeField] private RectTransform panelTransform; // パネル本体のTransform（スケールアニメーション用）
        [SerializeField] private float fadeDuration = 0.3f;
        
        [Header("Open/Close Animation")]
        [SerializeField] private float openScale = 1.0f;
        [SerializeField] private float closeScale = 0.8f;
        [SerializeField] private Ease openEase = Ease.OutBack;
        [SerializeField] private Ease closeEase = Ease.InBack;
        
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
            
            // 初期スケール設定
            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.one * closeScale;
            }
        }
        
        /// <summary>
        /// パネルを表示
        /// </summary>
        public void Show()
        {
            Debug.Log("[SoundSettingsUI] Show() called");
            
            if (settingsPanel == null)
            {
                Debug.LogError("[SoundSettingsUI] settingsPanel is null!");
                return;
            }
            
            var audio = Core.AudioManager.Instance;
            if (audio == null)
            {
                Debug.LogError("[SoundSettingsUI] AudioManager.Instance is null!");
                // AudioManagerがなくても開けるようにする（仮）
                settingsPanel.SetActive(true);
                Debug.Log("[SoundSettingsUI] Panel opened without AudioManager");
                return;
            }
            
            Debug.Log("[SoundSettingsUI] Opening panel with AudioManager");
            Debug.Log($"[SoundSettingsUI] settingsPanel: name={settingsPanel.name}, InstanceID={settingsPanel.GetInstanceID()}");
            Debug.Log($"[SoundSettingsUI] Before: panel.active={settingsPanel.activeSelf}, canvasGroup.alpha={(canvasGroup != null ? canvasGroup.alpha.ToString() : "null")}, scale={(panelTransform != null ? panelTransform.localScale.ToString() : "null")}");
            
            settingsPanel.SetActive(true);
            
            // スライダーの初期値を設定
            InitializeSliders(audio);
            
            // アニメーションを一時的に無効化してテスト
            // スケールアニメーション（ポップイン）
            if (panelTransform != null)
            {
                panelTransform.DOKill();
                panelTransform.localScale = Vector3.one * openScale; // 即座に最終サイズに
            }
            
            // フェードイン - 一時的にアニメーションなしで即座に表示
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f; // 即座に不透明に
                canvasGroup.blocksRaycasts = true;
            }
            
            Debug.Log($"[SoundSettingsUI] After: panel.active={settingsPanel.activeSelf}, canvasGroup.alpha={(canvasGroup != null ? canvasGroup.alpha.ToString() : "null")}");
            
            audio.PlaySE(Data.SEType.ButtonClick);
            Debug.Log("[SoundSettingsUI] Show() completed");
        }
        
        /// <summary>
        /// パネルを非表示
        /// </summary>
        public void Hide()
        {
            Debug.Log("[SoundSettingsUI] Hide() called!");
            Debug.Log($"[SoundSettingsUI] Hide() StackTrace:\n{System.Environment.StackTrace}");
            
            if (settingsPanel == null) return;
            
            // 閉じるボタンのリアクション
            if (closeButton != null)
            {
                closeButton.transform.DOKill();
                closeButton.transform.DOPunchScale(Vector3.one * -0.1f, 0.15f, 5, 0.5f);
            }
            
            // スケールアニメーション（ポップアウト）
            if (panelTransform != null)
            {
                panelTransform.DOKill();
                panelTransform.DOScale(closeScale, fadeDuration).SetEase(closeEase);
            }
            
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, fadeDuration).OnComplete(() => {
                    settingsPanel.SetActive(false);
                    Debug.Log("[SoundSettingsUI] Panel set to inactive via DOFade.OnComplete");
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

