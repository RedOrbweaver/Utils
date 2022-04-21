#if GODOT
using System.Data;
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
    public delegate void SignalHandler(params object[] args);
    public class SignalHandlerObject : Godot.Object
    {
        public SignalHandler handler;
        public void Grabber(params object[] args)
        {
            handler(args.Cast<object>().ToArray<object>());
        }
        public SignalHandlerObject(SignalHandler handler)
        {
            this.handler = handler;
        }
        
    }
    public class ConnectionHandle
    {
        protected Godot.Object _obj;
        protected string _signal;
        protected SignalHandlerObject _handler;
        public Godot.Object SignalEmitter {get => _obj;}
        public bool Connected {get; protected set;} = true;
        public void Disconnect(bool ignoreerrors = false)
        {
            if(!ignoreerrors)
                Assert(Connected, $"Attempted to disconnect the signal {_signal} of object {_obj.GetObjectTypeAndName()} multiple times" );
            _obj.Disconnect(_signal, _handler, ignoreerrors, ignoreerrors);
            Connected = false;
        }
        public ConnectionHandle(Godot.Object obj, string signal, SignalHandlerObject handler)
        {
            this._obj = obj;
            this._signal = signal;
            this._obj = handler;
        }
    }
    public static ConnectionHandle Connect(this Godot.Object obj, string signal, SignalHandler func, bool autodisconnect = true, bool ignoreerrors = false)
    {
        if(!ignoreerrors)
            Assert(obj.HasSignal(signal));
        if(autodisconnect)
            obj.Disconnect(signal, func, true, true);
        var sho = new SignalHandlerObject(func);
        Error err = obj.Connect(signal, sho, nameof(sho.Grabber));
        if(!ignoreerrors)
            Assert(err, $"Failed to connect signal {signal} of object {obj.GetObjectTypeAndName()}");
        return new ConnectionHandle(obj, signal, sho);
    }
    public static void Disconnect(this Godot.Object obj, string signal, SignalHandlerObject handler, bool ignorenotconnected = true, bool ignoreerrors = false)
    {
        if(!ignoreerrors)
            Assert(obj.HasSignal(signal), $"object {obj.GetObjectTypeAndName()} does not name a signal {signal}");
        var conns = obj.GetSignalConnectionList(signal);
        foreach(Godot.Collections.Dictionary it in conns)
        {
            if(it["target"] is SignalHandlerObject sho)
            {
                if(sho == handler)
                {
                    obj.Disconnect(signal, sho, nameof(sho.Grabber));
                    return;
                }
            }
        }
        if(!ignorenotconnected)
            Assert(false, "Did not find the specified connection for " + signal);
    }
    public static void Disconnect(this Godot.Object obj, string signal, SignalHandler func, bool ignorenotconnected = true, bool ignoreerrors = false)
    {
        if(!ignoreerrors)
            Assert(obj.HasSignal(signal), $"object {obj.GetObjectTypeAndName()} does not name a signal {signal}");
        var conns = obj.GetSignalConnectionList(signal);
        foreach(Godot.Collections.Dictionary it in conns)
        {
            if(it["target"] is SignalHandlerObject sho)
            {
                if(sho.handler == func || sho.handler.Method == func.Method)
                {
                    obj.Disconnect(signal, sho, nameof(sho.Grabber));
                }
            }
        }
        if(!ignorenotconnected)
            Assert(false, "Did not find the specified connection for " + signal);
    }
    public static void Disconnect(this Godot.Object obj, ConnectionHandle handle, bool ignoreerrors = false)
    {
        if(!ignoreerrors)
            Assert(handle.SignalEmitter == obj, $@"The signal emitter ({handle.SignalEmitter.GetObjectTypeAndName()})
                did not match the current object ({handle.SignalEmitter.GetObjectTypeAndName()})");
        handle.Disconnect(ignoreerrors);
    }
    public static int DisconnectAll(this Godot.Object obj, string signal)
    {
        var conns = obj.GetSignalConnectionList(signal);
        foreach(Godot.Collections.Dictionary it in conns)
        {
            obj.Disconnect((string)it["signal"], (Godot.Object)it["target"], (string)it["method"]);
        }
        return conns.Count;
    }
    static T GetElementOrDefault<T>(T[] a, int i)
    {
        if(a.Length <= i)
            return default(T);
        return a[i];
    }
    static T GetElementOrDefault<T>(object[] a, int i)
    {
        if(a.Length <= i)
            return default(T);
        var v = a[i];
        if(v == null)
            return default(T);
        return (T)v;
    }
    public static ConnectionHandle OnButtonPressed(this Button obj, Action func)
    {
        return Connect(obj, "pressed", (args) => func());
    }
    public static ConnectionHandle OnToggled(this CheckBox obj, Action<bool> func)
    {
        return Connect(obj, "toggled", (args) => func(GetElementOrDefault<bool>(args, 0)));
    }
    public static ConnectionHandle OnValueChanged(this Slider obj, Action<float> func)
    {
        return Connect(obj, "value_changed", (args) => func(GetElementOrDefault<float>(args, 0)));
    }
    public static ConnectionHandle OnMouseEntered(this Area obj, Action func)
    {
        return Connect(obj, "mouse_entered", (args) => func());
    }
    public static ConnectionHandle OnMouseExited(this Area obj, Action func)
    {
        return Connect(obj, "mouse_exited", (args) => func());
    }

    public static ConnectionHandle OnFileSelected(this FileDialog dialog, Action<string> func)
    {
        return Connect(dialog, "file_selected", (args) => func(GetElementOrDefault<string>(args, 0)));
    }
    public static ConnectionHandle OnDirectorySelected(this FileDialog dialog, Action<string> func)
    {
        return Connect(dialog, "dir_selected", (args) => func(GetElementOrDefault<string>(args, 0)));
    }
}

#endif