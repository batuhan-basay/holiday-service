using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HolidayService.Api.OpenApi;

internal sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureNamedOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfo(description));
        }

        var scheme = new OpenApiSecurityScheme
        {
            Name = "X-API-Key",
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "API key issued to the calling client.",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "ApiKey"
            }
        };

        options.AddSecurityDefinition("ApiKey", scheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [scheme] = Array.Empty<string>()
        });
    }

    public void Configure(string? name, SwaggerGenOptions options) => Configure(options);

    private static OpenApiInfo CreateInfo(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "Holiday API",
            Version = description.ApiVersion.ToString(),
            Description =
                "Public holidays, long weekends and business-day arithmetic for 100+ countries.",
            Contact = new OpenApiContact
            {
                Name = "Batuhan Enes Başay",
                Url = new Uri("https://github.com/batuhan-basay")
            },
            License = new OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        };

        if (description.IsDeprecated)
        {
            info.Description += " This API version has been deprecated.";
        }

        return info;
    }
}
