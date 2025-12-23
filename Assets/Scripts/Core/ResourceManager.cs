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
            currentMotivation = settings.maxMotivation;
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
            onMotivationChanged?.Invoke(currentMotivation, settings.maxMotivation);
            onImpressionsChanged?.Invoke(totalImpressions);
        }

        public void AddFollowers(int amount)
        {
            currentFollowers += amount;
            if (currentFollowers < 0) currentFollowers = 0;
            onFollowersChanged?.Invoke(currentFollowers);
        }

        public void AddImpression(float rate)
        {
            // Monster Mode Multiplier check
            bool isMonster = currentMental <= settings.monsterThreshold; // OR use isMonsterMode
            float finalRate = rate * (isMonster ? settings.monsterModeMultiplier : 1.0f);
            
            long gained = (long)(currentFollowers * finalRate);
            totalImpressions += gained;
            onImpressionsChanged?.Invoke(totalImpressions);
        }

        public void DamageMental(int amount)
        {
            currentMental -= amount;
            if (currentMental <= 0)
            {
                currentMental = 0;
                // Trigger Game Over logic handled by GameManager usually
            }
            else if (!hasTriggeredMonsterMode && currentMental <= settings.monsterThreshold)
            {
                // Trigger Monster Mode (Once per game session/stage)
                isMonsterMode = true;
                hasTriggeredMonsterMode = true;
                onMonsterModeTriggered?.Invoke();
                Debug.Log("[ResourceManager] Monster Mode Triggered!");
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
                onMotivationChanged?.Invoke(currentMotivation, settings.maxMotivation);
                return true;
            }
            return false;
        }

        public void ResetMotivation()
        {
            currentMotivation = settings.maxMotivation;
            onMotivationChanged?.Invoke(currentMotivation, settings.maxMotivation);
        }
    }
}
