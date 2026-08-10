using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 분기 실적 발표 전용 팝업
// 사업 결과·배당·실적 공시가 한꺼번에 쏟아져서 메인 알림창이 감당하지 못하므로 따로 뺐다
// 화면을 전환하는 것이 아니라 위에 덮어씌우는 방식이라, 닫으면 보던 화면으로 그대로 돌아간다
public class QuarterReportPanel : MonoBehaviour
{
    [Header("표시 대상")]
    public TextMeshProUGUI titleText;   // 상단 제목 (예: "3분기 실적 발표")
    public TextMeshProUGUI bodyText;    // 발표 내용 전체
    public RectTransform scrollContent; // 내용이 길어지면 늘어나는 스크롤 영역

    void Awake()
    {
        gameObject.SetActive(false); // 게임 시작 시에는 꺼둔다
    }

    // 분기 결산이 끝나면 GameManager가 호출한다
    public void Show(int quarter, List<string> lines)
    {
        // 발표할 내용이 없으면 굳이 팝업을 띄우지 않는다 (사업도 배당도 없는 조용한 분기)
        if (lines == null || lines.Count == 0) return;

        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // 다른 UI에 가리지 않도록 맨 앞으로

        if (titleText != null) titleText.text = $"{quarter}분기 실적 발표";
        if (bodyText != null) bodyText.text = string.Join("\n", lines);

        // 내용 줄 수에 맞춰 스크롤 영역 높이를 다시 잡는다 (안 하면 긴 내용이 잘린다)
        if (scrollContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
    }

    // 닫기 버튼 OnClick에 연결
    public void OnCloseButton()
    {
        gameObject.SetActive(false);
    }
}
