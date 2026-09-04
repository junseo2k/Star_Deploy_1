using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Delusion.UI.Vault
{
    /// <summary>
    /// 보유 종목 한 줄을 담당하는 프리팹용 컴포넌트.
    /// 화면(DelusionVaultScreen)이 이 프리팹을 종목 수만큼 복제한 뒤 Apply()로 값만 채워 넣습니다.
    /// 줄의 생김새(위치·크기·색·폰트)는 전부 프리팹에서 눈으로 편집하면 됩니다.
    /// </summary>
    public sealed class VaultHoldingRow : MonoBehaviour
    {
        [Header("줄 안의 표시 요소 — 비워두면 그 항목만 건너뜁니다")]
        [SerializeField] private Image logo;
        [SerializeField] private TMP_Text companyNameText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private TMP_Text pricesText;
        [SerializeField] private TMP_Text evaluationText;
        [SerializeField] private TMP_Text returnText;
        [Tooltip("줄 사이 구분선. 마지막 줄에서는 자동으로 꺼집니다")]
        [SerializeField] private GameObject divider;
        [Tooltip("줄 전체를 누를 때 쓰는 버튼. 없으면 클릭이 무시됩니다")]
        [SerializeField] private Button clickButton;

        // 컴포넌트를 붙이는 순간 자식 이름으로 자동 연결한다 (Logo, CompanyName, Quantity, ...)
        private void Reset()
        {
            AutoWire();
        }

        /// <summary>자식 오브젝트 이름을 보고 위 칸들을 자동으로 채웁니다.</summary>
        public void AutoWire()
        {
            logo = FindChild<Image>("Logo");
            companyNameText = FindChild<TMP_Text>("CompanyName");
            quantityText = FindChild<TMP_Text>("Quantity");
            pricesText = FindChild<TMP_Text>("Prices");
            evaluationText = FindChild<TMP_Text>("Evaluation");
            returnText = FindChild<TMP_Text>("Return");

            var dividerTransform = transform.Find("Divider");
            divider = dividerTransform != null ? dividerTransform.gameObject : null;

            clickButton = GetComponent<Button>();
        }

        /// <summary>종목 하나의 값을 이 줄에 채워 넣습니다.</summary>
        /// <param name="item">보여줄 종목</param>
        /// <param name="logoSprite">로고 그림 (없으면 로고를 숨깁니다)</param>
        /// <param name="showDivider">아래 구분선을 그릴지 (마지막 줄은 false)</param>
        /// <param name="onClicked">줄을 눌렀을 때 종목 이름을 전달받을 곳</param>
        public void Apply(VaultHoldingItem item, Sprite logoSprite, bool showDivider, Action<string> onClicked)
        {
            if (item == null)
                return;

            // 이름이 비어 있어도 줄이 깨지지 않게 한 번 걸러준다
            string companyLabel = string.IsNullOrEmpty(item.companyName) ? "이름 없음" : item.companyName;

            if (logo != null)
            {
                logo.sprite = logoSprite;
                logo.enabled = logoSprite != null;   // 로고가 없으면 흰 사각형이 남지 않도록 끈다
            }

            if (companyNameText != null) companyNameText.text = companyLabel;
            if (quantityText != null) quantityText.text = item.quantity + "주";
            if (pricesText != null)
                pricesText.text = "평단 " + item.averagePrice.ToString("N0") + " G  ·  현재 " + item.currentPrice.ToString("N0") + " G";
            if (evaluationText != null) evaluationText.text = item.EvaluatedValue.ToString("N0") + " G";

            if (returnText != null)
            {
                returnText.text = DelusionVaultScreen.FormatPercent(item.ReturnPercent);
                returnText.color = DelusionVaultScreen.ChangeColor(item.ReturnPercent);   // 오르면 빨강, 내리면 파랑
            }

            if (divider != null) divider.SetActive(showDivider);

            if (clickButton != null)
            {
                // 복제된 줄이 이전 종목의 클릭을 물고 있지 않도록 매번 비우고 다시 건다
                clickButton.onClick.RemoveAllListeners();
                if (onClicked != null)
                {
                    string clickedName = item.companyName;
                    clickButton.onClick.AddListener(() => onClicked(clickedName));
                }
            }
        }

        // 자식 중에서 이름이 같은 컴포넌트를 찾는다 (꺼져 있는 오브젝트도 포함)
        private T FindChild<T>(string childName) where T : Component
        {
            foreach (T candidate in GetComponentsInChildren<T>(true))
            {
                if (candidate.gameObject.name == childName)
                    return candidate;
            }
            return null;
        }
    }
}
