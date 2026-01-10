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
        
        [Header("Flaming")]
        [ReadOnly] public int flamingSeeds = 0;
        [ReadOnly] public int flamingLevel = 0;
        [ReadOnly] public bool isOnFire = false;
        
        // Persistent modifiers
        private int bonusMaxMotivation = 0;
        public int MaxMotivation => settings != null ? settings.maxMotivation + bonusMaxMotivation : 3;
        public int MaxMental => settings != null ? settings.maxMental : 10;

        private bool hasTriggeredMonsterMode = false;
        
        // ========== スコア上限システム ==========
        public const long MAX_SCORE = 999_999_999L; // カンスト値
        private bool hasNotifiedMaxScore = false;

        [Header("Events")]
        public UnityEvent<int> onFollowersChanged;
        public UnityEvent<int, int> onMentalChanged; // current, max
        public UnityEvent<int, int> onMotivationChanged; // current, max
        public UnityEvent<long> onImpressionsChanged;
        public UnityEvent onMonsterModeTriggered;
        
        // Flaming event: seeds, level, isOnFire
        public System.Action<int, int, bool> onFlamingChanged;
        
        // Gain events for UI notification
        public UnityEvent<int> onFollowerGained;
        public UnityEvent<long, float> onImpressionGained; // amount, rate

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
            hasNotifiedMaxScore = false; // スコア上限通知フラグリセット
            
            // Reset flaming
            flamingSeeds = 0;
            flamingLevel = 0;
            isOnFire = false;
            
            // Reset infection
            infectionRate = 0f;

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
            
            // Notify gain if positive
            if (amount > 0)
            {
                onFollowerGained?.Invoke(amount);
            }
        }

        public long AddImpression(float rate)
        {
            // Monster Mode Multiplier check
            bool isMonster = currentMental <= settings.monsterThreshold; // OR use isMonsterMode
            float finalRate = rate * (isMonster ? settings.monsterModeMultiplier : 1.0f);
            
            long gained = (long)(currentFollowers * finalRate);
            totalImpressions += gained;
            
            // ========== スコア上限チェック ==========
            if (totalImpressions >= MAX_SCORE)
            {
                totalImpressions = MAX_SCORE;
                
                // 初回到達時のみ通知
                if (!hasNotifiedMaxScore)
                {
                    hasNotifiedMaxScore = true;
                    GameManager.Instance?.NotifyMaxScoreReached();
                    Debug.Log($"[ResourceManager] MAX SCORE REACHED: {MAX_SCORE:N0}");
                }
            }
            
            onImpressionsChanged?.Invoke(totalImpressions);
            
            // Notify gain if positive
            if (gained > 0)
            {
                onImpressionGained?.Invoke(gained, finalRate);
            }
            
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
        
        // ========== Flaming System ==========
        
        /// <summary>
        /// 種を追加。炎上中は炎上度に加算
        /// </summary>
        public void AddFlamingSeeds(int count)
        {
            if (isOnFire)
            {
                flamingLevel += count;
                Debug.Log($"[ResourceManager] Flaming: Added {count} to flamingLevel (on fire), total: {flamingLevel}");
            }
            else
            {
                flamingSeeds += count;
                Debug.Log($"[ResourceManager] Flaming: Added {count} seeds, total: {flamingSeeds}");
            }
            onFlamingChanged?.Invoke(flamingSeeds, flamingLevel, isOnFire);
        }
        
        /// <summary>
        /// 炎上判定。成功でseeds→level変換
        /// </summary>
        public bool TryTriggerFlaming(float rate)
        {
            if (isOnFire || flamingSeeds == 0) return false;
            
            if (Random.value <= rate)
            {
                flamingLevel = flamingSeeds;
                flamingSeeds = 0;
                isOnFire = true;
                Debug.Log($"[ResourceManager] Flaming TRIGGERED! Level: {flamingLevel}");
                onFlamingChanged?.Invoke(0, flamingLevel, true);
                return true;
            }
            Debug.Log($"[ResourceManager] Flaming avoided (roll failed). Seeds: {flamingSeeds}");
            return false;
        }
        
        /// <summary>
        /// 炎上ダメージを消費して取得
        /// </summary>
        public int ConsumeFlamingLevel()
        {
            int damage = flamingLevel;
            flamingLevel = 0;
            isOnFire = false;
            Debug.Log($"[ResourceManager] Consumed flamingLevel: {damage}");
            onFlamingChanged?.Invoke(flamingSeeds, 0, false);
            return damage;
        }
        
        /// <summary>
        /// ターン開始時に炎上状態をリセット（種は維持）
        /// </summary>
        public void ResetFlamingTurn()
        {
            flamingLevel = 0;
            isOnFire = false;
            Debug.Log($"[ResourceManager] Flaming turn reset. Seeds: {flamingSeeds}");
            onFlamingChanged?.Invoke(flamingSeeds, 0, false);
        }
        
        /// <summary>
        /// 全炎上パラメータリセット（ゲームリセット時）
        /// </summary>
        public void ResetFlaming()
        {
            flamingSeeds = 0;
            flamingLevel = 0;
            isOnFire = false;
            onFlamingChanged?.Invoke(0, 0, false);
        }
        
        /// <summary>
        /// スコア上限通知フラグのリセット（デバッグ用）
        /// </summary>
        public void ResetMaxScoreFlag()
        {
            hasNotifiedMaxScore = false;
            Debug.Log("[ResourceManager] Max score flag reset");
        }
        
        // ========== Infection System (Zombie Deck) ==========
        
        [Header("Infection")]
        [ReadOnly] public float infectionRate = 0f;
        
        public System.Action<float> onInfectionChanged;
        /// <summary>
        /// 感染度がリセットされた時に発火（previousRate, reducedAmount）
        /// </summary>
        public System.Action<float, float> onInfectionReset;
        
        /// <summary>
        /// 感染度を増減する
        /// </summary>
        public void AddInfection(float amount)
        {
            infectionRate = Mathf.Clamp(infectionRate + amount, 0f, 100f);
            Debug.Log($"[ResourceManager] Infection changed by {amount}, now: {infectionRate}%");
            onInfectionChanged?.Invoke(infectionRate);
        }
        
        /// <summary>
        /// 感染度をリセット（リシャッフル時、モンスターカードA使用時）
        /// </summary>
        /// <param name="reductionRate">減少率（0-100%、100%=完全リセット、50%=半減）</param>
        public float ResetInfection(float reductionRate = 100f)
        {
            float previousRate = infectionRate;
            float reduction = infectionRate * (reductionRate / 100f);
            infectionRate = Mathf.Clamp(infectionRate - reduction, 0f, 100f);
            Debug.Log($"[ResourceManager] Infection reduced by {reductionRate}%: {previousRate}% -> {infectionRate}%");
            onInfectionChanged?.Invoke(infectionRate);
            
            // 減少があった場合のみイベント発火
            if (reduction > 0)
            {
                onInfectionReset?.Invoke(previousRate, reduction);
            }
            
            return previousRate;
        }
        
        /// <summary>
        /// 感染度によるフォロワー減少量を計算
        /// </summary>
        public int CalculateInfectionPenalty()
        {
            if (infectionRate <= 0) return 0;
            return Mathf.FloorToInt(currentFollowers * (infectionRate / 100f));
        }
        
        /// <summary>
        /// 感染度ペナルティを適用（フォロワー減少）
        /// </summary>
        public void ApplyInfectionPenalty()
        {
            Debug.Log($"[ResourceManager] ApplyInfectionPenalty CALLED. infectionRate={infectionRate}, followers={currentFollowers}");
            int penalty = CalculateInfectionPenalty();
            Debug.Log($"[ResourceManager] ApplyInfectionPenalty: calculated penalty = {penalty}");
            if (penalty > 0)
            {
                currentFollowers -= penalty;
                if (currentFollowers < 0) currentFollowers = 0;
                Debug.Log($"[ResourceManager] Infection penalty: -{penalty} followers (rate: {infectionRate}%)");
                onFollowersChanged?.Invoke(currentFollowers);
            }
            else
            {
                Debug.Log($"[ResourceManager] ApplyInfectionPenalty: penalty is 0 or less, skipping");
            }
        }
    }
}
