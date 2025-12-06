using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

public class Config:IDisposable
{
    private JsonObject config_data;
    private string file_path = "config.json";
    public Config()
    {
        if (!File.Exists(file_path))
            File.WriteAllText(file_path, "{}");
        config_data = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(file_path))!;
    }
    public string? Get(string key)=>config_data[key]?.ToString();
    public void Set(string key, string value)=>config_data[key]=value;
    public void Dispose()=>File.WriteAllText(file_path,config_data.ToJsonString());
}