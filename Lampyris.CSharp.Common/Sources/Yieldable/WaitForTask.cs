namespace Lampyris.Crypto.Server.Lite;

using System.Collections;
using System.Threading.Tasks;

public class WaitForTask : IEnumerator
{
    private Task m_Task;

    public WaitForTask(Task task)
    {
        m_Task = task;
    }

    public bool MoveNext()
    {
        if (m_Task == null || m_Task.IsCompleted)
        {
            return true;
        }
        return false;
    }

    public void Reset()
    {
        
    }

    public object? Current => null;
}