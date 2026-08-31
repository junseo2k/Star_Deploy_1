using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Delusion.UI.News
{
    /// <summary>
    /// 확정된 1920×1080 뉴스 탭 시안을 UGUI로 재구성합니다.
    /// 모든 문구와 종목은 런타임 데이터이며, 시대 배경만 이미지 에셋입니다.
    /// </summary>
    public sealed class DelusionNewsScreen : MonoBehaviour
    {
        private const string GeneratedRootName = "__DelusionNewsScreen_2026_08_31";
        private const string ResourceRoot = "DelusionNewsUI/";

        public static readonly Color Ink = Hex("#1D2221");
        public static readonly Color Muted = Hex("#747873");
        public static readonly Color Light = Hex("#F2EEE7");
        public static readonly Color Gold = Hex("#E4BD61");
        public static readonly Color Up = Hex("#D64D43");
        public static readonly Color Down = Hex("#2578BA");
        public static readonly Color Flat = Hex("#8B8D89");
        // 시대 배경 그림을 쓰지 않을 때 시대 카드를 채우는 단색
        public static readonly Color EraCardFill = Hex("#2A3330");

        [Header("글꼴")]
        [Tooltip("본문용 한글 TMP Font Asset. 비워두면 에디터가 Assets/Font의 NotoSansKR SDF를 자동으로 연결합니다.")]
        [SerializeField] private TMP_FontAsset koreanRegular;
        [Tooltip("굵은 글씨용 TMP Font Asset. 비워두면 본문 폰트를 그대로 쓰고 굵기 효과만 적용합니다.")]
        [SerializeField] private TMP_FontAsset koreanBold;

        [Header("데이터")]
        [SerializeField] private DelusionNewsScreenData data = new DelusionNewsScreenData();

        [Header("공통 배경")]
        [Tooltip("게임에서 이미 쓰는 집무실 배경 Sprite가 있으면 연결하세요.")]
        [SerializeField] private Sprite sharedOfficeBackground;
        [SerializeField] private bool drawBundledDarkBackground = false;

        [Header("탭 이동 이벤트")]
        [SerializeField] private UnityEvent onHomeRequested = new UnityEvent();
        [SerializeField] private UnityEvent onMarketRequested = new UnityEvent();
        [SerializeField] private UnityEvent onWalletRequested = new UnityEvent();
        [SerializeField] private NewsCompanyClickedEvent onCompanyClicked = new NewsCompanyClickedEvent();

        [Header("동작")]
        [SerializeField] private bool buildOnEnable = false;
        [Tooltip("끄면 이 화면 자체의 사이드바를 그리지 않는다. 프로젝트에 이미 SidebarTabManager 같은 전역 사이드바가 있을 때 끈다.")]
        [SerializeField] private bool buildSidebar = false;
        [Tooltip("끄면 이 화면 자체의 상단 상태바(시대/골드/날짜)를 그리지 않는다. 프로젝트에 이미 전역 상단바가 있을 때 끈다.")]
        [SerializeField] private bool buildStatusBar = false;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField, HideInInspector] private bool sampleDataInitialized;

        private RectTransform generatedRoot;

        public DelusionNewsScreenData Data => data;
        public NewsCompanyClickedEvent OnCompanyClicked => onCompanyClicked;

        // 한글 폰트가 하나라도 연결돼 있는지 확인 (에디터에서 자동 연결 여부를 판단할 때 사용)
        public bool HasKoreanFont => koreanRegular != null;

        // 폰트 필드는 private이라 외부에서 직접 못 넣는다. 에디터 스크립트와 런타임 코드가 이 메서드로 연결한다.
        // bold를 비워서 부르면 본문 폰트를 그대로 쓰고 굵기 효과만 들어간다.
        public void SetFonts(TMP_FontAsset regular, TMP_FontAsset bold = null)
        {
            koreanRegular = regular;
            koreanBold = bold != null ? bold : regular;
        }

        private void Reset()
        {
            SeedSampleData();
            sampleDataInitialized = true;
        }

        private void OnEnable()
        {
            if (buildOnEnable && transform.Find(GeneratedRootName) == null)
                Rebuild();
        }

        [ContextMenu("확정 뉴스 탭 UI 생성 / 갱신")]
        public void Rebuild()
        {
            EnsureSampleData();
            RemoveGeneratedRoot();

            generatedRoot = CreateRect(GeneratedRootName, transform as RectTransform);
            Stretch(generatedRoot);

            BuildBackground(generatedRoot);
            if (buildSidebar) BuildSidebar(generatedRoot);
            if (buildStatusBar) BuildStatusBar(generatedRoot);
            BuildHeader(generatedRoot);
            BuildEraCard(generatedRoot);
            BuildTodayNews(generatedRoot);
            BuildBottomCards(generatedRoot);
        }

        public void SetData(DelusionNewsScreenData newData, bool rebuild = true)
        {
            data = newData ?? new DelusionNewsScreenData();
            sampleDataInitialized = true;
            if (rebuild)
                Rebuild();
        }

        public void LoadJson(TextAsset json, bool rebuild = true)
        {
            if (json == null || string.IsNullOrWhiteSpace(json.text))
                return;
            SetData(JsonUtility.FromJson<DelusionNewsScreenData>(json.text), rebuild);
        }

        public void SetStatus(string era, int gold, int day, bool rebuild = true)
        {
            if (data == null)
                data = new DelusionNewsScreenData();
            data.currentEra = era;
            data.currentGold = gold;
            data.currentDay = day;
            sampleDataInitialized = true;
            if (rebuild)
                Rebuild();
        }

        public void ShowQuietDay(bool rebuild = true)
        {
            if (data == null)
                data = new DelusionNewsScreenData();
            if (data.todayNews == null)
                data.todayNews = new List<NewsCompanyItem>();
            data.todayNews.Clear();
            sampleDataInitialized = true;
            if (rebuild)
                Rebuild();
        }

        private void BuildBackground(RectTransform parent)
        {
            if (!drawBundledDarkBackground && sharedOfficeBackground == null)
                return;

            var sprite = sharedOfficeBackground != null ? sharedOfficeBackground : LoadSprite("background_tile");
            var background = CreateImage("SharedOfficeBackground", parent, sprite);
            Stretch(background.rectTransform);
            background.type = sharedOfficeBackground != null ? Image.Type.Simple : Image.Type.Tiled;
            background.preserveAspect = false;
            background.color = sharedOfficeBackground != null ? new Color(0.42f, 0.42f, 0.42f, 1f) : Color.white;

            var shade = CreateImage("ReadabilityShade", parent, null);
            Stretch(shade.rectTransform);
            shade.color = new Color(0f, 0f, 0f, sharedOfficeBackground != null ? 0.54f : 0.18f);
        }

        private void BuildSidebar(RectTransform parent)
        {
            var sidebar = CreateImage("Sidebar", parent, LoadSprite("sidebar_base"));
            SetTopLeft(sidebar.rectTransform, Vector2.zero, new Vector2(142f, 1080f));
            sidebar.type = Image.Type.Tiled;

            var separator = CreateImage("SidebarGoldSeparator", parent, LoadSprite("sidebar_separator"));
            SetTopLeft(separator.rectTransform, new Vector2(140f, 0f), new Vector2(2f, 1080f));

            var brand = CreateImage("BrandRing", parent, LoadSprite("brand_ring"));
            SetTopLeft(brand.rectTransform, new Vector2(39f, 73f), new Vector2(64f, 64f));
            brand.preserveAspect = true;

            var marker = CreateImage("NewsActiveMarker", parent, LoadSprite("active_marker"));
            SetTopLeft(marker.rectTransform, new Vector2(31f, 532f), new Vector2(6f, 44f));

            BuildSidebarButton(parent, "Home", "icon_home", new Vector2(48f, 284f), onHomeRequested);
            BuildSidebarButton(parent, "Market", "icon_market", new Vector2(48f, 424f), onMarketRequested);
            BuildSidebarButton(parent, "News", "icon_news", new Vector2(48f, 529f), null);
            BuildSidebarButton(parent, "Wallet", "icon_wallet", new Vector2(48f, 650f), onWalletRequested);
        }

        private void BuildSidebarButton(RectTransform parent, string name, string spriteName, Vector2 iconPosition, UnityEvent callback)
        {
            var hit = CreateImage("SidebarButton_" + name, parent, null);
            SetTopLeft(hit.rectTransform, new Vector2(26f, iconPosition.y - 20f), new Vector2(90f, 90f));
            hit.color = new Color(1f, 1f, 1f, 0.001f);
            hit.raycastTarget = callback != null;

            var icon = CreateImage("Icon", hit.rectTransform, LoadSprite(spriteName));
            SetTopLeft(icon.rectTransform, new Vector2(iconPosition.x - 26f, 20f), new Vector2(46f, 46f));
            icon.preserveAspect = true;

            if (callback == null)
                return;
            var button = hit.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(callback.Invoke);
        }

        private void BuildStatusBar(RectTransform parent)
        {
            var bar = CreateImage("TopStatusBar", parent, LoadSprite("status_bar_9slice"));
            SetTopLeft(bar.rectTransform, new Vector2(951f, 37f), new Vector2(800f, 76f));
            bar.type = Image.Type.Sliced;

            BuildStatusItem(bar.rectTransform, "Era", "icon_sun", 34f, "현재 시대 :", data.currentEra);
            BuildStatusItem(bar.rectTransform, "Gold", "icon_coin", 315f, "현재 골드 :", data.currentGold.ToString("N0"));
            BuildStatusItem(bar.rectTransform, "Day", "icon_hourglass", 588f, "현재 날짜 :", data.currentDay.ToString());
        }

        private void BuildStatusItem(RectTransform parent, string name, string iconName, float x, string label, string value)
        {
            var icon = CreateImage(name + "Icon", parent, LoadSprite(iconName));
            SetTopLeft(icon.rectTransform, new Vector2(x, 13f), new Vector2(42f, 42f));
            icon.preserveAspect = true;
            CreateText(name + "Label", parent, label, new Vector2(x + 53f, 9f), new Vector2(150f, 22f), 13f, true, Light, TextAlignmentOptions.Left);
            CreateText(name + "Value", parent, value, new Vector2(x + 53f, 31f), new Vector2(190f, 35f), 22f, true, Light, TextAlignmentOptions.Left);
        }

        private void BuildHeader(RectTransform parent)
        {
            CreateText("Title", parent, "뉴스", new Vector2(249f, 88f), new Vector2(280f, 55f), 48f, true, Light, TextAlignmentOptions.Left);
            CreateText("Subtitle", parent, "오늘 대륙에서 일어난 일", new Vector2(249f, 145f), new Vector2(380f, 28f), 18f, false, new Color(0.82f, 0.82f, 0.79f), TextAlignmentOptions.Left);
        }

        private void BuildEraCard(RectTransform parent)
        {
            // 배경 이름을 비워두면 그림 없이 단색 카드로 그린다
            Sprite eraSprite = null;
            if (!string.IsNullOrEmpty(data.eraBackgroundResource))
            {
                eraSprite = Resources.Load<Sprite>(ResourceRoot + "Era/" + data.eraBackgroundResource);
                if (eraSprite == null)
                    eraSprite = Resources.Load<Sprite>(ResourceRoot + "Era/magic_era_background");
            }

            var card = CreateImage("EraCard", parent, eraSprite);
            SetTopLeft(card.rectTransform, new Vector2(245f, 201f), new Vector2(1504f, 196f));
            card.preserveAspect = false;
            if (eraSprite == null)
                card.color = EraCardFill;

            var overlay = CreateImage("EraReadabilityOverlay", card.rectTransform, null);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0.01f, 0.045f, 0.055f, 0.16f);

            var moon = CreateImage("EraSymbol", card.rectTransform, LoadSprite("icon_moon"));
            SetTopLeft(moon.rectTransform, new Vector2(49f, 32f), new Vector2(108f, 108f));
            moon.preserveAspect = true;

            CreateText("EraName", card.rectTransform, data.eraName, new Vector2(174f, 35f), new Vector2(390f, 48f), 37f, true, Light, TextAlignmentOptions.Left);
            CreateText("EraDay", card.rectTransform, data.eraDay + "일째 / 약 " + data.eraEstimatedDays + "일", new Vector2(174f, 86f), new Vector2(360f, 32f), 22f, false, Light, TextAlignmentOptions.Left);

            BuildProgress(card.rectTransform, new Vector2(174f, 140f), new Vector2(470f, 18f), data.eraEstimatedDays <= 0 ? 0f : data.eraDay / (float)data.eraEstimatedDays, false);

            CreateText("EraSummary", card.rectTransform, data.eraSummary, new Vector2(855f, 44f), new Vector2(580f, 42f), 24f, true, Light, TextAlignmentOptions.Left);

            var trends = data.industryTrends ?? new List<NewsIndustryTrend>();
            var x = 855f;
            for (var i = 0; i < Mathf.Min(4, trends.Count); i++)
            {
                BuildTrendTag(card.rectTransform, trends[i], x, 111f);
                x += 142f;
            }
        }

        private void BuildTrendTag(RectTransform parent, NewsIndustryTrend trend, float x, float y)
        {
            var tag = CreateImage("Trend_" + trend.industryName, parent, LoadSprite("tag_9slice"));
            SetTopLeft(tag.rectTransform, new Vector2(x, y), new Vector2(128f, 50f));
            tag.type = Image.Type.Sliced;
            var color = trend.isRising ? Up : Down;
            CreateText("Label", tag.rectTransform, trend.industryName + (trend.isRising ? " ▲" : " ▼"), Vector2.zero, new Vector2(128f, 50f), 18f, true, color, TextAlignmentOptions.Center);
        }

        private void BuildTodayNews(RectTransform parent)
        {
            CreateText("TodayNewsTitle", parent, "오늘의 뉴스", new Vector2(249f, 423f), new Vector2(330f, 39f), 28f, true, Light, TextAlignmentOptions.Left);

            var card = CreateImage("TodayNewsCard", parent, LoadSprite("card_cream_9slice"));
            SetTopLeft(card.rectTransform, new Vector2(245f, 467f), new Vector2(1504f, 255f));
            card.type = Image.Type.Sliced;

            var viewport = CreateRect("Viewport", card.rectTransform);
            SetTopLeft(viewport, new Vector2(20f, 10f), new Vector2(1464f, 235f));
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;

            var newsItems = data.todayNews ?? new List<NewsCompanyItem>();
            var contentHeight = newsItems.Count == 0 ? 235f : Mathf.Max(235f, newsItems.Count * 116f);
            content.sizeDelta = new Vector2(1464f, contentHeight);

            if (newsItems.Count == 0)
            {
                CreateText("QuietTitle", content, "오늘은 조용한 날입니다", new Vector2(48f, 65f), new Vector2(700f, 44f), 28f, true, Ink, TextAlignmentOptions.Left);
                CreateText("QuietBody", content, "시대 흐름과 기업 실적만 주가에 반영됩니다", new Vector2(48f, 111f), new Vector2(850f, 34f), 18f, true, Ink, TextAlignmentOptions.Left);
            }
            else
            {
                for (var i = 0; i < newsItems.Count; i++)
                    BuildNewsRow(content, newsItems[i], i, i < newsItems.Count - 1);
            }

            var scrollRect = card.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = newsItems.Count > 2;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 34f;
        }

        private void BuildNewsRow(RectTransform parent, NewsCompanyItem item, int index, bool divider)
        {
            var y = index * 116f;
            var row = CreateImage("NewsRow_" + item.companyName, parent, null);
            SetTopLeft(row.rectTransform, new Vector2(0f, y), new Vector2(1464f, 116f));
            row.color = new Color(1f, 1f, 1f, 0.001f);
            row.raycastTarget = true;

            var logo = CreateImage("Logo", row.rectTransform, ResolveLogo(item.logoOverride, item.logoResource));
            SetTopLeft(logo.rectTransform, new Vector2(34f, 18f), new Vector2(80f, 80f));
            logo.preserveAspect = true;

            CreateText("Headline", row.rectTransform, item.headline, new Vector2(150f, 25f), new Vector2(920f, 39f), 27f, true, Ink, TextAlignmentOptions.Left);
            CreateText("Description", row.rectTransform, item.description, new Vector2(150f, 67f), new Vector2(980f, 31f), 18f, true, Ink, TextAlignmentOptions.Left);
            CreateText("Change", row.rectTransform, FormatChange(item.changePercent), new Vector2(1245f, 34f), new Vector2(150f, 48f), 31f, true, ChangeColor(item.changePercent), TextAlignmentOptions.Right);

            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var companyName = item.companyName;
            button.onClick.AddListener(() => onCompanyClicked?.Invoke(companyName));

            if (!divider)
                return;
            var line = CreateImage("Divider", row.rectTransform, null);
            SetTopLeft(line.rectTransform, new Vector2(10f, 115f), new Vector2(1444f, 1f));
            line.color = new Color(0.77f, 0.74f, 0.69f, 0.55f);
        }

        private void BuildBottomCards(RectTransform parent)
        {
            BuildMoverCard(parent, "급상승", data.gainers, new Vector2(245f, 745f), new Vector2(490f, 277f));
            BuildMoverCard(parent, "급하락", data.losers, new Vector2(757f, 745f), new Vector2(497f, 277f));
            BuildQuarterCard(parent, new Vector2(1275f, 745f), new Vector2(474f, 277f));
        }

        private void BuildMoverCard(RectTransform parent, string title, List<NewsMoverItem> items, Vector2 position, Vector2 size)
        {
            var card = CreateImage(title + "Card", parent, LoadSprite("card_cream_9slice"));
            SetTopLeft(card.rectTransform, position, size);
            card.type = Image.Type.Sliced;

            CreateText("Title", card.rectTransform, title, new Vector2(37f, 24f), new Vector2(size.x - 74f, 42f), 28f, true, Ink, TextAlignmentOptions.Left);
            var divider = CreateImage("HeaderDivider", card.rectTransform, null);
            SetTopLeft(divider.rectTransform, new Vector2(29f, 72f), new Vector2(size.x - 58f, 1f));
            divider.color = new Color(0.77f, 0.74f, 0.69f, 0.55f);

            if (items == null)
                return;
            for (var i = 0; i < Mathf.Min(3, items.Count); i++)
                BuildMoverRow(card.rectTransform, items[i], i, size.x);
        }

        private void BuildMoverRow(RectTransform parent, NewsMoverItem item, int index, float cardWidth)
        {
            var y = 82f + index * 59f;
            var row = CreateImage("Mover_" + item.companyName, parent, null);
            SetTopLeft(row.rectTransform, new Vector2(26f, y), new Vector2(cardWidth - 52f, 56f));
            row.color = new Color(1f, 1f, 1f, 0.001f);
            row.raycastTarget = true;

            var logo = CreateImage("Logo", row.rectTransform, ResolveLogo(item.logoOverride, item.logoResource));
            SetTopLeft(logo.rectTransform, new Vector2(0f, 3f), new Vector2(50f, 50f));
            logo.preserveAspect = true;
            CreateText("Company", row.rectTransform, item.companyName, new Vector2(66f, 11f), new Vector2(cardWidth - 230f, 35f), 19f, true, Ink, TextAlignmentOptions.Left);
            CreateText("Change", row.rectTransform, FormatChange(item.changePercent), new Vector2(cardWidth - 162f, 10f), new Vector2(100f, 35f), 19f, true, ChangeColor(item.changePercent), TextAlignmentOptions.Right);

            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var companyName = item.companyName;
            button.onClick.AddListener(() => onCompanyClicked?.Invoke(companyName));
        }

        private void BuildQuarterCard(RectTransform parent, Vector2 position, Vector2 size)
        {
            var card = CreateImage("QuarterCard", parent, LoadSprite("card_cream_9slice"));
            SetTopLeft(card.rectTransform, position, size);
            card.type = Image.Type.Sliced;

            CreateText("Title", card.rectTransform, "다가오는 분기", new Vector2(37f, 24f), new Vector2(size.x - 74f, 42f), 28f, true, Ink, TextAlignmentOptions.Left);
            var divider = CreateImage("HeaderDivider", card.rectTransform, null);
            SetTopLeft(divider.rectTransform, new Vector2(29f, 72f), new Vector2(size.x - 58f, 1f));
            divider.color = new Color(0.77f, 0.74f, 0.69f, 0.55f);

            CreateText("Days", card.rectTransform, data.daysToQuarter + "일 후", new Vector2(38f, 86f), new Vector2(280f, 62f), 44f, true, Ink, TextAlignmentOptions.Left);
            CreateText("Description", card.rectTransform, "분기 마감 및 배당 지급", new Vector2(38f, 150f), new Vector2(340f, 33f), 19f, true, Ink, TextAlignmentOptions.Left);

            var elapsed = data.quarterLength <= 0 ? 0f : 1f - data.daysToQuarter / (float)data.quarterLength;
            BuildProgress(card.rectTransform, new Vector2(38f, 203f), new Vector2(size.x - 76f, 18f), elapsed, true);

            // 게이지 아래에 분기 진행 상황을 숫자로 적는다 — 며칠 지났고 며칠 남았는지 한눈에 보이게
            var elapsedDays = Mathf.Max(0, data.quarterLength - data.daysToQuarter);
            CreateText("Progress", card.rectTransform,
                elapsedDays + "일 / " + data.quarterLength + "일  (" + data.daysToQuarter + "일 남음)",
                new Vector2(38f, 226f), new Vector2(size.x - 76f, 30f), 17f, true, Ink, TextAlignmentOptions.Left);
        }

        private void BuildProgress(RectTransform parent, Vector2 position, Vector2 size, float value, bool showSegments)
        {
            var track = CreateImage("ProgressTrack", parent, LoadSprite("progress_track_9slice"));
            SetTopLeft(track.rectTransform, position, size);
            track.type = Image.Type.Sliced;

            var fill = CreateImage("ProgressFill", track.rectTransform, LoadSprite("progress_fill_9slice"));
            SetTopLeft(fill.rectTransform, Vector2.zero, new Vector2(size.x * Mathf.Clamp01(value), size.y));
            fill.type = Image.Type.Sliced;

            if (!showSegments)
                return;
            for (var i = 1; i < 5; i++)
            {
                var line = CreateImage("Segment_" + i, track.rectTransform, null);
                SetTopLeft(line.rectTransform, new Vector2(size.x * i / 5f, 0f), new Vector2(2f, size.y));
                line.color = new Color(0.95f, 0.93f, 0.89f, 0.85f);
            }
        }

        private Sprite ResolveLogo(Sprite overrideSprite, string resourceName)
        {
            if (overrideSprite != null)
                return overrideSprite;
            if (string.IsNullOrWhiteSpace(resourceName))
                return null;
            return Resources.Load<Sprite>(ResourceRoot + "Logos/" + resourceName);
        }

        private TMP_Text CreateText(string objectName, RectTransform parent, string value, Vector2 topLeft, Vector2 size, float fontSize, bool bold, Color color, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(objectName, parent);
            SetTopLeft(rect, topLeft, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value ?? string.Empty;
            label.font = bold && koreanBold != null ? koreanBold : koreanRegular != null ? koreanRegular : TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.color = color;
            label.alignment = alignment;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableWordWrapping = false;
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
            if (data == null)
                data = new DelusionNewsScreenData();

            if (sampleDataInitialized)
                return;

            SeedSampleData();
            sampleDataInitialized = true;
        }

        private void SeedSampleData()
        {
            if (data == null)
                data = new DelusionNewsScreenData();

            if (data.industryTrends == null || data.industryTrends.Count == 0)
            {
                data.industryTrends = new List<NewsIndustryTrend>
                {
                    Trend("마법공학", true), Trend("제약", true), Trend("광업", false), Trend("건설", false)
                };
            }

            if (data.todayNews == null || data.todayNews.Count == 0)
            {
                data.todayNews = new List<NewsCompanyItem>
                {
                    News("윈드시어 매직스", "윈드시어, 신형 마나 엔진 공개", "왕실 기술진의 첫 시험을 성공적으로 통과했습니다", 4.8f, "17_windshire-magics"),
                    News("블레이즈마인 광산", "블레이즈마인, 야간 채굴 중단", "지하 갱도 안전 점검으로 생산이 일시 중단됐습니다", -2.1f, "01_blazemine-mining")
                };
            }

            if (data.gainers == null || data.gainers.Count == 0)
            {
                data.gainers = new List<NewsMoverItem>
                {
                    Mover("윈드시어 매직스", 4.8f, "17_windshire-magics"),
                    Mover("아이언하트 공업", 3.4f, "09_ironheart-industries"),
                    Mover("코타 상업은행", 2.3f, "08_kota-commercial-bank")
                };
            }

            if (data.losers == null || data.losers.Count == 0)
            {
                data.losers = new List<NewsMoverItem>
                {
                    Mover("블레이즈마인 광산", -2.1f, "01_blazemine-mining"),
                    Mover("에이더니얼 자산관리", -0.8f, "07_aethernial-asset-management"),
                    Mover("드워프 트레일 수송", -0.6f, "28_dwarf-trail-transport")
                };
            }
        }

        private static NewsIndustryTrend Trend(string name, bool rising)
        {
            return new NewsIndustryTrend { industryName = name, isRising = rising };
        }

        private static NewsCompanyItem News(string company, string headline, string description, float change, string logo)
        {
            return new NewsCompanyItem { companyName = company, headline = headline, description = description, changePercent = change, logoResource = logo };
        }

        private static NewsMoverItem Mover(string company, float change, string logo)
        {
            return new NewsMoverItem { companyName = company, changePercent = change, logoResource = logo };
        }

        private static string FormatChange(float value)
        {
            return (value > 0f ? "+" : string.Empty) + value.ToString("0.0") + "%";
        }

        private static Color ChangeColor(float value)
        {
            return value > 0f ? Up : value < 0f ? Down : Flat;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out var color);
            return color;
        }
    }
}
