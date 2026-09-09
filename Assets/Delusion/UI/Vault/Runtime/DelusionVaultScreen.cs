using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Delusion.UI.Vault
{
    /// <summary>
    /// 확정된 6차 자산(금고) 탭 시안을 1920×1080 UGUI로 재구성합니다.
    /// 모든 숫자와 종목은 런타임 데이터이며, 평가액 순으로 자동 정렬됩니다.
    /// </summary>
    public sealed class DelusionVaultScreen : MonoBehaviour
    {
        private const string GeneratedRootName = "__DelusionVaultScreen_2026_08_31";
        private const string ResourceRoot = "DelusionVaultUI/";
        private const float HoldingRowHeight = 130f;
        private const int VisibleHoldingRows = 4;

        public static readonly Color Ink = Hex("#1D2221");
        public static readonly Color Muted = Hex("#747873");
        public static readonly Color Light = Hex("#F2EEE7");
        public static readonly Color Gold = Hex("#E4BD61");
        public static readonly Color Up = Hex("#D64D43");
        public static readonly Color Down = Hex("#2578BA");
        public static readonly Color Flat = Hex("#8B8D89");
        public static readonly Color ShareholderBackground = Hex("#073335");

        [Header("글꼴")]
        [Tooltip("동봉된 NotoSansKR-400.ttf로 만든 Dynamic TMP Font Asset")]
        [SerializeField] private TMP_FontAsset koreanRegular;
        [Tooltip("동봉된 NotoSansKR-700.ttf로 만든 굵은 TMP Font Asset. 여기가 비면 아래 굵기 값으로 대신 두껍게 만든다")]
        [SerializeField] private TMP_FontAsset koreanBold;
        [Tooltip("굵은 글씨를 얼마나 두껍게 할지 (0 = 그대로). Korean Bold에 진짜 볼드 폰트를 넣으면 이 값은 무시된다")]
        [Range(0f, 0.3f)]
        [SerializeField] private float boldThickness = 0.15f;

        [Header("굵게 표시할 항목 — 보유 종목 줄")]
        [Tooltip("종목 이름 (예: 블레이즈마인 광산)")]
        [SerializeField] private bool boldCompanyName = true;
        [Tooltip("보유 수량 (예: 3주)")]
        [SerializeField] private bool boldQuantity = false;
        [Tooltip("평단가와 현재가 줄")]
        [SerializeField] private bool boldPrices = false;
        [Tooltip("평가액 (예: 2,550 G)")]
        [SerializeField] private bool boldEvaluation = true;
        [Tooltip("등락률 (예: +3.7%)")]
        [SerializeField] private bool boldReturn = false;

        [Header("보유 종목 줄")]
        [Tooltip("연결하면 이 프리팹을 종목 수만큼 복제해서 목록을 만든다. 비워두면 예전처럼 코드가 직접 그린다")]
        [SerializeField] private VaultHoldingRow holdingRowPrefab;

        [Header("데이터")]
        [SerializeField] private DelusionVaultScreenData data = new DelusionVaultScreenData();

        [Header("공통 배경")]
        [Tooltip("홈·시장·뉴스 탭에서 이미 쓰는 집무실 배경 Sprite를 연결하세요.")]
        [SerializeField] private Sprite sharedOfficeBackground;
        [Tooltip("켜면 패키지에 들어 있는 어두운 배경을 깐다. 기존 화면 배경을 그대로 쓸 거라면 꺼 둔다.")]
        [SerializeField] private bool drawBundledDarkBackground = false;

        [Header("탭 이동 이벤트")]
        [SerializeField] private UnityEvent onHomeRequested = new UnityEvent();
        [SerializeField] private UnityEvent onMarketRequested = new UnityEvent();
        [SerializeField] private UnityEvent onNewsRequested = new UnityEvent();
        [SerializeField] private VaultCompanyClickedEvent onCompanyClicked = new VaultCompanyClickedEvent();

        [Header("동작")]
        [SerializeField] private bool buildOnEnable = false;
        [Tooltip("끄면 이 화면 자체의 사이드바를 그리지 않는다. 프로젝트에 이미 SidebarTabManager 같은 전역 사이드바가 있을 때 끈다.")]
        [SerializeField] private bool buildSidebar = false;
        [Tooltip("끄면 이 화면 자체의 상단 상태바(시대/골드/날짜)를 그리지 않는다. 프로젝트에 이미 전역 상단바가 있을 때 끈다.")]
        [SerializeField] private bool buildStatusBar = false;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField, HideInInspector] private bool sampleDataInitialized;

        private RectTransform generatedRoot;
        private Material thickMaterial;   // 굵은 글씨용 재질. 매번 새로 만들지 않고 한 벌을 돌려쓴다

        public DelusionVaultScreenData Data => data;
        public VaultCompanyClickedEvent OnCompanyClicked => onCompanyClicked;

        // 한글 폰트가 연결돼 있는지 확인 (에디터에서 자동 연결 여부를 판단할 때 사용)
        public bool HasKoreanFont => koreanRegular != null;

        // 굵은 글씨용 폰트가 본문 폰트와 다른 별개 폰트인지 (같으면 진짜 볼드가 아니다)
        public bool HasRealBoldFont => koreanBold != null && koreanBold != koreanRegular;

        // 이미 연결된 본문 폰트 (에디터가 굵은 폰트만 따로 채워 넣을 때 쓴다)
        public TMP_FontAsset KoreanRegular => koreanRegular;

        // 한 줄의 높이. 프리팹이 연결돼 있으면 프리팹 높이를 그대로 쓴다
        private float RowHeight
        {
            get
            {
                if (holdingRowPrefab != null)
                {
                    var prefabRect = holdingRowPrefab.transform as RectTransform;
                    if (prefabRect != null && prefabRect.sizeDelta.y > 1f)
                        return prefabRect.sizeDelta.y;
                }
                return HoldingRowHeight;
            }
        }

        // 에디터가 만든 프리팹을 연결해 줄 때 쓰는 통로
        public void SetHoldingRowPrefab(VaultHoldingRow prefab)
        {
            holdingRowPrefab = prefab;
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

        [ContextMenu("확정 자산 탭 UI 생성 / 갱신")]
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
            BuildSummary(generatedRoot);
            BuildHoldings(generatedRoot);
            BuildMajorShareholders(generatedRoot);
        }

        public void SetData(DelusionVaultScreenData newData, bool rebuild = true)
        {
            data = newData ?? new DelusionVaultScreenData();
            sampleDataInitialized = true;
            if (rebuild)
                Rebuild();
        }

        // bold를 비워서 부르면 본문 폰트를 그대로 쓰고 굵기 효과만 들어간다
        public void SetFonts(TMP_FontAsset regular, TMP_FontAsset bold = null, bool rebuild = true)
        {
            koreanRegular = regular;
            koreanBold = bold != null ? bold : regular;
            if (rebuild)
                Rebuild();
        }

        public void RecalculateSummary(bool rebuild = true)
        {
            EnsureData();
            long evaluated = 0;
            long costFromHoldings = 0;
            if (data.holdings != null)
            {
                foreach (var item in data.holdings)
                {
                    if (item == null)
                        continue;
                    evaluated = MoneyMath.Add(evaluated, item.EvaluatedValue);
                    costFromHoldings = MoneyMath.Add(costFromHoldings, (long)Mathf.Max(0, item.quantity) * Mathf.Max(0, item.averagePrice));
                }
            }

            // 매수 원금이 밖에서 안 들어왔으면 보유 목록에서 직접 더한다
            if (data.purchaseCost <= 0)
                data.purchaseCost = costFromHoldings;

            // 총 자산 = 현금 + 주식 평가액 − 갚아야 할 대출금
            // 빌린 골드는 내 돈이 아니므로 빼야 실제로 남는 재산이 나온다
            data.totalAssets = MoneyMath.Add(MoneyMath.Add(data.cash, -System.Math.Max(0L, data.debt)), evaluated);

            // 손익은 주식만 본다 — "산 값보다 지금 얼마나 올랐나" (홈 화면 상단과 같은 기준)
            data.totalProfitAmount = evaluated - data.purchaseCost;
            data.totalProfitPercent = data.purchaseCost <= 0
                ? 0f
                : data.totalProfitAmount / (float)data.purchaseCost * 100f;
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

            var marker = CreateImage("WalletActiveMarker", parent, LoadSprite("active_marker"));
            SetTopLeft(marker.rectTransform, new Vector2(31f, 653f), new Vector2(6f, 44f));

            BuildSidebarButton(parent, "Home", "icon_home", new Vector2(48f, 284f), onHomeRequested);
            BuildSidebarButton(parent, "Market", "icon_market", new Vector2(48f, 424f), onMarketRequested);
            BuildSidebarButton(parent, "News", "icon_news", new Vector2(48f, 529f), onNewsRequested);
            BuildSidebarButton(parent, "Wallet", "icon_wallet", new Vector2(48f, 650f), null);
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
            SetTopLeft(bar.rectTransform, new Vector2(951f, 24f), new Vector2(800f, 78f));
            bar.type = Image.Type.Sliced;

            BuildStatusItem(bar.rectTransform, "Era", "icon_sun", 34f, "현재 시대 :", data.currentEra);
            BuildStatusItem(bar.rectTransform, "Gold", "icon_coin", 315f, "현재 골드 :", data.currentGold.ToString());
            BuildStatusItem(bar.rectTransform, "Day", "icon_hourglass", 588f, "현재 날짜 :", data.currentDay.ToString());
        }

        private void BuildStatusItem(RectTransform parent, string name, string iconName, float x, string label, string value)
        {
            var icon = CreateImage(name + "Icon", parent, LoadSprite(iconName));
            SetTopLeft(icon.rectTransform, new Vector2(x, 14f), new Vector2(42f, 42f));
            icon.preserveAspect = true;
            CreateText(name + "Label", parent, label, new Vector2(x + 53f, 8f), new Vector2(150f, 22f), 13f, true, Light, TextAlignmentOptions.Left);
            CreateText(name + "Value", parent, value, new Vector2(x + 53f, 31f), new Vector2(190f, 35f), 22f, true, Light, TextAlignmentOptions.Left);
        }

        private void BuildHeader(RectTransform parent)
        {
            CreateText("Title", parent, "내 금고", new Vector2(202f, 43f), new Vector2(300f, 58f), 48f, true, Light, TextAlignmentOptions.Left);
            CreateText("Subtitle", parent, "대표님의 자산 현황", new Vector2(202f, 102f), new Vector2(380f, 28f), 18f, false, new Color(0.82f, 0.82f, 0.79f), TextAlignmentOptions.Left);
        }

        private void BuildSummary(RectTransform parent)
        {
            var card = CreateImage("AssetSummary", parent, LoadSprite("card_cream_9slice"));
            SetTopLeft(card.rectTransform, new Vector2(200f, 152f), new Vector2(1550f, 100f));
            card.type = Image.Type.Sliced;

            var profitColor = ChangeColor(data.totalProfitPercent);
            CreateText("TotalLabel", card.rectTransform, "총 자산", new Vector2(52f, 31f), new Vector2(120f, 36f), 22f, true, Ink, TextAlignmentOptions.Left);
            CreateText("TotalValue", card.rectTransform, data.totalAssets.ToString("N0") + " G", new Vector2(165f, 12f), new Vector2(380f, 70f), 52f, true, Ink, TextAlignmentOptions.Left);
            // 대출이 있으면 현금 옆에 같이 보여준다 (총 자산에서 이미 빠진 금액이라는 걸 알 수 있게)
            var cashLine = "현금 " + data.cash.ToString("N0") + " G";
            if (data.debt > 0)
                cashLine += "  ·  대출 " + data.debt.ToString("N0") + " G";
            CreateText("Cash", card.rectTransform, cashLine, new Vector2(625f, 29f), new Vector2(420f, 42f), 23f, false, Ink, TextAlignmentOptions.Center);

            CreateText("ProfitLabel", card.rectTransform, "투자 손익", new Vector2(1068f, 31f), new Vector2(130f, 36f), 22f, true, Ink, TextAlignmentOptions.Left);
            CreateText("ProfitPercent", card.rectTransform, FormatPercent(data.totalProfitPercent), new Vector2(1174f, 10f), new Vector2(220f, 70f), 48f, true, profitColor, TextAlignmentOptions.Left);
            CreateText("ProfitAmount", card.rectTransform, FormatSignedAmount(data.totalProfitAmount), new Vector2(1384f, 15f), new Vector2(145f, 34f), 20f, true, profitColor, TextAlignmentOptions.Right);
            CreateText("ProfitBase", card.rectTransform,
                data.purchaseCost > 0 ? "매수 원금 " + data.purchaseCost.ToString("N0") + " G 대비" : "아직 산 종목이 없습니다",
                new Vector2(1310f, 52f), new Vector2(219f, 31f), 16f, false, Ink, TextAlignmentOptions.Right);
        }

        private void BuildHoldings(RectTransform parent)
        {
            var holdings = GetSortedHoldings();
            CreateText("HoldingsTitle", parent, "보유 종목 (" + holdings.Count + ")", new Vector2(203f, 269f), new Vector2(350f, 40f), 26f, false, Light, TextAlignmentOptions.Left);
            CreateText("HoldingsSort", parent, "평가액 많은 순", new Vector2(1490f, 269f), new Vector2(260f, 40f), 22f, false, Light, TextAlignmentOptions.Right);

            var card = CreateImage("HoldingsCard", parent, LoadSprite("card_cream_9slice"));
            SetTopLeft(card.rectTransform, new Vector2(200f, 313f), new Vector2(1550f, 525f));
            card.type = Image.Type.Sliced;
            card.raycastTarget = true;   // 카드 위에서 마우스 휠을 굴리면 목록이 스크롤되게 한다

            // 뷰포트에도 투명한 Image를 깔아둔다.
            // 그래야 종목 줄 사이의 빈 공간 위에서도 휠·드래그 입력이 잡힌다 (Image가 없으면 입력이 그냥 통과한다)
            var viewportImage = CreateImage("Viewport", card.rectTransform, null);
            var viewport = viewportImage.rectTransform;
            // 카드 안쪽(521)을 넘지 않는 선에서 4줄이 보이도록 뷰포트 높이를 잡는다
            SetTopLeft(viewport, new Vector2(20f, 2f), new Vector2(1490f, Mathf.Min(RowHeight * VisibleHoldingRows, 521f)));
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(1470f, Mathf.Max(RowHeight * VisibleHoldingRows, holdings.Count * RowHeight));

            if (holdings.Count == 0)
            {
                CreateText("EmptyHoldings", content, "아직 보유한 종목이 없습니다", new Vector2(46f, 205f), new Vector2(800f, 50f), 29f, true, Ink, TextAlignmentOptions.Left);
                CreateText("EmptyHoldingsBody", content, "시장에서 종목을 매수하면 이곳에 표시됩니다", new Vector2(46f, 258f), new Vector2(900f, 34f), 19f, false, Muted, TextAlignmentOptions.Left);
            }
            else
            {
                for (var i = 0; i < holdings.Count; i++)
                {
                    var showDivider = i < holdings.Count - 1;

                    // 프리팹이 연결돼 있으면 복제해서 쓰고, 없으면 예전처럼 코드로 그린다
                    if (holdingRowPrefab != null)
                        SpawnHoldingRow(content, holdings[i], i, showDivider);
                    else
                        BuildHoldingRow(content, holdings[i], i, showDivider);
                }
            }

            var scrollRect = card.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = RowHeight * 0.55f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;

            if (holdings.Count > VisibleHoldingRows)
            {
                var scrollbar = BuildNeutralScrollbar(card.rectTransform, holdings.Count);
                scrollRect.verticalScrollbar = scrollbar;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                scrollRect.verticalScrollbarSpacing = 0f;
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        // 프리팹을 복제해 한 줄을 만든다. 값 채우기는 프리팹의 VaultHoldingRow가 맡는다
        private void SpawnHoldingRow(RectTransform parent, VaultHoldingItem item, int index, bool divider)
        {
            var row = Instantiate(holdingRowPrefab, parent);
            row.name = "Holding_" + item.companyName;

            var rect = row.transform as RectTransform;
            if (rect != null)
            {
                var width = rect.sizeDelta.x > 1f ? rect.sizeDelta.x : 1470f;
                SetTopLeft(rect, new Vector2(0f, index * RowHeight), new Vector2(width, RowHeight));
            }

            row.Apply(item, ResolveLogo(item.logoOverride, item.logoResource), divider,
                clickedName => onCompanyClicked?.Invoke(clickedName));
        }

        // 에디터에서 "지금 디자인 그대로" 프리팹을 뜰 때 쓴다. 한 줄만 그려서 돌려준다
        public GameObject CreateHoldingRowTemplate(RectTransform parent, VaultHoldingItem item)
        {
            BuildHoldingRow(parent, item, 0, true);
            return parent.childCount > 0 ? parent.GetChild(parent.childCount - 1).gameObject : null;
        }

        private void BuildHoldingRow(RectTransform parent, VaultHoldingItem item, int index, bool divider)
        {
            var row = CreateImage("Holding_" + item.companyName, parent, null);
            SetTopLeft(row.rectTransform, new Vector2(0f, index * RowHeight), new Vector2(1470f, RowHeight));
            row.color = new Color(1f, 1f, 1f, 0.001f);
            row.raycastTarget = true;

            var logo = CreateImage("Logo", row.rectTransform, ResolveLogo(item.logoOverride, item.logoResource));
            SetTopLeft(logo.rectTransform, new Vector2(44f, 24f), new Vector2(82f, 82f));
            logo.preserveAspect = true;

            // 이름이 비어 있어도 여기서 멈추지 않도록 한 번 걸러준다 (예전엔 null이면 아래 목록 전체가 안 그려졌다)
            var companyLabel = string.IsNullOrEmpty(item.companyName) ? "이름 없음" : item.companyName;
            var nameWidth = Mathf.Clamp(companyLabel.Length * 34f + 18f, 240f, 520f);
            // 굵기는 인스펙터의 "굵게 표시할 항목" 체크박스로 정한다
            CreateText("CompanyName", row.rectTransform, companyLabel, new Vector2(168f, 30f), new Vector2(nameWidth, 42f), 31f, boldCompanyName, Ink, TextAlignmentOptions.Left);
            CreateText("Quantity", row.rectTransform, item.quantity + "주", new Vector2(180f + nameWidth, 34f), new Vector2(90f, 34f), 21f, boldQuantity, Ink, TextAlignmentOptions.Left);
            CreateText("Prices", row.rectTransform,
                "평단 " + item.averagePrice.ToString("N0") + " G  ·  현재 " + item.currentPrice.ToString("N0") + " G",
                new Vector2(168f, 75f), new Vector2(570f, 32f), 21f, boldPrices, Ink, TextAlignmentOptions.Left);

            CreateText("Evaluation", row.rectTransform, item.EvaluatedValue.ToString("N0") + " G", new Vector2(1190f, 27f), new Vector2(230f, 43f), 31f, boldEvaluation, Ink, TextAlignmentOptions.Right);
            CreateText("Return", row.rectTransform, FormatPercent(item.ReturnPercent), new Vector2(1190f, 72f), new Vector2(230f, 36f), 27f, boldReturn, ChangeColor(item.ReturnPercent), TextAlignmentOptions.Right);

            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var companyName = item.companyName;
            button.onClick.AddListener(() => onCompanyClicked?.Invoke(companyName));

            if (!divider)
                return;

            var line = CreateImage("Divider", row.rectTransform, null);
            SetTopLeft(line.rectTransform, new Vector2(0f, RowHeight - 1f), new Vector2(1470f, 1f));
            line.color = new Color(0.77f, 0.74f, 0.69f, 0.5f);
        }

        private Scrollbar BuildNeutralScrollbar(RectTransform parent, int totalRows)
        {
            var track = CreateImage("HoldingsScrollbar", parent, null);
            SetTopLeft(track.rectTransform, new Vector2(1525f, 37f), new Vector2(5f, 450f));
            track.color = new Color(0.84f, 0.83f, 0.80f, 0.72f);
            track.raycastTarget = true;

            var slidingArea = CreateRect("SlidingArea", track.rectTransform);
            Stretch(slidingArea);

            var handle = CreateImage("Handle", slidingArea, null);
            Stretch(handle.rectTransform);
            handle.color = new Color(0.58f, 0.58f, 0.56f, 0.88f);
            handle.raycastTarget = true;

            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.numberOfSteps = 0;
            scrollbar.size = Mathf.Clamp01(VisibleHoldingRows / (float)Mathf.Max(VisibleHoldingRows, totalRows));
            scrollbar.value = 1f;
            return scrollbar;
        }

        private void BuildMajorShareholders(RectTransform parent)
        {
            CreateText("MajorTitle", parent, "대주주 현황", new Vector2(203f, 850f), new Vector2(360f, 38f), 25f, false, Light, TextAlignmentOptions.Left);

            var shareholders = data.majorShareholders ?? new List<VaultMajorShareholderItem>();
            if (shareholders.Count == 0)
            {
                var emptyCard = CreateImage("NoMajorShareholder", parent, LoadSprite("card_cream_9slice"));
                SetTopLeft(emptyCard.rectTransform, new Vector2(200f, 890f), new Vector2(1550f, 150f));
                emptyCard.type = Image.Type.Sliced;
                CreateText("EmptyTitle", emptyCard.rectTransform, "아직 대주주인 기업이 없습니다", new Vector2(56f, 42f), new Vector2(680f, 42f), 29f, true, Ink, TextAlignmentOptions.Left);
                CreateText("EmptyBody", emptyCard.rectTransform, "기업의 일정 지분 이상을 보유하면 이곳에 표시됩니다", new Vector2(56f, 87f), new Vector2(900f, 31f), 18f, false, Muted, TextAlignmentOptions.Left);
                return;
            }

            var viewport = CreateRect("MajorShareholderViewport", parent);
            SetTopLeft(viewport, new Vector2(200f, 890f), new Vector2(1550f, 150f));
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(1550f, shareholders.Count * 160f);

            for (var i = 0; i < shareholders.Count; i++)
                BuildMajorShareholderCard(content, shareholders[i], i * 160f);

            if (shareholders.Count > 1)
            {
                var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
                scrollRect.viewport = viewport;
                scrollRect.content = content;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 72f;
            }
        }

        private void BuildMajorShareholderCard(RectTransform parent, VaultMajorShareholderItem item, float y)
        {
            var card = CreateImage("Major_" + item.companyName, parent, LoadSprite("shareholder_card_9slice"));
            SetTopLeft(card.rectTransform, new Vector2(0f, y), new Vector2(1550f, 150f));
            card.type = Image.Type.Sliced;
            card.raycastTarget = true;

            var logoBacking = CreateImage("LogoBacking", card.rectTransform, LoadSprite("circle_cream"));
            SetTopLeft(logoBacking.rectTransform, new Vector2(65f, 18f), new Vector2(114f, 114f));
            logoBacking.preserveAspect = true;

            var logo = CreateImage("Logo", logoBacking.rectTransform, ResolveLogo(item.logoOverride, item.logoResource));
            SetTopLeft(logo.rectTransform, new Vector2(15f, 15f), new Vector2(84f, 84f));
            logo.preserveAspect = true;

            CreateText("CompanyName", card.rectTransform, item.companyName, new Vector2(215f, 30f), new Vector2(610f, 48f), 36f, true, Gold, TextAlignmentOptions.Left);
            CreateText("Ownership", card.rectTransform, "보유 지분 " + item.ownershipPercent.ToString("0.0") + "%", new Vector2(215f, 82f), new Vector2(420f, 35f), 25f, false, Gold, TextAlignmentOptions.Left);

            var crown = CreateImage("Crown", card.rectTransform, LoadSprite("icon_crown"));
            SetTopLeft(crown.rectTransform, new Vector2(1125f, 30f), new Vector2(94f, 94f));
            crown.preserveAspect = true;

            var badgeTitle = item.isLargestShareholder ? "최대주주" : "대주주";
            CreateText("BadgeTitle", card.rectTransform, badgeTitle, new Vector2(1260f, 34f), new Vector2(230f, 48f), 39f, true, Gold, TextAlignmentOptions.Left);
            CreateText("BadgeBody", card.rectTransform, "대주주 기준 달성", new Vector2(1260f, 87f), new Vector2(230f, 31f), 20f, false, Gold, TextAlignmentOptions.Left);

            var button = card.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var companyName = item.companyName;
            button.onClick.AddListener(() => onCompanyClicked?.Invoke(companyName));
        }

        private List<VaultHoldingItem> GetSortedHoldings()
        {
            var result = new List<VaultHoldingItem>();
            if (data.holdings != null)
            {
                foreach (var item in data.holdings)
                    if (item != null)
                        result.Add(item);
            }
            result.Sort((a, b) => b.EvaluatedValue.CompareTo(a.EvaluatedValue));
            return result;
        }

        private Sprite ResolveLogo(Sprite overrideSprite, string resourceName)
        {
            if (overrideSprite != null)
                return overrideSprite;
            if (string.IsNullOrWhiteSpace(resourceName))
                return null;
            return Resources.Load<Sprite>(ResourceRoot + "Logos/" + resourceName);
        }

        private void EnsureSampleData()
        {
            if (sampleDataInitialized)
                return;
            if (data == null)
                data = new DelusionVaultScreenData();
            if ((data.holdings == null || data.holdings.Count == 0) &&
                (data.majorShareholders == null || data.majorShareholders.Count == 0))
                SeedSampleData();
            sampleDataInitialized = true;
        }

        private void EnsureData()
        {
            if (data == null)
                data = new DelusionVaultScreenData();
            if (data.holdings == null)
                data.holdings = new List<VaultHoldingItem>();
            if (data.majorShareholders == null)
                data.majorShareholders = new List<VaultMajorShareholderItem>();
        }

        private void SeedSampleData()
        {
            data = new DelusionVaultScreenData
            {
                currentEra = "마법의 시대",
                currentGold = 5000,
                currentDay = 42,
                totalAssets = 12340,
                cash = 5000,
                debt = 0,
                purchaseCost = 0,          // 0이면 보유 목록에서 직접 더한다
                startingAssets = 5000,
                totalProfitAmount = 1920,
                totalProfitPercent = 18.4f,
                holdings = new List<VaultHoldingItem>
                {
                    Holding("윈드시어 매직스", "17_windshire-magics", 3, 820, 850),
                    Holding("아이언하트 공업", "09_ironheart-industries", 2, 590, 610),
                    Holding("에이더니얼 자산관리", "07_aethernial-asset-management", 4, 285, 280),
                    Holding("드래곤 금고", "05_dragon-vault", 1, 940, 950),
                    Holding("블레이즈마인 광산", "01_blazemine-mining", 2, 400, 380)
                },
                majorShareholders = new List<VaultMajorShareholderItem>
                {
                    new VaultMajorShareholderItem
                    {
                        companyName = "윈드시어 매직스",
                        logoResource = "17_windshire-magics",
                        ownershipPercent = 12f,
                        isLargestShareholder = true
                    }
                }
            };
        }

        private static VaultHoldingItem Holding(string name, string logo, int quantity, int average, int current)
        {
            return new VaultHoldingItem
            {
                companyName = name,
                logoResource = logo,
                quantity = quantity,
                averagePrice = average,
                currentPrice = current
            };
        }

        private TMP_FontAsset Font(bool bold)
        {
            var requested = bold ? koreanBold : koreanRegular;
            if (requested != null)
                return requested;
            if (bold && koreanRegular != null)
                return koreanRegular;
            if (!bold && koreanBold != null)
                return koreanBold;
            return TMP_Settings.defaultFontAsset;
        }

        // 폰트 원본 재질을 복사해 "면 두께(Face Dilate)"만 올린 재질을 만든다
        // 원본 재질을 직접 건드리면 게임 전체의 같은 폰트 글자가 다 두꺼워지므로 복사본을 쓴다
        private Material GetThickMaterial(TMP_FontAsset font)
        {
            if (thickMaterial == null || thickMaterial.mainTexture != font.material.mainTexture)
            {
                thickMaterial = new Material(font.material);
                thickMaterial.name = font.name + " (굵게)";
                thickMaterial.hideFlags = HideFlags.DontSave;
            }

            thickMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, boldThickness);
            return thickMaterial;
        }

        public static string FormatPercent(float value)
        {
            if (Mathf.Abs(value) < 0.005f)
                return "0.0%";
            return (value > 0f ? "+" : "-") + Mathf.Abs(value).ToString("0.0") + "%";
        }

        private static string FormatSignedAmount(long value)
        {
            if (value == 0)
                return "0 G";
            return (value > 0 ? "▲ " : "▼ ") + Mathf.Abs(value).ToString("N0") + " G";
        }

        public static Color ChangeColor(float value)
        {
            if (value > 0.005f)
                return Up;
            if (value < -0.005f)
                return Down;
            return Flat;
        }

        private Sprite LoadSprite(string name)
        {
            return Resources.Load<Sprite>(ResourceRoot + name);
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            if (parent == null)
                throw new InvalidOperationException("DelusionVaultScreen은 RectTransform 아래에 배치해야 합니다.");
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI CreateText(string name, RectTransform parent, string value, Vector2 position, Vector2 size, float fontSize, bool bold, Color color, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            SetTopLeft(rect, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value ?? string.Empty;
            text.font = Font(bold);
            text.fontSize = fontSize;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = color;
            text.alignment = alignment;

            // 진짜 볼드 폰트가 없으면 글자 면을 살짝 부풀려서 굵게 보이게 한다
            // (TMP의 Bold 스타일은 폰트에 굵은 글꼴이 있어야 실제로 두꺼워진다)
            if (bold && !HasRealBoldFont && boldThickness > 0f && text.font != null)
                text.fontSharedMaterial = GetThickMaterial(text.font);
            text.textWrappingMode = TextWrappingModes.NoWrap;   // 예전 enableWordWrapping = false 와 같다
            // Ellipsis는 상자 높이에 한 줄이 안 들어가면 글자를 통째로 안 그린다.
            // 좌표가 1920×1080 고정이라 상자가 글자 크기보다 조금 작은 곳이 있어서, 넘치더라도 그리는 쪽으로 둔다
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void RemoveGeneratedRoot()
        {
            var existing = transform.Find(GeneratedRootName);
            if (existing == null)
                return;
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        private static Color Hex(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out var color) ? color : Color.white;
        }
    }
}
