using UnityEngine;
using System.Collections.Generic;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// サムネイル撮影用に、一定間隔でPostViewにポストを投稿するシンプルなスクリプト。
    /// 既存のゲームシステムには依存していません。
    /// </summary>
    public class SimplePostLoop : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("PostViewプレハブ")]
        [SerializeField] private GameObject postPrefab;
        
        [Tooltip("ポストを追加するコンテナ（Vertical Layout Group推奨）")]
        [SerializeField] private Transform timelineContainer;
        
        [Header("Settings")]
        [Tooltip("投稿間隔（秒）")]
        [SerializeField] private float postInterval = 2.0f;
        
        [Tooltip("表示する最大ポスト数")]
        [SerializeField] private int maxPosts = 5;
        
        [Tooltip("ループ再生するか（falseの場合、リストを1周したら停止）")]
        [SerializeField] private bool loop = true;
        
        [Header("Post Data")]
        [Tooltip("投稿するテキストのリスト")]
        [SerializeField] private List<string> postTexts = new List<string>()
        {
            "今日も配信がんばるぞ！",
            "フォロワーさんありがとう💕",
            "新曲できました！聴いてね🎵",
            "深夜テンションで草",
            "これはバズる予感..."
        };
        
        [Tooltip("インプレッション数の範囲（最小）")]
        [SerializeField] private long minImpressions = 100;
        
        [Tooltip("インプレッション数の範囲（最大）")]
        [SerializeField] private long maxImpressions = 10000;
        
        [Tooltip("ポストに使用するアイコン（オプション）")]
        [SerializeField] private Sprite postIcon;
        
        private float timer;
        private int currentIndex;
        private bool isRunning = true;

        private void Start()
        {
            // 最初のポストを即座に追加
            if (postTexts.Count > 0)
            {
                AddPost();
            }
        }

        private void Update()
        {
            if (!isRunning) return;
            if (postPrefab == null || timelineContainer == null) return;
            if (postTexts.Count == 0) return;

            timer += Time.deltaTime;

            if (timer >= postInterval)
            {
                timer = 0f;
                currentIndex++;

                // インデックス管理
                if (currentIndex >= postTexts.Count)
                {
                    if (loop)
                    {
                        currentIndex = 0;
                    }
                    else
                    {
                        isRunning = false;
                        return;
                    }
                }

                AddPost();
            }
        }

        private void AddPost()
        {
            // プレハブを生成
            GameObject postObj = Instantiate(postPrefab, timelineContainer);
            
            // PostViewコンポーネントを取得してデータを設定
            PostView view = postObj.GetComponent<PostView>();
            if (view != null)
            {
                string text = postTexts[currentIndex];
                long impressions = (long)Random.Range((float)minImpressions, (float)maxImpressions + 1);
                view.SetContent(text, impressions, postIcon);
            }
            
            // 新しい投稿を一番上に配置
            postObj.transform.SetAsFirstSibling();

            // 最大数を超えたら古いものを削除
            // Note: Destroy()は即座に削除しないため、whileではなくforで安全に回数制限
            int excessCount = timelineContainer.childCount - maxPosts;
            for (int i = 0; i < excessCount && timelineContainer.childCount > 0; i++)
            {
                // 一番下（最も古い）の子を削除
                Transform oldest = timelineContainer.GetChild(timelineContainer.childCount - 1);
                if (Application.isPlaying)
                {
                    Destroy(oldest.gameObject);
                }
                else
                {
                    DestroyImmediate(oldest.gameObject);
                }
            }
        }
        
        /// <summary>
        /// 外部から投稿を追加する
        /// </summary>
        public void TriggerPost()
        {
            if (postTexts.Count > 0)
            {
                AddPost();
                currentIndex = (currentIndex + 1) % postTexts.Count;
            }
        }
        
        /// <summary>
        /// 自動投稿の開始/停止
        /// </summary>
        public void SetRunning(bool running)
        {
            isRunning = running;
        }
    }
}
