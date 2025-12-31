using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ApprovalMonster.UI
{
    /// <summary>
    /// クレジット表示パネルUI
    /// </summary>
    public class CreditsUI : MonoBehaviour
    {
        public static CreditsUI Instance { get; private set; }
        
        [Header("Panel")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton; // タイトル画面などに配置するボタン
        
        [Header("Content")]
        [SerializeField] private TextMeshProUGUI creditsText;
        
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.2f;
        
        private CanvasGroup _canvasGroup;
        
        // クレジット内容（MITライセンス・SIL OFL準拠）
        private const string CREDITS_CONTENT = @"【クレジット / Credits】

■ 使用フォント / Fonts

LINE Seed JP
Copyright 2022 LY Corporation
Licensed under SIL Open Font License 1.1
https://seed.line.me/

BIZ UDゴシック
Copyright 2022 The BIZ UDGothic Project Authors
Licensed under SIL Open Font License 1.1
https://github.com/googlefonts/morisawa-biz-ud-gothic

■ 使用ライブラリ / Libraries

unityroom-tweet
Copyright (c) naichilab
Licensed under MIT License
https://github.com/naichilab/unityroom-tweet

unityroom ランキング機能
https://unityroom.com

NaughtyAttributes
Copyright (c) 2017 Denis Rizov
Licensed under MIT License
https://github.com/dbrizov/NaughtyAttributes

■ 使用アセット / Assets
(Unity Asset Store licenses apply)

・DOTween Pro - Demigiant
・Easy Save 3 - Moodkie
・Feel - More Mountains
・Utage - 株式会社マッドネスト

■ 素材 / Materials

音楽・SE素材: 音ロジック
https://otologic.jp

イラスト素材: イラストAC
https://www.ac-illust.com

■ 開発環境 / Development
Unity - Unity Technologies

---
MIT License Notice:
Permission is hereby granted, free of charge, 
to any person obtaining a copy of this software 
and associated documentation files.

SIL Open Font License Notice:
This Font Software is licensed under the 
SIL Open Font License, Version 1.1.

---
";
        
        private void Awake()
        {
            // シングルトン
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                enabled = false;
                return;
            }
            
            _canvasGroup = creditsPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = creditsPanel.AddComponent<CanvasGroup>();
            }
            
            // ボタン設定
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            
            if (openButton != null)
            {
                openButton.onClick.AddListener(Show);
            }
            
            // クレジットテキスト設定
            if (creditsText != null)
            {
                creditsText.text = CREDITS_CONTENT;
            }
            
            // 初期状態で非表示
            creditsPanel.SetActive(false);
        }
        
        /// <summary>
        /// クレジットパネルを表示
        /// </summary>
        public void Show()
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            
            creditsPanel.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, fadeDuration);
        }
        
        /// <summary>
        /// クレジットパネルを非表示
        /// </summary>
        public void Hide()
        {
            Core.AudioManager.Instance?.PlaySE(Data.SEType.ButtonClick);
            
            _canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                creditsPanel.SetActive(false);
                _canvasGroup.blocksRaycasts = false;
            });
        }
    }
}
