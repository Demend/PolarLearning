﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polar.Common
{
    /// <summary>
    /// Буферизует элементы и порциями их процессирует
    /// </summary>
    /// <typeparam name="T">тип элементов для буферизации</typeparam>
    public class BufferredProcessing<T>
    {
        public delegate void ProcessElements(IEnumerable<T> elements);
        int portion;
        T[] elements;
        int position;
        private ProcessElements handler;
        public BufferredProcessing(int portion, ProcessElements handler)
        {
            this.portion = portion;
            this.handler = handler;
            this.elements = new T[portion];
            this.position = 0;
        }
        public void Add(T el)
        {
            elements[position++] = el;
            if (position == portion) Flush();
        }
        public void Flush()
        {
            if (position == 0) return;
            int pos = position;
            position = 0;
            handler(elements.Take(pos));
        }
        public T[] GetElements() 
        { 
            if (position == 0) return new T[0]; 
            var els = elements.Take(position).ToArray();
            position = 0;
            return els;
        }
    }
}
