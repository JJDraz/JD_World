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
            services.Configure<JDInfoRest>(Configuration.GetSection("Info"));
            services.Configure<PagingOptions>(Configuration.GetSection("DefaultPagingOptions"));

            // Inject data services
            services.AddScoped<IWorldService, WorldService>();
            services.AddScoped<IResidentService, ResidentService>();
            services.AddScoped<IUserService, UserService>();

            services.AddAuthorization(opt =>
            {
                opt.AddPolicy("ViewAllUsersPolicy",
                    p => p.RequireAuthenticatedUser().RequireRole("TenantAdmin"));

                opt.AddPolicy("RegisterUsersPolicy",
                    p => p.RequireAuthenticatedUser().RequireRole("TenantAdmin"));

                opt.AddPolicy("ViewAllResidentsPolicy",
                    p => p.RequireAuthenticatedUser().RequireRole("TenantAdmin"));

                opt.AddPolicy("ViewAllWorldsPolicy",
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

                    AddTestData(dbContext, userManager);
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
            // ==================  WASHINGTON USERS  =====================
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
            // ==================  ADAMS USERS  =====================
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

            // ==================                   =====================
            // ==================  JEFFERSON USERS  =====================
            // ==================                   =====================

            // Add Tenant Admin User to ADAMS Tenant
            user = new UserDto
            {
                Email = "JJones@jefferson.com",
                UserName = "JJones@jefferson.com",
                FirstName = "Johnny",
                LastName = "Jones",
                TenantName = "JEFFERSON",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await userManager.CreateAsync(user, _password);

            // Put the user in the admin role
            await userManager.AddToRoleAsync(user, "TenantAdmin");
            await userManager.UpdateAsync(user);
        }


        // ********************************************************
        // ******************                 *********************
        // ******************  ADD TEST DATA  *********************
        // ******************                 *********************
        // ********************************************************
        private static void AddTestData(
            JDWorldAPIContext context,
            UserManager<UserDto> userManager)
        {

            // ==================                    =====================
            // ==================  WASHINGTON WORLDS =====================
            // ==================                    =====================

            var wrld_wash1 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("ee2b83be-91db-4de5-8122-35a9e9195976"),
                WorldName = "Wild West",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.1",
                VoiceIP = "151.122.2.1"
            }).Entity;

            var wrld_wash2 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("804c64e3-9bfa-4ca0-8300-33960b11a55c"),
                WorldName = "Wellington",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.2",
                VoiceIP = "151.122.2.2"
            }).Entity;

            var wrld_wash3 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("8035be4e-eeea-46a2-b828-c4218df34757"),
                WorldName = "Wilson Athletic",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.3",
                VoiceIP = "151.122.2.3"
            }).Entity;

            var wrld_wash4 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("c9008f7e-0a9c-47d3-8e4a-f0247634870e"),
                WorldName = "Wilcox Electric",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.4",
                VoiceIP = "151.122.2.4"
            }).Entity;

            var wrld_wash5 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("61406e2d-2d22-46e2-a2ff-92ebc4345a25"),
                WorldName = "Walking Footwear",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.5",
                VoiceIP = "151.122.2.5"
            }).Entity;

            var wrld_wash6 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("daf9a15b-e144-46af-8dec-d1b3de493d58"),
                WorldName = "Willies Bar",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.6",
                VoiceIP = "151.122.2.6"
            }).Entity;

            var wrld_wash7 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("95020f7b-b43f-4b7f-a644-4c2c81a54291"),
                WorldName = "Weather Proofers",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.7",
                VoiceIP = "151.122.2.7"
            }).Entity;

            var wrld_wash8 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("6ba9fe54-6394-447b-bb80-66f65f379782"),
                WorldName = "Wide Lens Crafters",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.8",
                VoiceIP = "151.122.2.8"
            }).Entity;

            var wrld_wash9 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("90d81ae9-bfdf-48ca-9476-8802f4e4bcd2"),
                WorldName = "Wonder World",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.9",
                VoiceIP = "151.122.2.9"
            }).Entity;

            var wrld_wash10 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("ec260967-0caf-4a88-b3b1-9a12eba392d0"),
                WorldName = "Waiters Wear",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.10",
                VoiceIP = "151.122.2.10"
            }).Entity;

            var wrld_wash11 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("406d2ef2-40d3-42e6-b188-53a726930154"),
                WorldName = "Wassup",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.11",
                VoiceIP = "151.122.2.11"
            }).Entity;

            var wrld_wash12 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("31ffb2a5-8504-4a8c-8a12-d2c37ced95ec"),
                WorldName = "White Whale Fishing",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.12",
                VoiceIP = "151.122.2.12"
            }).Entity;

            var wrld_wash13 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("a280c64e-8514-44e8-976b-caa27e0e61d2"),
                WorldName = "Winter Jackets",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.13",
                VoiceIP = "151.122.2.13"
            }).Entity;

            var wrld_wash14 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("301df04d-8679-4b1b-ab92-0a586ae53d08"),
                WorldName = "Whitmore Services",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.14",
                VoiceIP = "151.122.2.14"
            }).Entity;

            var wrld_wash15 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("cdb038f0-549c-42e5-8993-09f26b79ff7b"),
                WorldName = "Websters Dictionary",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.15",
                VoiceIP = "151.122.2.15"
            }).Entity;

            var wrld_wash16 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("261f5861-2189-4ab9-b3a7-191df7877dac"),
                WorldName = "Wonka Bars",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.16",
                VoiceIP = "151.122.2.16"
            }).Entity;

            var wrld_wash17 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("e84bd49e-28e0-4baa-9864-2947025fb253"),
                WorldName = "Wonka Bars",
                TenantName = "WASHINGTON",
                ServerIP = "151.122.1.16",
                VoiceIP = "151.122.2.16"
            }).Entity;

            // ==================               =====================
            // ==================  ADAMS WORLDS =====================
            // ==================               =====================

            var wrld_adam1 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("55174804-9993-4712-a271-eb3c907cdc48"),
                WorldName = "Argent Energy",
                TenantName = "ADAMS",
                ServerIP = "151.123.1.1",
                VoiceIP = "151.123.2.1"
            }).Entity;

            var wrld_adam2 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("861e5218-e055-4abb-a7b2-4cdc9113cb54"),
                WorldName = "Anderson Little",
                TenantName = "ADAMS",
                ServerIP = "151.123.1.2",
                VoiceIP = "151.123.2.2"
            }).Entity;

            var wrld_adam3 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("ace927e1-650b-52f6-7882-3bcb8222ba43"),
                WorldName = "Aspen Dental",
                TenantName = "ADAMS",
                ServerIP = "151.123.1.3",
                VoiceIP = "151.123.2.3"
            }).Entity;

            var wrld_adam4 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("29949379-a2dc-4cf6-87f7-aba707dbf985"),
                WorldName = "Atwater Tackle",
                TenantName = "ADAMS",
                ServerIP = "151.123.1.4",
                VoiceIP = "151.123.2.4"
            }).Entity;

            var wrld_adam5 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("469afb80-3bba-43c6-943d-bafe665eee5c"),
                WorldName = "Amber Paints",
                TenantName = "ADAMS",
                ServerIP = "151.123.1.5",
                VoiceIP = "151.123.2.5"
            }).Entity;

            var wrld_adam6 = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("4bc0c42d-e18f-4d00-91e2-b3c430c6c47c"),
                WorldName = "Acker Packers",
                TenantName = "ADAMS",
                ServerIP = "151.123.1.6",
                VoiceIP = "151.123.2.6"
            }).Entity;

            // ==================                   =====================
            // ==================  JEFFERSON WORLDS =====================
            // ==================                   =====================

            var wrld_jeff = context.Worlds.Add(new WorldDto
            {
                Id = Guid.Parse("7cd1d53d-f001-004d-e291-c47cb3c430c6"),
                WorldName = "Just Another World",
                TenantName = "JEFFERSON",
                ServerIP = "151.124.1.1",
                VoiceIP = "151.124.2.1"
            });

            var washUser1 = userManager.Users.SingleOrDefault(u => u.Email == "WRipple@washington.com");
            var washUser2 = userManager.Users.SingleOrDefault(u => u.Email == "WWilson@washington.com");
            var adamUser1 = userManager.Users.SingleOrDefault(u => u.Email == "AApple@adams.com");
            var adamUser2 = userManager.Users.SingleOrDefault(u => u.Email == "ACousins@adams.com");

            // ==================                       =====================
            // ==================  WASHINGTON RESIDENTS =====================
            // ==================                       =====================

            context.Residents.Add(new ResidentDto
            {
                Id = Guid.Parse("2eac8dea-2749-42b3-9d21-8eb2fc0fd6bd"),
                WorldName = wrld_wash1.WorldName,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                WorldRole = "WorldAdmin",
                WorldUserEmail = washUser1.Email
            });

            context.Residents.Add(new ResidentDto
            {
                Id = Guid.Parse("58423b3e-18c0-453d-859d-17e74ceb2a7b"),
                WorldName = wrld_wash1.WorldName,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                WorldRole = "Citizen",
                WorldUserEmail = washUser2.Email
            });

            context.Residents.Add(new ResidentDto
            {
                Id = Guid.Parse("a8682418-1163-485c-b9ef-c800dc959147"),
                WorldName = wrld_wash2.WorldName,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                WorldRole = "WorldAdmin",
                WorldUserEmail = washUser2.Email
            });

            // ==================                  =====================
            // ==================  ADAMS RESIDENTS =====================
            // ==================                  =====================

            context.Residents.Add(new ResidentDto
            {
                Id = Guid.Parse("b143337e-5e5f-487b-943f-4aa047d0be7d"),
                WorldName = wrld_adam1.WorldName,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                WorldRole = "Citizen",
                WorldUserEmail = adamUser1.Email
            });

            context.Residents.Add(new ResidentDto
            {
                Id = Guid.Parse("8b3053e2-2172-474a-b74c-030b47a3d849"),
                WorldName = wrld_adam2.WorldName,
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow,
                WorldRole = "WorldAdmin",
                WorldUserEmail = adamUser2.Email
            });

            context.SaveChanges();
        }
    }
}
