// Build-time metadata inspection only. Reads generated declarations with Cecil;
// it never loads the supplied game assembly or invokes native methods.
using System;
using Mono.Cecil;
static class BindingFieldTypes
{
    static void Print(TypeDefinition type,string assembly)
    {
        foreach(var field in type.Fields)Console.WriteLine(assembly+"\t"+type.FullName.Replace('/','.')+"\t"+field.Name+"\t"+field.FieldType.FullName.Replace('/','.'));
        foreach(var nested in type.NestedTypes)Print(nested,assembly);
    }
    static int Main(string[] args)
    {
        foreach(string file in args){var assembly=AssemblyDefinition.ReadAssembly(file);foreach(var type in assembly.MainModule.Types)Print(type,assembly.Name.Name);}
        return 0;
    }
}
