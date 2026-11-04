using System.Text;

namespace CyberInternauts.CmdStarter.Repl.Remote;

/// <summary>
/// A writer that turns what a command writes into protocol lines: each complete line of text becomes <c>prefix + text</c> on the target.
/// An unfinished line is kept until its end or until <see cref="FlushPending"/>. Writers that share a target share its lock,
/// so lines of two writers never mix.
/// </summary>
internal sealed class PrefixedLineWriter : TextWriter
{
    private readonly string prefix;
    private readonly TextWriter target;
    private readonly object gate;
    private readonly StringBuilder pending = new();

    internal PrefixedLineWriter(string prefix, TextWriter target, object gate)
    {
        this.prefix = prefix;
        this.target = target;
        this.gate = gate;
    }

    public override Encoding Encoding => target.Encoding;

    public override void Write(char value)
    {
        lock (gate)
        {
            Append(value);
        }
    }

    public override void Write(string? value)
    {
        if (value is null)
        {
            return;
        }

        lock (gate)
        {
            foreach (var character in value)
            {
                Append(character);
            }
        }
    }

    // Writes the end of an unfinished line as a line of its own (the response of a command must end with its own lines).
    internal void FlushPending()
    {
        lock (gate)
        {
            if (pending.Length > 0)
            {
                EmitPending();
            }
        }
    }

    private void Append(char value)
    {
        if (value == '\n')
        {
            EmitPending();
        }
        else
        {
            pending.Append(value);
        }
    }

    private void EmitPending()
    {
        if (pending.Length > 0 && pending[^1] == '\r')
        {
            pending.Length--;
        }

        target.Write(prefix);
        target.Write(pending.ToString());
        target.Write('\n');
        target.Flush();
        pending.Clear();
    }
}
