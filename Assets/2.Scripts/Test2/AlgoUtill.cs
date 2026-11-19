using System;
using System.Collections.Generic;
/// <summary>
/// Reserve System
/// </summary>
/// <typeparam name="T"> </typeparam>
public class ReserveSystem<T>
{
    Dictionary<int, T> Content = new();
    Queue<int> ITicket = new();
    LinkedList<int> Reserve = new();
    Dictionary<int, LinkedListNode<int>> Nodes = new();

    public ReserveSystem()
    {
        ITicket.Enqueue(0);
    }

    public int Count => Content.Count;

    public bool First(out T value)
    {
        if(Reserve.First == null) { value = default; return false; }
        value = Content[Reserve.First.Value];
        return true;
    }

    public int Register(T Object)
    {
        int NextT;
        if (!ITicket.TryDequeue(out NextT)) NextT = Content.Count;
        Content[NextT] = Object;
        Nodes.Add(NextT, Reserve.AddLast(NextT));
        return NextT;
    }
    public bool CancleInteract(int _ticket)
    {
        if (!Content.Remove(_ticket)) return false;
        if (Nodes.Remove(_ticket, out var node)) Reserve.Remove(node);
        ITicket.Enqueue(_ticket);
        return true;
    }

    /// <param name="Reuse"> 0 : Remove , 1 : Reuse(First), 2 : Reuse(Last)</param>
    /// <returns>Reserved Object</returns>
    public T Use(int Reuse = 0)
    {
        if (Reserve.First == null) return default(T);
        int curTicket = Reserve.First.Value;
        var some = Content[curTicket];
        switch (Reuse)
        {
            case 0: CancleInteract(curTicket); break;
            case 1: break;
            case 2: var node = Reserve.First; Reserve.RemoveFirst(); Reserve.AddLast(node); break;
        }
        return some;
    }

    /// <summary>
    /// Use For Default != null;
    /// </summary>
    /// <param name="Reuse"> 0 : Remove , 1 : Reuse(First), 2 : Reuse(Last)</param>
    public bool TryUse(out T some, int Reuse = 0)
    {
        if (Reserve.First == null) { some = default; return false; }
        int curTicket = Reserve.First.Value;
        some = Content[curTicket];
        switch (Reuse)
        {
            case 0: CancleInteract(curTicket); break;
            case 1: break;
            case 2: var node = Reserve.First; Reserve.RemoveFirst(); Reserve.AddLast(node); break;
        }
        return true;
    }
}

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

