using System.Threading.Channels;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Microsoft.Playwright;


namespace Handler{
    public class theResponse{
        [JsonPropertyName("status")]
        public bool Status {get;set;}
        [JsonPropertyName("html")]
        public string Html {get;set;}
        [JsonPropertyName("err")]
        public string? Err {get;set;}
        [JsonPropertyName("vars")]
        public object? Vars {get;set;}
        public theResponse(bool status, string html, string? err, object? vars){
            Status = status;
            Html = html;
            Err = err;
            Vars = vars;
            }
        }

    public class RequestTask{
        public string URL {get;set;}
        public string? Vars {get;set;}
        public Channel<theResponse> chan;
        public RequestTask(string url, string? vars){
            URL = url;
            Vars = vars;
            chan=Channel.CreateBounded<theResponse>(1);
        }
        async public Task<theResponse?> GetResponse(){
            while(await chan.Reader.WaitToReadAsync()){
                var response = await chan.Reader.ReadAsync();
                return response;
            }
            return null;
        }
    }

    public class PlaywrightMan{
        private readonly Channel<RequestTask> _channel = Channel.CreateBounded<RequestTask>(10);
        private bool _isRunning = true;
        private readonly IBrowser _browser;

        public bool Write(RequestTask task ){
            return _channel.Writer.TryWrite(task);
        }
        public PlaywrightMan(){
            // var playwright = Playwright.CreateAsync().Result;
            // _browser= playwright.Firefox.LaunchAsync().Result;
            _browser=Playwright.CreateAsync().Result.Firefox.LaunchAsync().Result;
        }
        public void start (int concurrency=2){
            //start listening
            for(int i =0;i<concurrency;i++){
                Task.Run(async()=>{
                    int ThreadID = i;
                    var page = await _browser.NewPageAsync();
                    while(_isRunning  && await _channel.Reader.WaitToReadAsync()){
                        RequestTask t = await _channel.Reader.ReadAsync();
                        Console.WriteLine($"Thread {ThreadID} is processing {t.URL}");
                        try{
                            if(page.IsClosed){
                                page = await _browser.NewPageAsync();
                            }
                            PageGotoOptions op = new PageGotoOptions(){
                                Timeout=8000,
                                WaitUntil=WaitUntilState.DOMContentLoaded
                            };
                            
                            await page.GotoAsync(t.URL);
                            string html = await page.ContentAsync();
                            object? vars = null;
                            if(t.Vars!=null){
                                vars= await page.EvaluateAsync<object>(t.Vars);
                            }
                            
                            // t.chan.Writer.TryWrite(new theResponse(true,html,null,JsonDocument.Parse(JsonSerializer.Serialize(vars))));
                            t.chan.Writer.TryWrite(new theResponse(true,html,null,vars));
                            // await page.CloseAsync();
                        }catch(Exception e){
                            t.chan.Writer.TryWrite(new theResponse(false,"",e.Message,null));
                        }
                    }
                });
                }
            }
        }
}
