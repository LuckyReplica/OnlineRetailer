using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CustomerApi.Data;
using CustomerApi.Models;
using System.Threading.Tasks;
using CustomerApi.Infrastructure;

namespace CustomerApi
{
    public class Startup
    {
        string cloudAMQPConnectionString = "host=crow.rmq.cloudamqp.com;virtualHost=lbutosez;username=lbutosez;password=qXd-hBNlJiQ-i0cWEKHmcuLHZIsMLTHt";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // In-memory database:
            services.AddDbContext<CustomerApiContext>(opt => opt.UseInMemoryDatabase("CustomersDb"));

            // Register repositories for dependency injection.
            services.AddScoped<IRepository<Customer>, CustomerRepository>();

            // Register database initializer for dependency injection.
            services.AddTransient<IDbInitializer, DbInitializer>();

            // Changed from .AddMcv.
            services.AddControllers(options => options.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                // Initialize the database
                var services = scope.ServiceProvider;
                var dbContext = services.GetService<CustomerApiContext>();
                var dbInitializer = services.GetService<IDbInitializer>();
                dbInitializer.Initialize(dbContext);
            }

            // Create a message listener in a separate thread.
            Task.Factory.StartNew(() =>
                new MessageListener(app.ApplicationServices, cloudAMQPConnectionString).Start());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
