using UnityEngine;

// 등락 표시처럼 여러 화면에서 똑같이 쓰이는 텍스트 포맷을 모아둔 헬퍼
// 색상 규칙(상승 빨강 / 하락 파랑)을 한곳에서만 관리하기 위함
public static class UIFormat
{
    public const string RiseColor = "#FF4444"; // 상승 빨강
    public const string FallColor = "#4488FF"; // 하락 파랑

    // 등락률을 부호·색상이 붙은 문자열로 변환 (예: "<color=#FF4444>+1.25%</color>")
    // decimals로 소수점 자릿수를 지정 (기본 2자리)
    public static string SignedPercent(float percent, int decimals = 2)
    {
        string sign = percent >= 0f ? "+" : "";
        string color = percent >= 0f ? RiseColor : FallColor;
        return $"<color={color}>{sign}{percent.ToString($"F{decimals}")}%</color>";
    }

    // 색상 없이 부호만 붙인 등락률 (예: "+3%")
    public static string SignedPercentPlain(int percent)
    {
        return percent > 0 ? $"+{percent}%" : $"{percent}%";
    }

    // 골드 금액에 천 단위 구분 기호를 붙인다 (예: 2930000 → "2,930,000 G")
    // 매출·영업이익처럼 자릿수가 큰 값을 읽기 쉽게 표시하기 위함
    // 매개변수가 long이라 int 값도 그대로 넣을 수 있다
    public static string Gold(long amount)
    {
        return $"{amount:N0} G";
    }

    // 체질을 한글 배지 문구로 변환 (상세 패널 배지에 사용)
    public static string CompanyTypeLabel(Stock.CompanyType type)
    {
        switch (type)
        {
            case Stock.CompanyType.Stable: return "안정형";
            case Stock.CompanyType.Resource: return "자원형";
            case Stock.CompanyType.HighRisk: return "고위험형";
            case Stock.CompanyType.Growth: return "성장형";
            default: return "";
        }
    }

    // 성격을 한글 배지 문구로 변환
    public static string PersonalityLabel(Stock.Personality personality)
    {
        switch (personality)
        {
            case Stock.Personality.DividendLover: return "배당 성애자";
            case Stock.Personality.BusinessManiac: return "사업 미치광이";
            case Stock.Personality.VaultKeeper: return "금고 지기";
            default: return "";
        }
    }

    // 소유주를 한글 배지 문구로 변환
    public static string OwnerLabel(Stock.OwnerType owner)
    {
        switch (owner)
        {
            case Stock.OwnerType.Family: return "가문 소유";
            case Stock.OwnerType.Guild: return "길드 소유";
            case Stock.OwnerType.Commoner: return "평민 소유";
            case Stock.OwnerType.Royal: return "왕실 소유";
            default: return "";
        }
    }
}
