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
        
        private CharacterProfile currentProfile;
        private Coroutine idleCoroutine;
        private bool isReacting = false;
        private bool shouldLoopReaction = false;
        
        private void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();
        }
        
        public void SetProfile(CharacterProfile profile)
        {
            Debug.Log($"[CharacterAnimator] SetProfile called with: {(profile != null ? profile.name : "null")}");
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
            switch (type)
            {
                case ReactionType.Happy_1: reactionSprite = currentProfile.reactionHappy_1; break;
                case ReactionType.Happy_2: reactionSprite = currentProfile.reactionHappy_2; break;
                case ReactionType.Happy_3: reactionSprite = currentProfile.reactionHappy_3; break;
                case ReactionType.Sad_1: reactionSprite = currentProfile.reactionSad_1; break;
                case ReactionType.Sad_2: reactionSprite = currentProfile.reactionSad_2; break;
            }
            
            if (reactionSprite == null) return;
            
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
            targetImage.transform.DOKill(true); // Complete active tweens
            
            // Reset transforms just in case
            targetImage.transform.localScale = Vector3.one;
            targetImage.transform.localRotation = Quaternion.identity;
            
            switch (type)
            {
                case ReactionType.Happy_1:
                case ReactionType.Happy_2:
                    // Jump/Punch Scale
                    targetImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1);
                    break;
                
                case ReactionType.Happy_3:
                    // Big Jump / Rotate
                    targetImage.transform.DOPunchScale(Vector3.one * 0.3f, 0.6f, 10, 1);
                    targetImage.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.6f, 10, 1);
                    break;

                case ReactionType.Sad_1:
                    // Shake
                    targetImage.transform.DOShakePosition(0.5f, strength: 30f, vibrato: 20);
                    break;
                    
                case ReactionType.Sad_2:
                    // Small shake or punch rotation
                    targetImage.transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.5f);
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
