using System;
using UnityEngine;

namespace Utkaka.ScaleNineSlicer.UI
{
    [CreateAssetMenu(fileName = "CapsuleSprite", menuName = "2D/Capsule Sprite", order = 999)]
    public class CapsuleSprite : ScriptableObject
    {
        [SerializeField]
        private Sprite _sprite;
        [SerializeField]
        private float _innerRadius;
        [SerializeField]
        private float _outerRadius;
        [SerializeField]
        private Vector2 _center;

        public Sprite Sprite => _sprite;

        public float InnerRadius => _innerRadius;

        public float OuterRadius => _outerRadius;

        public Vector2 Center => _center;
        
        private void OnValidate()
        {
            _outerRadius = Math.Min(_outerRadius, Math.Min(_sprite.rect.width / 2.0f, _sprite.rect.height/ 2.0f));
            _innerRadius = Math.Min(_innerRadius, _outerRadius);
        }
    }
}