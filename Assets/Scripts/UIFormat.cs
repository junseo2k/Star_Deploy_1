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
}
