using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MelonPrefManager.UI
{
    // A Slider Scrollbar which automatically resizes for the content size (no pooling).
    // Currently just used for the C# Console input field.

    public class AutoSliderScrollbar
    {
        internal static void UpdateInstances()
        {
            foreach (var instance in Instances)
            {
                if (!instance.Enabled)
                    continue;

                instance.Update();
            }
        }

        internal static readonly List<AutoSliderScrollbar> Instances = new List<AutoSliderScrollbar>();

        public GameObject UIRoot
        {
            get
            {
                if (Slider)
                    return Slider.gameObject;
                return null;
            }
        }

        public bool Enabled => UIRoot.activeInHierarchy;

        //public event Action<float> OnValueChanged;

        internal readonly Scrollbar Scrollbar;
        internal readonly Slider Slider;
        internal RectTransform ContentRect;
        internal RectTransform ViewportRect;

        //internal InputFieldScroller m_parentInputScroller;

        public AutoSliderScrollbar(Scrollbar scrollbar, Slider slider, RectTransform contentRect, RectTransform viewportRect)
        {
            Instances.Add(this);

            this.Scrollbar = scrollbar;
            this.Slider = slider;
            this.ContentRect = contentRect;
            this.ViewportRect = viewportRect;

            this.Scrollbar.onValueChanged.AddListener(this.OnScrollbarValueChanged);
            this.Slider.onValueChanged.AddListener(this.OnSliderValueChanged);

            this.Slider.Set(0f, false);
        }

        private float lastAnchorPosition;
        private float lastContentHeight;
        private float lastViewportHeight;
        private bool _refreshWanted;

        public void Update()
        {
            if (!Enabled)
                return;

            _refreshWanted = false;
            if (ContentRect.localPosition.y != lastAnchorPosition)
            {
                lastAnchorPosition = ContentRect.localPosition.y;
                _refreshWanted = true;
            }
            if (ContentRect.rect.height != lastContentHeight)
            {
                lastContentHeight = ContentRect.rect.height;
                _refreshWanted = true;
            }
            if (ViewportRect.rect.height != lastViewportHeight)
            {
                lastViewportHeight = ViewportRect.rect.height;
                _refreshWanted = true;
            }

            if (_refreshWanted)
            {
                UpdateSliderHandle();
            }
        }

        public void UpdateSliderHandle()
        {
            // calculate handle size based on viewport / total data height
            var totalHeight = ContentRect.rect.height;
            var viewportHeight = ViewportRect.rect.height;

            if (totalHeight <= viewportHeight)
            {
                Slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
                Slider.value = 0f;
                Slider.interactable = false;
                return;
            }

            var handleHeight = viewportHeight * Math.Min(1, viewportHeight / totalHeight);
            handleHeight = Math.Max(15f, handleHeight);

            // resize the handle container area for the size of the handle (bigger handle = smaller container)
            var container = Slider.m_HandleContainerRect;
            container.offsetMax = new Vector2(container.offsetMax.x, -(handleHeight * 0.5f));
            container.offsetMin = new Vector2(container.offsetMin.x, handleHeight * 0.5f);

            // set handle size
            Slider.handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, handleHeight);

            // if slider is 100% height then make it not interactable
            Slider.interactable = !Mathf.Approximately(handleHeight, viewportHeight);

            //float val = 0f;
            //if (totalHeight > 0f && totalHeight != viewportHeight)
            //    val = (float)((decimal)ContentRect.anchoredPosition.y / (decimal)(totalHeight - viewportHeight));
            //
            //PrefManagerMod.Log("Setting slider val to " + val + ", anchored pos: " + ContentRect.anchoredPosition.y + ", totalh: " + totalHeight + ", viewportH: " + viewportHeight);
            //
            //Slider.value = val;
        }

        public void OnScrollbarValueChanged(float value)
        {
            value = 1f - value;
            if (this.Slider.value != value)
                this.Slider.Set(value, false);
            //OnValueChanged?.Invoke(value);
        }

        public void OnSliderValueChanged(float value)
        {
            value = 1f - value;
            this.Scrollbar.value = value;
            //OnValueChanged?.Invoke(value);
        }
    }
}