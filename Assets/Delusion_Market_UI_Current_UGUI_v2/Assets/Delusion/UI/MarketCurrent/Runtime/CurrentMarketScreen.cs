using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Delusion.UI.MarketCurrent
{
    [Serializable]
    public sealed class CurrentMarketCompanyInfo
    {
        public string companyName;
        public string nation;
        public string industry;
        public string logoResource;
        public int price;
        public float changePercent;
        public string status = "사업 진행 중";
        public Sprite logoOverride;
        public List<float> priceHistory = new List<float>();
    }

    [Serializable]
    public sealed class CurrentMarketCompanyEvent : UnityEvent<string> { }

    /// <summary>
    /// 2026-08-23 첨부 화면만을 기준으로 전체 시장 화면을 생성합니다.
    /// 기존 홈 UI, 구형 사이드바, 78px 로고 슬롯을 참조하지 않습니다.
    /// </summary>
    public sealed class CurrentMarketScreen : MonoBehaviour
    {
        private const string GeneratedRootName = "__CurrentMarketScreen_2026_08_23";
        private const string ResourceRoot = "DelusionMarketCurrentUI/";

        public static readonly Color Ink = Hex("#1E2422");
        public static readonly Color Muted = Hex("#777A75");
        public static readonly Color LightText = Hex("#E8E8E3");
        public static readonly Color Up = Hex("#D64D43");
        public static readonly Color Down = Hex("#3477B4");
        public static readonly Color Flat = Hex("#8B8D89");
        public static readonly Color Gold = Hex("#D5A936");

        [Header("글꼴")]
        [Tooltip("동봉된 NotoSansKR-400.ttf로 만든 Dynamic TMP Font Asset을 연결하세요.")]
        [SerializeField] private TMP_FontAsset koreanRegular;
        [Tooltip("동봉된 NotoSansKR-700.ttf로 만든 Dynamic TMP Font Asset을 연결하세요.")]
        [SerializeField] private TMP_FontAsset koreanBold;

        [Header("상단 상태")]
        [SerializeField] private string currentEra = "계약의 시대";
        [SerializeField] private int currentGold = 5000;
        [SerializeField] private int currentDay = 42;

        [Header("종목")]
        [SerializeField] private List<CurrentMarketCompanyInfo> companies = new List<CurrentMarketCompanyInfo>();
        [SerializeField] private CurrentMarketCompanyEvent onCompanyClicked = new CurrentMarketCompanyEvent();

        [Header("기준 해상도")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

        private readonly List<string> filters = new List<string>
        {
            "전체", "마법공학", "광업", "금융", "방산", "농업", "물류", "건설", "제약", "소비재", "오락"
        };

        private RectTransform generatedRoot;

        public IReadOnlyList<CurrentMarketCompanyInfo> Companies => companies;
        public CurrentMarketCompanyEvent OnCompanyClicked => onCompanyClicked;

        private void Reset()
        {
            EnsureSampleData();
        }

        [ContextMenu("현재 첨부본 시장 UI 생성 / 갱신")]
        public void Rebuild()
        {
            EnsureSampleData();
            RemoveGeneratedRoot();

            generatedRoot = CreateRect(GeneratedRootName, transform as RectTransform);
            Stretch(generatedRoot);

            BuildBackground(generatedRoot);
            BuildSidebar(generatedRoot);
            BuildStatusBar(generatedRoot);
            BuildHeader(generatedRoot);
            BuildFilters(generatedRoot);
            BuildScrollList(generatedRoot);
            ApplyFilter("전체");
        }

        public void SetCompanies(List<CurrentMarketCompanyInfo> newCompanies, bool rebuild = true)
        {
            companies = newCompanies ?? new List<CurrentMarketCompanyInfo>();
            if (rebuild)
                Rebuild();
        }

        public void SetStatus(string era, int gold, int day, bool rebuild = true)
        {
            currentEra = era;
            currentGold = gold;
            currentDay = day;
            if (rebuild)
                Rebuild();
        }

        public void ApplyFilter(string selected)
        {
            if (generatedRoot == null)
                generatedRoot = transform.Find(GeneratedRootName) as RectTransform;
            if (generatedRoot == null)
                return;

            var cards = generatedRoot.GetComponentsInChildren<CurrentMarketCardView>(true);
            foreach (var card in cards)
                card.gameObject.SetActive(selected == "전체" || card.Industry == selected);

            var buttons = generatedRoot.GetComponentsInChildren<CurrentMarketFilterButton>(true);
            foreach (var button in buttons)
                button.SetSelected(button.FilterName == selected);

            LayoutRebuilder.ForceRebuildLayoutImmediate(generatedRoot);
        }

        public void NotifyCompanyClicked(string companyName)
        {
            onCompanyClicked?.Invoke(companyName);
        }

        private void BuildBackground(RectTransform parent)
        {
            var background = CreateImage("MainBackground", parent, LoadSprite("main_background"));
            Stretch(background.rectTransform);
            background.type = Image.Type.Tiled;
            background.color = Color.white;
        }

        private void BuildSidebar(RectTransform parent)
        {
            var sidebar = CreateImage("Sidebar", parent, LoadSprite("sidebar_base_9slice"));
            SetTopLeft(sidebar.rectTransform, Vector2.zero, new Vector2(142f, 1080f));
            sidebar.type = Image.Type.Sliced;

            var separator = CreateImage("SidebarGoldSeparator", parent, LoadSprite("sidebar_separator"));
            SetTopLeft(separator.rectTransform, new Vector2(140f, 0f), new Vector2(1f, 1080f));

            var brand = CreateImage("BrandRing", parent, LoadSprite("brand_ring"));
            SetTopLeft(brand.rectTransform, new Vector2(39f, 51f), new Vector2(64f, 64f));
            brand.preserveAspect = true;

            var active = CreateImage("MarketActiveBackground", parent, LoadSprite("sidebar_active_9slice"));
            SetTopLeft(active.rectTransform, new Vector2(0f, 345f), new Vector2(142f, 89f));
            active.type = Image.Type.Sliced;

            var marker = CreateImage("MarketActiveMarker", parent, LoadSprite("sidebar_active_marker"));
            SetTopLeft(marker.rectTransform, new Vector2(31f, 371f), new Vector2(5f, 38f));

            BuildSidebarIcon(parent, "Home", "icon_home", new Vector2(47f, 244f), new Vector2(46f, 46f));
            BuildSidebarIcon(parent, "Market", "icon_market", new Vector2(47f, 368f), new Vector2(46f, 46f));
            BuildSidebarIcon(parent, "Trend", "icon_trend", new Vector2(47f, 495f), new Vector2(46f, 46f));
            BuildSidebarIcon(parent, "Wallet", "icon_wallet", new Vector2(47f, 620f), new Vector2(46f, 46f));
        }

        private void BuildSidebarIcon(RectTransform parent, string name, string spriteName, Vector2 position, Vector2 size)
        {
            var icon = CreateImage("SidebarIcon_" + name, parent, LoadSprite(spriteName));
            SetTopLeft(icon.rectTransform, position, size);
            icon.preserveAspect = true;
        }

        private void BuildStatusBar(RectTransform parent)
        {
            var bar = CreateImage("TopStatusBar", parent, LoadSprite("top_status_bar_9slice"));
            SetTopLeft(bar.rectTransform, new Vector2(920f, 42f), new Vector2(790f, 74f));
            bar.type = Image.Type.Sliced;

            BuildStatusItem(bar.rectTransform, "Era", "icon_sun", 36f, "현재 시대 :", currentEra);
            BuildStatusItem(bar.rectTransform, "Gold", "icon_coin", 316f, "현재 골드 :", currentGold.ToString("N0"));
            BuildStatusItem(bar.rectTransform, "Day", "icon_hourglass", 589f, "현재 날짜 :", currentDay.ToString());
        }

        private void BuildStatusItem(RectTransform parent, string name, string iconName, float x, string label, string value)
        {
            var icon = CreateImage(name + "Icon", parent, LoadSprite(iconName));
            SetTopLeft(icon.rectTransform, new Vector2(x, 12f), new Vector2(39f, 39f));
            icon.preserveAspect = true;
            CreateText(name + "Label", parent, label, new Vector2(x + 51f, 9f), new Vector2(130f, 21f), 12f, true, LightText, TextAlignmentOptions.Left);
            CreateText(name + "Value", parent, value, new Vector2(x + 51f, 31f), new Vector2(180f, 34f), 21f, true, LightText, TextAlignmentOptions.Left);
        }

        private void BuildHeader(RectTransform parent)
        {
            CreateText("Title", parent, "시장", new Vector2(241f, 86f), new Vector2(260f, 55f), 46f, true, LightText, TextAlignmentOptions.Left);
            CreateText("Subtitle", parent, "대륙의 40개 기업을 살펴보세요", new Vector2(241f, 135f), new Vector2(390f, 27f), 17f, false, new Color(0.76f, 0.77f, 0.74f), TextAlignmentOptions.Left);
        }

        private void BuildFilters(RectTransform parent)
        {
            var widths = new[] { 76f, 108f, 78f, 78f, 78f, 78f, 78f, 78f, 78f, 91f, 78f };
            var x = 241f;
            var normal = LoadSprite("filter_chip_normal_9slice");
            var selected = LoadSprite("filter_chip_selected_9slice");
            var hover = LoadSprite("filter_chip_hover_9slice");

            for (var i = 0; i < filters.Count; i++)
            {
                var width = widths[i];
                var image = CreateImage("Filter_" + filters[i], parent, normal);
                SetTopLeft(image.rectTransform, new Vector2(x, 165f), new Vector2(width, 51f));
                image.type = Image.Type.Sliced;
                image.raycastTarget = true;

                var label = CreateText("Label", image.rectTransform, filters[i], Vector2.zero, new Vector2(width, 51f), 17f, i == 0, LightText, TextAlignmentOptions.Center);
                var button = image.gameObject.AddComponent<CurrentMarketFilterButton>();
                button.Configure(this, image, label, normal, selected, hover, filters[i]);

                if (filters[i] == "마법공학")
                {
                    var boomDot = CreateImage("BoomDot", image.rectTransform, null);
                    SetTopLeft(boomDot.rectTransform, new Vector2(width - 13f, 7f), new Vector2(6f, 6f));
                    boomDot.color = Gold;
                }

                x += width + 11f;
            }
        }

        private void BuildScrollList(RectTransform parent)
        {
            var scrollRoot = CreateRect("CompanyScroll", parent);
            SetTopLeft(scrollRoot, new Vector2(241f, 250f), new Vector2(1614f, 722f));

            var scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 42f;

            var viewport = CreateRect("Viewport", scrollRoot);
            SetTopLeft(viewport, Vector2.zero, new Vector2(1581f, 722f));
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(1581f, 0f);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(779f, 165f);
            grid.spacing = new Vector2(23f, 18f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            BuildScrollbar(scrollRoot, scrollRect);

            foreach (var company in companies)
                BuildCompanyCard(content, company);
        }

        private void BuildScrollbar(RectTransform parent, ScrollRect scrollRect)
        {
            var bar = CreateImage("Scrollbar", parent, LoadSprite("scrollbar_track_9slice"));
            SetTopLeft(bar.rectTransform, new Vector2(1606f, 0f), new Vector2(7f, 722f));
            bar.type = Image.Type.Sliced;
            bar.raycastTarget = true;

            var slidingArea = CreateRect("Sliding Area", bar.rectTransform);
            Stretch(slidingArea);
            var handle = CreateImage("Handle", slidingArea, LoadSprite("scrollbar_handle_9slice"));
            Stretch(handle.rectTransform);
            handle.type = Image.Type.Sliced;
            handle.raycastTarget = true;

            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private void BuildCompanyCard(RectTransform parent, CurrentMarketCompanyInfo company)
        {
            var card = CreateImage("Card_" + company.companyName, parent, LoadSprite("market_card_9slice"));
            card.type = Image.Type.Sliced;
            card.raycastTarget = true;

            var logoSprite = company.logoOverride;
            if (logoSprite == null && !string.IsNullOrWhiteSpace(company.logoResource))
                logoSprite = Resources.Load<Sprite>(ResourceRoot + "Logos/" + company.logoResource);
            if (logoSprite == null)
                logoSprite = Resources.Load<Sprite>(ResourceRoot + "Logos/logo_empty");

            var logo = CreateImage("LogoSlot", card.rectTransform, logoSprite);
            SetTopLeft(logo.rectTransform, new Vector2(16f, 18f), new Vector2(118f, 124f));
            logo.preserveAspect = true;

            CreateText("CompanyName", card.rectTransform, company.companyName, new Vector2(144f, 33f), new Vector2(300f, 35f), 25f, true, Ink, TextAlignmentOptions.Left);
            CreateText("NationIndustry", card.rectTransform, company.nation + " · " + company.industry, new Vector2(144f, 75f), new Vector2(290f, 28f), 17f, false, Muted, TextAlignmentOptions.Left);

            var dot = CreateImage("StatusDot", card.rectTransform, null);
            SetTopLeft(dot.rectTransform, new Vector2(147f, 116f), new Vector2(13f, 13f));
            dot.color = new Color(0.64f, 0.64f, 0.62f);
            CreateText("Status", card.rectTransform, company.status, new Vector2(167f, 109f), new Vector2(230f, 28f), 16f, false, Muted, TextAlignmentOptions.Left);

            var chartRect = CreateRect("MiniChart", card.rectTransform);
            SetTopLeft(chartRect, new Vector2(427f, 38f), new Vector2(185f, 90f));
            var sparkline = chartRect.gameObject.AddComponent<CurrentMarketSparkline>();
            var trendColor = company.changePercent > 0f ? Up : company.changePercent < 0f ? Down : Flat;
            sparkline.SetValues(company.priceHistory, trendColor);

            CreateText("Price", card.rectTransform, company.price.ToString("N0") + " G", new Vector2(650f, 30f), new Vector2(96f, 36f), 24f, true, Ink, TextAlignmentOptions.Right);
            var sign = company.changePercent > 0f ? "+" : string.Empty;
            CreateText("Change", card.rectTransform, sign + company.changePercent.ToString("0.0") + "%", new Vector2(650f, 86f), new Vector2(96f, 36f), 21f, true, trendColor, TextAlignmentOptions.Right);

            var view = card.gameObject.AddComponent<CurrentMarketCardView>();
            view.Configure(this, card, LoadSprite("market_card_9slice"), LoadSprite("market_card_hover_9slice"), company.companyName, company.industry);
        }

        private TMP_Text CreateText(string objectName, RectTransform parent, string value, Vector2 topLeft, Vector2 size, float fontSize, bool bold, Color color, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(objectName, parent);
            SetTopLeft(rect, topLeft, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.font = bold && koreanBold != null ? koreanBold : koreanRegular != null ? koreanRegular : TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.color = color;
            label.alignment = alignment;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private static Image CreateImage(string objectName, RectTransform parent, Sprite sprite)
        {
            var rect = CreateRect(objectName, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateRect(string objectName, RectTransform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            if (parent != null)
                rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetTopLeft(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            rect.sizeDelta = size;
        }

        private static Sprite LoadSprite(string name)
        {
            return Resources.Load<Sprite>(ResourceRoot + name);
        }

        private void RemoveGeneratedRoot()
        {
            var old = transform.Find(GeneratedRootName);
            if (old == null)
                return;
            if (Application.isPlaying)
                Destroy(old.gameObject);
            else
                DestroyImmediate(old.gameObject);
        }

        private void EnsureSampleData()
        {
            if (companies != null && companies.Count > 0)
                return;

            companies = new List<CurrentMarketCompanyInfo>
            {
                Company("윈드시어 매직스", "알브레이", "마법공학", "01-windshire-magics", 850, 4.8f, "사업 진행 중", 41,44,43,49,47,55,58,66),
                Company("드래곤 금고", "바이브레스", "금융", "02-dragon-vault", 950, 1.2f, "사업 진행 중", 53,52,54,55,54,57,58,60),
                Company("아이언하트 공업", "벨리르", "방산", "03-ironheart-industries", 610, 3.4f, "사업 진행 중", 46,45,49,51,50,56,54,61),
                Company("블레이즈마인 광산", "벨리르", "광업", "04-blazemine-mining", 380, -2.1f, "사업 진행 중", 64,62,65,59,61,55,56,50),
                Company("아우렐리온 캐피탈", "마노사", "금융", "05-aurelion-capital", 420, 0.6f, "사업 진행 중", 51,52,50,53,54,53,55,56),
                Company("브루트스틸 철강", "원 오르쿠스", "광업", "06-brutesteel", 335, 0f, "사업 준비 중", 50,50,53,49,50,50,50,50),
                Company("에이더니얼 자산관리", "바이브레스", "금융", "07-aethernial-asset-management", 280, -0.8f, "사업 진행 중", 59,58,58,57,55,56,54,53),
                Company("코타 상업은행", "마노사", "금융", "08-kota-commercial-bank", 690, 2.3f, "사업 진행 중", 46,48,49,48,52,54,53,58),

                Company("아이언코어 철강", "벨리르", "광업", "09-ironcore-steel", 570, 1.1f, "사업 진행 중", 50,51,52,51,54,55,55,57),
                Company("글램머락 광산", "원 오르쿠스", "광업", "10-glamourrock-mining", 245, -1.4f, "사업 진행 중", 56,58,55,54,56,52,53,51),
                Company("브레스마크 용병단", "바이브레스", "방산", "11-breathmark-mercenaries", 780, 5.7f, "사업 진행 중", 41,45,43,50,55,54,62,68),
                Company("워메이커 용병단", "원 오르쿠스", "방산", "12-warmaker-mercenaries", 510, 6.2f, "사업 진행 중", 40,44,48,46,53,59,57,66),
                Company("아이언리폼 방산", "마노사", "방산", "13-ironreform-defense", 185, -3.5f, "신규 사업 진행 중", 62,60,57,59,54,55,51,48),

                Company("오르카그리 농업연합", "원 오르쿠스", "농업", "14-orkagri-union", 260, 0.8f, "사업 진행 중", 49,50,50,51,50,52,52,53),
                Company("마노사 그레인", "마노사", "농업", "15-manosa-grain", 320, 0.2f, "사업 진행 중", 51,51,52,51,52,52,52,53),
                Company("플레임스케일 농업", "바이브레스", "농업", "16-flamescale-agriculture", 430, 1.7f, "사업 진행 중", 47,49,48,51,50,53,54,56),
                Company("실바나 농원", "알브레이", "농업", "17-sylvana-farm", 390, -0.4f, "사업 진행 중", 55,54,55,54,54,53,54,53),

                Company("마지노 테크놀로지", "마노사", "마법공학", "18-magino-technology", 720, 4.1f, "연구 진행 중", 41,43,48,46,52,55,58,63),
                Company("마나코어 시스템즈", "마노사", "마법공학", "19-manacore-systems", 465, -1.8f, "신제품 개발 중", 60,61,58,57,59,54,55,52),
                Company("마나펠 마법공예", "원 오르쿠스", "마법공학", "20-manafel-crafts", 205, 2.5f, "사업 진행 중", 45,46,48,47,50,52,51,55),

                Company("에버그린 에센스", "알브레이", "제약", "21-evergreen-essence", 540, 1.0f, "사업 진행 중", 51,52,51,53,53,54,54,55),
                Company("알케미아 제약", "마노사", "제약", "22-alchemia-pharma", 410, 2.9f, "생산 확대 중", 44,47,46,50,52,51,55,58),
                Company("루트힐 약초원", "알브레이", "제약", "23-roothill-herbs", 225, -0.3f, "사업 진행 중", 53,53,54,53,52,53,52,52),
                Company("아우라스케일 생명과학", "바이브레스", "제약", "24-aurascale-bioscience", 350, 7.4f, "연구 진행 중", 39,43,41,48,52,57,55,65),

                Company("스카이마크 물류", "마노사", "물류", "25-skymark-logistics", 610, 1.3f, "사업 진행 중", 50,51,50,52,54,53,55,56),
                Company("글로발 로지스틱스", "마노사", "물류", "26-global-logistics", 355, -0.7f, "사업 진행 중", 55,55,54,56,53,54,52,52),
                Company("스카이포지 운송", "바이브레스", "물류", "27-skyforge-transport", 475, 3.1f, "노선 확장 중", 43,46,45,49,51,53,52,57),
                Company("드워프 트레일 수송", "벨리르", "물류", "28-dwarf-trail-transport", 295, -2.6f, "사업 진행 중", 61,58,59,56,54,55,51,49),

                Company("스틸빌더 건설", "벨리르", "건설", "29-steelbuilder", 445, 0.9f, "사업 진행 중", 50,51,51,52,51,53,53,54),
                Company("솔리스타 부동산", "마노사", "건설", "30-solista-real-estate", 680, 2.0f, "도시 개발 중", 46,48,49,50,52,51,54,56),
                Company("하이락 건축", "벨리르", "건설", "31-highrock-architecture", 310, 1.6f, "외부 수주 진행 중", 48,49,51,50,52,53,54,55),
                Company("그린루트 건설", "알브레이", "건설", "32-greenroot-construction", 285, 0.3f, "사업 진행 중", 51,51,52,52,51,52,52,52),

                Company("크레센트 유통", "마노사", "소비재", "33-crescent-retail", 525, 1.4f, "사업 진행 중", 48,49,51,50,52,54,53,55),
                Company("글림윙 보석공예", "바이브레스", "소비재", "34-glimwing-jewelry", 740, -1.2f, "사업 진행 중", 57,56,58,55,54,55,52,53),
                Company("페어브리즈 의류", "알브레이", "소비재", "35-fairbreeze-clothing", 365, 2.2f, "공급 계약 진행 중", 45,47,46,50,51,53,52,56),
                Company("실버포지 장신구", "벨리르", "소비재", "36-silverforge-jewelry", 395, 1.8f, "사업 진행 중", 47,48,50,49,52,51,54,55),

                Company("하이애너 맥주", "벨리르", "오락", "37-hyena-beer", 275, 0.5f, "사업 진행 중", 50,50,51,51,52,51,52,53),
                Company("본파이어 주류", "원 오르쿠스", "오락", "38-bonfire-liquor", 190, 3.7f, "축제 납품 중", 42,45,46,49,48,53,55,59),
                Company("이부코타 극장", "마노사", "오락", "39-ibu-kota-theater", 460, -4.2f, "대형 공연 준비 중", 64,61,62,57,55,52,54,48),
                Company("오르크 파이트클럽", "원 오르쿠스", "오락", "40-orc-fight-club", 235, 5.3f, "대형 매치 진행 중", 40,42,46,44,51,55,54,62),
            };
        }

        private static CurrentMarketCompanyInfo Company(string name, string nation, string industry, string logoResource, int price, float change, string status, params float[] history)
        {
            return new CurrentMarketCompanyInfo
            {
                companyName = name,
                nation = nation,
                industry = industry,
                logoResource = logoResource,
                price = price,
                changePercent = change,
                status = status,
                priceHistory = new List<float>(history)
            };
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var color);
            return color;
        }
    }
}
