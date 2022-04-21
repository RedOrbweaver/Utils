//#define DEBUG_EXTENDED_PROPERTIES
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
#if GODOT 
using ExtraMath;
using Godot;
#endif
using Newtonsoft.Json;
using static System.Math;
using Expression = System.Linq.Expressions.Expression;

public static partial class Utils
{
#if GODOT
    public static void DestroyNode(Node node)
    {
        if (node.GetParent() is Node parent)
            parent.RemoveChild(node);
        node.QueueFree();
    }
    public static void Destroy(this Node node)
    {
        DestroyNode(node);
    }
    public static string EnsurePathExists(string path)
    {
        Assert(!FileExists(path), "This function is meant for directories, not files");
        if (DirectoryExists(path))
            return "";
        string dif = "";
        List<string> parts = new List<string>();
        while (!DirectoryExists(path))
        {
            string subpath = "";
            if (path.Last() == '/')
                path = path.Remove(path.Length - 1);
            while (path.Last() != '/')
            {
                subpath += path.Last();
                path = path.Remove(path.Length - 1);
            }
            parts.Add(new string(subpath.Reverse().ToArray()));
        }
        var dm = new Directory();
        parts.Reverse();
        foreach (var it in parts)
        {
            path = ConcatPaths(path, it);
            dm.MakeDir(path);
            dif = ConcatPaths(dif, it);
        }
        return dif;
    }
    public static T DeserializeFromFile<T>(string path, bool returnnullonfail = false) where T : class
    {
        Assert(FileExists(path));
        T o = null;
        try
        {
            o = JsonConvert.DeserializeObject<T>(ReadFile(path));
        }
        catch (Exception)
        {
            if (!returnnullonfail)
                throw;
            return null;
        }

        if (!returnnullonfail)
            Assert(o != null);

        return o;
    }
    public static void SerializeToFile<T>(string path, T o) where T : class
    {
        Assert(o != null);

        var data = JsonConvert.SerializeObject(o);
        WriteFile(path, data);
    }
    public static bool FileExists(string path)
    {
        return new Directory().FileExists(path);
    }
    public static string ReadFile(string path)
    {
        Assert(FileExists(path));
        File fm = new File();
        Assert(fm.Open(path, File.ModeFlags.Read));
        string dt = fm.GetAsText();
        fm.Close();
        return dt;
    }
    public static void WriteFile(string path, string data)
    {
        Assert(data != null);

        File fm = new File();
        Assert(fm.Open(path, File.ModeFlags.Write));
        fm.StoreString(data);
        fm.Close();
    }
    public static bool DirectoryExists(string path)
    {
        return new Directory().DirExists(path);
    }
    public static List<T> GetChildrenRecrusively<T>(this Node root, bool restrichtomatchingparents = false)
    {
        List<T> ret = new List<T>(8);

        void SearchRecursively(Node n)
        {
            foreach (var it in n.GetChildren())
            {
                Assert(it is Node);
                if (it is T)
                {
                    ret.Add((T)it);
                }
                if (!restrichtomatchingparents || it is T)
                {
                    SearchRecursively((Node)it);
                }
            }
        }
        SearchRecursively(root);

        return ret;
    }
    [Conditional("DEBUG")]
    public static void Assert(Error b, string msg)
    {
        if (b == Error.Ok)
            return;
        GD.PrintErr($"ASSERTION FAILURE (Error.{b.ToString()}):");
        GD.PrintErr(msg);
        //Debugger.Break();
        throw new AssertionFailureException(msg);
    }
    [Conditional("DEBUG")]
    public static void Assert(Error b)
    {
        Assert(b, $"An unspecified assertion failure, error code {b.ToString()}, has been triggered!");
    }
    public static List<string> ListDirectoryFilesRecursively(string path, Func<string, bool> filter)
    {
        List<string> ret = new List<string>();
        Directory dm = new Directory();
        void ListRecursively(string path)
        {
            foreach (var it in ListDirectoryContents(path))
            {
                if (dm.DirExists(it))
                {
                    ListRecursively(it);
                }
                else
                {
                    Assert(dm.FileExists(it));
                    if (filter(it))
                    {
                        ret.Add(it);
                    }
                }
            }
        }
        ListRecursively(path);
        return ret;
    }
    public static List<string> ListDirectoryFilesRecursively(string path)
    {
        return ListDirectoryFilesRecursively(path, s => true);
    }
    public static List<string> ListDirectoryContents(string path, Func<string, bool> filter, bool relativepaths = false, bool skiphidden = true)
    {
        List<string> ret = new List<string>(8);
        Directory dm = new Directory();
        Assert(dm.Open(path) == Error.Ok);
        dm.ListDirBegin(true, skiphidden);
        while (true)
        {
            string s = dm.GetNext();
            if (s == "")
                break;
            if (filter(ConcatPaths(path, s)))
                ret.Add((relativepaths) ? s : ConcatPaths(path, s));
        }
        dm.ListDirEnd();
        return ret;
    }
    public static List<string> ListDirectoryContents(string path, bool relativepaths = false, bool skiphidden = true)
    {
        return ListDirectoryContents(path, s => true, relativepaths, skiphidden);
    }
    public static List<string> ListDirectoryFiles(string path, bool relativepaths = false, bool skiphidden = true)
    {
        return ListDirectoryContents(path, s =>
        {
            return new Directory().FileExists(s);
        }, relativepaths, skiphidden);
    }
    public static List<string> ListDirectorySubDirs(string path, bool relativepaths = false, bool skiphidden = true)
    {
        return ListDirectoryContents(path, s =>
        {
            return new Directory().DirExists(s);
        }, relativepaths, skiphidden);
    }

    public static T FindChild<T>(this Node parent)
    {
        foreach (var it in parent.GetChildren())
        {
            if (it.GetType() == typeof(T) || it is T)
                return (T)it;
        }
        return default(T);
    }
    public static List<T> FindChildren<T>(this Node parent, bool recursive = false) 
    {   
        var ret = new List<T>();
        if(recursive)
            return FindChildrenRecursively<T>(parent);
        foreach(var it in parent.GetChildren())
        {
            if(it is T || it.GetType() == typeof(T))
                ret.Add((T)it);
        }
        return ret;
    }
    public static List<T> FindChildrenRecursively<T>(this Node parent) 
    {
        return GetChildrenRecrusively<T>(parent);
    }
    public static T GetNodeSafe<T>(this Node root, string path, string not_found_message) where T : Node
    {
        Node node = root.GetNodeOrNull(path);
        Assert<Exception>(node != null, not_found_message);
        Assert(node is T, $"Node {path} was not of type {typeof(T).Name}");
        return (T)node;
    }
    public static T GetNodeSafe<T>(this Node root, string path) where T : Node
    {
        return root.GetNodeSafe<T>(path, $"Could not find node: {path}");
    }
    public class GDProperty
    {
        public string Name {get; protected set;}
        public Godot.Variant.Type Type {get; protected set;}
        public Godot.PropertyHint Hint {get; protected set;}
        public string HintString {get; protected set;} = "";
        
        public GDProperty(string name, Godot.Variant.Type type, Godot.PropertyHint hint, string hintstring = "")
        {
            Assert(name != null && hintstring != null);
            this.Name = name;
            this.Type = type;
            this.Hint = hint;
            this.HintString = hintstring;
        }
        public GDProperty(Godot.Collections.Dictionary prop)
        {
            Assert(prop.Contains("name"), "Not a valid property! Missing 'name'!");
            Assert(prop.Contains("type"), "Not a valid property! Missing 'type'!");
            Assert(prop.Contains("hint"), "Not a valid property! Missing 'hint'!");
            this.Name = (string)prop["name"];
            this.Type = (Godot.Variant.Type)prop["type"];
            this.Hint = (Godot.PropertyHint)prop["hint"];
            if(prop.Contains("hint_string"))
                this.HintString = (string)prop["hint_string"];
            else
                this.HintString = "";
        }
        public static GDProperty FromDictionary(Godot.Collections.Dictionary dict)
        {
            return new GDProperty(dict);
        }
        public static Godot.Collections.Dictionary ToDictionary(GDProperty prop)
        {
            var ret = new Godot.Collections.Dictionary();
            ret.Add("name", prop.Name);
            ret.Add("type", prop.Type);
            ret.Add("hint", prop.Hint);
            ret.Add("hint_string", prop.HintString);
            return ret;
        }
        public Godot.Collections.Dictionary ToDictionary()
        {
            return ToDictionary(this);
        }
        public static implicit operator Godot.Collections.Dictionary(GDProperty prop)
        {
            return prop.ToDictionary();
        }
    }
    public interface IExtendedProperties
    {
        Dictionary<string, (Func<object> getter, Action<object> setter, Func<bool> predicate, GDProperty prop)> ExtendedProperties {get; set;}
        List<string> ExtendedPropertiesOrdered {get; set;}
        void LoadProperties();
        void EnsureNotNull()
        {
            if(ExtendedProperties == null)
                ExtendedProperties = new Dictionary<string, (Func<object> getter, Action<object> setter, Func<bool> predicate, GDProperty prop)>();
            if(ExtendedPropertiesOrdered == null)
                ExtendedPropertiesOrdered = new List<string>();
        }
        void ReloadProperties()
        {
            #if DEBUG_EXTENDED_PROPERTIES
            GD.Print("Reloading");
            #endif
            EnsureNotNull();
            ClearProperties();
            LoadProperties();
        }
        bool OnGet(string property, out object ret)
        {
            if(!IsValid())
                ReloadProperties();
            Assert(IsValid());
            if(ExtendedProperties.ContainsKey(property))
            {
                ret = ExtendedProperties[property].getter();
                #if DEBUG_EXTENDED_PROPERTIES
                GD.Print("GET "+ property + " = " + ((ret == null) ? "NULL" : ret.ToString()));
                #endif
                return true;
            }
            ret = null;
            
            #if DEBUG_EXTENDED_PROPERTIES
            GD.Print("GET "+ property + " = NULL");
            #endif
            return false;
        }
        bool OnSet(string property, object value)
        {
            if(!IsValid() || !ExtendedProperties.ContainsKey(property))
                ReloadProperties();
            Assert(IsValid());
            #if DEBUG_EXTENDED_PROPERTIES
            GD.Print("SET "+ property + " = " + ((value == null) ? "NULL" : value.ToString()));
            #endif
            if(ExtendedProperties.ContainsKey(property))
            {
                ExtendedProperties[property].setter(value);
                return true;
            }
            return false;
        }
        bool IsValid()
        {
            EnsureNotNull();
            return ExtendedProperties.Count == ExtendedPropertiesOrdered.Count &&
                   ExtendedPropertiesOrdered.All(it => ExtendedProperties.ContainsKey(it)) &&
                   ExtendedProperties.All(it => ExtendedPropertiesOrdered.Contains(it.Key));
        }
        Godot.Collections.Array OnGetPropertyList()
        {
            if(!IsValid())
                ReloadProperties();
            Assert(IsValid());
            var ret = new Godot.Collections.Array();
            foreach(var k in ExtendedPropertiesOrdered)
            {
                if(ExtendedProperties[k].predicate())
                    ret.Add(ExtendedProperties[k].prop.ToDictionary());
            }
            return ret;
        }
        void AddProperty(GDProperty prop, Func<object> getter, Action<object> setter, Func<bool> predicate = null)
        {
            EnsureNotNull();
            if(predicate == null)
                predicate = () => true;
            Assert(!ExtendedProperties.ContainsKey(prop.Name), "Property already present!");
            ExtendedProperties.Add(prop.Name, (getter, setter, predicate, prop));
            ExtendedPropertiesOrdered.Add(prop.Name);
        }
        void RemoveProperty(string name, bool ignoremissing = false)
        {
            EnsureNotNull();
            if(!ExtendedProperties.ContainsKey(name))
            {
                Assert(ignoremissing, $"Extended property '{name}' was not found!");
                if(ExtendedPropertiesOrdered.Contains(name))
                    ExtendedPropertiesOrdered.Remove(name);
                return;
            }
            ExtendedPropertiesOrdered.Remove(name);
            ExtendedProperties.Remove(name);
        }
        void ClearProperties()
        {
            EnsureNotNull();
            ExtendedPropertiesOrdered.Clear();
            ExtendedProperties.Clear();
        }
    }
    public static string GetObjectName(this Godot.Object obj)
    {
        if(obj is Node node)
            return node.Name;
        return obj.GetType().Name;
    }
    public static string GetObjectTypeAndName(this Godot.Object obj)
    {
        if(obj is Node node)
            return $"{node.GetType().Name} ({node.Name})";
        return obj.GetType().Name;
    }
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class LoadFrom : System.Attribute
    {
        static int _registered = 0;
        public string Path;
        public static List<(string path, Action<Resource> onend)> _toLoad;
        public static List<(string path, Action<Resource> onend)> ToLoad
        {
            get
            {
                if(_toLoad == null || _toLoad.Count != _registered)
                {
                    _toLoad = new List<(string path, Action<Resource> onend)>();
                    var props = FindAllPropertiesWithAttribute<LoadFrom>();
                    foreach(var it in props)
                    {
                        var attr = it.GetCustomAttribute<LoadFrom>();
                        Assert(attr != null);
                        var path = attr.Path;
                        var setter = it.GetSetMethod(true);
                        Assert(setter.IsStatic, "All LoadFrom subscribers should be static!");
                        Action<Resource> act = res => setter.Invoke(null, new object[]{res});
                        _toLoad.Add((path, act));
                    }
                    _registered = _toLoad.Count;
                }
                return _toLoad;
            }
        }
        public LoadFrom(string path)
        {
            Assert(FileExists(path), "could not find resource: " + path);
            this.Path = path;
            _registered++;
        }
    }
#endif
}
