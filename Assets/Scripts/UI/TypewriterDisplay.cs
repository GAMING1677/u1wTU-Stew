using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 文字列配列をタイプライター効果で順番に表示するコンポーネント
/// </summary>
public class TypewriterDisplay : MonoBehaviour
{
    [Header("表示設定")]
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private string[] messages = { "こんにちは", "おはよう" };

    [Header("タイプライター設定")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float typeSpeed = 0.05f; // 1文字あたりの秒数

    [Header("待機時間設定")]
    [SerializeField] private float delayBeforeStart = 0.5f; // 開始前の待機時間
    [SerializeField] private float delayBetweenMessages = 1.0f; // メッセージ間の待機時間

    [Header("ループ設定")]
    [SerializeField] private bool loop = true;

    private Coroutine displayCoroutine;

    private void Start()
    {
        Debug.Log("[TypewriterDisplay] Start() called");
        StartDisplay();
    }

    /// <summary>
    /// 表示を開始する
    /// </summary>
    public void StartDisplay()
    {
        Debug.Log("[TypewriterDisplay] StartDisplay() called");
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }
        displayCoroutine = StartCoroutine(DisplayMessagesCoroutine());
    }

    /// <summary>
    /// 表示を停止する
    /// </summary>
    public void StopDisplay()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }
    }

    private IEnumerator DisplayMessagesCoroutine()
    {
        Debug.Log($"[TypewriterDisplay] DisplayMessagesCoroutine started. targetText={targetText}, messages.Length={messages?.Length ?? 0}");
        
        if (targetText == null)
        {
            Debug.LogError("[TypewriterDisplay] targetText is NULL!");
            yield break;
        }
        
        if (messages == null || messages.Length == 0)
        {
            Debug.LogError("[TypewriterDisplay] messages is NULL or empty!");
            yield break;
        }

        Debug.Log($"[TypewriterDisplay] Waiting {delayBeforeStart}s before start...");
        yield return new WaitForSecondsRealtime(delayBeforeStart);

        do
        {
            foreach (string message in messages)
            {
                Debug.Log($"[TypewriterDisplay] Displaying message: {message}");
                
                if (useTypewriter)
                {
                    yield return StartCoroutine(TypewriteText(message));
                }
                else
                {
                    targetText.text = message;
                }

                Debug.Log($"[TypewriterDisplay] Waiting {delayBetweenMessages}s before next message...");
                yield return new WaitForSecondsRealtime(delayBetweenMessages);
            }
            Debug.Log("[TypewriterDisplay] All messages displayed. Loop=" + loop);
        } while (loop);
    }

    private IEnumerator TypewriteText(string text)
    {
        targetText.text = "";

        foreach (char c in text)
        {
            targetText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        Debug.Log($"[TypewriterDisplay] Finished typing: {text}");
    }

    /// <summary>
    /// インスペクターからメッセージ配列を設定する
    /// </summary>
    public void SetMessages(string[] newMessages)
    {
        messages = newMessages;
    }
}
