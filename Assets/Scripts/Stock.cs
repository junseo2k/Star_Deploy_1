using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

// 주식 정보를 담는 ScriptableObject — 인스펙터에서 각 주식 데이터를 에셋으로 생성·관리
[CreateAssetMenu(fileName = "Stock", menuName = "Scriptable Objects/Stock")]
public class Stock : ScriptableObject
{
    [System.Serializable]
    public struct ProductInfo
    {
        public string productName;
        public Sprite productIcon;
    }
    public string StockName;        // 주식 이름
    public int StockBasePrice;      // 변동 전 최초 기준 가격
    public string StockDescription; // 주식 설명
    public string StockDetailDescription; // 상세 패널에 표시되는 기업 상세 설명
    public Sprite StockIcon;        // 주식 아이콘 이미지
    public string FamilyTag;        // 주식 분류 태그 — 종족 (예: Dwarf, Elf, Dragon, Human)
    public string IndustryTag;      // 주식 분류 태그 — 산업 (예: Mining, War)
    public int StockCurrentPrice;   // 사건 등으로 변동된 현재 주식 가격
    public int revenue;             // 기업 매출액
    public int OperatingProfit;     // 기업 영업이익
    public int marketCap;           // 시가총액
    public int TotalShare;          // 발행 주식 총 수량
    public string[] RecentIssue;    // 최근 주요 이슈 목록 (상세 패널 이슈 탭에 표시)
    public ProductInfo[] products;
    [System.NonSerialized] // 저장 안 되는 런타임 전용
    public List<Case> runtimeIssues = new List<Case>();
    public float StockChangePrice;
}
