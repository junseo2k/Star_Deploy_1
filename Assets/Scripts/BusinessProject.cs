using UnityEngine;
using UnityEngine.Serialization;

// 사업 템플릿 — 회사가 벌일 수 있는 신사업 데이터
[CreateAssetMenu(fileName = "BusinessProject", menuName = "Scriptable Objects/BusinessProject")]
public class BusinessProject : ScriptableObject
{
    public string projectName;        // 사업 이름 (예: "미스릴 신광맥 개발")
    // 어느 회사의 사업인지는 Stock.businesses 배열이 이 에셋을 담는 쪽에서 정한다 — 여기서 회사를 지정하지 않는다
    public int investmentCost;        // 투자 비용 — 착수할 때 회사 금고에서 한 번에 빠져나간다
    public int durationQuarters;      // 사업 기간 (분기 수) — 분기 마감일에 결과가 발표되므로 턴이 아닌 분기로 센다

    [Range(0, 100)]
    public int successRate;           // 성공 확률 (%)

    // 성공은 크게, 실패는 작게 — 한 번 실패로 회사가 죽지 않지만 연속 실패하면 서서히 무너진다
    // FormerlySerializedAs: 이름만 바꾼 것이고 값은 그대로다 — 안 붙이면 이미 입력해둔 40개 사업의
    // 기존 incomeBoost/incomeDrop 값이 새 이름을 못 찾아 전부 0으로 초기화된다
    [FormerlySerializedAs("incomeBoost")]
    public int revenueBoost;          // 성공 시 평소 매출 영구 증가량 (순수입이 아니라 매출에 붙는다)
    [FormerlySerializedAs("incomeDrop")]
    public int revenueDrop;           // 실패 시 평소 매출 영구 감소량
    public int assetBoost;            // 성공 시 금고 증가량 (새 설비·광맥이 회사 재산이 된 것)

    // 뉴스 문구
    public string startNews;          // 착수 시 (예: "블레이즈마인이 미스릴 신광맥 개발에 착수했다")
    public string successNews;        // 성공 시 (예: "블레이즈마인의 미스릴 신광맥 개발이 대성공!")
    public string failNews;           // 실패 시 (예: "블레이즈마인의 신광맥에서 아무것도 나오지 않았다...")
}