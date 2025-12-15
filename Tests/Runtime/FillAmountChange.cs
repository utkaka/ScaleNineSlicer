using System;
using UnityEngine;
using UnityEngine.UI;
using Utkaka.ScaleNineSlicer.UI;

namespace Utkaka.ScaleNineSlicer.Tests.Runtime
{
    public class FillAmountChange : MonoBehaviour
    {
        private SlicedImage _slicedImage;
        private Image _image;
        private bool _isSliced;

        private void Awake()
        {
            _slicedImage = GetComponent<SlicedImage>();
            _image = GetComponent<Image>();
            _isSliced = _slicedImage != null;
        }

        private void Update()
        {
            if (_isSliced)
            {
                _slicedImage.fillAmount += 0.01f;
            }
            else
            {
                _image.fillAmount += 0.01f;
            }
        }
    }
}