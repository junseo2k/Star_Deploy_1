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

    // Show()가 여는 중인지 표시 — Awake의 자동 숨김과 부딪히지 않게 하려고 둔다
    private bool isOpening;

    void Awake()
    {
        // 이 오브젝트는 씬에 꺼진 채로 저장돼 있다. 꺼진 오브젝트의 Awake는 게임 시작 때가 아니라
        // 처음으로 SetActive(true)가 불리는 순간, 즉 첫 분기 발표 때 비로소 실행된다.
        // 그때 여기서 도로 꺼버리면 첫 발표(1분기)만 팝업이 사라진다 — 실제로 그래서 1분기 팝업이 안 떴다.
        // Show()가 연 활성화라면 건드리지 않고 그대로 둔다
        if (isOpening) return;

        gameObject.SetActive(false); // 씬에 켜둔 채 저장했더라도 게임 시작 시에는 꺼둔다
    }

    // 분기 결산이 끝나면 GameManager가 호출한다
    public void Show(int quarter, List<string> lines)
    {
        // 발표할 내용이 없으면 굳이 팝업을 띄우지 않는다 (사업도 배당도 없는 조용한 분기)
        if (lines == null || lines.Count == 0) return;

        // Awake가 아직 안 돈 상태에서 켜는 것일 수 있으므로, 그 사이에만 표시를 세워둔다
        isOpening = true;
        gameObject.SetActive(true);
        isOpening = false;

        transform.SetAsLastSibling(); // 다른 UI에 가리지 않도록 맨 앞으로

        if (titleText != null) titleText.text = $"{quarter}분기 실적 발표";
        if (bodyText != null) bodyText.text = BuildBody(lines);

        // 내용 줄 수에 맞춰 스크롤 영역 높이를 다시 잡는다 (안 하면 긴 내용이 잘린다)
        if (scrollContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
    }

    // 닫기 버튼 OnClick에 연결
    public void OnCloseButton()
    {
        gameObject.SetActive(false);
    }

    // ── 리포트 본문 구성 ──────────────────────────────

    // 한 분기에 수십 줄이 쏟아지므로 그냥 이어 붙이면 신사업 착수 같은 중요한 소식이 실적 목록에 묻힌다.
    // 그래서 말머리([신사업], [실적] 등)로 줄을 분류해 소제목이 붙은 묶음으로 다시 짠다.
    // 순서는 "급한 것 → 원인 → 결과" — 상장폐지/부도 → 사업 → 배당 → 실적
    private static readonly Section[] Sections =
    {
        new Section("■ 긴급",            new[] { "[상장폐지]", "[부도 위기]", "[경고]" }),
        new Section("■ 사업 결과",       new[] { "[사업 성공]", "[사업 실패]" }),
        new Section("■ 새로 시작한 사업", new[] { "[신사업]" }),
        new Section("■ 진행 중인 사업",   new[] { "[진행 중]" }),
        new Section("■ 배당",            new[] { "[배당]", "[배당 삭감]", "[배당 중단]" }),
        new Section("■ 실적 공시",       new[] { "[실적]" }),
    };

    // 소제목 하나와, 그 아래로 모을 말머리 목록
    private struct Section
    {
        public string title;
        public string[] prefixes;

        public Section(string title, string[] prefixes)
        {
            this.title = title;
            this.prefixes = prefixes;
        }
    }

    static string BuildBody(List<string> lines)
    {
        List<string> remaining = new List<string>(lines); // 어느 묶음에도 안 들어간 줄을 추리기 위한 사본
        List<string> result = new List<string>();

        foreach (Section section in Sections)
        {
            List<string> matched = new List<string>();
            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                if (!StartsWithAny(remaining[i], section.prefixes)) continue;
                matched.Add(remaining[i]);
                remaining.RemoveAt(i);
            }
            if (matched.Count == 0) continue;

            matched.Reverse(); // 뒤에서부터 훑었으므로 원래 순서로 되돌린다

            if (result.Count > 0) result.Add(""); // 묶음 사이에 빈 줄 하나
            result.Add($"{section.title} ({matched.Count}건)");
            result.AddRange(matched);
        }

        // 어느 말머리에도 안 걸린 줄(시대 소식 등)은 맨 아래에 그대로 붙인다
        if (remaining.Count > 0)
        {
            if (result.Count > 0) result.Add("");
            result.Add("■ 그 밖의 소식");
            result.AddRange(remaining);
        }

        return string.Join("\n", result);
    }

    static bool StartsWithAny(string line, string[] prefixes)
    {
        foreach (string prefix in prefixes)
            if (line.StartsWith(prefix)) return true;
        return false;
    }
}
