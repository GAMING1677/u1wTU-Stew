using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// サムネイル撮影用などに、登録したSpriteを一定間隔でループ再生するだけのシンプルなスクリプト。
    /// 既存のゲームシステムには依存していません。
    /// </summary>
    public class SimpleCharacterLoop : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("アニメーションさせる対象のImage（未設定ならこのオブジェクトのImageを使用）")]
        [SerializeField] private Image targetImage;

        [Tooltip("ループ再生する画像のリスト")]
        [SerializeField] private List<Sprite> frames;

        [Tooltip("切り替え間隔（秒）")]
        [SerializeField] private float interval = 0.5f;

        private float timer;
        private int currentIndex;

        private void Start()
        {
            // Imageが割り当てられていなければ自身のImageを取得
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            // 初期画像の設定
            UpdateSprite();
        }

        private void Update()
        {
            // 画像リストが空なら何もしない
            if (frames == null || frames.Count == 0) return;
            if (targetImage == null) return;

            timer += Time.deltaTime;

            if (timer >= interval)
            {
                timer = 0f;
                currentIndex++;
                
                // インデックスをループさせる
                if (currentIndex >= frames.Count)
                {
                    currentIndex = 0;
                }

                UpdateSprite();
            }
        }

        private void UpdateSprite()
        {
            if (frames != null && frames.Count > 0 && targetImage != null)
            {
                targetImage.sprite = frames[currentIndex];
            }
        }
    }
}
