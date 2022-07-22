using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Python.Runtime;
using System.Linq;

namespace PyScripting.ScriptingEngine
{
    public class PyInterpreter
    {
        private PyStream outStream;
        PyModule ns;
        public PyInterpreter(PyStream outStream)
        {
            Runtime.PythonDLL = @"python310.dll";
            this.outStream = outStream;
            //var initsrc = InitialiserCode.GetInitialiserSource();
            var initScript = File.ReadAllText("startup.py");

            using (Py.GIL())
            {
                PyObject initcode = PythonEngine.Compile(initScript);
                PyModule smbs = new PyModule("smbs");
                smbs.Execute(initcode);
                PyModule.SysModules.SetItem("smbs", smbs);

                ns = Py.CreateScope("ns");
                //dynamic sys = Py.Import("sys");
            }
        }

        public void RunSource(string src)
        {
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                sys.stdout = outStream;
                ns.Set("sys", sys);

                PyObject code = PythonEngine.Compile(src);
                ns.Execute(code);
            }
        }
    }
}
