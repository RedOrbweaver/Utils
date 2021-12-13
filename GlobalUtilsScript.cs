using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using ExtraMath;
using Godot;
using Newtonsoft.Json;
using static System.Math;
using static Globals;
using static Utils;
using Expression = System.Linq.Expressions.Expression;

[Tool]
public class GlobalUtilsScript : Node
{
    //static GlobalUtilsScript _GUS;

    ConcurrentQueue<Action> _toExec = new ConcurrentQueue<Action>();
    void DequeDeferred()
    {
        System.Action action;
        Assert(_toExec.TryDequeue(out action), "Error: queue desynchronization");
        action();
    }
    public void QueueDeferred(System.Action action)
    {
        Assert(action != null);
        _toExec.Enqueue(action);
        CallDeferred(nameof(DequeDeferred));
    }
    public static GlobalUtilsScript GUS;
    // {
    //     get
    //     {
    //         if(_GUS == null)
    //         {
    //             if(Engine.EditorHint)
    //             {
    //                 _GUS = new GlobalUtilsScript();
    //             }
    //             else 
    //                 throw new NullReferenceException("GLOBAL UTILITY SCRIPT NOT ATTACHED! Did you forget to add it before using any of its features?");
    //         }
    //         return _GUS;
    //     }
    //     protected set
    //     {
    //         _GUS = value;
    //     }
    // }
    public GlobalUtilsScript()
    {
        GUS = this;
        // Assert(_GUS == null || Engine.EditorHint);
        // _GUS = this;
    }
}