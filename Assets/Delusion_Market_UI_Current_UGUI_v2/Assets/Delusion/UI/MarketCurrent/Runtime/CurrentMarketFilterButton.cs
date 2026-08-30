using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Delusion.UI.MarketCurrent
{
    public sealed class CurrentMarketFilterButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private CurrentMarketScreen owner;
        private Image background;
        private TMP_Text label;
        private Sprite normalSprite;
        private Sprite selectedSprite;
        private Sprite hoverSprite;
        private string filterName;
        private bool selected;

        public string FilterName => filterName;

        public void Configure(CurrentMarketScreen screen, Image image, TMP_Text text, Sprite normal, Sprite selectedState, Sprite hover, string value)
        {
            owner = screen;
            background = image;
            label = text;
            normalSprite = normal;
            selectedSprite = selectedState;
            hoverSprite = hover;
            filterName = value;
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (background != null)
                background.sprite = selected ? selectedSprite : normalSprite;
            if (label != null)
            {
                label.color = selected ? CurrentMarketScreen.Ink : CurrentMarketScreen.LightText;
                label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!selected && background != null && hoverSprite != null)
                background.sprite = hoverSprite;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!selected && background != null)
                background.sprite = normalSprite;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.ApplyFilter(filterName);
        }
    }
}
