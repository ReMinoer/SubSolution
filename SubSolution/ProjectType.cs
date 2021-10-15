using SubSolution.Utils;

namespace SubSolution
{
    public enum ProjectType
    {
        [ProjectTypeName("Solution folder")]
        [ProjectTypeGuid("2150E333-8FDC-42A3-9474-1A3956D46DE8")]
        Folder,
        [ProjectTypeName("C++")]
        [ProjectTypeGuid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942")]
        Cpp,
        [ProjectTypeName("C# (old)")]
        [ProjectTypeGuid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")]
        CSharpLegacy,
        [ProjectTypeName("C#")]
        [ProjectTypeGuid("9A19103F-16F7-4668-BE54-9A1E7A4F7556")]
        CSharpDotNetSdk,
        [ProjectTypeName("VB.NET (old)")]
        [ProjectTypeGuid("F184B08F-C81C-45F6-A57F-5ABD9991F28F")]
        VisualBasicLegacy,
        [ProjectTypeName("VB.NET")]
        [ProjectTypeGuid("778DAE3C-4631-46EA-AA77-85C1314464D9")]
        VisualBasicDotNetSdk,
        [ProjectTypeName("F# (old)")]
        [ProjectTypeGuid("F2A71F9B-5D33-465A-A702-920D77279786")]
        FSharpLegacy,
        [ProjectTypeName("F#")]
        [ProjectTypeGuid("6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705")]
        FSharpDotNetSdk,
        [ProjectTypeName("Python")]
        [ProjectTypeGuid("888888A0-9F3D-457C-B088-3A5042F75D52")]
        Python,
        [ProjectTypeName("Node.js")]
        [ProjectTypeGuid("9092AA53-FB77-4645-B42D-1CCCA6BD08BD")]
        NodeJs,
        [ProjectTypeName("SQL")]
        [ProjectTypeGuid("00D1A9C2-B5F0-4AF3-8072-F6C62B433612")]
        Sql,
        [ProjectTypeName("Windows Application Packaging")]
        [ProjectTypeGuid("C7167F0D-BC9F-4E6E-AFE1-012C56B48DB5")]
        Wap
    }
}