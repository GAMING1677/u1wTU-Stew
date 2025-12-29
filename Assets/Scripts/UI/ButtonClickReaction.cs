using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// ボタンクリック時のリアクションを追加する汎用コンポーネント
    /// Buttonコンポーネントと同じGameObjectにアタッチして使用
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonClickReaction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Punch Scale Settings")]
        [Tooltip("クリック時のスケール変化量")]
        [SerializeField] private float punchScale = -0.1f;
        
        [Tooltip("アニメーション時間")]
        [SerializeField] private float duration = 0.15f;
        
        [Tooltip("振動回数")]
        [SerializeField] private int vibrato = 5;
        
        [Tooltip("弾性")]
        [SerializeField] private float elasticity = 0.5f;
        
        [Header("Press Down Settings")]
        [Tooltip("押下時のスケール")]
        [SerializeField] private float pressDownScale = 0.95f;
        
        [Tooltip("押下アニメーション時間")]
        [SerializeField] private float pressDownDuration = 0.08f;
        
        [Header("Options")]
        [Tooltip("クリック時にSEを再生するか")]
        [SerializeField] private bool playSE = false;
        
        [Tooltip("再生するSE（playSEがtrueの場合）")]
        [SerializeField] private Data.SEType seType = Data.SEType.ButtonClick;
        
        private Button button;
        private Vector3 originalScale;
        private bool isPressed = false;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            originalScale = transform.localScale;
            
            // onClickにリアクションを追加
            button.onClick.AddListener(OnButtonClick);
        }
        
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
            transform.DOKill();
        }
        
        /// <summary>
        /// ボタン押下時（PointerDown）
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            isPressed = true;
            transform.DOKill();
            transform.DOScale(originalScale * pressDownScale, pressDownDuration).SetEase(Ease.OutQuad);
        }
        
        /// <summary>
        /// ボタン離した時（PointerUp）
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPressed) return;
            
            isPressed = false;
            transform.DOKill();
            transform.localScale = originalScale;
        }
        
        /// <summary>
        /// ボタンクリック時
        /// </summary>
        private void OnButtonClick()
        {
            // パンチスケールアニメーション
            transform.DOKill();
            transform.localScale = originalScale;
            transform.DOPunchScale(Vector3.one * punchScale, duration, vibrato, elasticity);
            
            // SE再生
            if (playSE)
            {
                Core.AudioManager.Instance?.PlaySE(seType);
            }
        }
    }
}
