// ObjectPool.cs — generic object pool for MonoBehaviour components.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintGame
{
    public class ObjectPool<T> where T : Component
    {
        private readonly Stack<T>   _free  = new Stack<T>();
        private readonly List<T>    _all   = new List<T>();
        private readonly T          _prefab;
        private readonly Transform  _parent;
        private readonly Action<T>  _onGet;
        private readonly Action<T>  _onReturn;

        public ObjectPool(T prefab, Transform parent, int warmSize = 0,
                          Action<T> onGet = null, Action<T> onReturn = null)
        {
            _prefab   = prefab;
            _parent   = parent;
            _onGet    = onGet;
            _onReturn = onReturn;

            for (int i = 0; i < warmSize; i++)
                Return(Create());
        }

        public T Get()
        {
            T obj = _free.Count > 0 ? _free.Pop() : Create();
            obj.gameObject.SetActive(true);
            _onGet?.Invoke(obj);
            return obj;
        }

        public void Return(T obj)
        {
            obj.gameObject.SetActive(false);
            _onReturn?.Invoke(obj);
            _free.Push(obj);
        }

        private T Create()
        {
            var go = UnityEngine.Object.Instantiate(_prefab.gameObject, _parent);
            var comp = go.GetComponent<T>();
            _all.Add(comp);
            go.SetActive(false);
            return comp;
        }

        public void ReturnAll()
        {
            foreach (var obj in _all)
                if (obj.gameObject.activeSelf) Return(obj);
        }
    }
}
