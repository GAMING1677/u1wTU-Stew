using UnityEngine;
using TMPro;

namespace ApprovalMonster.UI
{
    public class PostView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI contentText;
        [Header("Metrics")]
        [SerializeField] private TextMeshProUGUI likesText;
        [SerializeField] private TextMeshProUGUI rtText;
        [SerializeField] private TextMeshProUGUI repliesText;
        [SerializeField] private TextMeshProUGUI impressionsText;

        public void SetContent(string text, long impressionCount)
        {
            if (contentText != null)
            {
                contentText.text = text;
            }
            
            // Show Base Impressions
            if (impressionsText != null) impressionsText.text = impressionCount.ToString("N0");
            
            // Calculate Metrics
            // Likes: 20-50%
            int likes = Mathf.CeilToInt(impressionCount * Random.Range(0.2f, 0.5f));
            if (likesText != null) likesText.text = likes.ToString("N0");
            
            // RT: 5-60%
            int rt = Mathf.CeilToInt(impressionCount * Random.Range(0.05f, 0.6f));
            if (rtText != null) rtText.text = rt.ToString("N0");
            
            // Replies: 1-20%
            int replies = Mathf.CeilToInt(impressionCount * Random.Range(0.01f, 0.2f));
            if (repliesText != null) repliesText.text = replies.ToString("N0");
        }
    }
}
