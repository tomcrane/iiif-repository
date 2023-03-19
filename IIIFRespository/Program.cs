using IIIFRepository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<RepositorySettings>(builder.Configuration.GetSection("IIIFRepository"));

var settings = builder.Configuration.Get<RepositorySettings>();
var iiifContainer = Path.Combine(settings!.FileSystemRoot, Constants.IIIFContainer);
if (!Directory.Exists(iiifContainer))
{
    Directory.CreateDirectory(iiifContainer);
}

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
