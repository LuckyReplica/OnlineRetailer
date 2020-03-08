using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderApi.Data;
using OrderApi.Models;
using Microsoft.OpenApi.Models;
using System;
using OrderApi.Infrastructure;
using SharedModels;

namespace OrderApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // Base URL for the product service when the solution is executed using docker-compose.
        // The product service (running as a container) listens on this URL for HTTP requests
        // from other services specified in the docker compose file (which in this solution is
        // the order service).
        Uri productServiceBaseUrl = new Uri("http://productapi/api/products/");

        // RabbitMQ connection string (I use CloudAMQP as a RabbitMQ server).
        // Remember to replace this connectionstring with youur own.
        string cloudAMQPConnectionString =
            "host=crow.rmq.cloudamqp.com;virtualHost=lbutosez;username=lbutosez;password=qXd-hBNlJiQ-i0cWEKHmcuLHZIsMLTHt";

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // In-memory database:
            services.AddDbContext<OrderApiContext>(opt => opt.UseInMemoryDatabase("OrdersDb"));

            // Register repositories for dependency injection
            services.AddScoped<IRepository<OrderApi.Models.Order>, OrderRepository>();

            // Register database initializer for dependency injection
            services.AddTransient<IDbInitializer, DbInitializer>();

            // Register product service gateway for dependency injection
            services.AddSingleton<IServiceGateway<Product>>(new
                ProductsServiceGateway(productServiceBaseUrl));

            // Register MessagePublisher (a messaging gateway) for dependency injection
            services.AddSingleton<IMessagePublisher>(new
                MessagePublisher(cloudAMQPConnectionString));

            services.AddMvc(options => options.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
                
                // Swagger UI displays at /swagger when this is commented out.
                //c.RoutePrefix = string.Empty;
            });

            // Initialize the database
            using (var scope = app.ApplicationServices.CreateScope())
            {
                // Initialize the database
                var services = scope.ServiceProvider;
                var dbContext = services.GetService<OrderApiContext>();
                var dbInitializer = services.GetService<IDbInitializer>();
                dbInitializer.Initialize(dbContext);
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
