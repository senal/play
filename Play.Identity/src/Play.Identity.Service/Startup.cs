using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.Settings;
using Play.Identity.Service.Entities;
using Play.Identity.Service.HostedServices;
using Play.Identity.Service.Settings;

namespace Play.Identity.Service {
    public class Startup {
        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {

            BsonSerializer.RegisterSerializer (new GuidSerializer (MongoDB.Bson.BsonType.String));
            var serviceSettings = Configuration.GetSection (nameof (ServiceSettings)).Get<ServiceSettings> ( );
            var mongoDbSettings = Configuration.GetSection (nameof (MongoDbSettings)).Get<MongoDbSettings> ( );
            var identityServerSettings = Configuration.GetSection (nameof (IdentityServerSettings)).Get<IdentityServerSettings> ( );

            services.Configure<IdentitySettings> (Configuration.GetSection (nameof (IdentitySettings)))
                .AddDefaultIdentity<ApplicationUser> ( )
                .AddRoles<ApplicationRole> ( )
                .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
                (
                    connectionString: mongoDbSettings.ConnectionString,
                    serviceSettings.ServiceName
                );
            // Identity server configurations
            services.AddIdentityServer (options => {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                })
                .AddAspNetIdentity<ApplicationUser> ( ) // Integrate Identity server with ASPIdentity
                .AddInMemoryApiScopes (identityServerSettings.ApiScopes)
                .AddInMemoryClients (identityServerSettings.Clients)
                .AddInMemoryApiResources (identityServerSettings.ApiResources)
                .AddInMemoryIdentityResources (identityServerSettings.IdentityResources)
                .AddDeveloperSigningCredential ( ); // This is only in Dev enviroment, in production we must use a valid certificate to sign tokens

            services.AddLocalApiAuthentication ( ); // Protecting Identity server controls
            services.AddControllers ( );
            services.AddHostedService<IdentitySeedHostedService> ( );
            services.AddSwaggerGen (c => {
                c.SwaggerDoc ("v1", new OpenApiInfo { Title = "Play.Identity.Service", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment ( )) {
                app.UseDeveloperExceptionPage ( );
                app.UseSwagger ( );
                app.UseSwaggerUI (c => c.SwaggerEndpoint ("/swagger/v1/swagger.json", "Play.Identity.Service v1"));
            }

            app.UseHttpsRedirection ( );
            app.UseStaticFiles ( );

            app.UseRouting ( );
            app.UseIdentityServer ( );
            app.UseAuthorization ( );

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ( );
                // enabling Razor pages
                endpoints.MapRazorPages ( );
            });
        }
    }
}