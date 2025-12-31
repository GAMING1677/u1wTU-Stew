using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using ApprovalMonster.Data;

namespace ApprovalMonster.UI
{
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        
        [Header("Flash Effect Curves")]
        [SerializeField] private AnimationCurve flashUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve flashDownCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private float flashDuration = 0.6f;
        
        private CharacterProfile currentProfile;
        private Coroutine idleCoroutine;
        private bool isReacting = false;
        private bool shouldLoopReaction = false;
        
        private void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();
        }
        
        /// <summary>
        /// フラッシュエフェクト付きでプロフィールを変更（UIFlashシェーダー使用）
        /// </summary>
        public void SetProfileWithFlash(CharacterProfile profile, float? customDuration = null)
        {
            float duration = customDuration ?? flashDuration;
            StartCoroutine(SetProfileWithFlashCo(profile, duration));
        }
        
        private IEnumerator SetProfileWithFlashCo(CharacterProfile profile, float duration)
        {
            Debug.Log($"[CharacterAnimator] SetProfileWithFlash START: {profile?.name}, duration: {duration}");
            
            StopIdle();
            
            if (targetImage != null && profile != null)
            {
                Material flashMat = targetImage.material;
                
                // シェーダーに_FlashIntensityプロパティがあるか確認
                if (flashMat != null && flashMat.HasProperty("_FlashIntensity"))
                {
                    Debug.Log("[CharacterAnimator] Using flash shader");
                    
                    // Phase 1: フラッシュ増加 (0 → 3)
                    float elapsed = 0f;
                    float flashUpTime = duration * 0.3f;
                    
                    while (elapsed < flashUpTime)
                    {
                        float t = elapsed / flashUpTime; // 0~1の正規化された時間
                        float curveValue = flashUpCurve.Evaluate(t); // カーブから値を取得
                        float intensity = curveValue * 3f; // 0~3にスケール
                        flashMat.SetFloat("_FlashIntensity", intensity);
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    
                    flashMat.SetFloat("_FlashIntensity", 3f);
                    Debug.Log("[CharacterAnimator] Phase 1 complete (flash max)");
                    
                    // Phase 2: スプライト変更
                    currentProfile = profile;
                    if (profile.idleFrames != null && profile.idleFrames.Count > 0)
                    {
                        targetImage.sprite = profile.idleFrames[0];
                        Debug.Log($"[CharacterAnimator] Sprite changed to: {profile.idleFrames[0].name}");
                    }
                    
                    // Phase 3: フラッシュ減少 (3 → 0)
                    elapsed = 0f;
                    float flashDownTime = duration * 0.7f;
                    
                    while (elapsed < flashDownTime)
                    {
                        float t = elapsed / flashDownTime; // 0~1の正規化された時間
                        float curveValue = flashDownCurve.Evaluate(t); // カーブから値を取得
                        float intensity = curveValue * 3f; // カーブは1→0なので、3→0になる
                        flashMat.SetFloat("_FlashIntensity", intensity);
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    
                    flashMat.SetFloat("_FlashIntensity", 0f);
                    Debug.Log("[CharacterAnimator] Phase 3 complete (flash end)");
                }
                else
                {
                    // シェーダーがない、またはプロパティがない場合はフォールバック
                    Debug.LogWarning("[CharacterAnimator] Flash shader not available, using instant switch");
                    SetProfile(profile);
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning("[CharacterAnimator] targetImage or profile is null");
                yield break;
            }
            
            // idleアニメ再生
            if (gameObject.activeInHierarchy)
            {
                StartIdle();
            }
            

        }
        
        public void SetProfile(CharacterProfile profile)
        {

            currentProfile = profile;
            
            // Stop existing animation
            StopIdle();
            
            if (currentProfile != null && targetImage != null)
            {
                // Set initial sprite
                if (currentProfile.idleFrames != null && currentProfile.idleFrames.Count > 0)
                {
                    targetImage.sprite = currentProfile.idleFrames[0];
                    // Removed SetNativeSize
                    
                    // Only start coroutine if active
                    if (gameObject.activeInHierarchy)
                    {
                        StartIdle();
                    }
                }
                else if (currentProfile.reactionHappy_1 != null) // Fallback
                {
                    targetImage.sprite = currentProfile.reactionHappy_1;
                    // Removed SetNativeSize
                }
            }
        }
        
        private void OnEnable()
        {
            if (currentProfile != null && !isReacting)
            {
                StartIdle();
            }
        }
        
        private void OnDisable()
        {
            StopIdle();
            // 状態をリセット（再度有効化された時にアニメーションが正しく開始されるように）
            isReacting = false;
            shouldLoopReaction = false;
        }

        private void StartIdle()
        {
            if (idleCoroutine != null) StopCoroutine(idleCoroutine);
            
            // Safety check
            if (!gameObject.activeInHierarchy) return;
            
            idleCoroutine = StartCoroutine(IdleLoop());
        }
        
        private void StopIdle()
        {
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine);
                idleCoroutine = null;
            }
        }
        
        private IEnumerator IdleLoop()
        {
            if (currentProfile == null || currentProfile.idleFrames == null || currentProfile.idleFrames.Count == 0)
                yield break;
                
            Debug.Log("[CharacterAnimator] Starting Idle Loop");
                
            int frameIndex = 0;
            
            while (!isReacting)
            {
                // Play one full loop of animation
                for (int i = 0; i < currentProfile.idleFrames.Count; i++)
                {
                    targetImage.sprite = currentProfile.idleFrames[frameIndex];
                    frameIndex = (frameIndex + 1) % currentProfile.idleFrames.Count;
                    yield return new WaitForSeconds(currentProfile.frameRate);
                }
                
                // Random wait
                float waitTime = Random.Range(currentProfile.minIdleWait, currentProfile.maxIdleWait);
                // Debug.Log($"[CharacterAnimator] Idle loop finished. Waiting {waitTime:.2f}s");
                yield return new WaitForSeconds(waitTime);
            }
        }
        
        public enum ReactionType
        {
            Happy_1,
            Happy_2,
            Happy_3,
            Sad_1,
            Sad_2
        }
        
        public void PlayReaction(ReactionType type, bool loop = false)
        {
            if (currentProfile == null || isReacting) return;
            
            Sprite reactionSprite = null;
            AudioClip characterSE = null;
            SEType fallbackSEType = SEType.ReactionHappy_1; // デフォルト
            
            switch (type)
            {
                case ReactionType.Happy_1: 
                    reactionSprite = currentProfile.reactionHappy_1; 
                    characterSE = currentProfile.seHappy_1;
                    fallbackSEType = SEType.ReactionHappy_1;
                    break;
                case ReactionType.Happy_2: 
                    reactionSprite = currentProfile.reactionHappy_2; 
                    characterSE = currentProfile.seHappy_2;
                    fallbackSEType = SEType.ReactionHappy_2;
                    break;
                case ReactionType.Happy_3: 
                    reactionSprite = currentProfile.reactionHappy_3; 
                    characterSE = currentProfile.seHappy_3;
                    fallbackSEType = SEType.ReactionHappy_3;
                    break;
                case ReactionType.Sad_1: 
                    reactionSprite = currentProfile.reactionSad_1; 
                    characterSE = currentProfile.seSad_1;
                    fallbackSEType = SEType.ReactionSad_1;
                    break;
                case ReactionType.Sad_2: 
                    reactionSprite = currentProfile.reactionSad_2; 
                    characterSE = currentProfile.seSad_2;
                    fallbackSEType = SEType.ReactionSad_2;
                    break;
            }
            
            if (reactionSprite == null) return;
            
            // キャラ固有SEがあれば使用、なければデフォルトSEにフォールバック
            if (characterSE != null)
            {
                Core.AudioManager.Instance?.PlaySE(characterSE);
            }
            else
            {
                Core.AudioManager.Instance?.PlaySE(fallbackSEType);
            }
            
            shouldLoopReaction = loop;
            StartCoroutine(ReactionRoutine(reactionSprite, type));
        }
        
        public void StopCurrentReaction()
        {
            if (isReacting)
            {
                Debug.Log("[CharacterAnimator] StopCurrentReaction called - stopping loop and returning to idle");
                shouldLoopReaction = false;
                // The coroutine will handle cleanup on next iteration
            }
        }
        
        private IEnumerator ReactionRoutine(Sprite sprite, ReactionType type)
        {
            Debug.Log($"[CharacterAnimator] Reaction Start: {type}. Sprite: {(sprite != null ? sprite.name : "NULL")}. Loop: {shouldLoopReaction}");
            isReacting = true;
            StopIdle(); // Stop idle loop temporarily
            
            while (isReacting)
            {
                targetImage.sprite = sprite;
                // Removed SetNativeSize
                
                // Play Tween Effects
                PlayTweenEffect(type);
                
                yield return new WaitForSeconds(currentProfile.reactionDuration);
                
                // Check if we should continue looping
                if (!shouldLoopReaction)
                {
                    break;
                }
            }
            
            // Resume Idle
            isReacting = false;
            shouldLoopReaction = false;
            
            // Force reset transform to ensure no residual scale/rotation
            targetImage.transform.DOKill();
            targetImage.transform.localScale = Vector3.one;
            targetImage.transform.localRotation = Quaternion.identity;
            
            // Restore idle frame
            if (currentProfile.idleFrames != null && currentProfile.idleFrames.Count > 0)
            {
                targetImage.sprite = currentProfile.idleFrames[0];
                // Removed SetNativeSize
            }
            
            StartIdle();
            Debug.Log("[CharacterAnimator] Reaction End, Resuming Idle");
        }
        
        private void PlayTweenEffect(ReactionType type)
        {
            RectTransform rt = targetImage.rectTransform;
            
            switch (type)
            {
                case ReactionType.Happy_1:
                case ReactionType.Happy_2:
                case ReactionType.Happy_3:
                    // Pop animation
                    rt.DOKill();
                    rt.localScale = Vector3.one;
                    rt.DOPunchScale(Vector3.one * 0.2f, currentProfile.reactionDuration, 5, 0.5f);
                    break;
                    
                case ReactionType.Sad_1:
                case ReactionType.Sad_2:
                    // ぶるぶる震えるアニメーション（小刻み振動）
                    rt.DOKill();
                    Vector2 originalPos = rt.anchoredPosition;
                    
                    // DOTweenでシェイクアニメーション
                    rt.DOShakeAnchorPos(
                        duration: currentProfile.reactionDuration,
                        strength: 3f,  // 震えの強さ（ピクセル）
                        vibrato: 30,   // 振動回数（高いほど細かく震える）
                        randomness: 90,
                        snapping: false,
                        fadeOut: false
                    ).OnComplete(() => {
                        // 元の位置に戻す
                        rt.anchoredPosition = originalPos;
                    });
                    break;
            }
        }
        
        private void OnDestroy()
        {
            StopIdle();
            if (targetImage != null) targetImage.transform.DOKill();
        }
    }
}
