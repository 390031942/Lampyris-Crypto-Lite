namespace Lampyris.Crypto.Server.Lite;

using System.Collections;

public static class CoroutineManager
{
    private static List<IEnumerator> coroutines = new List<IEnumerator>();

    public static void StartCoroutine(IEnumerator coroutine)
    {
        coroutines.Add(coroutine);
    }

    public static void RemoveCoroutine(IEnumerator coroutine)
    {
        coroutines.Remove(coroutine);
    }

    public static void OnUpdate()
    {
        for (int i = coroutines.Count - 1; i >= 0; i--)
        {
            bool needMoveNext = false;
            IEnumerator coroutine = coroutines[i];
            if (coroutine.Current != null && coroutine.Current is IEnumerator nestedCoroutine)
            {
                if (nestedCoroutine.MoveNext())
                {
                    needMoveNext = true;
                }
            }
            else {
                needMoveNext = true;
            }

            if(needMoveNext)
            {
                if (!coroutine.MoveNext())
                {
                    coroutines.Remove(coroutine);
                }
            }
        }
    }
}
