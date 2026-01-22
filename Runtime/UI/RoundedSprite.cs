using System;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    [CreateAssetMenu(fileName = "RoundedSprite", menuName = "2D/Rounded Sprite", order = 999)]
    public class RoundedSprite : ScriptableObject
    {
        [SerializeField]
        private Sprite _sprite;
        [SerializeField]
        private float _outerRadius;
        [SerializeField]
        private Vector2 _outerCenter;
        [SerializeField]
        private float _innerRadius;
        [SerializeField]
        private Vector2 _innerCenter;

        public Sprite Sprite => _sprite;

        public float InnerRadius => _innerRadius;

        public float OuterRadius => _outerRadius;

        public Vector2 OuterCenter => _outerCenter;

        public Vector2 InnerCenter => _innerCenter;

        private void OnValidate()
        {
            _outerRadius = Mathf.Max(_outerRadius, 0);
            _innerRadius = Mathf.Max(_innerRadius, 0);
            if (_sprite == null) return;
            _outerRadius = Mathf.Min(_outerRadius, Math.Min(_sprite.rect.width / 2.0f, _sprite.rect.height/ 2.0f));
            _innerRadius = Mathf.Min(_innerRadius, _outerRadius);
        }
    }
}