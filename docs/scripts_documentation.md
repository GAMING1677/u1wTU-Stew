# スクリプトドキュメント (Scripts Documentation)

> [!NOTE]
> このドキュメントは `Assets/Scripts` 以下のソースコード構造と主要クラスの役割を解説するものです。

## 1. ディレクトリ構造
*   `Assets/Scripts/`
    *   `Core/`: ゲームのコアロジック、マネージャークラス。
    *   `Data/`: ScriptableObjectなどのデータ定義。
    *   `UI/`: ユーザーインターフェース制御、演出。
    *   `Editor/`: Unityエディタ拡張ツール。

## 2. 主要クラス・システム

### Core System (`Core/`)
ゲームの進行と状態管理を担う中核システム。

*   **GameManager** (`GameManager.cs`)
    *   **役割**: シングルトン。ゲームループ全体（開始、ターン進行、終了）を管理。
    *   **機能**:
        *   `StartGame()`, `EndGame()`: ゲームの開始・終了処理。
        *   `StartTurn()`, `EndTurn()`: ターンの切り替わり処理。
        *   `TryPlayCard()`: カードプレイの条件判定と実行、モンスターモードの制御。
        *   `CheckScoreClear()`: ノルマ達成状況の監視とステージクリア判定。
*   **ResourceManager** (`ResourceManager.cs`)
    *   **役割**: ゲーム内リソース（数値）の管理。
    *   **管理項目**:
        *   `CurrentImpression`: 現在の獲得インプレッション。
        *   `CurrentFollowers`: 現在のフォロワー数。
        *   `CurrentMental`: 現在のメンタル値。
        *   `Flaming`: 炎上システム（火種、炎上レベル、炎上状態）の管理。
        *   `Infection`: ゾンビデッキ用感染度の管理・ペナルティ計算。
*   **StageManager** (`StageManager.cs`)
    *   **役割**: ステージ進行の管理。
    *   **機能**: 全ステージリストの保持、ステージ選択、次のステージへの遷移。現在および将来のアンロック機能の基盤。
*   **AudioManager** (`AudioManager.cs`)
    *   **役割**: BGMとSE（効果音）の再生管理。
*   **DraftManager** (`DraftManager.cs`)
    *   **役割**: カードドラフト（報酬選択）シーンのロジック管理。
    *   **機能**: インプレッション数に応じた確率テーブル（DraftTierProbability）に基づき、Common/Rare/Epicの出現率を変動させる。
*   **SaveDataManager** (`SaveDataManager.cs`)
    *   **役割**: データの永続化管理（Easy Save 3 使用）。
    *   **管理項目**: ステージごとのハイスコア、クリア済みステージ、グローバルハイスコア。
*   **DeckManager** (`DeckManager.cs`)
    *   **役割**: デッキ（山札、手札、捨て札）の状態管理。
    *   **機能**: シャッフル、ドロー、カードの移動（手札→捨て札など）、特定カードのカウント。

### Data Structures (`Data/`)
ゲームの静的データ定義。ScriptableObjectを活用し、Unityエディタ上で調整可能になっている。

*   **CardData** (`CardData.cs`)
    *   カード1枚ごとの定義。
    *   `CardName`, `Description`: 表示用テキスト。
    *   `Cost`: 消費メンタル/モチベーション。
    *   `Effects`: インプレッション獲得倍率、フォロワー増加、特殊効果（ゾンビ化等）。
    *   `PlayCondition`: 特定モード限定などのプレイ条件。
    *   `Rarity`: Basic, Common, Rare, Epic。
*   **StageData** (`StageData.cs`)
    *   ステージごとの設定。
    *   `QuotaScore`: ターン毎の目標スコア。
    *   `MaxTurnCount`: 制限ターン数。
    *   `ClearCondition`: クリア条件定義。
    *   `MonsterModePreset`: モンスターモード用演出データ。
*   **GameSettings** (`GameSettings.cs`)
    *   ゲーム全体の定数（初期設定、モンスターモード閾値、確率設定など）を管理。
*   **CutInPreset** (`CutInPreset.cs`)
    *   カットイン演出用データ（テキスト、画像、サウンドのセット）。
*   **TutorialData** (`TutorialData.cs`)
    *   チュートリアルのステップごとの表示内容（テキスト、画像、ハイライト位置）。

### UI System (`UI/`)
画面表示とユーザー入力のハンドリング。DOTweenを多用したアニメーションが含まれる。

*   **UIManager** (`UIManager.cs`)
    *   **役割**: UI全体の統括マネージャー。各パネルの表示・非表示切り替え。
*   **DeckViewerUI** (`DeckViewerUI.cs`) / **CardPoolViewerUI** (`CardPoolViewerUI.cs`)
    *   **役割**: デッキ一覧やカードプール一覧の表示管理。
*   **TutorialPlayer** (`TutorialPlayer.cs`)
    *   **役割**: チュートリアル画面の制御。紙芝居形式の進行やスポットライト演出。
*   **SoundSettingsUI** (`SoundSettingsUI.cs`)
    *   **役割**: 音量設定画面の制御。スライダーによるボリューム調整。
*   **DraftRankTableUI** (`DraftRankTableUI.cs`)
    *   **役割**: ドラフト時のランク確率テーブル表示など。
*   **Common/Utility UI**:
    *   `ButtonClickReaction.cs`: ボタンクリック時の共通アニメーション（凹む、音が鳴る）。
    *   `TutorialSpotlight.cs`: 特定のUI要素以外を暗くする演出。

## 3. 外部ライブラリ・依存関係
*   **DOTween**: UIアニメーション（移動、フェード、スケール変化）全般で必須。
*   **TextMeshPro (TMPro)**: 高品質なテキスト描画に使用。

## 4. エディタ拡張 (`Editor/`)
*   **SetupTools.cs**: シーンの初期セットアップ（Canvas生成、必須Managerの配置など）を自動化するメニューコマンド。
