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
            // Likes: 5-10%
            int likes = Mathf.CeilToInt(impressionCount * Random.Range(0.05f, 0.1f));
            if (likesText != null) likesText.text = likes.ToString("N0");
            
            // RT: 20-50%
            int rt = Mathf.CeilToInt(impressionCount * Random.Range(0.2f, 0.5f));
            if (rtText != null) rtText.text = rt.ToString("N0");
            
            // Replies: 10-30%
            int replies = Mathf.CeilToInt(impressionCount * Random.Range(0.1f, 0.3f));
            if (repliesText != null) repliesText.text = replies.ToString("N0");
        }
    }
}
