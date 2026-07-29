using System;

namespace EasyEPlanner.StandaloneEditor
{
    /// <summary>
    /// No-op реализация лога для standalone-режима.
    /// </summary>
    public class StandaloneLog : ILog
    {
        public bool IsEmpty() => true;

        public void ShowLog() { }

        public void HideLog() { }

        public void Clear() { }

        public bool Active() => false;

        public void EnableButtons() { }

        public void DisableButtons() { }

        public void AddMessage(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }

        public void ShowLastLine() { }

        public void SetProgress(int val) { }

        public bool IsNull() => false;
    }
}
