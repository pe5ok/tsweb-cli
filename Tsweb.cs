using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
public class TsWebCli : IDisposable
{
    private static string host = "tsweb.ru", proto = "https";
    public bool debug {get;set;}
    private Encoding koi;
    private HttpClient hc;
    private Config conf;
    private string mode, uri;
    private string? cookie;
    private bool InvalidLogin(string response, string? path)
    {
        if (path == "index" || path == "index2")
        {
            if(response.Contains("You are currently logged in as"))return false;
            if(response.Contains("Thank you for logging in"))return false;
            if(response.Contains("Your contest has been changed to"))return false;
            return true;
        }
        if (path == "contest")
        {
            return response.Contains("ID mismatch");
        }
        if (path == "submit")
        {
            return response.Contains("Login error: session data missing or expired");
        }
        return false;
    }
    private async Task<string> EnsureParse(HttpResponseMessage result)
    {
        if (result.StatusCode.CompareTo(HttpStatusCode.OK) != 0)
            throw new Exception($"Response code: {result.StatusCode}");
        string content = koi.GetString(await result.Content.ReadAsByteArrayAsync());
        if(debug)Console.WriteLine($"{result.Headers}==================\n{content}");
        if(result.Headers.Contains("Set-cookie"))
            foreach(string c in result.Headers.GetValues("Set-cookie").First().Split(';'))
                if(c.StartsWith("tsw=")){
                    cookie = c;
                    conf.Set("cookie", c.Trim());
                    hc.DefaultRequestHeaders.Remove("Cookie");
                    hc.DefaultRequestHeaders.Add("Cookie", cookie);
                    break;
                }
        return content;
    }
    public TsWebCli()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        koi = Encoding.GetEncoding(20866);
        hc = new HttpClient();
        conf = new Config();
        mode = conf.Get("mode") ?? "t";
        uri = $"{proto}://{host}/{mode}";
        cookie = conf.Get("cookie")??"none";
        hc.DefaultRequestHeaders.Add("Cookie", cookie);
    }
    public async Task<int> Login()
    {
        Console.Write("Login: ");
        string login = (Console.ReadLine() ?? "").Trim();
        Console.Write("Password: ");
        StringBuilder password = new();
        ConsoleKey key;
        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;
            if (key == ConsoleKey.Backspace && password.Length > 0)
            {
                Console.Write("\b \b");
                password.Remove(password.Length-1, 1);
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                Console.Write("*");
                password.Append(keyInfo.KeyChar);
            }
        } while (key != ConsoleKey.Enter); 
        Console.WriteLine();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>{
                ["op"] = "login",
                ["team"] = login,
                ["password"] = password.ToString()
            }
        );
        string content = await EnsureParse(await hc.PostAsync($"{uri}/index", form));
        if (InvalidLogin(content, "index"))
            throw new Exception("Log in failure.");
        Console.WriteLine("Login successful.");
        return 0;
    }
    public async Task<int> Submit(string file_path)
    {
        string? _cmp = conf.Get("compiler"), _tsk = conf.Get("task");
        if(_cmp is null || _tsk is null)
            throw new Exception($"{((_cmp is null)?"":"Compiler was not set.\n")}{((_tsk is null)?"":"Task was not set.\n")}");
        var form = new MultipartFormDataContent();
        var prob = new StringContent(_tsk);
        prob.Headers.Clear();
        prob.Headers.Add("Content-Disposition",$"form-data; name=\"prob\"");
        form.Add(prob,"\"lang\"");
        var cmp = new StringContent(_cmp);
        cmp.Headers.Clear();
        cmp.Headers.Add("Content-Disposition",$"form-data; name=\"lang\"");
        form.Add(cmp,"\"prob\"");
        var solv = new StringContent(File.ReadAllText(file_path));
        solv.Headers.Clear();
        solv.Headers.Add("Content-Disposition",$"form-data; name=\"solution\"");
        form.Add(solv,"\"prob\"");
        string content = await EnsureParse(await hc.PostAsync($"{uri}/submit",form));
        return 0;
    }
    public async Task<int> ListSubmits()
    {
        string content = await EnsureParse(await hc.GetAsync($"{uri}/index2"));
        content = await EnsureParse(await hc.GetAsync($"{uri}/index2"));
        if (InvalidLogin(content, "index2"))
            throw new Exception("Log in failure.");
        content = content.Split("?</SPAN><BR /><BR />")[1].Split("<BR /><HR>Some")[0];
        if(content.Contains("No old"))
            Console.WriteLine("No old messages yet.");
        else
        {
            foreach(string s in content.Split("<BR />",StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Reverse())
            {
                if(s[0]!='&')continue;
                Console.WriteLine(s.Substring(s.IndexOf(';')+1));
            }
        }
        return 0;
    }
    public async Task<int> ListContests()
    {
        string content = await EnsureParse(await hc.GetAsync($"{uri}/contests?mask=1"));
        if (InvalidLogin(content, "contests"))
            throw new Exception("Log in failure.");
        content = content.Split("<TABLE BORDER=1 ELLPADDING=2 CELLSPACING=3>")[1]
                         .Split("</TR>")[1].Split("</TABLE>")[0].Trim();
        content = content.Substring(4, content.Length-9);
        string[] kw = {"</tr>","<tr>"}, wk = {"<td>","</td>"};
        foreach (string c in content.Split(kw, StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Reverse()){
            var _c = c.Split(wk, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            string label = _c[1].Substring(_c[1].IndexOf(">")+1);
            Console.WriteLine($"{_c[0].Substring(_c[0].IndexOf(">")+1)}\t{label.Substring(0, label.IndexOf("<"))}");
        }
        return 0;
    }
    public async Task<int>ListCompilers()
    {
        string content = await EnsureParse(await hc.GetAsync($"{uri}/submit"));
        if (InvalidLogin(content, "submit"))
            throw new Exception("Log in failure.");
        var _l = content.Split("<SELECT NAME=lang>")[1].Split("</SELECT>")[0]
                        .Split("</OPTION>",StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        for(int i = 1; i < _l.Length; ++i)
        {
            int b = _l[i].IndexOf("=")+1, e = _l[i].IndexOf(">",b);
            Console.WriteLine($"{_l[i].Substring(b, e-b)}\t{_l[i].Substring(e+1)}");
        }
        return 0;
    }
    public async Task<int> ListTasks()
    {
        string content = await EnsureParse(await hc.GetAsync($"{uri}/submit"));
        if (InvalidLogin(content, "submit"))
            throw new Exception("Log in failure.");
        var _l = content.Split("<SELECT NAME=prob>")[1].Split("</SELECT>")[0]
                        .Split("</OPTION>",StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        for(int i = 1; i < _l.Length; ++i)
        {
            int b = _l[i].IndexOf("=")+1, e = _l[i].IndexOf(">",b);
            Console.WriteLine($"{_l[i].Substring(b, e-b)}\t{_l[i].Substring(e+1)}");
        }
        return 0;
    }
    public async Task<int> SetContest(string contest_id)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string,string>{
            ["op"]="changecontest",
            ["newcontestid"]=contest_id
        });
        string content = await EnsureParse(await hc.PostAsync($"{uri}/index", form));
        if (InvalidLogin(content, "index"))
            throw new Exception("Log in failure.");
        return 0;
    }
    public int SetTask(string task)
    {
        conf.Set("task", task);
        return 0;
    }
    public int SetCompiler(string compiler)
    {
        conf.Set("compiler",compiler);
        return 0;
    }
    public void Dispose()
    {
        conf.Dispose();
    }

}