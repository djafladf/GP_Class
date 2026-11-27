using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public static class EventUtil
{
    static void AddButtonEvent(EventTrigger eventTrigger, EventTriggerType Type, Action<PointerEventData> Event)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = Type;
        entry.callback.AddListener((data) => { Event((PointerEventData)data); });
        eventTrigger.triggers.Add(entry);
    }

    public static void ButtonInit(EventTrigger eventTrigger, Action<PointerEventData> OnPointer, Action<PointerEventData> OutPointer,
        Action<PointerEventData> ClickPointer)
    {
        //data.pointerId : left -> -1, right -> -2, Wheel -> -3;
        if (OnPointer != null) AddButtonEvent(eventTrigger, EventTriggerType.PointerEnter, OnPointer);

        if (OutPointer != null) AddButtonEvent(eventTrigger, EventTriggerType.PointerExit, OutPointer);

        if (ClickPointer != null) AddButtonEvent(eventTrigger, EventTriggerType.PointerClick, ClickPointer);
    }
}

public static class EventUtil<T>
{
    static void AddEvent(EventTrigger eventTrigger, EventTriggerType Type, Action<PointerEventData, T> Event, T param)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = Type;
        entry.callback.AddListener((data) => { Event((PointerEventData)data, param); });
        eventTrigger.triggers.Add(entry);
    }

    public static void ButtonInit(EventTrigger eventTrigger, Action<PointerEventData, T> OnPointer, Action<PointerEventData, T> OutPointer, 
        Action<PointerEventData, T> ClickPointer, T i)
    {
        //data.pointerId : left -> -1, right -> -2, Wheel -> -3;
        if (OnPointer != null) AddEvent(eventTrigger, EventTriggerType.PointerEnter, OnPointer, i);

        if (OutPointer != null) AddEvent(eventTrigger, EventTriggerType.PointerExit, OutPointer, i);

        if (ClickPointer != null) AddEvent(eventTrigger, EventTriggerType.PointerClick, ClickPointer, i);
    }
}

public class LinkedSet<T>
{
    LinkedList<T> value = new();
    Dictionary<T, LinkedListNode<T>> Nodes = new();

    public void Add(T item)
    {
        Nodes.Add(item,value.AddLast(item));
    }
    public bool Remove(T item)
    {
        if(Nodes.Remove(item,out var t)) { value.Remove(t); return true; }
        return false;
    }

    public bool First(out T var) 
    { 
        if(value.First == null) { var = default; return false; }
        var = value.First.Value; return true;
    }
    public bool Last(out T var)
    {
        if (value.First == null) { var = default; return false; }
        var = value.Last.Value; return true;
    }
}

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

