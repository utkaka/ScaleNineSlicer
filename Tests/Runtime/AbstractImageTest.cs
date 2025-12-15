using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Utkaka.ScaleNineSlicer.UI;

namespace Utkaka.ScaleNineSlicer.Tests.Runtime
{

    public class AbstractImageTest
    {
        protected Camera Camera;
        protected Canvas Canvas;
        protected RectTransform ImageContainer;
        private Sprite _sprite;
    
        [SetUp]
        public virtual void SetUp()
        {
            Camera = new GameObject("UICamera", typeof(Camera)).GetComponent<Camera>();
            Camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            Camera.orthographic = true;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Color.black;

            Canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            Canvas.gameObject.hideFlags = HideFlags.HideAndDontSave;
            Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            Canvas.worldCamera = Camera;
            Canvas.pixelPerfect = false;

            var scaler = Canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1.0f;
            scaler.referencePixelsPerUnit = 100.0f;
            
            ImageContainer = new GameObject("Container", typeof(RectTransform)).GetComponent<RectTransform>();
            ImageContainer.gameObject.hideFlags = HideFlags.HideAndDontSave;
            ImageContainer.SetParent(Canvas.transform, false);
            ImageContainer.anchorMin = Vector2.zero;
            ImageContainer.anchorMax = Vector2.one;
            ImageContainer.offsetMin = Vector2.zero;
            ImageContainer.offsetMax = Vector2.zero;

            _sprite = Resources.Load<Sprite>("Test");
        }
    
        [TearDown]
        public virtual void TearDown()
        {
            if (ImageContainer != null)
                Object.DestroyImmediate(ImageContainer.gameObject);
            if (Canvas != null)
                Object.DestroyImmediate(Canvas.gameObject);
            if (Camera != null)
                Object.DestroyImmediate(Camera.gameObject);
            if (_sprite != null)
                Resources.UnloadAsset(_sprite);
        }
        
        protected Sprite CreateSprite(TextureWrapMode wrapMode, SpriteMeshType spriteMeshType, Vector4 border)
        {
            var texture = Object.Instantiate(_sprite.texture);
            texture.wrapMode = wrapMode;
            return Sprite.Create(
                texture,
                _sprite.rect,
                _sprite.pivot,
                _sprite.pixelsPerUnit,
                0,
                spriteMeshType,
                border
            );
        }
        
        protected Image CreateImage(Sprite sprite, bool preserveAspect, bool useSpriteMesh, bool fillCenter, int pixelsPerUnitMultiplier, Vector2Int size)
        {
            var transform = CreateImageTransform(size);
            var image = transform.gameObject.AddComponent<ImageWithMeshStatistics>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.useSpriteMesh = useSpriteMesh;
            image.fillCenter = fillCenter;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            return image;
        }
        
        protected SlicedImage CreateSlicedImage(Sprite sprite, bool preserveAspect, bool useSpriteMesh, bool fillCenter, int pixelsPerUnitMultiplier, Vector2Int size)
        {
            var transform = CreateImageTransform(size);
            var image = transform.gameObject.AddComponent<SlicedImageWithMeshStatistics>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.useSpriteMesh = useSpriteMesh;
            image.fillCenter = fillCenter;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            return image;
        }

        private RectTransform CreateImageTransform(Vector2Int size)
        {
            var rect = new GameObject("Image", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.hideFlags = HideFlags.HideAndDontSave;
            rect.SetParent(ImageContainer, false);
            rect.sizeDelta = size;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            return rect;
        }
    }
}