using System;
using System.Collections.Immutable;
using System.Net.Http;
using GreenPipes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Common.Settings;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;
using Polly;
using Polly.Timeout;

namespace Play.Inventory.Service {
    public class Startup {
        private const string AllowedOriginSetting = "AllowedOrigin";
        private ServiceSettings serviceSettings;

        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            serviceSettings = Configuration.GetSection (nameof (ServiceSettings)).Get<ServiceSettings> ( );
            services.AddMongo ( )
                .AddMongoRepository<InventoryItem> ("inventoryItems")
                .AddMongoRepository<CatalogItem> ("catalogItems")
                .AddMassTransitWithRabbitMq ( retryConfigurator => {
                    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                    retryConfigurator.Ignore(typeof(UnknownItemException));
                })
                .AddJwtBearerAuthentication ( );

            AddCatalogClient (services);
            services.AddControllers ( );
            services.AddSwaggerGen (c => {
                c.SwaggerDoc ("v1", new OpenApiInfo { Title = "Play.Inventory.Service", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment ( )) {
                app.UseDeveloperExceptionPage ( );
                app.UseSwagger ( );
                app.UseSwaggerUI (c => c.SwaggerEndpoint ("/swagger/v1/swagger.json", "Play.Inventory.Service v1"));
                app.UseCors (builder => {
                    builder.WithOrigins (Configuration[AllowedOriginSetting])
                        .AllowAnyHeader ( )
                        .AllowAnyMethod ( );
                });
            }

            app.UseHttpsRedirection ( );

            app.UseRouting ( );
            app.UseAuthentication ( );
            app.UseAuthorization ( );

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ( );
            });
        }

        private static void AddCatalogClient (IServiceCollection services) {
            services.AddHttpClient<CatalogClient> (client => {
                    client.BaseAddress = new Uri ("https://localhost:5001");
                })
                .AddTransientHttpErrorPolicy (builder => builder.Or<TimeoutRejectedException> ( ).WaitAndRetryAsync (
                    5,
                    retryAttempt => TimeSpan.FromSeconds (Math.Pow (2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt) => {
                        // Do not try this in production as it spit out the following warning:
                        /*
                        /* Startup.cs(44,43): warning ASP0000: Calling 'BuildServiceProvider' from application code results in an additional copy of singleton services being created. Consider alternatives such as dependency injecting services as parameters to 'Configure'. 
                        */
                        var serviceProvider = services.BuildServiceProvider ( );
                        serviceProvider.GetService<ILogger<CatalogClient>> ( ) ?
                            .LogWarning ($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
                    }
                ))
                .AddTransientHttpErrorPolicy (builder => builder.Or<TimeoutRejectedException> ( ).CircuitBreakerAsync (
                    3,
                    TimeSpan.FromSeconds (15),
                    onBreak: (response, timespan) => {
                        var serviceProvider = services.BuildServiceProvider ( );
                        serviceProvider.GetService<ILogger<CatalogClient>> ( ) ?
                            .LogWarning ($"Opening the circute for : {timespan.TotalSeconds} seconds...");
                    },
                    onReset: ( ) => {
                        var serviceProvider = services.BuildServiceProvider ( );
                        serviceProvider.GetService<ILogger<CatalogClient>> ( ) ?
                            .LogWarning ($"Closing the circute ...");
                    }
                ))
                .AddPolicyHandler (Policy.TimeoutAsync<HttpResponseMessage> (1));
        }
    }
}