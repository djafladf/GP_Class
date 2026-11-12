using System;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;

public class PriorityQueue<T>
{
    List<T> ele = new List<T>();
    public int Count => ele.Count;
    public bool isEmpty => ele.Count == 0;
    public void Clear() => ele.Clear();
    
    Func<T,T, bool> Compare;
    

    public PriorityQueue(Func<T,T, bool> compare)
    {
        Compare = compare ?? throw new ArgumentNullException(nameof(compare));
    }

    public void Remove(T item)
    {
        if (ele.Count == 0) throw new InvalidOperationException("empty");
        ele.Remove(item);
    } 
    public T top()
    {
        if (ele.Count == 0) throw new InvalidOperationException("empty");
        return ele[0];
    }

    public void push(T x)
    {
        ele.Add(x);
        int i = ele.Count - 1;
        while (true)
        {
            int p = (i - 1) >> 1;
            if (i <= 0 || !Compare(ele[i], ele[p])) break;
            (ele[i], ele[p]) = (ele[p], ele[i]);
            i = p;
        }
    }
    public T pop()
    {
        if (ele.Count == 0) throw new InvalidOperationException("empty");
        var top = ele[0];
        int lr = ele.Count - 1;
        ele[0] = ele[lr]; ele.RemoveAt(lr);
        if (ele.Count > 0)
        {
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < ele.Count && Compare(ele[l], ele[m])) m = l;
                if (r < ele.Count && Compare(ele[r], ele[m])) m = r;
                if (m == i) break;
                (ele[i], ele[m]) = (ele[m], ele[i]); i = m;
            }
        }
        return top;
    }
}


public class PriorityQueue<T1,T2>
{
    List<(T1,T2)> ele = new List<(T1,T2)>();
    public int Count => ele.Count;
    public bool isEmpty => ele.Count == 0;
    public void Clear() => ele.Clear();
    Func<(T1, T2), (T1, T2), bool> Compare;

    public PriorityQueue(Func<(T1, T2), (T1, T2), bool> compare)
    {
        Compare = compare ?? throw new ArgumentNullException(nameof(compare));
    }

    public (T1,T2) top()
    {
        if (ele.Count == 0) throw new InvalidOperationException("empty");
        return ele[0];
    }

    public void push((T1,T2) x)
    {
        ele.Add(x);
        int i = ele.Count - 1;
        while (true)
        {
            int p = (i - 1) >> 1;
            if (i <= 0 || !Compare(ele[i], ele[p])) break;
            (ele[i], ele[p]) = (ele[p], ele[i]);
            i = p;
        }
    }
    public (T1,T2) pop()
    {
        if (ele.Count == 0) throw new InvalidOperationException("empty");
        var top = ele[0];
        int lr = ele.Count - 1;
        ele[0] = ele[lr]; ele.RemoveAt(lr);
        if (ele.Count > 0)
        {
            int i = 0;
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, m = i;
                if (l < ele.Count && Compare(ele[l], ele[m])) m = l;
                if (r < ele.Count && Compare(ele[r], ele[m])) m = r;
                if (m == i) break;
                (ele[i], ele[m]) = (ele[m], ele[i]); i = m;
            }
        }
        return top;
    }
}

