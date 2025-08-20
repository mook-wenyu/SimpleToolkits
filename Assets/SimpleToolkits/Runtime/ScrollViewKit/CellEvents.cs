using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleToolkits
{
    /// <summary>
    /// 指针事件转发组件：解耦 MonoBehaviour 回调与 Binder。
    /// </summary>
    public sealed class CellEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private Action _onClick;
        private Action _onPointerDown;
        private Action _onPointerUp;

        public void Setup(Action onClick, Action onPointerDown, Action onPointerUp)
        {
            _onClick = onClick;
            _onPointerDown = onPointerDown;
            _onPointerUp = onPointerUp;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _onPointerDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _onPointerUp?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke();
        }
    }
}
