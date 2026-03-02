using UnityEngine;
using TMPro;

/// <summary>
/// 원통 외곽에 배치되는 점수 마커. 닉네임 표시 및 추월 판정용 높이를 보관합니다.
/// </summary>
public class ScoreMarker : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nicknameText;

    /// <summary>마커가 나타내는 점수(높이). 월드 Y와 동일 단위.</summary>
    public float ScoreY { get; private set; }

    /// <summary>플레이어가 이 마커를 추월했는지 여부. 추월 후에도 cullBelowPlayer만큼 아래로 내려갈 때까지 화면에 유지됩니다.</summary>
    public bool IsOvertaken { get; set; }

    public int Rank { get; private set; }
    public string Nickname { get; private set; } = "";

    public void SetData(int rank, string nickname, float scoreY)
    {
        Rank = rank;
        Nickname = nickname ?? "";
        ScoreY = scoreY;
        if (nicknameText != null)
            nicknameText.text = nickname ?? "";
    }

    private void Reset()
    {
        nicknameText = GetComponentInChildren<TextMeshProUGUI>(true);
    }
}
