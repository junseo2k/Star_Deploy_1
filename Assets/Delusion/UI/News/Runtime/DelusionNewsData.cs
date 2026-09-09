using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Delusion.UI.News
{
    [Serializable]
    public sealed class NewsIndustryTrend
    {
        public string industryName;
        public bool isRising;
    }

    [Serializable]
    public sealed class NewsCompanyItem
    {
        public string companyName;
        public string headline;
        [TextArea(1, 2)] public string description;
        public float changePercent;
        [Tooltip("Resources/DelusionNewsUI/Logos 아래의 확장자 없는 파일명")]
        public string logoResource;
        public Sprite logoOverride;
    }

    [Serializable]
    public sealed class NewsMoverItem
    {
        public string companyName;
        public float changePercent;
        [Tooltip("Resources/DelusionNewsUI/Logos 아래의 확장자 없는 파일명")]
        public string logoResource;
        public Sprite logoOverride;
    }

    [Serializable]
    public sealed class DelusionNewsScreenData
    {
        [Header("상단 상태")]
        public string currentEra = "마법의 시대";
        public long currentGold = 5000;
        public int currentDay = 42;

        [Header("시대")]
        public string eraName = "마법의 시대";
        public int eraDay = 12;
        public int eraEstimatedDays = 30;
        [TextArea(1, 2)] public string eraSummary = "마법공학·제약이 성장하고, 광업·건설이 침체합니다";
        [Tooltip("Resources/DelusionNewsUI/Era 아래의 확장자 없는 파일명")]
        public string eraBackgroundResource = "magic_era_background";
        public List<NewsIndustryTrend> industryTrends = new List<NewsIndustryTrend>();

        [Header("오늘의 뉴스")]
        public List<NewsCompanyItem> todayNews = new List<NewsCompanyItem>();

        [Header("등락")]
        public List<NewsMoverItem> gainers = new List<NewsMoverItem>();
        public List<NewsMoverItem> losers = new List<NewsMoverItem>();

        [Header("분기")]
        public int daysToQuarter = 8;
        public int quarterLength = 10;
    }

    [Serializable]
    public sealed class NewsCompanyClickedEvent : UnityEvent<string> { }
}
