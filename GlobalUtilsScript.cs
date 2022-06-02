#if GODOT
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
    Node _currentSceneRoot;
    public void SetMainScene(Node node)
    {
        var root = GetTree().Root;
        if(_currentSceneRoot != null)
        {
            Assert(_currentSceneRoot.GetParent() == root);
            DestroyNode(_currentSceneRoot);
        }
        _currentSceneRoot = node;
        root.AddChild(_currentSceneRoot);
    }
    public static GlobalUtilsScript GUS;
    public GlobalUtilsScript()
    {
        GUS = this;
    }
}
#endif