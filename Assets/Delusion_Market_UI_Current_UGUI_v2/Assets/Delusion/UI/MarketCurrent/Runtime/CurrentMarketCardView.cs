using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Delusion.UI.MarketCurrent
{
    public sealed class CurrentMarketCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private CurrentMarketScreen owner;
        private Image background;
        private Sprite normalSprite;
        private Sprite hoverSprite;
        private string companyName;
        private string industry;

        public string Industry => industry;

        public void Configure(CurrentMarketScreen screen, Image cardBackground, Sprite normal, Sprite hover, string name, string sector)
        {
            owner = screen;
            background = cardBackground;
            normalSprite = normal;
            hoverSprite = hover;
            companyName = name;
            industry = sector;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null && hoverSprite != null)
                background.sprite = hoverSprite;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (background != null && normalSprite != null)
                background.sprite = normalSprite;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.NotifyCompanyClicked(companyName);
        }
    }
}
