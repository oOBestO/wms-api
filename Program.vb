Imports System.Text
Imports Microsoft.AspNetCore.Authentication.JwtBearer
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.IdentityModel.Tokens
Imports wms_api.Data
Imports wms_api.Services

Module Program
    Sub Main(args As String())
        Dim builder = WebApplication.CreateBuilder(args)

        Dim connectionString = builder.Configuration.GetConnectionString("WmsDatabase")
        builder.Services.AddDbContext(Of WmsDbContext)(Sub(options) options.UseSqlServer(connectionString))

        builder.Services.AddControllers()
        builder.Services.AddEndpointsApiExplorer()
        builder.Services.AddSwaggerGen()

        builder.Services.AddSingleton(Of JwtTokenService)()

        Dim jwtKey = builder.Configuration.GetValue(Of String)("Jwt:Key")
        Dim jwtIssuer = builder.Configuration.GetValue(Of String)("Jwt:Issuer")
        Dim jwtAudience = builder.Configuration.GetValue(Of String)("Jwt:Audience")

        builder.Services.AddAuthentication(Sub(options)
                                                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme
                                                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme
                                            End Sub).
            AddJwtBearer(Sub(options)
                             options.TokenValidationParameters = New TokenValidationParameters With {
                                 .ValidateIssuer = True,
                                 .ValidIssuer = jwtIssuer,
                                 .ValidateAudience = True,
                                 .ValidAudience = jwtAudience,
                                 .ValidateIssuerSigningKey = True,
                                 .IssuerSigningKey = New SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                                 .ValidateLifetime = True,
                                 .ClockSkew = TimeSpan.FromMinutes(1)
                             }
                         End Sub)

        builder.Services.AddAuthorization()

        Const CorsPolicyName As String = "VueFrontend"
        Dim allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get(Of String())()
        If allowedOrigins Is Nothing OrElse allowedOrigins.Length = 0 Then
            allowedOrigins = {"http://localhost:5173", "http://localhost:4173"}
        End If

        builder.Services.AddCors(Sub(options)
                                      options.AddPolicy(CorsPolicyName,
                                          Sub(policy)
                                              policy.WithOrigins(allowedOrigins).
                                                  AllowAnyHeader().
                                                  AllowAnyMethod()
                                          End Sub)
                                  End Sub)

        Dim app = builder.Build()

        If app.Environment.IsDevelopment() Then
            app.UseSwagger()
            app.UseSwaggerUI()
        End If

        app.UseCors(CorsPolicyName)
        app.UseAuthentication()
        app.UseAuthorization()
        app.MapControllers()

        Using scope = app.Services.CreateScope()
            Dim db = scope.ServiceProvider.GetRequiredService(Of WmsDbContext)()
            db.Database.EnsureCreated()

            ' EnsureCreated() only builds the schema when the database itself is brand new;
            ' it no-ops once any tables already exist. This project doesn't use EF Core
            ' migrations, so a table added after the first run (StockTransactions) needs to
            ' be created explicitly for databases that predate it. Guarded and idempotent,
            ' so it's a no-op both on fresh databases (EnsureCreated already made the table)
            ' and on repeated app restarts.
            db.Database.ExecuteSqlRaw("
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StockTransactions')
                BEGIN
                    CREATE TABLE StockTransactions (
                        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                        MaterialId UNIQUEIDENTIFIER NOT NULL,
                        CompanyCode NVARCHAR(50) NOT NULL DEFAULT 'parent',
                        MaterialName NVARCHAR(200) NOT NULL,
                        Unit NVARCHAR(50) NOT NULL,
                        Delta DECIMAL(18,3) NOT NULL,
                        QuantityBefore DECIMAL(18,3) NOT NULL,
                        QuantityAfter DECIMAL(18,3) NOT NULL,
                        UserId UNIQUEIDENTIFIER NULL,
                        UserName NVARCHAR(200) NULL,
                        CreatedAt DATETIME2 NOT NULL
                    );
                    CREATE INDEX IX_StockTransactions_MaterialId ON StockTransactions(MaterialId);
                    CREATE INDEX IX_StockTransactions_CompanyCode ON StockTransactions(CompanyCode);
                END
            ")

            db.Database.ExecuteSqlRaw("
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
                BEGIN
                    CREATE TABLE Users (
                        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                        Name NVARCHAR(200) NOT NULL,
                        Email NVARCHAR(256) NOT NULL,
                        PasswordHash NVARCHAR(MAX) NOT NULL,
                        CompanyCode NVARCHAR(50) NOT NULL,
                        PositionCode NVARCHAR(50) NOT NULL DEFAULT 'general',
                        CreatedAt DATETIME2 NOT NULL
                    );
                    CREATE UNIQUE INDEX IX_Users_Email ON Users(Email);
                END
            ")

            ' Accounts registered before job positions existed default to "general"
            ' (view-only, the least-privileged tier) rather than silently inheriting
            ' full access — same backfill-on-column-add pattern as CompanyCode below.
            db.Database.ExecuteSqlRaw("
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PositionCode')
                BEGIN
                    ALTER TABLE Users ADD PositionCode NVARCHAR(50) NOT NULL CONSTRAINT DF_Users_PositionCode DEFAULT 'general';
                END
            ")

            ' Materials/StockTransactions predate per-company data isolation. On a database
            ' that already has these tables (from before this change), backfill existing rows
            ' to the "parent" company so nothing becomes orphaned/invisible after the column
            ' is added — same guarded, idempotent pattern as the table-creation blocks above.
            db.Database.ExecuteSqlRaw("
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Materials') AND name = 'CompanyCode')
                BEGIN
                    ALTER TABLE Materials ADD CompanyCode NVARCHAR(50) NOT NULL CONSTRAINT DF_Materials_CompanyCode DEFAULT 'parent';
                    CREATE INDEX IX_Materials_CompanyCode ON Materials(CompanyCode);
                END
            ")

            db.Database.ExecuteSqlRaw("
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('StockTransactions') AND name = 'CompanyCode')
                BEGIN
                    ALTER TABLE StockTransactions ADD CompanyCode NVARCHAR(50) NOT NULL CONSTRAINT DF_StockTransactions_CompanyCode DEFAULT 'parent';
                    CREATE INDEX IX_StockTransactions_CompanyCode ON StockTransactions(CompanyCode);
                END
            ")

            ' Rows written before this column existed have no known actor — left NULL
            ' rather than backfilled, since there's no real name to put there.
            db.Database.ExecuteSqlRaw("
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('StockTransactions') AND name = 'UserId')
                BEGIN
                    ALTER TABLE StockTransactions ADD UserId UNIQUEIDENTIFIER NULL;
                    ALTER TABLE StockTransactions ADD UserName NVARCHAR(200) NULL;
                END
            ")
        End Using

        app.Run()
    End Sub
End Module
