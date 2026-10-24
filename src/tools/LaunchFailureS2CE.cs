using System.ComponentModel;

namespace S2CE.Tools;

internal static class LaunchFailureS2CE
{
    public static string Describe(Exception exception, bool generated, bool restore)
    {
        if (!generated) return "操作未完成，请先关闭 SAI 并检查目录是否可写。";
        if (exception is Win32Exception { NativeErrorCode: 4551 or 577 or 1260 })
            return (restore ? "已清理主题副本，但原版 SAI 被 Windows 安全策略阻止启动。" :
                "主题副本已生成，但被 Windows 应用控制或签名策略阻止启动。") +
                "\n请查看系统拦截提示。重新生成或重新打包不能保证解除拦截；签名需要覆盖最终运行的 EXE。" +
                "\n原版文件和已保存主题未被删除。";
        return "处理完成，但未能启动 SAI。";
    }
}
