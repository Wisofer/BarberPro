using BarberNic.Data;
using BarberNic.Models.Entities;
using BarberNic.Services;
using BarberNic.Services.IServices;
using BarberNic.Services.Interfaces;
using BarberNic.Services.Implementations;
using BarberNic.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

// Configurar Npgsql para manejar DateTime correctamente con PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Inicializar Firebase solo una vez (al inicio de la aplicación)
var firebaseCredentialsPath = Path.Combine(builder.Environment.ContentRootPath, "Secrets", "firebase_credentials.json");

if (File.Exists(firebaseCredentialsPath))
{
    try
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            var credential = GoogleCredential.FromFile(firebaseCredentialsPath);
            var appOptions = new AppOptions()
            {
                Credential = credential
            };
            
            FirebaseApp.Create(appOptions);
        }
    }
    catch (Exception)
    {
        // No lanzar excepción aquí para que la app pueda iniciar
    }
}

// Agregar servicios al contenedor
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Permitir deserializar strings como nombres de enum
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddControllersWithViews(); // Mantener para MVC web

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "BarberNic API", 
        Version = "v1",
        Description = "API para gestión de reservas de barberías"
    });
    
    // Configurar JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configurar URLs en minúsculas
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Configurar Entity Framework con PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Configurar JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurado");
        var issuer = jwtSettings["Issuer"] ?? "BarberNic";
        var audience = jwtSettings["Audience"] ?? "BarberNicUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Cookies para MVC web
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Cookies para MVC web
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // Esquema por defecto
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => // Para MVC web
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Name = "BarberNic.Auth";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => // Para API
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});


// Configurar Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Barber", policy => policy.RequireRole("Barber"));
    // Mantener políticas antiguas para MVC
    options.AddPolicy("Administrador", policy => policy.RequireClaim("Rol", SD.RolAdministrador));
    options.AddPolicy("Normal", policy => policy.RequireClaim("Rol", SD.RolNormal, SD.RolAdministrador));
});

// Configurar CORS para API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurar sesiones (para MVC web)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Registrar servicios nuevos (API)
        builder.Services.AddScoped<BarberNic.Services.Interfaces.IAuthService, BarberNic.Services.Implementations.AuthService>();
builder.Services.AddScoped<IBarberService, BarberService>();
        builder.Services.AddScoped<IAppointmentService, BarberNic.Services.Implementations.AppointmentService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IServiceService, ServiceService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IWorkingHoursService, WorkingHoursService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IHelpSupportService, HelpSupportService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

// Registrar servicios antiguos (MVC web - mantener compatibilidad)
        builder.Services.AddScoped<BarberNic.Services.IServices.IAuthService, BarberNic.Services.AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();

var app = builder.Build();

// Verificar Firebase después de construir la app
if (FirebaseApp.DefaultInstance == null)
{
    try
    {
        var firebaseCredsPath = Path.Combine(app.Environment.ContentRootPath, "Secrets", "firebase_credentials.json");
        if (File.Exists(firebaseCredsPath))
        {
            var credential = GoogleCredential.FromFile(firebaseCredsPath);
            var appOptions = new AppOptions() { Credential = credential };
            FirebaseApp.Create(appOptions);
        }
    }
    catch (Exception)
    {
        // Error silencioso
    }
}

// Aplicar migraciones e inicializar datos
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Aplicar migraciones
        dbContext.Database.Migrate();

        // Crear usuario admin antiguo si no existe (MVC web)
        InicializarUsuarioAdmin.CrearAdminSiNoExiste(dbContext, logger);
        
        // Crear usuario admin nuevo si no existe (API)
        InicializarSistema.CrearAdminUserSiNoExiste(dbContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

// Configurar el pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
    // Habilitar Swagger solo en desarrollo
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BarberNic API v1");
        c.RoutePrefix = "swagger"; // Swagger disponible en /swagger
    });
}

// Manejar códigos de estado
app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");

// Configurar cache para archivos estáticos
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
        else
        {
            var path = ctx.File.Name.ToLower();
            if (path.EndsWith(".js") || path.EndsWith(".css"))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
            }
            else if (path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg") || 
                     path.EndsWith(".gif") || path.EndsWith(".svg") || path.EndsWith(".ico"))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=2592000");
            }
            else
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=3600");
            }
        }
    }
});

app.UseRouting();

// Habilitar CORS
app.UseCors("AllowAll");

// Habilitar sesiones
app.UseSession();

// Habilitar Authentication y Authorization
app.UseAuthentication();
app.UseAuthorization();

// Ruta raíz - Redirigir al login del sistema web
app.MapGet("/", () => Results.Redirect("/login")).ExcludeFromDescription();

// Mapear controladores API
app.MapControllers();

// Configurar rutas MVC (mantener para web)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
