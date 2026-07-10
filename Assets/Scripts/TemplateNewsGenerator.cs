using UnityEngine;

// NewsTemplate 하나를 받아서 무작위 조합으로 즉석 Case를 만들어주는 헬퍼
// 생성된 Case는 기존 GameManager.AddActiveNews()에 그대로 넘기면 됨
public static class TemplateNewsGenerator
{
    public static Case Generate(NewsTemplate template)
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

        return generated;
    }
}