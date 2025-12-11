using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Lampyris.Crypto.Server.Lite;

public class WaitForHttpResponse : IEnumerator
{
    private Task<HttpResponseMessage> m_task;
    private HttpResponseMessage? m_response;
    private HttpClient? m_client;

    public WaitForHttpResponse(string url, HttpClient? client = null)
    {
        m_client = client ?? new HttpClient();
        m_task = m_client.GetAsync(url);
    }

    public WaitForHttpResponse(string url, string content, HttpClient? client = null)
    {
        m_client = client ?? new HttpClient();
        var requestBody = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded");
        m_task = m_client.PostAsync(url, requestBody); 
    }

    public bool MoveNext()
    {
        if (m_task.IsCompleted)
        {
            try
            {
                m_response = m_task.Result;
                return true;
            }
            catch { 
                return false;
            }
        }
        return false;
    }

    public void Reset() { }

    public object Current => m_response;

    public string Result
    {
        get
        {
            if (m_task != null && m_task.IsCompleted)
            {
                return m_task.Result.Content.ReadAsStringAsync().Result;
            }
            return "";
        }
    }
}