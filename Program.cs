
namespace taskManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddEndpointsApiExplorer(); //This registers Swagger
            builder.Services.AddSwaggerGen();//creates API docs


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseSwagger();  //enables backend
            app.UseSwaggerUI();     // shows UI
        
            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
