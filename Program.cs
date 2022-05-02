using Handler;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {

// }
// app.UseSwagger();
// app.UseSwaggerUI();
// app.UseHttpsRedirection();

PlaywrightMan pm = new  PlaywrightMan();
// Task.Run(()=>pm.start());
pm.start();

app.MapGet("/render",async (HttpRequest request)=>{
    var url = request.Query["url"];
    var vars = request.Query["vars"];
    if (url == Microsoft.Extensions.Primitives.StringValues.Empty){
        return Results.BadRequest("url is required");
    }
    var task = new RequestTask(url,vars);
    if(pm.Write(task)){
    var response = await task.GetResponse();
        if(response!=null){
                return Results.Json(response);
        }else{
            return Results.Json(new theResponse(false,"","Too Busy",null));
        }
    }
    return Results.Json(new theResponse(false,"","I cant wait, i dowt know why",null));

});
app.Map("/",()=>"<h1>Hello Bitch. Pls dial </h1>");

app.Run();
Console.WriteLine("Hello World!");
