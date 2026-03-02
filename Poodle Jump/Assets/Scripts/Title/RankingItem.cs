using UnityEngine;
using TMPro;

/// <summary>
/// 랭킹 리스트 한 줄(순위, 닉네임, 점수)을 표시하는 UI 아이템.
/// ScrollView Content 아래에 rankingItemPrefab으로 인스턴스됩니다.
/// </summary>
public class RankingItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    /// <summary>순위, 닉네임, 점수로 텍스트를 갱신합니다. rank가 0 이하면 순위/점수는 "-"로 표시합니다.</summary>
    public void SetData(int rank, string name, long score)
    {
        if (rankText != null)
            rankText.text = rank > 0 ? rank.ToString() : "-";
        if (nicknameText != null)
            nicknameText.text = name ?? "";
        if (scoreText != null)
            scoreText.text = rank > 0 ? score.ToString() : "-";
    }
}
