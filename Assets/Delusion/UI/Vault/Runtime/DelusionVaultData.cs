using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Delusion.UI.Vault
{
    [Serializable]
    public sealed class VaultHoldingItem
    {
        public string companyName;
        [Tooltip("Resources/DelusionVaultUI/Logos 아래의 확장자 없는 파일명")]
        public string logoResource;
        public int quantity;
        public int averagePrice;
        public int currentPrice;
        public Sprite logoOverride;

        public int EvaluatedValue => Mathf.Max(0, quantity) * Mathf.Max(0, currentPrice);
        public float ReturnPercent => averagePrice <= 0
            ? 0f
            : (currentPrice - averagePrice) / (float)averagePrice * 100f;
    }

    [Serializable]
    public sealed class VaultMajorShareholderItem
    {
        public string companyName;
        [Tooltip("Resources/DelusionVaultUI/Logos 아래의 확장자 없는 파일명")]
        public string logoResource;
        [Range(0f, 100f)] public float ownershipPercent;
        public bool isLargestShareholder = true;
        public Sprite logoOverride;
    }

    [Serializable]
    public sealed class DelusionVaultScreenData
    {
        [Header("상단 상태")]
        public string currentEra = "마법의 시대";
        public int currentGold = 5000;
        public int currentDay = 42;

        [Header("자산 요약")]
        [Tooltip("현금 + 주식 평가액 − 대출금 (RecalculateSummary가 계산한다)")]
        public int totalAssets = 12340;
        public int cash = 5000;
        [Tooltip("갚아야 할 대출금. 총 자산에서 빼진다")]
        public int debt = 0;
        [Tooltip("보유 주식을 살 때 쓴 돈의 합. 손익률의 기준이 된다")]
        public int purchaseCost = 0;
        [Tooltip("예전 방식(시작 자산 대비) 계산에 쓰던 값 — 지금은 표시에 쓰지 않는다")]
        public int startingAssets = 5000;
        [Tooltip("주식 평가액 − 매수 원금")]
        public int totalProfitAmount = 1920;
        [Tooltip("매수 원금 대비 손익률(%)")]
        public float totalProfitPercent = 18.4f;

        [Header("보유 종목")]
        public List<VaultHoldingItem> holdings = new List<VaultHoldingItem>();

        [Header("대주주")]
        public List<VaultMajorShareholderItem> majorShareholders = new List<VaultMajorShareholderItem>();
    }

    [Serializable]
    public sealed class VaultCompanyClickedEvent : UnityEvent<string> { }
}
