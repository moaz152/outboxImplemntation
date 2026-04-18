using OrderService.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrderServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
