Imports Microsoft.AspNetCore.Builder
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports wms_api.Data

Module Program
    Sub Main(args As String())
        Dim builder = WebApplication.CreateBuilder(args)

        Dim connectionString = builder.Configuration.GetConnectionString("WmsDatabase")
        builder.Services.AddDbContext(Of WmsDbContext)(Sub(options) options.UseSqlServer(connectionString))

        builder.Services.AddControllers()
        builder.Services.AddEndpointsApiExplorer()
        builder.Services.AddSwaggerGen()

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
                        MaterialName NVARCHAR(200) NOT NULL,
                        Unit NVARCHAR(50) NOT NULL,
                        Delta DECIMAL(18,3) NOT NULL,
                        QuantityBefore DECIMAL(18,3) NOT NULL,
                        QuantityAfter DECIMAL(18,3) NOT NULL,
                        CreatedAt DATETIME2 NOT NULL
                    );
                    CREATE INDEX IX_StockTransactions_MaterialId ON StockTransactions(MaterialId);
                END
            ")
        End Using

        app.Run()
    End Sub
End Module
