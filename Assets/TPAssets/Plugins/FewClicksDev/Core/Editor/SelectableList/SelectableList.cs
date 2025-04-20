namespace FewClicksDev.Core.SelectableList
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    using static FewClicksDev.Core.EditorDrawer;

    [System.Serializable]
    public class SelectableList<T> where T : SelectableItem
    {
        public Vector2 ScrollPosition { get; set; }
        public float CollapsedItemHeight { get; private set; } = DEFAULT_LINE_HEIGHT;

        [SerializeField] protected List<T> itemsInTheList = new();
        [SerializeField] protected T firstSelectedItem = null;

        public List<T> Items => itemsInTheList;
        public int Count => itemsInTheList.Count;

        public void Init(List<T> _items, float _collapsedItemHeight)
        {
            Destroy();
            itemsInTheList = _items.ToList(); //Make a copy

            foreach (var _item in itemsInTheList)
            {
                _item.OnExpandStateChanged += RefreshHeights;
            }

            CollapsedItemHeight = _collapsedItemHeight;
            RefreshHeights(null);
        }

        public void Destroy()
        {
            foreach (var _item in itemsInTheList)
            {
                _item.OnExpandStateChanged -= RefreshHeights;
            }

            itemsInTheList.Clear();
        }

        public void HandleSelection(T _item, Event _event)
        {
            if (_event.alt)
            {
                _item.ToggleExpandState();
            }

            if (_event.shift == false && _event.control == false)
            {
                UnselectAll();
                _item.ToggleSelectedState();
                firstSelectedItem = _item;
            }
            else if (_event.control)
            {
                firstSelectedItem = _item;
                _item.ToggleSelectedState();
            }
            else if (firstSelectedItem != null && _event.shift)
            {
                SelectInRange(itemsInTheList.IndexOf(firstSelectedItem), itemsInTheList.IndexOf(_item));
            }
        }

        public void ClearFirstSelected()
        {
            firstSelectedItem = null;
        }

        public void SelectAll()
        {
            foreach (var _item in itemsInTheList)
            {
                _item.IsSelected = true;
            }
        }

        public void UnselectAll()
        {
            foreach (var _item in itemsInTheList)
            {
                _item.IsSelected = false;
            }
        }

        public void ExpandAll()
        {
            foreach (var _item in itemsInTheList)
            {
                _item.IsExpanded = true;
            }
        }

        public void CollapseAll()
        {
            foreach (var _item in itemsInTheList)
            {
                _item.IsExpanded = false;
            }
        }

        public void SelectInRange(int _start, int _end)
        {
            if (_start == _end)
            {
                return;
            }

            int _startIndex = Mathf.Min(_start, _end);
            int _endIndex = Mathf.Max(_start, _end);

            for (int _index = _startIndex; _index <= _endIndex; _index++)
            {
                itemsInTheList[_index].IsSelected = true;
            }
        }

        public List<T> GetSelectedItems()
        {
            List<T> _selectedGroups = new List<T>();

            foreach (var _item in itemsInTheList)
            {
                if (_item.IsSelected)
                {
                    _selectedGroups.Add(_item);
                }
            }

            return _selectedGroups;
        }

        public bool IsVisible(T _item, float _visibleAreaHeight)
        {
            float _itemHeight = _item.StartPositionAndHeight.y;

            bool _itemIsBelowVisibleArea = _item.StartPositionAndHeight.x > ScrollPosition.y + _visibleAreaHeight + (2f * _itemHeight);
            bool _itemIsAboveVisibleArea = _item.StartPositionAndHeight.x + _itemHeight < ScrollPosition.y;

            return _itemIsBelowVisibleArea == false && _itemIsAboveVisibleArea == false;
        }

        public void RefreshHeights(SelectableItem _item)
        {
            if (itemsInTheList.IsNullOrEmpty())
            {
                return;
            }

            int _indexOfItem = _item == null ? 0 : Mathf.Clamp(itemsInTheList.IndexOf(_item as T) - 1, 0, itemsInTheList.Count - 1);

            if (_indexOfItem == -1 || _indexOfItem == itemsInTheList.Count - 1)
            {
                return;
            }

            float _currentY = itemsInTheList[_indexOfItem].StartPositionAndHeight.x;

            for (int i = _indexOfItem; i < itemsInTheList.Count; i++)
            {
                itemsInTheList[i].RecalculateHeightAndStartPosition(_currentY);
                _currentY += itemsInTheList[i].StartPositionAndHeight.y + 2f;
            }
        }
    }
}
