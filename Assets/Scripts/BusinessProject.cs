using UnityEngine;

// 사업 템플릿 — 회사가 벌일 수 있는 신사업 데이터
[CreateAssetMenu(fileName = "BusinessProject", menuName = "Scriptable Objects/BusinessProject")]
public class BusinessProject : ScriptableObject
{
    public string projectName;        // 사업 이름 (예: "미스릴 신광맥 개발")
    public Stock targetStock;         // 이 사업을 벌이는 회사
    public int investmentCost;        // 투자 비용 — 착수할 때 회사 금고에서 한 번에 빠져나간다
    public int durationQuarters;      // 사업 기간 (분기 수) — 분기 마감일에 결과가 발표되므로 턴이 아닌 분기로 센다

    [Range(0, 100)]
    public int successRate;           // 성공 확률 (%)

    // 성공은 크게, 실패는 작게 — 한 번 실패로 회사가 죽지 않지만 연속 실패하면 서서히 무너진다
    public int incomeBoost;           // 성공 시 수입 영구 증가량
    public int incomeDrop;            // 실패 시 수입 영구 감소량
    public int assetBoost;            // 성공 시 금고 증가량 (새 설비·광맥이 회사 재산이 된 것)

    // 뉴스 문구
    public string startNews;          // 착수 시 (예: "블레이즈마인이 미스릴 신광맥 개발에 착수했다")
    public string successNews;        // 성공 시 (예: "블레이즈마인의 미스릴 신광맥 개발이 대성공!")
    public string failNews;           // 실패 시 (예: "블레이즈마인의 신광맥에서 아무것도 나오지 않았다...")
}