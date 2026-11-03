using System;
using System.Threading;

namespace Antd.Wpf.Controls;

public enum InputSearchReason
{
    Enter,
    Button,
    Clear,
}

public sealed class InputSearchEventArgs : EventArgs
{
    internal InputSearchEventArgs(string text, InputSearchReason reason, CancellationToken cancellationToken)
    {
        Text = text;
        Reason = reason;
        CancellationToken = cancellationToken;
    }

    public string Text { get; }
    public InputSearchReason Reason { get; }
    public CancellationToken CancellationToken { get; }
    public bool Cancel { get; set; }
}
