using UnityEngine;

// 사업 템플릿 — 회사가 벌일 수 있는 신사업 데이터
[CreateAssetMenu(fileName = "BusinessProject", menuName = "Scriptable Objects/BusinessProject")]
public class BusinessProject : ScriptableObject
{
    public string projectName;        // 사업 이름 (예: "미스릴 신광맥 개발")
    public Stock targetStock;         // 이 사업을 벌이는 회사
    public int investmentCost;        // 투자 비용 (사업 기간 동안 매턴 영업이익에서 차감)
    public int duration;              // 사업 기간 (턴 수)

    [Range(0, 100)]
    public int successRate;           // 성공 확률 (%)

    public int revenueBoost;          // 성공 시 baseRevenue 영구 증가량

    // 뉴스 문구
    public string startNews;          // 착수 시 (예: "블레이즈마인이 미스릴 신광맥 개발에 착수했다")
    public string successNews;        // 성공 시 (예: "블레이즈마인의 미스릴 신광맥 개발이 대성공!")
    public string failNews;           // 실패 시 (예: "블레이즈마인의 신광맥에서 아무것도 나오지 않았다...")
}