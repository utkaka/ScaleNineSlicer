using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScaleNineSlicer.Editor {
    public class SpriteNineSlicerWindow : EditorWindow {
        private enum ChildView {
            Borders = 0,
            Preview = 1
        }
        
        [SerializeField]
        private VisualTreeAsset _uxmlRoot;
        [SerializeField]
        private VisualTreeAsset _uxmlBorderEditor;
        [SerializeField]
        private VisualTreeAsset _uxmlSlicePreview;

        private VisualElement _root;

        private Button _prevButton;
        private Label _currentSpriteIndex;
        private Button _nextButton;
        private ToolbarToggle _bordersToggle;
        private ToolbarToggle _previewToggle;
        private Toggle _trimAlphaToggle;
        private Toggle _trimCenterToggle;
        private VisualElement _container;
        private VisualElement _bordersView;
        private VisualElement _sliceView;
        private BordersEditor _bordersEditor;
        private SlicePreview _slicePreview;

        private int _selectedIndex;
        private List<Sprite> _selectedSprites;
        private SpriteInfo _selectedSpriteInfo;
        private SpriteInfo _trimmedSpriteInfo;

        [MenuItem("Window/2D/Sprite Nine Slicer")]
        public static void ShowWindow() {
            var window = GetWindow<SpriteNineSlicerWindow>();
            window.titleContent = new GUIContent("Sprite Nine Slicer");
        }

        public void CreateGUI() {
            _root = rootVisualElement;
            _uxmlRoot.CloneTree(_root);
            _prevButton = _root.Q<Button>("PrevSprite");
            _currentSpriteIndex = _root.Q<Label>("CurrentSpriteIndex");
            _nextButton = _root.Q<Button>("NextSprite");
            _bordersToggle = _root.Q<ToolbarToggle>("Borders");
            _previewToggle = _root.Q<ToolbarToggle>("Preview");
            _trimCenterToggle = _root.Q<Toggle>("TrimCenter");
            _trimAlphaToggle = _root.Q<Toggle>("TrimAlpha");
            _container = _root.Q<VisualElement>(name: "Container");
            
            _prevButton.RegisterCallback<ClickEvent>(evt => {
                if (!CanProceedWithCurrentChanges())  return;
                _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
                UpdateSelectedSprite();
            });
            _nextButton.RegisterCallback<ClickEvent>(evt => {
                if (!CanProceedWithCurrentChanges())  return;
                _selectedIndex = Mathf.Min(_selectedSprites.Count - 1, _selectedIndex + 1);
                UpdateSelectedSprite();
            });
            
            _bordersToggle.RegisterCallback<ClickEvent>(evt => { ShowView(ChildView.Borders);});
            _previewToggle.RegisterCallback<ClickEvent>(evt => { ShowView(ChildView.Preview);});
            _trimCenterToggle.RegisterValueChangedCallback(OnTrimChange);
            _trimAlphaToggle.RegisterValueChangedCallback(OnTrimChange);
            _bordersView = _uxmlBorderEditor.CloneTree();
            _bordersView.AddToClassList("main-area");
            _sliceView = _uxmlSlicePreview.CloneTree();
            _sliceView.AddToClassList("main-area");
            _bordersEditor = new BordersEditor(_bordersView);
            _slicePreview = new SlicePreview(_sliceView);
            _root.Q<ToolbarButton>(name:"Revert").RegisterCallback<ClickEvent>((evt) => { DiscardChanges();});
            _root.Q<ToolbarButton>(name:"Apply").RegisterCallback<ClickEvent>((evt) => { SaveChanges();});
            Selection.selectionChanged += UpdateSelection;
            ShowView(ChildView.Borders);
            UpdateSelection();
        }

        public override void SaveChanges() {
            hasUnsavedChanges = false;
            _trimmedSpriteInfo.WriteTextureToFile(AssetDatabase.GetAssetPath(_selectedSprites[_selectedIndex].texture));

            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_selectedSprites[_selectedIndex].texture));            
            textureImporter.spriteBorder = _trimmedSpriteInfo.Border.ToVector4();
            textureImporter.SaveAndReimport();

            UpdateSelectedSprite();
            base.SaveChanges();
        }

        public override void DiscardChanges() {
            hasUnsavedChanges = false;
            
            _trimAlphaToggle.SetValueWithoutNotify(false);
            _trimCenterToggle.SetValueWithoutNotify(false);
            
            UpdateSelectedSprite();
            base.DiscardChanges();
        }

        private void OnDestroy() {
            Selection.selectionChanged -= UpdateSelection;
            _selectedSpriteInfo.BorderChange -= UpdateTrimmedSpriteInfo;
        }

        private void ShowView(ChildView view) {
            _bordersToggle.value = view == ChildView.Borders;
            _previewToggle.value = view == ChildView.Preview;
            if (view == ChildView.Preview) {
                if (_container.Contains(_bordersView)) _container.Remove(_bordersView);   
                _container.Add(_sliceView);   
            } else {
                if (_container.Contains(_sliceView)) _container.Remove(_sliceView);
                _container.Add(_bordersView);
            }
        }

        private bool CanProceedWithCurrentChanges() {
            if (!hasUnsavedChanges) return true;
            var selectedOption = EditorUtility.DisplayDialogComplex("SpriteNineSlicer - Unsaved Changes Detected",
                "SpriteNineSlice.EditorWindow.SpriteNineSlicerWindow has unsaved changes.",
                "Save",
                "Discard",
                "Cancel");
            switch (selectedOption) {
                case 0:
                    SaveChanges();
                    return true;
                case 1:
                    DiscardChanges();
                    return true;
                default:
                    return false;
            }
        }
        
        private void UpdateSelection() {
            if (!CanProceedWithCurrentChanges())  return;
            _selectedSprites = SpriteNineSlicerContextMenu.GetSelectedSprites();
            _selectedIndex = 0;
            UpdateSelectedSprite();
        }

        private void UpdateSelectedSprite() {
            if (_selectedSpriteInfo != null) {
                _selectedSpriteInfo.BorderChange -= UpdateTrimmedSpriteInfo;
            }
            if (_selectedSprites.Count == 0) {
                _container.style.display = DisplayStyle.None;
                _root.SetEnabled(false);
                return;
            }
            _root.SetEnabled(true);
            _container.style.display = DisplayStyle.Flex;
            _prevButton.SetEnabled(_selectedIndex > 0);
            _currentSpriteIndex.text = _selectedSprites.Count > 0 ? $"{_selectedIndex + 1}/{_selectedSprites.Count}" : "-";
            _nextButton.SetEnabled(_selectedIndex < _selectedSprites.Count - 1);

            var selectedSprite = _selectedSprites[_selectedIndex];
            var imagePath = AssetDatabase.GetAssetPath(selectedSprite); 
            var importer = (TextureImporter) AssetImporter.GetAtPath(imagePath); 
            _trimAlphaToggle.SetEnabled(importer.spriteImportMode == SpriteImportMode.Single);
            _trimCenterToggle.SetEnabled(importer.spriteImportMode == SpriteImportMode.Single);
            
            _selectedSpriteInfo = new SpriteInfo(imagePath, selectedSprite.texture.width, selectedSprite.texture.height,
                    selectedSprite.border.ToInt4(), selectedSprite.texture.filterMode);
            _selectedSpriteInfo.BorderChange += UpdateTrimmedSpriteInfo;

            _bordersEditor.SetSpriteInfo(_selectedSpriteInfo);
            
            UpdateTrimmedSpriteInfo();
            hasUnsavedChanges = false;
        }
        
        private void OnTrimChange(ChangeEvent<bool> evt) {
            UpdateTrimmedSpriteInfo();
        }

        private void UpdateTrimmedSpriteInfo() {
            hasUnsavedChanges = true;
            _trimmedSpriteInfo = new SpriteInfo(_selectedSpriteInfo);
            if (_trimAlphaToggle.value) {
                _trimmedSpriteInfo.TrimAlpha();
            }
            if (_trimCenterToggle.value) {
                _trimmedSpriteInfo.TrimCenter();
            }
            _slicePreview.SetSpriteInfo(_trimmedSpriteInfo);
        }
    }
}