using UnityEngine;
using UnityEngine.Events;
using ApprovalMonster.Data;
using NaughtyAttributes;

namespace ApprovalMonster.Core
{
    public class ResourceManager : MonoBehaviour
    {
        [Header("Settings")]
        [Expandable]
        public GameSettings settings;

        [Header("Runtime Values")]
        [ReadOnly] public int currentFollowers;
        [ReadOnly] public int currentMental;
        [ReadOnly] public int currentMotivation;
        [ReadOnly] public long totalImpressions;
        [ReadOnly] public bool isMonsterMode = false;
        
        // Persistent modifiers
        private int bonusMaxMotivation = 0;
        public int MaxMotivation => settings != null ? settings.maxMotivation + bonusMaxMotivation : 3;
        public int MaxMental => settings != null ? settings.maxMental : 10;

        private bool hasTriggeredMonsterMode = false;

        [Header("Events")]
        public UnityEvent<int> onFollowersChanged;
        public UnityEvent<int, int> onMentalChanged; // current, max
        public UnityEvent<int, int> onMotivationChanged; // current, max
        public UnityEvent<long> onImpressionsChanged;
        public UnityEvent onMonsterModeTriggered;

        private void Start()
        {
            if (settings != null)
            {
                Initialize(settings);
            }
        }

        public void Initialize(GameSettings gameSettings)
        {
            settings = gameSettings;
            currentFollowers = settings.initialFollowers;
            currentMental = settings.maxMental;
            
            bonusMaxMotivation = 0; // Reset bonus
            currentMotivation = MaxMotivation;
            
            totalImpressions = 0;
            
            // Re-initialize flags
            isMonsterMode = false;
            hasTriggeredMonsterMode = false;

            BroadcastAll();
        }

        private void BroadcastAll()
        {
            onFollowersChanged?.Invoke(currentFollowers);
            onMentalChanged?.Invoke(currentMental, settings.maxMental);
            onMotivationChanged?.Invoke(currentMotivation, MaxMotivation);
            onImpressionsChanged?.Invoke(totalImpressions);
        }

        public void AddFollowers(int amount)
        {
            currentFollowers += amount;
            if (currentFollowers < 0) currentFollowers = 0;
            onFollowersChanged?.Invoke(currentFollowers);
        }

        public long AddImpression(float rate)
        {
            // Monster Mode Multiplier check
            bool isMonster = currentMental <= settings.monsterThreshold; // OR use isMonsterMode
            float finalRate = rate * (isMonster ? settings.monsterModeMultiplier : 1.0f);
            
            long gained = (long)(currentFollowers * finalRate);
            totalImpressions += gained;
            onImpressionsChanged?.Invoke(totalImpressions);
            
            return gained;
        }

        public void DamageMental(int amount)
        {
            Debug.Log($"[ResourceManager] DamageMental called: amount={amount}, currentMental={currentMental}");
            
            currentMental -= amount;
            
            Debug.Log($"[ResourceManager] After damage: currentMental={currentMental}, threshold={settings.monsterThreshold}, hasTriggeredMonsterMode={hasTriggeredMonsterMode}");
            
            if (currentMental <= 0)
            {
                currentMental = 0;
                Debug.Log("[ResourceManager] Mental <= 0, would trigger GameOver");
                // Trigger Game Over logic handled by GameManager usually
            }
            // REMOVED: Automatic monster mode triggering
            // GameManager now handles monster mode detection and triggering manually
            // This prevents immediate flag setting and allows proper sequencing with cut-ins
            /*
            else if (!hasTriggeredMonsterMode && currentMental <= settings.monsterThreshold)
            {
                Debug.Log($"[ResourceManager] *** MONSTER MODE CONDITIONS MET *** currentMental({currentMental}) <= threshold({settings.monsterThreshold}), hasTriggered={hasTriggeredMonsterMode}");
                
                // Trigger Monster Mode (Once per game session/stage)
                isMonsterMode = true;
                hasTriggeredMonsterMode = true;
                
                // Heal mental: currentMental / 2 (rounded up)
                int healAmount = Mathf.CeilToInt(currentMental / 2f);
                currentMental += healAmount;
                Debug.Log($"[ResourceManager] Monster Mode Triggered! Healed {healAmount} mental (from {currentMental - healAmount} to {currentMental})");
                
                // Invoke event
                Debug.Log($"[ResourceManager] Invoking onMonsterModeTriggered event. Listener count: {onMonsterModeTriggered?.GetPersistentEventCount()}");
                onMonsterModeTriggered?.Invoke();
                Debug.Log("[ResourceManager] onMonsterModeTriggered event invoked");
            }
            */
            else
            {
                Debug.Log($"[ResourceManager] Monster mode handled by GameManager. mental={currentMental}, threshold={settings.monsterThreshold}");
            }
            
            onMentalChanged?.Invoke(currentMental, settings.maxMental);
        }

        public void HealMental(int amount)
        {
            currentMental += amount;
            if (currentMental > settings.maxMental) currentMental = settings.maxMental;
            onMentalChanged?.Invoke(currentMental, settings.maxMental);
        }

        public bool UseMotivation(int amount)
        {
            if (currentMotivation >= amount)
            {
                currentMotivation -= amount;
                onMotivationChanged?.Invoke(currentMotivation, MaxMotivation);
                return true;
            }
            return false;
        }

        public void AddMotivation(int amount)
        {
            currentMotivation += amount;
            if (currentMotivation < 0) currentMotivation = 0;
            if (currentMotivation > MaxMotivation) currentMotivation = MaxMotivation;
            onMotivationChanged?.Invoke(currentMotivation, MaxMotivation);
        }

        public void IncreaseMaxMotivation(int amount)
        {
            bonusMaxMotivation += amount;
            // Update UI with new max
            onMotivationChanged?.Invoke(currentMotivation, MaxMotivation);
        }

        public void ResetMotivation()
        {
            currentMotivation = MaxMotivation;
            onMotivationChanged?.Invoke(currentMotivation, MaxMotivation);
        }
    }
}
