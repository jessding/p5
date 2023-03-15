using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Python.Runtime;
using System.Dynamic;
using System.Linq;

public class PythonManager : MonoBehaviour
{
    public static PythonManager Instance { get; private set; }

    public static IReadOnlyDictionary<string, string> DEFAULT_MODULES = new Dictionary<string, string>() {
        { "np", "numpy" }
    };

    public static class Scope
    {
        public class Context
        {
            private PyModule pyScope;
            private static ExpandoObject internObj = new();
            private static IDictionary<string, object> internDict { get { return internObj;  } }

            public interface IPythonScoped
            {
                public void ScopedCode(Context ctx);
            }

            public class PythonCode : IPythonScoped
            {
                private string pythonCode;

                PythonCode(string code)
                {
                    pythonCode = code;
                }

                public void ScopedCode(Context ctx)
                {
                    ctx.pyScope.Exec(pythonCode);
                }
            }

            Context(PyModule pyScope)
            {
                this.pyScope = pyScope;
                LoadModules(DEFAULT_MODULES);
            }
            public Context LoadModule(string module, string asName = null) {
                asName ??= module;
                internDict[asName] = pyScope.Import(module);

                return this;
            }
            public Context LoadModules(string[] modules, string[] asNames = null) {
                asNames ??= modules;
                for (var i = 0; i < modules.Count(); i++) LoadModule(modules[i], asNames[i]);

                return this;
            }

            public Context LoadModules(IReadOnlyDictionary<string, string> modules)
            { 
                return LoadModules(modules.Keys.ToArray(), modules.Values.ToArray());
            }

            public Context SetVariable(string name, object value)
            {
                pyScope.Set(name, value);
                return this;
            }

            public Context SetVariables(string[] names, object[] values)
            {
                for (var i = 0; i < names.Count(); i++) SetVariable(names[i], values[i]);
                return this;
            }

            public Context SetVariables(IReadOnlyDictionary<string, string> variables)
            {
                return SetVariables(variables.Keys.ToArray(), variables.Values.ToArray());
            }
        }

        public static Dictionary<string, PyObject> Exec(Context.IPythonScoped pyCode, Dictionary<string, object> pyParams = null, List<string> pyReturns = null)
        {
            Dictionary<string, PyObject> toReturn = new();
            pyParams ??= new();
            pyReturns ??= new();

            using (Py.GIL())
            {
                using (var pyScope = Py.CreateScope())
                {

                }
            }

            return toReturn;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            // TODO: Add in platform-specific libraries for Mac and Linux.
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    Runtime.PythonDLL = Application.dataPath + "/StreamingAssets/embedded-python/python310.dll";
                    break;
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    Runtime.PythonDLL = Application.dataPath + "/StreamingAssets/embedded-python/libpython3.10.dylib";
                    break;
            }
            PythonEngine.PythonHome="/Users/jessding/miniconda3";
            
            PythonEngine.Initialize();
            Debug.Log("Python Initiated");
            using(Py.GIL()) // Wrap any code that accesses Python in this.
            {
                dynamic np = Py.Import("numpy");
                Debug.Log("pi: " + np.pi);
            }
        }
    }

    private void OnApplicationQuit()
    {

        if (PythonEngine.IsInitialized)
        {
            Debug.Log("Shutting Down Python Engine.");
            PythonEngine.Shutdown();
        }
        
    }
}
