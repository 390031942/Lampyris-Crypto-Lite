namespace Lampyris.CSharp.Common;

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

public class ScriptExecutor
{
    /// <summary>
    /// 脚本文件的入口类
    /// </summary>
    private static readonly string SCRIPT_CLASS_NAME = "ScriptEntrance";

    /// <summary>
    /// 脚本文件的入口函数
    /// </summary>
    private static readonly string SCRIPT_METHOD_NAME = "Main";

    /// <summary>
    /// 运行指定路径的脚本文件
    /// </summary>

    public static async Task<object?> Run(string script)
    {
        return await ExecuteScript(script, SCRIPT_CLASS_NAME, SCRIPT_METHOD_NAME);
    }

    /// <summary>
    /// 动态加载并执行脚本文件
    /// </summary>
    private static async Task<object?> ExecuteScript(string scriptPath, string className, string methodName, params object[] parameters)
    {
        if (!File.Exists(scriptPath))
        {
            Logger.LogError($"执行脚本失败ScriptPath={scriptPath},MethodName={methodName}:脚本文件不存在");
            return null;
        }

        // 读取脚本内容
        string scriptCode = await File.ReadAllTextAsync(scriptPath);

        // 编译脚本
        (Assembly? assembly, string? error) = CompileScript(scriptCode);
        if (assembly == null)
        {
            return null;
        }

        // 查找并执行方法
        return InvokeMethod(assembly, className, methodName, parameters);
    }

    /// <summary>
    /// 编译脚本代码
    /// </summary>
    /// <param name="scriptCode">脚本代码</param>
    /// <returns>(编译后的程序集,编译输出)</returns>
    private static (Assembly?,string?) CompileScript(string scriptCode)
    {
        // 创建语法树
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);

        // 引用程序集
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // mscorlib
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location), // System.Console
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location), // System.Threading.Tasks
            MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location) // 当前程序集
        };

        // 编译选项
        CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        // 创建编译器
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "DynamicScriptAssembly_" + scriptCode.GetHashCode(),
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: options
        );

        // 编译到内存流
        StringBuilder sb = new StringBuilder();
        using (var ms = new MemoryStream())
        {
            EmitResult result = compilation.Emit(ms);

            // 输出编译信息
            foreach (var diagnostic in result.Diagnostics)
            {
                sb.Append(diagnostic.ToString());
            }
            
            ms.Seek(0, SeekOrigin.Begin);
            return (result.Success ? Assembly.Load(ms.ToArray()) : null, sb.ToString());
        }
    }

    /// <summary>
    /// 调用程序集中的方法
    /// </summary>
    private static object? InvokeMethod(Assembly assembly, string className, string methodName, params object[] parameters)
    {
        // 查找包含方法的类型
        Type? type = assembly.GetType(className);
        if (type == null)
        {
            throw new InvalidOperationException("Class 'Script' not found in script.");
        }

        // 查找方法
        MethodInfo? method = type.GetMethod(methodName);
        if (method == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found in class 'Script'.");
        }

        // 创建类实例并调用方法
        object? instance = Activator.CreateInstance(type);
        return method.Invoke(instance, parameters);
    }
}