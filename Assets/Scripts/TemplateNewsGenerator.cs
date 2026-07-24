using UnityEngine;

// NewsTemplate 하나를 받아서 무작위 조합으로 즉석 Case를 만들어주는 헬퍼
// 생성된 Case는 확정 뉴스면 바로, 불확정 뉴스면 대기열을 거쳐 GameManager.AddActiveNews()로 넘기면 됨
public static class TemplateNewsGenerator
{
    // Generate 결과를 담는 상자 — Case 데이터와 확정/불확정 여부를 같이 전달하기 위함
    public struct GeneratedNews
    {
        public Case caseData;     // 생성된 뉴스 데이터
        public bool isUncertain;  // true면 불확정 뉴스 (효과가 나중에 나타남)
    }

    public static GeneratedNews? Generate(NewsTemplate template)
    {
        // 재료가 하나도 없으면 생성 불가
        if (template.events.Length == 0) return null;

        // 이벤트 풀에서 완성된 (문구+회사) 짝 하나를 통째로 뽑음 — 회사를 따로 뽑지 않음
        var picked = template.events[Random.Range(0, template.events.Length)];
        Stock subject = picked.targetStock;
        if (subject == null) return null;

        // 런타임 전용 Case 인스턴스 생성 (에셋으로 저장되지 않음, 메모리에서만 존재)
        Case generated = ScriptableObject.CreateInstance<Case>();
        generated.CaseName = picked.eventText;
        generated.CaseDescription = picked.eventText;
        generated.affectedCase = new Stock[] { subject };

        generated.ChangePriceMin = new int[] { picked.changeMin };
        generated.ChangePriceMax = new int[] { picked.changeMax };

        generated.lastingDay = template.lastingDay;
        generated.lowPercent = template.lowPercent;
        generated.newsType = Case.NewsType.SmallCompanyNews;

        return new GeneratedNews { caseData = generated, isUncertain = picked.isUncertain };
    }
}