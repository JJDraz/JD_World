using AutoMapper;
using JD_Hateoas.Filters;
using JD_Hateoas.Helpers;
using JD_Hateoas.Paging;
using JDWorldAPI.Mapping;
using JDWorldAPI.Models;
using JDWorldAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JDWorldAPI
{
    public class Startup
    {
        private readonly int? _httpsPort;
        private readonly bool _isDev;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;

            // Get the HTTPS port (only in development) 
            _isDev = env.IsDevelopment();
            if (_isDev)
            {
                var launchJsonConfig = new ConfigurationBuilder()
                    .SetBasePath(env.ContentRootPath)
                    .AddJsonFile("Properties\\launchSettings.json")
                    .Build();
                _httpsPort = launchJsonConfig.GetValue<int>("iisSettings:iisExpress:sslPort");
            }

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            // Use an in-memory database for quick dev and testing
            // DO NOT DO THIS IN PRODUCTION - DUH!
            services.AddDbContext<JDWorldAPIContext>(opt =>
            {
                opt.UseInMemoryDatabase("JDWorldDB");
                opt.UseOpenIddict();
            });

            // Register the Identity services.
            services.AddIdentity<UserDto, UserRoleDto>()
                .AddEntityFrameworkStores<JDWorldAPIContext>()
                .AddDefaultTokenProviders();

            // Map some of the default claim names to the proper OpenID Connect claim names
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = Claims.Role;
            });

            services.AddOpenIddict()

                // Register the OpenIddict core services.
                .AddCore(options =>
                {
                    // Register the Dto Framework stores and models.
                    options.UseEntityFrameworkCore().UseDbContext<JDWorldAPIContext>();
                })

                // Register the OpenIddict server handler.
                .AddServer(options =>
                {
                    // Register the ASP.NET Core MVC binder used by OpenIddict.
                    // Note: if you don't call this method, you won't be able to
                    // bind OpenIdConnectRequest or OpenIdConnectResponse parameters.

                    //options.UseMvc();

                    options.UseAspNetCore();
                    // Enable the token endpoint.
                    options.SetTokenEndpointUris("/token");

                    // Enable the password flow.
                    options.AllowPasswordFlow();

                    // Accept anonymous clients (i.e clients that don't send a client_id).
                    options.AcceptAnonymousClients();

                    // Register the signing and encryption credentials.
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();

                    // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                    options.UseAspNetCore()
                           .EnableTokenEndpointPassthrough();
                })

                // Register the OpenIddict validation handler.
                // Note: the OpenIddict validation handler is only compatible with the
                // default token format or with reference tokens and cannot be used with
                // JWT tokens. For JWT tokens, use the Microsoft JWT bearer handler.
                .AddValidation(options =>
                {
                    // Import the configuration from the local OpenIddict server instance.
                    options.UseLocalServer();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });

            // Default Authentication scheme is OpenIddict so it MUST be after AddOpenIddict.
            services.AddAuthentication(options =>
            {
                //options.DefaultAuthenticateScheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;

            });

            // Auto Mapper Configurations
            var mappingProfile = new MappingProfile();
            var mappingConfig = mappingProfile.config;

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            services.AddResponseCaching();    // REQUIRED for JD_Hateoas class library

            services.AddMvc(opt =>
            {
                opt.Filters.Add(typeof(JsonExceptionFilter));   // REQUIRED for JD_Hateoas class library
                opt.Filters.Add(typeof(LinkRewritingFilter));   // REQUIRED for JD_Hateoas class library

                // HTTPS for all controllers
                opt.SslPort = _httpsPort;
                opt.Filters.Add(typeof(RequireHttpsAttribute));

                // REQUIRED for JD_Hateoas class library
                NewtonsoftJsonOutputFormatter jsonOutputFormatter = opt.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>().Single();
                opt.OutputFormatters.Remove(jsonOutputFormatter);
                opt.OutputFormatters.Add(new IonOutputFormatter(jsonOutputFormatter));

                // REQUIRED for JD_Hateoas class library - but you define your own profiles
                opt.CacheProfiles.Add("Static", new CacheProfile { Duration = 86400 });
                opt.CacheProfiles.Add("Collection", new CacheProfile { Duration = 60 });
                opt.CacheProfiles.Add("Resource", new CacheProfile { Duration = 180 });
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "JDWorldAPI", Version = "v1" });
            });

            services.AddRouting(opt => opt.LowercaseUrls = true);

            // STRONGLY RECOMMENDED for JD_Hateoas class library - Media Type version.  Additional versioning is optional.
            services.AddApiVersioning(opt =>
            {
                opt.ApiVersionReader = new MediaTypeApiVersionReader();
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ReportApiVersions = true;
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.ApiVersionSelector = new CurrentImplementationApiVersionSelector(opt);
            });

            // Inject configuration services
            services.Configure<CsOptions>(Configuration);
            services.Configure<PagingOptions>(Configuration.GetSection("DefaultPagingOptions"));

            // Inject data services
            services.AddScoped<IUserService, UserService>();

            services.AddAuthorization(opt =>
            {
                opt.AddPolicy("ViewAllUsersPolicy",
                    p => p.RequireAuthenticatedUser().RequireRole("TenantAdmin"));

                opt.AddPolicy("ViewAllAssignmentsPolicy",
                    p => p.RequireAuthenticatedUser().RequireRole("TenantAdmin"));
            });
        }
    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "JDWorldAPI v1"));

                using (var serviceScope = app.ApplicationServices.CreateScope())
                {
                    // Add test roles and users
                    var roleManager = serviceScope.ServiceProvider.GetService<RoleManager<UserRoleDto>>();
                    var userManager = serviceScope.ServiceProvider.GetService<UserManager<UserDto>>();

                    AddTestUsers(roleManager, userManager).Wait();

                    var dbContext = serviceScope.ServiceProvider.GetService<JDWorldAPIContext>();

                }
            }

            // STRONGLY RECOMMENDED for JD_Hateoas class library - set your own parameters
            app.UseHsts(opt =>
            {
                opt.MaxAge(days: 180);
                opt.IncludeSubdomains();
                opt.Preload();
            });

            // REQUIRED for JD_Hateoas class library
            app.UseResponseCaching();
            
            app.UseHttpsRedirection();

            app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        // ********************************************************
        // ******************                 *********************
        // ******************  ADD TEST USERS *********************
        // ******************                 *********************
        // ********************************************************
        private static async Task AddTestUsers(
            RoleManager<UserRoleDto> roleManager,
            UserManager<UserDto> userManager)
        {

            const string _password = "Supersecret123!!";

            // Add a test role
            await roleManager.CreateAsync(new UserRoleDto("TenantAdmin"));
            await roleManager.CreateAsync(new UserRoleDto("User"));

            // ==================                    =====================
            // ==================  WASHINGTON TENANT =====================
            // ==================                    =====================

            // Add Tenant Admin User to WASHINGTON Tenant
            var user = new UserDto
            {
                Email = "WTesterman@washington.com",
                UserName = "WTesterman@washington.com",
                FirstName = "William",
                LastName = "Testerman",
                TenantName = "WASHINGTON",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "TenantAdmin");
            await userManager.UpdateAsync(user);

            // Add Second Tenant Admin User to WASHINGTON Tenant
            user = new UserDto
            {
                Email = "WFlintstone@washington.com",
                UserName = "WFlintstone@washington.com",
                FirstName = "Wilma",
                LastName = "Flintstone",
                TenantName = "WASHINGTON",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "TenantAdmin");
            await userManager.UpdateAsync(user);

            // Add Simple User to WASHINGTON Tenant
            user = new UserDto
            {
                Email = "WWorldleader@washington.com",
                UserName = "WWorldleader@washington.com",
                FirstName = "Wolly",
                LastName = "Worldleader",
                TenantName = "WASHINGTON",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "User");
            await userManager.UpdateAsync(user);

            // Add Second Simple User to WASHINGTON Tenant
            user = new UserDto
            {
                Email = "WWright@washington.com",
                UserName = "WWright@washington.com",
                FirstName = "Walter",
                LastName = "Wright",
                TenantName = "WASHINGTON",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "User");
            await userManager.UpdateAsync(user);

            // Add Third Simple User to WASHINGTON Tenant
            user = new UserDto
            {
                Email = "WRipple@washington.com",
                UserName = "WRipple@washington.com",
                FirstName = "Winona",
                LastName = "Ripple",
                TenantName = "WASHINGTON",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "User");
            await userManager.UpdateAsync(user);

            // Add Fourth Simple User to WASHINGTON Tenant
            user = new UserDto
            {
                Email = "WWilson@washington.com",
                UserName = "WWilson@washington.com",
                FirstName = "Wendle",
                LastName = "Wilson",
                TenantName = "WASHINGTON",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "User");
            await userManager.UpdateAsync(user);

            // ==================               =====================
            // ==================  ADAMS TENANT =====================
            // ==================               =====================

            // Add Tenant Admin User to ADAMS Tenant
            user = new UserDto
            {
                Email = "AArturo@adams.com",
                UserName = "AArturo@adams.com",
                FirstName = "Andrew",
                LastName = "Arturo",
                TenantName = "ADAMS",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "TenantAdmin");
            await userManager.UpdateAsync(user);

            // Add Simple User to ADAMS Tenant
            user = new UserDto
            {
                Email = "AApple@adams.com",
                UserName = "AApple@adams.com",
                FirstName = "Alfred",
                LastName = "Apple",
                TenantName = "ADAMS",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "User");
            await userManager.UpdateAsync(user);

            // Add Second Simple User to ADAMS Tenant
            user = new UserDto
            {
                Email = "ACousins@adams.com",
                UserName = "ACousins@adams.com",
                FirstName = "Amy",
                LastName = "Cousins",
                TenantName = "ADAMS",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "User");
            await userManager.UpdateAsync(user);
        }


    }
}
