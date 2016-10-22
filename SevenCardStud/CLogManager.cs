using UnityEngine;

public static class CLogManager
{
    public static void Log(string format)
    {
        Debug.Log(string.Format("[{0}] {1}", System.Threading.Thread.CurrentThread.ManagedThreadId, format));
    }
}