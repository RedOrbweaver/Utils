#if GODOT
using System;
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
using Expression = System.Linq.Expressions.Expression;

public static partial class Utils
{
    public static Vector3 GridTo3D(this Vector2I pos)
    {
        return GridPosTo3D(pos);
    }
    public static Vector3 GridPosTo3D(Vector2I pos)
    {
        return new Vector3(pos.x * Constants.TILE_SIDE_LENGTH, 0, -pos.y * Constants.TILE_SIDE_LENGTH);
    }
    public static Vector3 FadeVector(Vector3 v, float multip)
    {
        var d = v.Normalized() * multip;
        if(d.Length() > v.Length())
            return new Vector3(0,0,0);
        return v - d;
    }
    public static Vector3 Fade(this Vector3 v, float multip)
    {
        return FadeVector(v, multip);
    }
    public static Vector2 FadeVector(Vector2 v, float multip)
    {
        var d = v.Normalized() * multip;
        if(d.Length() > v.Length())
            return new Vector2(0,0);
        return v - d;
    }
    public static Vector2 Fade(this Vector2 v, float multip)
    {
        return FadeVector(v, multip);
    }
    public static void CallDeferred(Action action)
    {
        if(GlobalUtilsScript.GUS == null && Engine.EditorHint)
        {
            GD.PrintErr("Missed in-editor action!: " + action.ToString());
            return;
        }
        GlobalUtilsScript.GUS.QueueDeferred(action);
    }
    public static void Defer(Action action) => CallDeferred(action);
}
#endif
