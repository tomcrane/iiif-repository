using IIIFRepository;
using IIIFRespository.Requests;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<RepositorySettings>(builder.Configuration.GetSection("IIIFRepository"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<Storage, Storage>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
